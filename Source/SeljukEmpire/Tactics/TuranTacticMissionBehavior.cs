using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// Advanced Multi-Doctrine Seljuk Tactical AI Engine.
    /// Dynamically evaluates army ratios, terrain contours, commander skill, and battlefield dynamics.
    /// Implements: Turan Wolf-Trap, Nizamiye Shield Wall, Crossfire Steppe Circle, and High-Ground Ambush.
    /// Fully optimized with Zero-GC loops and fail-safe boundary protections.
    /// </summary>
    public class TuranTacticMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private const string SeljukCultureId = "seljuk";
        private const string SeljukKingdomId = "kingdom_seljuks";

        public enum TacticalDoctrine
        {
            Undecided,
            TuranWolfTrap,          // Cavalry & Horse Archer Crescent Pincer
            NizamiyeShieldWall,     // Heavy Infantry & Phalanx Choke Hold
            SteppeCrossfire,        // Composite Bow Crossfire & Skirmish Anchor
            HighGroundAmbush,       // Outnumbered Last Stand & Counter-Charge
            StandardEngineFallback  // Handover to Native Engine
        }

        public enum TacticalPhase
        {
            InitialAssessment,
            StagingAndSkirmish,
            FeignedRetreatBait,
            DualFlankEncirclement,
            DecisiveHammerCharge,
            BattleEnded
        }

        private TacticalDoctrine _activeDoctrine = TacticalDoctrine.Undecided;
        private TacticalPhase _currentPhase = TacticalPhase.InitialAssessment;
        private Team _seljukTeam;
        private Team _enemyTeam;
        private MissionTime _phaseTimer;
        private MissionTime _tickThrottleTimer;
        private Vec3 _anchorHighGround;
        private Vec3 _designatedKillzone;
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;

        public override void AfterStart()
        {
            base.AfterStart();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            try
            {
                // Execute only in active field battles
                if (Mission.Current == null || Mission.Current.IsSiegeBattle || Mission.Current.Mode != MissionMode.Battle)
                {
                    return;
                }

                // Periodic AI tick every 1.25 seconds to eliminate CPU stutter & garbage collection.
                // Uses its own timer, separate from _phaseTimer (which tracks phase duration) -
                // sharing one timer for both purposes meant this gate stopped throttling anything
                // after the first 1.25s, since _phaseTimer only resets on a phase transition.
                if (_tickThrottleTimer.ElapsedSeconds > 1.25f)
                {
                    _tickThrottleTimer = MissionTime.Now;
                    ExecuteTacticalDecisionLoop();
                }
            }
            catch (Exception)
            {
                // Absolute Fail-safe: In case of unexpected engine anomaly, degrade gracefully to native AI
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
            }
        }

        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == TacticalDoctrine.StandardEngineFallback) return;

            switch (_currentPhase)
            {
                case TacticalPhase.InitialAssessment:
                    EvaluateAndSelectDoctrine();
                    break;

                case TacticalPhase.StagingAndSkirmish:
                    ExecuteStagingAndSkirmish();
                    break;

                case TacticalPhase.FeignedRetreatBait:
                    ExecuteFeignedRetreat();
                    break;

                case TacticalPhase.DualFlankEncirclement:
                    ExecuteDualFlankEncirclement();
                    break;

                case TacticalPhase.DecisiveHammerCharge:
                    ExecuteDecisiveHammerCharge();
                    break;
            }
        }

        /// <summary>
        /// 1. Comprehensive Force Analysis & Dynamic Doctrine Selection
        /// </summary>
        private void EvaluateAndSelectDoctrine()
        {
            if (Mission.Current.Teams == null || Mission.Current.Teams.Count < 2)
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
                return;
            }

            // Only take over a side that is actually Seljuk-affiliated (culture or kingdom).
            // This mission behavior is added to every non-siege field battle in the game
            // (see SeljukSubModule.OnMissionBehaviorInitialize), so without this check it used
            // to hijack "whichever side is Defender" in every battle in the game, including
            // ones with no Seljuk involvement at all.
            if (IsSeljukTeam(Mission.Current.DefenderTeam))
            {
                _seljukTeam = Mission.Current.DefenderTeam;
            }
            else if (IsSeljukTeam(Mission.Current.AttackerTeam))
            {
                _seljukTeam = Mission.Current.AttackerTeam;
            }
            else
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
                return;
            }

            // Find opponent team
            foreach (var t in Mission.Current.Teams)
            {
                if (t != _seljukTeam && t.IsEnemyOf(_seljukTeam))
                {
                    _enemyTeam = t;
                    break;
                }
            }

            if (_seljukTeam == null || _enemyTeam == null)
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
                return;
            }

            // Tally unit types (Zero-GC loop)
            int totalFriendly = 0;
            int horseArchers = 0;
            int shockCav = 0;
            int infantry = 0;
            int footArchers = 0;

            foreach (var formation in _seljukTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                totalFriendly += formation.CountOfUnits;

                if (formation.FormationIndex == FormationClass.HorseArcher) horseArchers += formation.CountOfUnits;
                else if (formation.FormationIndex == FormationClass.Cavalry) shockCav += formation.CountOfUnits;
                else if (formation.FormationIndex == FormationClass.Infantry) infantry += formation.CountOfUnits;
                else if (formation.FormationIndex == FormationClass.Ranged) footArchers += formation.CountOfUnits;
            }

            int totalEnemy = 0;
            foreach (var enemyFormation in _enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits > 0) totalEnemy += enemyFormation.CountOfUnits;
            }

            if (totalFriendly == 0 || totalEnemy == 0)
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
                return;
            }

            Vec3 teamCenter = GetTeamCenterPosition(_seljukTeam);

            // Establish terrain anchor on closest highest ground
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f);
            _designatedKillzone = teamCenter;

            float cavRatio = (float)(horseArchers + shockCav) / totalFriendly;
            float infantryRatio = (float)infantry / totalFriendly;
            float outnumberRatio = (float)totalEnemy / totalFriendly;

            // DOCTRINE SELECTION RULES:
            if (outnumberRatio >= 1.8f)
            {
                // Outnumbered -> High-ground fortified defense & counter-ambush
                _activeDoctrine = TacticalDoctrine.HighGroundAmbush;
                DisplayDoctrineMessage("{=seljuk_tactic_high_ground}[Seljuk Tactical Command] High-Ground Defense initiated against a numerically superior enemy!", Colors.Yellow);
            }
            else if (cavRatio >= 0.30f && horseArchers >= 6)
            {
                // Cavalry heavy -> Classic Turan Wolf-Trap & Crescent Encirclement
                _activeDoctrine = TacticalDoctrine.TuranWolfTrap;
                DisplayDoctrineMessage("{=seljuk_tactic_wolf_trap}[Seljuk Tactical Command] Wolf-Trap and Turan Crescent tactic underway!", Colors.Yellow);
            }
            else if (infantryRatio >= 0.45f)
            {
                // Infantry heavy -> Nizamiye Impenetrable Shield Wall
                _activeDoctrine = TacticalDoctrine.NizamiyeShieldWall;
                DisplayDoctrineMessage("{=seljuk_tactic_shield_wall}[Seljuk Tactical Command] Nizamiye Shield Wall and choke-point defense formed!", Colors.Cyan);
            }
            else
            {
                // Balanced / Skirmish heavy -> Steppe Crossfire
                _activeDoctrine = TacticalDoctrine.SteppeCrossfire;
                DisplayDoctrineMessage("{=seljuk_tactic_crossfire}[Seljuk Tactical Command] Steppe Crescent Crossfire formation engaged!", Colors.Green);
            }

            _currentPhase = TacticalPhase.StagingAndSkirmish;
            _phaseTimer = MissionTime.Now;
        }

        /// <summary>
        /// 2. Stage 1: Spatial Positioning, Shield Wall Staging & Skirmish Probing
        /// </summary>
        private void ExecuteStagingAndSkirmish()
        {
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation infantry = _seljukTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _seljukTeam.GetFormation(FormationClass.Ranged);

            // Compute Left and Right Flanking Vectors
            Vec3 enemyPos = GetTeamCenterPosition(_enemyTeam);
            _leftFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, true, 85f);
            _rightFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, false, 85f);

            // Positioning Infantry on High Ground Anchor
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
            }

            // Foot archers placed right behind shield wall
            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                Vec3 enemyDir = (enemyPos - _anchorHighGround).NormalizedCopy();
                Vec3 archerPos = _anchorHighGround - (enemyDir * 12f);
                WorldPosition archerWorldPos = new WorldPosition(Mission.Current.Scene, TacticalFormationsHelper.ClampToMapBoundaries(archerPos));
                footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(archerWorldPos));
                footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }

            // Shock Cavalry Split to Flank Staging Points
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                WorldPosition flankPos = new WorldPosition(Mission.Current.Scene, _leftFlankPosition);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(flankPos));
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
            }

            // Horse Archers Probing & Skirmishing
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }

            // Transition Trigger: When enemy gets close (90m) or 20 seconds of skirmishing have passed
            if (_phaseTimer.ElapsedSeconds > 22f || IsEnemyWithinDistance(90f))
            {
                if (_activeDoctrine == TacticalDoctrine.TuranWolfTrap)
                {
                    _currentPhase = TacticalPhase.FeignedRetreatBait;
                }
                else
                {
                    _currentPhase = TacticalPhase.DualFlankEncirclement;
                }
                _phaseTimer = MissionTime.Now;
            }
        }

        /// <summary>
        /// 3. Stage 2: Feigned Retreat (Sahte Geri Çekilme). Draws enemy formation out of cohesion.
        /// </summary>
        private void ExecuteFeignedRetreat()
        {
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                // Fall back into killzone between the high-ground anchor and flanking wings
                WorldPosition killzoneWorldPos = new WorldPosition(Mission.Current.Scene, _designatedKillzone);
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderMove(killzoneWorldPos));
            }

            // Anti-Derp Timer & Enemy Bait Check
            if (_phaseTimer.ElapsedSeconds > 14f || IsEnemyWithinDistance(55f))
            {
                _currentPhase = TacticalPhase.DualFlankEncirclement;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=seljuk_tactic_lines_broken}[Wolf-Trap] The enemy line has broken! Dual-flank crescent encirclement begins!", Colors.Yellow);
            }
        }

        /// <summary>
        /// 4. Stage 3: Dual-Flank Encirclement (İki Kanattan Hilal Kuşatması)
        /// </summary>
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            // Shock Cavalry pincer strike on enemy flanks
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            // Horse archers circle rear
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                // If horse archers ran out of ammo, they charge with lances/sabers!
                if (TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers))
                {
                    horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                }
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (_phaseTimer.ElapsedSeconds > 16f || IsEnemyWithinDistance(30f))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                DisplayDoctrineMessage("{=seljuk_tactic_full_assault}[Nizamiye Advance] Hammer and Anvil assault! All lines advance!", Colors.Red);
            }
        }

        /// <summary>
        /// 5. Stage 4: Hammer & Anvil Decisive Charge (Topyekûn Taarruz & İmha)
        /// </summary>
        private void ExecuteDecisiveHammerCharge()
        {
            Formation infantry = _seljukTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _seljukTeam.GetFormation(FormationClass.Ranged);
            Formation shockCav = _seljukTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _seljukTeam.GetFormation(FormationClass.HorseArcher);

            // All formations unleash full frontal and flanking assault
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                infantry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                footArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (shockCav != null && shockCav.CountOfUnits > 0)
            {
                shockCav.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            // Once the decisive melee begins, return control smoothly to standard native engine
            if (_phaseTimer.ElapsedSeconds > 35f)
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
            }
        }

        /// <summary>
        /// True if this team is meaningfully Seljuk: either commanded by a Seljuk-culture or
        /// Kingdom.kingdom_seljuks-affiliated general, or made up of a majority of
        /// Culture.seljuk troops among its currently active agents.
        /// </summary>
        private static bool IsSeljukTeam(Team team)
        {
            if (team == null) return false;

            if (team.GeneralAgent?.Character is CharacterObject generalCharacter)
            {
                if (generalCharacter.Culture != null && generalCharacter.Culture.StringId == SeljukCultureId)
                {
                    return true;
                }
                if (generalCharacter.HeroObject?.Clan?.Kingdom != null && generalCharacter.HeroObject.Clan.Kingdom.StringId == SeljukKingdomId)
                {
                    return true;
                }
            }

            int seljukCount = 0;
            int sampledCount = 0;
            foreach (var formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                formation.ApplyActionOnEachUnit(agent =>
                {
                    if (agent == null || !agent.IsActive() || !(agent.Character is CharacterObject troopCharacter) || troopCharacter.Culture == null)
                    {
                        return;
                    }
                    sampledCount++;
                    if (troopCharacter.Culture.StringId == SeljukCultureId)
                    {
                        seljukCount++;
                    }
                });
            }

            return sampledCount > 0 && seljukCount * 2 >= sampledCount;
        }

        private static Vec3 GetTeamCenterPosition(Team team)
        {
            if (team == null) return Vec3.Zero;

            Vec3 accum = Vec3.Zero;
            int count = 0;

            foreach (var formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    accum += formation.OrderPosition.ToVec3();
                    count++;
                }
            }

            return count > 0 ? accum * (1.0f / count) : Vec3.Zero;
        }

        private bool IsEnemyWithinDistance(float distance)
        {
            if (_seljukTeam == null || _enemyTeam == null) return false;
            float distSq = distance * distance;
            Vec3 friendlyCenter = GetTeamCenterPosition(_seljukTeam);

            foreach (var enemyFormation in _enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits <= 0) continue;
                if (friendlyCenter.DistanceSquared(enemyFormation.OrderPosition.ToVec3()) < distSq)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            _seljukTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _seljukTeam = null;
            _enemyTeam = null;
        }

        private static void DisplayDoctrineMessage(string localizedKeyAndFallback, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(new TextObject(localizedKeyAndFallback).ToString(), color));
        }
    }
}
