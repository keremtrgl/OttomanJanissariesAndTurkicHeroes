using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// Multi-Doctrine Byzantine Tactical AI Engine - the Byzantine counterpart to
    /// TuranTacticMissionBehavior, reusing the same proven phase engine and
    /// TacticalFormationsHelper utilities (both are culture-agnostic) with Byzantine-flavored
    /// doctrine selection, unit priorities, and battle messages instead of Seljuk ones.
    ///
    /// Historical grounding: Byzantine field doctrine (as described in the Strategikon and later
    /// Taktika military manuals) emphasized disciplined combined-arms coordination - a steady
    /// Tagma (professional regular army) infantry line holding the center while Toxotai archers
    /// provide missile support, with the decisive blow delivered by armored Kataphraktoi shock
    /// cavalry once the enemy line is fixed or has broken cohesion. Byzantine commanders also
    /// explicitly adopted steppe-style feigned-retreat and encirclement tactics from centuries of
    /// warfare against Avars, Huns, and Turkic peoples, which is why this doctrine set can reuse
    /// the same phase structure as the Seljuk one without being historically dishonest about it -
    /// the two traditions converged on similar battlefield mechanics from different origins.
    ///
    /// This behavior and TuranTacticMissionBehavior can both be active in the same battle (e.g. a
    /// Seljuk army fighting a Byzantine one): each only ever issues orders to the single team it
    /// identifies as its own culture's, so the two never touch the same Formation objects and
    /// cannot race or conflict with each other.
    /// </summary>
    public class ByzantineTacticMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private const string ByzantineCultureId = "empire";
        private const string ByzantineKingdomId = "empire_s";

        public enum TacticalDoctrine
        {
            Undecided,
            KataphraktoiHammerAndAnvil,  // Heavy Shock Cavalry Pincer & Decisive Charge
            TagmaShieldWall,             // Disciplined Skutatoi Line & Choke Hold
            ToxotaiCrossfire,            // Composite Bow & Skirmish Anchor
            ThematicLastStand,           // Outnumbered High-Ground Defense
            StandardEngineFallback       // Handover to Native Engine
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
        private Team _byzantineTeam;
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

            // Only take over a side that is actually Byzantine-affiliated (culture or kingdom).
            // Without this check this behavior (added to every non-siege field battle, see
            // SeljukSubModule.OnMissionBehaviorInitialize) would hijack a side in every battle in
            // the game, including ones with no Byzantine involvement at all.
            if (IsByzantineTeam(Mission.Current.DefenderTeam))
            {
                _byzantineTeam = Mission.Current.DefenderTeam;
            }
            else if (IsByzantineTeam(Mission.Current.AttackerTeam))
            {
                _byzantineTeam = Mission.Current.AttackerTeam;
            }
            else
            {
                _activeDoctrine = TacticalDoctrine.StandardEngineFallback;
                return;
            }

            // Find opponent team
            foreach (var t in Mission.Current.Teams)
            {
                if (t != _byzantineTeam && t.IsEnemyOf(_byzantineTeam))
                {
                    _enemyTeam = t;
                    break;
                }
            }

            if (_byzantineTeam == null || _enemyTeam == null)
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

            foreach (var formation in _byzantineTeam.FormationsIncludingEmpty)
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

            Vec3 teamCenter = GetTeamCenterPosition(_byzantineTeam);

            // Establish terrain anchor on closest highest ground
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, 80f);
            _designatedKillzone = teamCenter;

            float cavRatio = (float)(horseArchers + shockCav) / totalFriendly;
            float infantryRatio = (float)infantry / totalFriendly;
            float outnumberRatio = (float)totalEnemy / totalFriendly;

            // DOCTRINE SELECTION RULES:
            if (outnumberRatio >= 1.8f)
            {
                // Outnumbered -> High-ground fortified thematic defense
                _activeDoctrine = TacticalDoctrine.ThematicLastStand;
                DisplayDoctrineMessage("{=byz_tactic_last_stand}[Byzantine Tactical Command] Thematic High-Ground Defense against a superior force!", Colors.Yellow);
            }
            else if (cavRatio >= 0.30f && shockCav >= 6)
            {
                // Cavalry heavy -> Kataphraktoi Hammer & Anvil
                _activeDoctrine = TacticalDoctrine.KataphraktoiHammerAndAnvil;
                DisplayDoctrineMessage("{=byz_tactic_hammer_anvil}[Byzantine Tactical Command] Kataphraktoi Hammer and Anvil deployed!", Colors.Yellow);
            }
            else if (infantryRatio >= 0.45f)
            {
                // Infantry heavy -> Tagma Shield Wall
                _activeDoctrine = TacticalDoctrine.TagmaShieldWall;
                DisplayDoctrineMessage("{=byz_tactic_shield_wall}[Byzantine Tactical Command] Tagma Shield Wall formed at the choke point!", Colors.Cyan);
            }
            else
            {
                // Balanced / Skirmish heavy -> Toxotai Crossfire
                _activeDoctrine = TacticalDoctrine.ToxotaiCrossfire;
                DisplayDoctrineMessage("{=byz_tactic_crossfire}[Byzantine Tactical Command] Toxotai Crossfire formation engaged!", Colors.Green);
            }

            _currentPhase = TacticalPhase.StagingAndSkirmish;
            _phaseTimer = MissionTime.Now;
        }

        /// <summary>
        /// 2. Stage 1: Spatial Positioning, Shield Wall Staging & Skirmish Probing
        /// </summary>
        private void ExecuteStagingAndSkirmish()
        {
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation infantry = _byzantineTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _byzantineTeam.GetFormation(FormationClass.Ranged);

            // Compute Left and Right Flanking Vectors
            Vec3 enemyPos = GetTeamCenterPosition(_enemyTeam);
            _leftFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, true, 85f);
            _rightFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, false, 85f);

            // Tagma infantry anchors the line on high ground
            if (infantry != null && infantry.CountOfUnits > 0)
            {
                WorldPosition anchorWorldPos = new WorldPosition(Mission.Current.Scene, _anchorHighGround);
                infantry.SetMovementOrder(MovementOrder.MovementOrderMove(anchorWorldPos));
                infantry.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
            }

            // Toxotai foot archers placed right behind the shield wall
            if (footArchers != null && footArchers.CountOfUnits > 0)
            {
                Vec3 enemyDir = (enemyPos - _anchorHighGround).NormalizedCopy();
                Vec3 archerPos = _anchorHighGround - (enemyDir * 12f);
                WorldPosition archerWorldPos = new WorldPosition(Mission.Current.Scene, TacticalFormationsHelper.ClampToMapBoundaries(archerPos));
                footArchers.SetMovementOrder(MovementOrder.MovementOrderMove(archerWorldPos));
                footArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }

            // Kataphraktoi shock cavalry staged on the flank, held back for the decisive blow
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                WorldPosition flankPos = new WorldPosition(Mission.Current.Scene, _leftFlankPosition);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderMove(flankPos));
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
            }

            // Horse archers (Vardariotai-style) probing and skirmishing
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                horseArchers.SetMovementOrder(MovementOrder.MovementOrderCharge);
                horseArchers.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            }

            // Transition Trigger: When enemy gets close (90m) or 20 seconds of skirmishing have passed
            if (_phaseTimer.ElapsedSeconds > 22f || IsEnemyWithinDistance(90f))
            {
                if (_activeDoctrine == TacticalDoctrine.KataphraktoiHammerAndAnvil)
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
        /// 3. Stage 2: Feigned Retreat - a tactic explicitly documented in Byzantine military
        /// manuals (learned from centuries of steppe warfare) to draw the enemy formation out of
        /// cohesion before the Kataphraktoi commit.
        /// </summary>
        private void ExecuteFeignedRetreat()
        {
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

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
                DisplayDoctrineMessage("{=byz_tactic_lines_broken}[Kataphraktoi] The enemy line has broken cohesion! Dual-flank envelopment begins!", Colors.Yellow);
            }
        }

        /// <summary>
        /// 4. Stage 3: Dual-Flank Encirclement
        /// </summary>
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

            // Kataphraktoi shock cavalry pincer strike on enemy flanks
            if (shockCavalry != null && shockCavalry.CountOfUnits > 0)
            {
                shockCavalry.SetArrangementOrder(ArrangementOrder.ArrangementOrderSkein);
                shockCavalry.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }

            // Horse archers circle the rear
            if (horseArchers != null && horseArchers.CountOfUnits > 0)
            {
                // If horse archers ran out of ammo, they charge with swords/lances!
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
                DisplayDoctrineMessage("{=byz_tactic_full_assault}[Tagma Advance] Hammer and Anvil complete! All lines advance!", Colors.Red);
            }
        }

        /// <summary>
        /// 5. Stage 4: Hammer & Anvil Decisive Charge
        /// </summary>
        private void ExecuteDecisiveHammerCharge()
        {
            Formation infantry = _byzantineTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _byzantineTeam.GetFormation(FormationClass.Ranged);
            Formation shockCav = _byzantineTeam.GetFormation(FormationClass.Cavalry);
            Formation horseArchers = _byzantineTeam.GetFormation(FormationClass.HorseArcher);

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

        private static void DisplayDoctrineMessage(string localizedKeyAndFallback, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(new TextObject(localizedKeyAndFallback).ToString(), color));
        }

        /// <summary>
        /// True if this team is meaningfully Byzantine: either commanded by a Byzantine-culture or
        /// Kingdom.empire_s-affiliated general, or made up of a majority of Culture.empire troops
        /// among its currently active agents.
        /// </summary>
        private static bool IsByzantineTeam(Team team)
        {
            if (team == null) return false;

            if (team.GeneralAgent?.Character is CharacterObject generalCharacter)
            {
                if (generalCharacter.Culture != null && generalCharacter.Culture.StringId == ByzantineCultureId)
                {
                    return true;
                }
                if (generalCharacter.HeroObject?.Clan?.Kingdom != null && generalCharacter.HeroObject.Clan.Kingdom.StringId == ByzantineKingdomId)
                {
                    return true;
                }
            }

            int byzantineCount = 0;
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
                    if (troopCharacter.Culture.StringId == ByzantineCultureId)
                    {
                        byzantineCount++;
                    }
                });
            }

            return sampledCount > 0 && byzantineCount * 2 >= sampledCount;
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
            if (_byzantineTeam == null || _enemyTeam == null) return false;
            float distSq = distance * distance;
            Vec3 friendlyCenter = GetTeamCenterPosition(_byzantineTeam);

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
            _byzantineTeam = null;
            _enemyTeam = null;
            _activeDoctrine = TacticalDoctrine.Undecided;
            _currentPhase = TacticalPhase.BattleEnded;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _byzantineTeam = null;
            _enemyTeam = null;
        }
    }
}
