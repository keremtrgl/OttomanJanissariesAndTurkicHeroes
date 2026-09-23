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
    /// The four battle doctrines every culture-specific tactical AI chooses between, named by
    /// what they do rather than by any one culture's name for them - each subclass supplies its
    /// own historically-flavored name and announcement text (see
    /// <see cref="DoctrineTacticMissionBehaviorBase.GetDoctrineAnnouncement"/>).
    /// </summary>
    public enum DoctrineArchetype
    {
        Undecided,
        CavalryEnvelopment,   // Seljuk: Turan Wolf-Trap        | Byzantine: Kataphraktoi Hammer & Anvil
        ShieldWall,           // Seljuk: Nizamiye Shield Wall   | Byzantine: Tagma Shield Wall
        Crossfire,            // Seljuk: Steppe Crossfire       | Byzantine: Toxotai Crossfire
        OutnumberedDefense,   // Seljuk: High-Ground Ambush     | Byzantine: Thematic Last Stand
        EngineFallback        // Hand control back to Native's own team AI for the rest of the battle
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

    /// <summary>
    /// Shared phase engine for the culture-specific multi-doctrine tactical AIs
    /// (<see cref="TuranTacticMissionBehavior"/>, <see cref="ByzantineTacticMissionBehavior"/>).
    ///
    /// Both used to be ~780-line copies of each other that differed only in their culture/kingdom
    /// ids, doctrine names, announcement strings and one doctrine-selection condition - so every
    /// fix (and there were many, see graphify-out/graph.md's v1.7.9-v1.8.1 history) had to be
    /// written twice and could silently land in only one of them. Everything culture-independent
    /// now lives here once; a subclass only answers "which team is mine", "can my cavalry run an
    /// envelopment" and "what do I call each doctrine".
    ///
    /// Flow (one decision pass every <see cref="DecisionTickIntervalSeconds"/>): pick a doctrine
    /// from the army's composition -> stage on the best nearby high ground while skirmishing ->
    /// (cavalry doctrine only) feigned retreat -> dual-flank encirclement -> decisive charge ->
    /// hand the battle back to Native's team AI. Every per-formation stance decision is delegated
    /// to the engine-independent, unit-tested <see cref="TacticalSituationAssessor"/>.
    ///
    /// Orders are only ever issued to formations the engine itself marks AI-controlled
    /// (<see cref="Formation.IsAIControlled"/>) - never to formations the player is commanding.
    /// </summary>
    public abstract class DoctrineTacticMissionBehaviorBase : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // --- Cadence -------------------------------------------------------------------------
        protected const float DecisionTickIntervalSeconds = 1.25f;
        private const float DoctrineReevaluationIntervalSeconds = 9f;

        // --- Doctrine selection ----------------------------------------------------------------
        private const float OutnumberedRatioThreshold = 1.8f;
        private const float CavalryDoctrineRatioThreshold = 0.30f;
        private const float InfantryDoctrineRatioThreshold = 0.45f;

        // --- Phase transitions (seconds in phase / team-center-to-enemy distance in meters) -----
        private const float StagingTimeoutSeconds = 22f;
        private const float StagingEngageDistance = 90f;
        private const float FeignedRetreatTimeoutSeconds = 14f;
        private const float FeignedRetreatBaitDistance = 55f;
        private const float EncirclementTimeoutSeconds = 16f;
        private const float EncirclementContactDistance = 30f;
        private const float DecisiveChargeHandoverSeconds = 35f;

        // --- Geometry ----------------------------------------------------------------------------
        private const float HighGroundSearchRadius = 80f;
        private const float FlankOffsetDistance = 85f;
        private const float KiteFallbackDistance = 25f;
        private const float KiteRangeFactor = 0.85f;
        private const float FootArcherStandoffDistance = 12f;

        /// <summary>
        /// A move order whose target is within this distance (squared, meters) of the formation's
        /// current move target is not re-issued. Re-issuing an identical order every decision tick
        /// cancels and re-applies it inside the engine each time, which makes a formation visibly
        /// hitch/re-path every 1.25s instead of simply continuing to its destination.
        /// </summary>
        private const float MoveOrderRepathToleranceSquared = 5f * 5f;

        private DoctrineArchetype _activeDoctrine = DoctrineArchetype.Undecided;
        private TacticalPhase _currentPhase = TacticalPhase.InitialAssessment;
        private Team _ownTeam;
        private Team _enemyTeam;
        private MissionTime _phaseTimer;
        private MissionTime _tickThrottleTimer;
        private MissionTime _doctrineReevalTimer;
        private Vec3 _anchorHighGround;
        private Vec3 _designatedKillzone;
        private Vec3 _leftFlankPosition;
        private Vec3 _rightFlankPosition;
        private bool? _cavalryUsesLeftFlank;
        private bool _shockCavalryCommittedToCharge;
        private bool _shockCavalryRegrouped;
        private MissionTime? _awaitOpeningStartTime;
        private bool _infantryCommittedToAdvance;
        private bool _infantryRegrouped;
        private MissionTime? _infantryHoldStartTime;

        /// <summary>Culture.StringId whose troops/general mark a team as this behavior's own.</summary>
        protected abstract string CultureId { get; }

        /// <summary>Kingdom.StringId whose general marks a team as this behavior's own.</summary>
        protected abstract string KingdomId { get; }

        /// <summary>
        /// Whether this culture's cavalry mix can run the <see cref="DoctrineArchetype.CavalryEnvelopment"/>
        /// doctrine (already gated on cavalry being at least 30% of the army). Seljuk doctrine rests
        /// on horse archers, Byzantine doctrine on armored shock cavalry.
        /// </summary>
        protected abstract bool IsCavalryEnvelopmentViable(int horseArcherCount, int shockCavalryCount);

        /// <summary>Localized announcement ("{=...}" string id + English fallback) shown when <paramref name="doctrine"/> is selected.</summary>
        protected abstract string GetDoctrineAnnouncement(DoctrineArchetype doctrine);

        /// <summary>Localized announcement ("{=...}" string id + English fallback) shown when the feigned retreat springs the trap.</summary>
        protected abstract string LinesBrokenAnnouncement { get; }

        /// <summary>Localized announcement ("{=...}" string id + English fallback) shown when the decisive all-lines charge begins.</summary>
        protected abstract string FullAssaultAnnouncement { get; }

        public DoctrineArchetype ActiveDoctrine => _activeDoctrine;

        public TacticalPhase CurrentPhase => _currentPhase;

        public override void AfterStart()
        {
            base.AfterStart();
            ResetBattleState();
            _currentPhase = TacticalPhase.InitialAssessment;
            _phaseTimer = MissionTime.Now;
            _tickThrottleTimer = MissionTime.Now;
            _doctrineReevalTimer = MissionTime.Now;
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

                // Uses its own timer, separate from _phaseTimer (which tracks phase duration) -
                // sharing one timer for both purposes meant this gate stopped throttling anything
                // after the first 1.25s, since _phaseTimer only resets on a phase transition.
                if (_tickThrottleTimer.ElapsedSeconds > DecisionTickIntervalSeconds)
                {
                    _tickThrottleTimer = MissionTime.Now;
                    ExecuteTacticalDecisionLoop();
                }
            }
            catch (Exception)
            {
                // Absolute fail-safe: on any unexpected engine state, degrade gracefully to Native AI.
                _activeDoctrine = DoctrineArchetype.EngineFallback;
            }
        }

        private void ExecuteTacticalDecisionLoop()
        {
            if (_activeDoctrine == DoctrineArchetype.EngineFallback) return;

            if (_currentPhase != TacticalPhase.InitialAssessment && _currentPhase != TacticalPhase.BattleEnded)
            {
                MaybeDowngradeDoctrine();
            }

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
        /// 1. Force analysis and doctrine selection.
        /// </summary>
        private void EvaluateAndSelectDoctrine()
        {
            if (Mission.Current.Teams == null || Mission.Current.Teams.Count < 2)
            {
                _activeDoctrine = DoctrineArchetype.EngineFallback;
                return;
            }

            // Only take over a side that is actually this culture's. This behavior is added to
            // every non-siege field battle in the game (see SeljukSubModule.OnMissionBehaviorInitialize),
            // so without this check it would hijack a side in battles this culture isn't even in.
            if (IsOwnCultureTeam(Mission.Current.DefenderTeam))
            {
                _ownTeam = Mission.Current.DefenderTeam;
            }
            else if (IsOwnCultureTeam(Mission.Current.AttackerTeam))
            {
                _ownTeam = Mission.Current.AttackerTeam;
            }
            else
            {
                _activeDoctrine = DoctrineArchetype.EngineFallback;
                return;
            }

            foreach (var team in Mission.Current.Teams)
            {
                if (team != _ownTeam && team.IsEnemyOf(_ownTeam))
                {
                    _enemyTeam = team;
                    break;
                }
            }

            if (_enemyTeam == null)
            {
                _activeDoctrine = DoctrineArchetype.EngineFallback;
                return;
            }

            int totalFriendly = 0;
            int horseArchers = 0;
            int shockCav = 0;
            int infantry = 0;

            foreach (var formation in _ownTeam.FormationsIncludingEmpty)
            {
                int count = formation.CountOfUnits;
                if (count <= 0) continue;
                totalFriendly += count;

                switch (formation.FormationIndex)
                {
                    case FormationClass.HorseArcher: horseArchers += count; break;
                    case FormationClass.Cavalry: shockCav += count; break;
                    case FormationClass.Infantry: infantry += count; break;
                }
            }

            int totalEnemy = 0;
            foreach (var enemyFormation in _enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits > 0) totalEnemy += enemyFormation.CountOfUnits;
            }

            if (totalFriendly == 0 || totalEnemy == 0)
            {
                _activeDoctrine = DoctrineArchetype.EngineFallback;
                return;
            }

            Vec3 teamCenter = GetTeamCenterPosition(_ownTeam);

            // Establish the terrain anchor on the best nearby high ground - prefer Native's own
            // slope-search evaluator when there is infantry to run it from. Team.GetFormation never
            // returns null (all formation slots are pre-created), so guard on CountOfUnits
            // explicitly, or an infantry-less army feeds a never-ticked Formation
            // (CachedAveragePosition == (0,0), invalid CachedMedianPosition) into the native search.
            Formation anchorInfantryFormation = _ownTeam.GetFormation(FormationClass.Infantry);
            FormationQuerySystem anchorQuerySystem = (anchorInfantryFormation != null && anchorInfantryFormation.CountOfUnits > 0)
                ? anchorInfantryFormation.QuerySystem
                : null;
            _anchorHighGround = TacticalFormationsHelper.FindOptimalHighGround(teamCenter, HighGroundSearchRadius, anchorQuerySystem);
            _designatedKillzone = teamCenter;

            float cavRatio = (float)(horseArchers + shockCav) / totalFriendly;
            float infantryRatio = (float)infantry / totalFriendly;
            float outnumberRatio = (float)totalEnemy / totalFriendly;

            if (outnumberRatio >= OutnumberedRatioThreshold)
            {
                _activeDoctrine = DoctrineArchetype.OutnumberedDefense;
            }
            else if (cavRatio >= CavalryDoctrineRatioThreshold && IsCavalryEnvelopmentViable(horseArchers, shockCav))
            {
                _activeDoctrine = DoctrineArchetype.CavalryEnvelopment;
            }
            else if (infantryRatio >= InfantryDoctrineRatioThreshold)
            {
                _activeDoctrine = DoctrineArchetype.ShieldWall;
            }
            else
            {
                _activeDoctrine = DoctrineArchetype.Crossfire;
            }

            Announce(GetDoctrineAnnouncement(_activeDoctrine), GetDoctrineColor(_activeDoctrine));

            _currentPhase = TacticalPhase.StagingAndSkirmish;
            _phaseTimer = MissionTime.Now;
        }

        /// <summary>
        /// 2. Stage 1: spatial positioning, reactive shield wall and skirmish probing.
        /// </summary>
        private void ExecuteStagingAndSkirmish()
        {
            Formation horseArchers = _ownTeam.GetFormation(FormationClass.HorseArcher);
            Formation shockCavalry = _ownTeam.GetFormation(FormationClass.Cavalry);
            Formation infantry = _ownTeam.GetFormation(FormationClass.Infantry);
            Formation footArchers = _ownTeam.GetFormation(FormationClass.Ranged);

            Vec3 enemyPos = GetTeamCenterPosition(_enemyTeam);
            _leftFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, true, FlankOffsetDistance);
            _rightFlankPosition = TacticalFormationsHelper.CalculateFlankVector(_anchorHighGround, enemyPos, false, FlankOffsetDistance);

            // Infantry anchors the line on the high ground - shield wall only when actually needed.
            if (IsCommandable(infantry))
            {
                // Emergency override: an enemy shock-cavalry charge is about to strike our front
                // within ~15 seconds - brace immediately even while still marching to the anchor,
                // rather than only reacting once DualFlankEncirclement begins (that phase can start
                // up to 22s / 90m into the battle, well after this signal would have fired first).
                if (infantry.QuerySystem.IsUnderCavalryChargeFromFront)
                {
                    SetArrangementIfChanged(infantry, ArrangementOrder.ArrangementOrderShieldWall);
                    SetStopIfChanged(infantry);
                }
                else
                {
                    SetMoveIfChanged(infantry, _anchorHighGround);

                    FormationQuerySystem closestEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                    bool hasSignificantEnemy = closestEnemyQs != null;
                    bool underRangedAttack = infantry.QuerySystem.IsUnderRangedAttack;

                    SetArrangementIfChanged(infantry,
                        TacticalSituationAssessor.ShouldFormShieldWall(hasSignificantEnemy, GetCombinedCavalryRatio(closestEnemyQs), underRangedAttack)
                            ? ArrangementOrder.ArrangementOrderShieldWall
                            : ArrangementOrder.ArrangementOrderLine);
                }
            }

            // Foot archers placed right behind the line - reactive stance, never a blind melee charge.
            ApplyFootArcherStance(footArchers);

            // Shock cavalry stages on a flank, held back for the decisive blow.
            if (IsCommandable(shockCavalry))
            {
                SetMoveIfChanged(shockCavalry, GetCavalryStagingFlank(shockCavalry));
                SetArrangementIfChanged(shockCavalry, ArrangementOrder.ArrangementOrderSkein);
            }

            // Horse archers probe and skirmish - reactive stance, never a blind melee charge.
            ApplyHorseArcherStance(horseArchers);

            if (_phaseTimer.ElapsedSeconds > StagingTimeoutSeconds || IsEnemyWithinDistance(StagingEngageDistance))
            {
                _currentPhase = _activeDoctrine == DoctrineArchetype.CavalryEnvelopment
                    ? TacticalPhase.FeignedRetreatBait
                    : TacticalPhase.DualFlankEncirclement;
                _phaseTimer = MissionTime.Now;
            }
        }

        /// <summary>
        /// 3. Stage 2: feigned retreat - horse archers fall back into the killzone between the
        /// high-ground anchor and the flanking wings to draw the enemy out of cohesion.
        /// </summary>
        private void ExecuteFeignedRetreat()
        {
            Formation horseArchers = _ownTeam.GetFormation(FormationClass.HorseArcher);

            if (IsCommandable(horseArchers))
            {
                SetMoveIfChanged(horseArchers, _designatedKillzone);
            }

            if (_phaseTimer.ElapsedSeconds > FeignedRetreatTimeoutSeconds || IsEnemyWithinDistance(FeignedRetreatBaitDistance))
            {
                _currentPhase = TacticalPhase.DualFlankEncirclement;
                _phaseTimer = MissionTime.Now;
                Announce(LinesBrokenAnnouncement, Colors.Yellow);
            }
        }

        /// <summary>
        /// 4. Stage 3: dual-flank encirclement - every arm switches to its reactive stance.
        /// </summary>
        private void ExecuteDualFlankEncirclement()
        {
            Formation shockCavalry = _ownTeam.GetFormation(FormationClass.Cavalry);

            ApplyShockCavalryStance(shockCavalry);
            ApplyHorseArcherStance(_ownTeam.GetFormation(FormationClass.HorseArcher));
            ApplyInfantryStance(_ownTeam.GetFormation(FormationClass.Infantry));
            ApplyFootArcherStance(_ownTeam.GetFormation(FormationClass.Ranged));

            // Only let the fixed-time/distance transition push the battle into the all-in final
            // phase once shock cavalry has actually committed to a charge (or already made its
            // regroup call) - otherwise this would drag a formation still correctly waiting out a
            // braced enemy line into a forced charge. Cavalry this behavior can't command (none,
            // or under the player's control) never gets to make that call, so it must not hold the
            // phase back either - previously a player-commanded cavalry formation left the battle
            // stuck in this phase for good.
            bool cavalryReadyToAdvance = !IsCommandable(shockCavalry)
                || _shockCavalryCommittedToCharge || _shockCavalryRegrouped;

            if (cavalryReadyToAdvance && (_phaseTimer.ElapsedSeconds > EncirclementTimeoutSeconds || IsEnemyWithinDistance(EncirclementContactDistance)))
            {
                _currentPhase = TacticalPhase.DecisiveHammerCharge;
                _phaseTimer = MissionTime.Now;
                Announce(FullAssaultAnnouncement, Colors.Red);
            }
        }

        /// <summary>
        /// 5. Stage 4: decisive hammer-and-anvil charge, then hand the battle back to Native.
        /// </summary>
        private void ExecuteDecisiveHammerCharge()
        {
            ApplyInfantryStance(_ownTeam.GetFormation(FormationClass.Infantry));
            ApplyFootArcherStance(_ownTeam.GetFormation(FormationClass.Ranged));
            ApplyShockCavalryStance(_ownTeam.GetFormation(FormationClass.Cavalry));
            ApplyHorseArcherStance(_ownTeam.GetFormation(FormationClass.HorseArcher));

            // Once the decisive melee is underway, return control smoothly to Native's team AI.
            if (_phaseTimer.ElapsedSeconds > DecisiveChargeHandoverSeconds)
            {
                _activeDoctrine = DoctrineArchetype.EngineFallback;
            }
        }

        /// <summary>
        /// 6. Periodic doctrine re-evaluation. One-way: once downgraded to OutnumberedDefense for
        /// heavy losses, this battle never upgrades back to a more aggressive doctrine.
        /// </summary>
        private void MaybeDowngradeDoctrine()
        {
            if (_activeDoctrine == DoctrineArchetype.OutnumberedDefense) return;
            if (_doctrineReevalTimer.ElapsedSeconds < DoctrineReevaluationIntervalSeconds) return;

            _doctrineReevalTimer = MissionTime.Now;

            float totalCasualtyRatio = 0f;
            int formationCount = 0;

            foreach (var formation in _ownTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                // CasualtyRatio is Native's surviving fraction (alive / (alive+dead)), not a
                // casualty fraction - invert it so this variable means what its name says.
                totalCasualtyRatio += 1f - formation.QuerySystem.CasualtyRatio;
                formationCount++;
            }

            if (formationCount == 0) return;

            if (TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(totalCasualtyRatio / formationCount))
            {
                _activeDoctrine = DoctrineArchetype.OutnumberedDefense;
            }
        }

        /// <summary>
        /// 7. Horse archer stance: extracts primitives from Formation.QuerySystem, asks
        /// TacticalSituationAssessor what to do, and translates the answer into orders. Never
        /// issues a blind melee charge while ammunition lasts.
        /// </summary>
        private void ApplyHorseArcherStance(Formation horseArchers)
        {
            if (!IsCommandable(horseArchers)) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(horseArchers);
            FormationQuerySystem closestEnemyQs = horseArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            switch (TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio))
            {
                case FormationStance.HoldAndSkirmish:
                    SetArrangementIfChanged(horseArchers, ArrangementOrder.ArrangementOrderLoose);
                    if (hasSignificantEnemy)
                    {
                        Vec3 archerPos = horseArchers.CachedAveragePosition.ToVec3();
                        Vec3 enemyPos = closestEnemyQs.Formation.CachedAveragePosition.ToVec3();
                        float kiteRange = horseArchers.QuerySystem.MissileRangeAdjusted * KiteRangeFactor;
                        // Only reposition when the enemy is actually close enough to shoot at -
                        // otherwise leave the current order alone rather than chasing a fallback
                        // point computed from a moving live position every tick.
                        if (archerPos.AsVec2.DistanceSquared(enemyPos.AsVec2) < kiteRange * kiteRange)
                        {
                            SetMoveIfChanged(horseArchers, TacticalFormationsHelper.CalculateFallbackVector(archerPos, enemyPos, KiteFallbackDistance));
                        }
                    }
                    break;

                case FormationStance.Pursue:
                    SetArrangementIfChanged(horseArchers, ArrangementOrder.ArrangementOrderSkein);
                    SetChargeIfChanged(horseArchers);
                    break;

                case FormationStance.Regroup:
                    SetMoveIfChanged(horseArchers, _anchorHighGround);
                    break;
            }
        }

        /// <summary>
        /// 8. Shock cavalry stance: gates the charge behind a braced-line check before committing,
        /// then keeps polling every tick while charging so a losing fight gets recalled instead of
        /// fought to the last horse.
        /// </summary>
        private void ApplyShockCavalryStance(Formation shockCavalry)
        {
            if (!IsCommandable(shockCavalry)) return;
            if (_shockCavalryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = shockCavalry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;

            FormationStance stance = TacticalSituationAssessor.AssessShockCavalryStance(
                isCurrentlyCharging: _shockCavalryCommittedToCharge,
                hasSignificantEnemyFormation: hasSignificantEnemy,
                enemyCurrentSpeed: hasSignificantEnemy ? closestEnemyQs.Formation.CachedCurrentVelocity.Length : 0f,
                enemyInfantryUnitRatio: hasSignificantEnemy ? closestEnemyQs.InfantryUnitRatio : 0f,
                enemyHasShieldUnitRatio: hasSignificantEnemy ? closestEnemyQs.HasShieldUnitRatio : 0f,
                enemyCasualtyRatio: hasSignificantEnemy ? 1f - closestEnemyQs.CasualtyRatio : 0f,
                secondsSinceAwaitOpeningStarted: _awaitOpeningStartTime.HasValue ? _awaitOpeningStartTime.Value.ElapsedSeconds : 0f,
                selfCasualtyRatio: 1f - shockCavalry.QuerySystem.CasualtyRatio,
                selfLocalPowerRatio: shockCavalry.QuerySystem.LocalPowerRatio,
                isDefensivePosture: _activeDoctrine == DoctrineArchetype.OutnumberedDefense);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_awaitOpeningStartTime.HasValue)
                    {
                        _awaitOpeningStartTime = MissionTime.Now;
                    }
                    SetArrangementIfChanged(shockCavalry, ArrangementOrder.ArrangementOrderSkein);
                    SetMoveIfChanged(shockCavalry, GetCavalryStagingFlank(shockCavalry));
                    break;

                case FormationStance.AdvanceAndCharge:
                    _shockCavalryCommittedToCharge = true;
                    SetArrangementIfChanged(shockCavalry, ArrangementOrder.ArrangementOrderSkein);
                    SetChargeIfChanged(shockCavalry);
                    break;

                case FormationStance.Regroup:
                    _shockCavalryRegrouped = true;
                    SetMoveIfChanged(shockCavalry, _anchorHighGround);
                    break;
            }
        }

        /// <summary>
        /// 9. Foot archer stance: the same decision logic as horse archers (a ranged formation's
        /// hold-and-skirmish/pursue/regroup choice doesn't depend on being mounted), so it reuses
        /// AssessHorseArcherStance directly. Only order translation differs: no
        /// ArrangementOrderSkein (a cavalry wedge formation, meaningless without horses).
        /// </summary>
        private void ApplyFootArcherStance(Formation footArchers)
        {
            if (!IsCommandable(footArchers)) return;

            bool hasAmmo = !TacticalFormationsHelper.IsRangedAmmoDepleted(footArchers);
            FormationQuerySystem closestEnemyQs = footArchers.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;
            float enemyPowerRatio = hasSignificantEnemy ? closestEnemyQs.LocalPowerRatio : 0f;

            switch (TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasSignificantEnemy, enemyPowerRatio))
            {
                case FormationStance.HoldAndSkirmish:
                {
                    SetArrangementIfChanged(footArchers, ArrangementOrder.ArrangementOrderLoose);
                    Vec3 archerPos = footArchers.CachedAveragePosition.ToVec3();
                    Vec3 referenceEnemyPos = hasSignificantEnemy
                        ? closestEnemyQs.Formation.CachedAveragePosition.ToVec3()
                        : GetTeamCenterPosition(_enemyTeam);
                    float kiteRange = footArchers.QuerySystem.MissileRangeAdjusted * KiteRangeFactor;
                    bool enemyInKiteRange = hasSignificantEnemy && archerPos.AsVec2.DistanceSquared(referenceEnemyPos.AsVec2) < kiteRange * kiteRange;

                    if (enemyInKiteRange)
                    {
                        SetMoveIfChanged(footArchers, TacticalFormationsHelper.CalculateFallbackVector(archerPos, referenceEnemyPos, KiteFallbackDistance));
                    }
                    else
                    {
                        // Enemy not close enough to kite from yet - hold a position just behind the
                        // anchor, facing the enemy, instead of ceding positioning to Native team AI
                        // for the whole approach. Direction is taken on the ground plane: the
                        // anchor carries real terrain height while formation positions don't, so a
                        // 3D direction would tilt and shorten the standoff distance on slopes.
                        Vec2 enemyDir = (referenceEnemyPos.AsVec2 - _anchorHighGround.AsVec2).Normalized();
                        Vec2 standoff = _anchorHighGround.AsVec2 - enemyDir * FootArcherStandoffDistance;
                        SetMoveIfChanged(footArchers, TacticalFormationsHelper.ClampToMapBoundaries(new Vec3(standoff.x, standoff.y, _anchorHighGround.z)));
                    }
                    break;
                }

                case FormationStance.Pursue:
                    SetArrangementIfChanged(footArchers, ArrangementOrder.ArrangementOrderLoose);
                    SetChargeIfChanged(footArchers);
                    break;

                case FormationStance.Regroup:
                    SetMoveIfChanged(footArchers, _anchorHighGround);
                    break;
            }
        }

        /// <summary>
        /// 10. Infantry stance: shield wall only when facing a cavalry-heavy enemy or already under
        /// missile fire (ShouldFormShieldWall), commits to an advance only when that's actually
        /// favorable, and disengages a losing advance instead of fighting to the last man.
        /// </summary>
        private void ApplyInfantryStance(Formation infantry)
        {
            if (!IsCommandable(infantry)) return;

            // Emergency override: an enemy shock-cavalry charge is about to strike our front
            // within ~15 seconds (decompile-verified: excludes horse archers and flank/rear hits by
            // design). Snap to a brace immediately, regardless of what the stance machine below was
            // doing - it resumes on its own once the window passes.
            if (infantry.QuerySystem.IsUnderCavalryChargeFromFront)
            {
                SetArrangementIfChanged(infantry, ArrangementOrder.ArrangementOrderShieldWall);
                SetStopIfChanged(infantry);
                return;
            }

            if (_infantryRegrouped) return; // one-way disengage for the rest of this battle

            FormationQuerySystem closestEnemyQs = infantry.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
            bool hasSignificantEnemy = closestEnemyQs != null;

            FormationStance stance = TacticalSituationAssessor.AssessInfantryStance(
                isCurrentlyAdvancing: _infantryCommittedToAdvance,
                hasSignificantEnemyFormation: hasSignificantEnemy,
                enemyCavalryUnitRatio: GetCombinedCavalryRatio(closestEnemyQs),
                isUnderHeavyRangedAttack: infantry.QuerySystem.IsUnderRangedAttack,
                enemyCasualtyRatio: hasSignificantEnemy ? 1f - closestEnemyQs.CasualtyRatio : 0f,
                secondsSinceHoldStarted: _infantryHoldStartTime.HasValue ? _infantryHoldStartTime.Value.ElapsedSeconds : 0f,
                selfCasualtyRatio: 1f - infantry.QuerySystem.CasualtyRatio,
                selfLocalPowerRatio: infantry.QuerySystem.LocalPowerRatio,
                isDefensivePosture: _activeDoctrine == DoctrineArchetype.OutnumberedDefense);

            switch (stance)
            {
                case FormationStance.AwaitOpening:
                    if (!_infantryHoldStartTime.HasValue)
                    {
                        _infantryHoldStartTime = MissionTime.Now;
                    }
                    SetArrangementIfChanged(infantry, ArrangementOrder.ArrangementOrderShieldWall);
                    SetMoveIfChanged(infantry, _anchorHighGround);
                    break;

                case FormationStance.AdvanceAndCharge:
                    _infantryCommittedToAdvance = true;
                    SetArrangementIfChanged(infantry, ArrangementOrder.ArrangementOrderLine);
                    SetChargeIfChanged(infantry);
                    break;

                case FormationStance.Regroup:
                    _infantryRegrouped = true;
                    SetArrangementIfChanged(infantry, ArrangementOrder.ArrangementOrderShieldWall);
                    SetMoveIfChanged(infantry, _anchorHighGround);
                    break;
            }
        }

        /// <summary>
        /// Enemy cavalry share of the closest significant enemy formation, horse archers included:
        /// the engine tracks them as a separate FormationClass (decompile-verified), so
        /// CavalryUnitRatio alone misses exactly the steppe horse-archer threat this mod is about.
        /// </summary>
        private static float GetCombinedCavalryRatio(FormationQuerySystem enemyQuerySystem)
        {
            return enemyQuerySystem != null
                ? enemyQuerySystem.CavalryUnitRatio + enemyQuerySystem.RangedCavalryUnitRatio
                : 0f;
        }

        /// <summary>
        /// The flank shock cavalry stages on. Picked once per battle - whichever flank is nearer to
        /// where the cavalry actually deployed - and then kept, so the formation neither gallops
        /// across the whole enemy front to reach the far wing (it used to always take the left one)
        /// nor flip-flops between wings as the recomputed flank points shift with the enemy.
        /// </summary>
        private Vec3 GetCavalryStagingFlank(Formation shockCavalry)
        {
            if (!_cavalryUsesLeftFlank.HasValue)
            {
                Vec2 cavalryPos = GetEffectivePosition(shockCavalry);
                _cavalryUsesLeftFlank = cavalryPos.DistanceSquared(_leftFlankPosition.AsVec2)
                    <= cavalryPos.DistanceSquared(_rightFlankPosition.AsVec2);
            }

            return _cavalryUsesLeftFlank.Value ? _leftFlankPosition : _rightFlankPosition;
        }

        /// <summary>
        /// True if this team is meaningfully this culture's: either commanded by a general of this
        /// culture or kingdom, or made up of a majority of this culture's troops among its
        /// currently active agents. Reads culture through BasicCharacterObject so the check also
        /// works outside the campaign (custom battles), where agents aren't CharacterObjects.
        /// </summary>
        private bool IsOwnCultureTeam(Team team)
        {
            if (team == null) return false;

            BasicCharacterObject general = team.GeneralAgent?.Character;
            if (general != null)
            {
                if (general.Culture?.StringId == CultureId)
                {
                    return true;
                }
                if (general is CharacterObject generalCharacter && generalCharacter.HeroObject?.Clan?.Kingdom?.StringId == KingdomId)
                {
                    return true;
                }
            }

            string cultureId = CultureId;
            int ownCultureCount = 0;
            int sampledCount = 0;
            foreach (var formation in team.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits <= 0) continue;
                formation.ApplyActionOnEachUnit(agent =>
                {
                    string troopCultureId = agent != null && agent.IsActive() ? agent.Character?.Culture?.StringId : null;
                    if (troopCultureId == null) return;

                    sampledCount++;
                    if (troopCultureId == cultureId)
                    {
                        ownCultureCount++;
                    }
                });
            }

            return sampledCount > 0 && ownCultureCount * 2 >= sampledCount;
        }

        /// <summary>
        /// Unit-weighted centroid of a team's actual formation positions, at real terrain height.
        /// Previously an unweighted mean of each formation's *order* position with z = 0: a 5-man
        /// leftover formation pulled the "center" as hard as a 200-man infantry block, the enemy's
        /// distance was measured to where it had been told to go rather than where it was, and the
        /// zero height made FindOptimalHighGround treat any terrain above sea level as "higher
        /// ground" than the army - including slopes below an army already standing on a hill.
        /// </summary>
        private static Vec3 GetTeamCenterPosition(Team team)
        {
            if (team == null) return Vec3.Zero;

            Vec2 weightedSum = Vec2.Zero;
            int unitCount = 0;

            foreach (var formation in team.FormationsIncludingEmpty)
            {
                int count = formation.CountOfUnits;
                if (count <= 0) continue;

                weightedSum += GetEffectivePosition(formation) * count;
                unitCount += count;
            }

            if (unitCount == 0) return Vec3.Zero;

            Vec2 center = weightedSum * (1f / unitCount);
            Scene scene = Mission.Current?.Scene;
            return new Vec3(center.x, center.y, scene != null ? scene.GetTerrainHeight(center) : 0f);
        }

        /// <summary>
        /// Where a formation actually is. CachedAveragePosition is (0,0) until the engine's first
        /// position-cache pass for a freshly populated formation, so fall back to its order position
        /// for that brief window rather than dragging a team centroid toward the map's corner.
        /// </summary>
        private static Vec2 GetEffectivePosition(Formation formation)
        {
            Vec2 position = formation.CachedAveragePosition;
            return position.x == 0f && position.y == 0f ? formation.OrderPosition : position;
        }

        private bool IsEnemyWithinDistance(float distance)
        {
            if (_ownTeam == null || _enemyTeam == null) return false;

            float distSq = distance * distance;
            Vec2 friendlyCenter = GetTeamCenterPosition(_ownTeam).AsVec2;

            foreach (var enemyFormation in _enemyTeam.FormationsIncludingEmpty)
            {
                if (enemyFormation.CountOfUnits <= 0) continue;
                if (friendlyCenter.DistanceSquared(GetEffectivePosition(enemyFormation)) < distSq)
                {
                    return true;
                }
            }
            return false;
        }

        // --- Order issuing -----------------------------------------------------------------------

        /// <summary>
        /// Whether this behavior may issue orders to <paramref name="formation"/>: it must have
        /// units, and the engine must consider it AI-controlled. Formations the player commands
        /// directly (as general, or as a sergeant of their own formation) are never touched - this
        /// behavior used to re-issue its own orders over the player's every 1.25s for the first
        /// minute or more of every battle their army was Seljuk/Byzantine. A player who wants the
        /// doctrine to run their army can hand it over with Native's own delegate-command toggle.
        /// </summary>
        private static bool IsCommandable(Formation formation)
        {
            return formation != null && formation.CountOfUnits > 0 && formation.IsAIControlled;
        }

        private static void SetArrangementIfChanged(Formation formation, ArrangementOrder order)
        {
            if (formation.ArrangementOrder.OrderEnum != order.OrderEnum)
            {
                formation.SetArrangementOrder(order);
            }
        }

        private static void SetChargeIfChanged(Formation formation)
        {
            if (formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Charge)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
            }
        }

        private static void SetStopIfChanged(Formation formation)
        {
            if (formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Stop)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderStop);
            }
        }

        private static void SetMoveIfChanged(Formation formation, Vec3 target)
        {
            if (formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Move
                && formation.OrderPosition.DistanceSquared(target.AsVec2) < MoveOrderRepathToleranceSquared)
            {
                return;
            }

            formation.SetMovementOrder(MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, target)));
        }

        // --- Announcements -------------------------------------------------------------------------

        private static Color GetDoctrineColor(DoctrineArchetype doctrine)
        {
            switch (doctrine)
            {
                case DoctrineArchetype.ShieldWall: return Colors.Cyan;
                case DoctrineArchetype.Crossfire: return Colors.Green;
                default: return Colors.Yellow;
            }
        }

        /// <summary>
        /// Shows a doctrine announcement - unless this behavior can't actually command any of its
        /// team's formations (the player is leading that army personally), in which case claiming
        /// "[Seljuk Tactical Command] ... underway!" would describe orders that are never given.
        /// </summary>
        private void Announce(string localizedKeyAndFallback, Color color)
        {
            if (string.IsNullOrEmpty(localizedKeyAndFallback) || !HasAnyCommandableFormation(_ownTeam)) return;

            InformationManager.DisplayMessage(new InformationMessage(new TextObject(localizedKeyAndFallback).ToString(), color));
        }

        private static bool HasAnyCommandableFormation(Team team)
        {
            if (team == null) return false;

            foreach (var formation in team.FormationsIncludingEmpty)
            {
                if (IsCommandable(formation)) return true;
            }
            return false;
        }

        // --- Lifecycle -------------------------------------------------------------------------------

        private void ResetBattleState()
        {
            _ownTeam = null;
            _enemyTeam = null;
            _activeDoctrine = DoctrineArchetype.Undecided;
            _cavalryUsesLeftFlank = null;
            _shockCavalryCommittedToCharge = false;
            _shockCavalryRegrouped = false;
            _awaitOpeningStartTime = null;
            _infantryCommittedToAdvance = false;
            _infantryRegrouped = false;
            _infantryHoldStartTime = null;
        }

        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
            ResetBattleState();
            _currentPhase = TacticalPhase.BattleEnded;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _ownTeam = null;
            _enemyTeam = null;
        }
    }
}
