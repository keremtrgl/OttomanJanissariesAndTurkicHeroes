using System;
using System.Diagnostics;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Benchmarks
{
    /// <summary>
    /// Microbenchmark for TacticalSituationAssessor - the pure decision-logic layer the reactive
    /// tactical AI (TuranTacticMissionBehavior / ByzantineTacticMissionBehavior) calls from its own
    /// tick handler. This never touches the running game (TacticalSituationAssessor.cs has zero
    /// TaleWorlds.* references, same as Source/SeljukEmpire.Tests/), so it measures the one part of
    /// the reactive AI's per-tick cost that CAN be measured outside a live Bannerlord instance: the
    /// assessor's own CPU cost per call. It does not and cannot measure the engine-side cost of
    /// gathering Formation.QuerySystem data or issuing orders, which only exists inside a running
    /// mission.
    ///
    /// Read alongside this: both TuranTacticMissionBehavior.OnMissionTick and
    /// ByzantineTacticMissionBehavior.OnMissionTick gate the entire decision loop behind a
    /// _tickThrottleTimer.ElapsedSeconds > 1.25f check, so the assessor (and the QuerySystem reads
    /// that feed it) run at most ~0.8 times/second per team, not every frame - the same "throttle
    /// the expensive path" philosophy BattlePerformanceOptimizer already uses (ragdoll budget,
    /// LOD distance culling). The numbers below exist to confirm that even a
    /// deliberately pessimistic per-call cost estimate, multiplied out at that throttled frequency
    /// and a generous formation count, stays a negligible fraction of a 60fps frame budget.
    /// </summary>
    internal static class Program
    {
        private const int WarmupIterations = 100_000;
        private const int MeasuredIterations = 5_000_000;

        private static void Main()
        {
            Console.WriteLine("TacticalSituationAssessor microbenchmark");
            Console.WriteLine($"({MeasuredIterations:N0} iterations per method, after {WarmupIterations:N0} warmup calls)");
            Console.WriteLine();

            double horseArcherNs = Benchmark("AssessHorseArcherStance", BenchHorseArcherStance);
            double shockCavalryNs = Benchmark("AssessShockCavalryStance", BenchShockCavalryStance);
            double infantryNs = Benchmark("AssessInfantryStance", BenchInfantryStance);
            double shieldWallNs = Benchmark("ShouldFormShieldWall", BenchShieldWall);
            double downgradeNs = Benchmark("ShouldDowngradeToDefensiveDoctrine", BenchDowngrade);

            Console.WriteLine();
            Console.WriteLine("--- Per-tick budget estimate ---");

            // Pessimistic worst case: every formation on both sides calls the most expensive
            // method every throttled tick. 12 formations/team (Bannerlord's own per-team
            // formation cap is far lower in practice) x 2 teams = 24 calls.
            const int formationsPerTeam = 12;
            const int teams = 2;
            int callsPerThrottledTick = formationsPerTeam * teams;
            double worstCaseNsPerCall = Math.Max(Math.Max(horseArcherNs, shockCavalryNs), infantryNs);
            double totalNsPerThrottledTick = worstCaseNsPerCall * callsPerThrottledTick;
            double totalUsPerThrottledTick = totalNsPerThrottledTick / 1000.0;

            const double frameBudgetMs = 1000.0 / 60.0; // 60fps
            double pctOfOneFrame = (totalUsPerThrottledTick / 1000.0) / frameBudgetMs * 100.0;

            Console.WriteLine($"Worst-case single call: {worstCaseNsPerCall:F1} ns");
            Console.WriteLine($"{callsPerThrottledTick} calls (pessimistic: {formationsPerTeam}/team x {teams} teams) = {totalUsPerThrottledTick:F2} us per throttled tick");
            Console.WriteLine($"Throttled tick fires at most once per 1.25s (see OnMissionTick's _tickThrottleTimer gate) -");
            Console.WriteLine($"that {totalUsPerThrottledTick:F2} us is {pctOfOneFrame:F4}% of a single 60fps frame ({frameBudgetMs:F2} ms), spent once every 75 frames.");
            Console.WriteLine();
            Console.WriteLine("Conclusion: the assessor's own decision logic is not a meaningful CPU cost at any");
            Console.WriteLine("plausible battle size. If a future battle-performance regression is ever reported");
            Console.WriteLine("against this mod, it is far more likely to be in Formation.QuerySystem access patterns");
            Console.WriteLine("or order issuance inside TuranTacticMissionBehavior/ByzantineTacticMissionBehavior");
            Console.WriteLine("themselves (neither of which this benchmark can reach outside a running game) than in");
            Console.WriteLine("this file.");
        }

        private static double Benchmark(string name, Action<int> body)
        {
            body(WarmupIterations);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var sw = Stopwatch.StartNew();
            body(MeasuredIterations);
            sw.Stop();

            double nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / MeasuredIterations;
            Console.WriteLine($"{name,-36} {nsPerCall,8:F2} ns/call");
            return nsPerCall;
        }

        // Each Bench* method cycles its inputs across a handful of representative
        // (favorable/unfavorable/edge-case) values instead of one fixed input, so the JIT
        // can't trivially constant-fold the whole call away and the timing reflects real
        // branch-prediction behavior across the method's actual decision space.

        private static void BenchHorseArcherStance(int iterations)
        {
            FormationStance result = default;
            for (int i = 0; i < iterations; i++)
            {
                bool hasAmmo = (i & 1) == 0;
                bool hasEnemy = (i & 2) == 0;
                float ratio = (i % 3 == 0) ? 0.5f : 1.5f;
                result = TacticalSituationAssessor.AssessHorseArcherStance(hasAmmo, hasEnemy, ratio);
            }
            if ((int)result == -1) Console.WriteLine("unreachable"); // keep result observed
        }

        private static void BenchShockCavalryStance(int iterations)
        {
            FormationStance result = default;
            for (int i = 0; i < iterations; i++)
            {
                bool charging = (i & 1) == 0;
                bool hasEnemy = (i & 2) == 0;
                float enemySpeed = (i % 3 == 0) ? 0.2f : 2.0f;
                float infRatio = (i % 5) / 5f;
                float shieldRatio = (i % 7) / 7f;
                float enemyCasualty = (i % 11) / 11f;
                float awaitSeconds = i % 30;
                float selfCasualty = (i % 13) / 13f;
                float selfPower = (i % 3 == 0) ? 0.5f : 1.5f;
                bool defensive = (i & 4) == 0;
                result = TacticalSituationAssessor.AssessShockCavalryStance(
                    charging, hasEnemy, enemySpeed, infRatio, shieldRatio, enemyCasualty,
                    awaitSeconds, selfCasualty, selfPower, defensive);
            }
            if ((int)result == -1) Console.WriteLine("unreachable");
        }

        private static void BenchInfantryStance(int iterations)
        {
            FormationStance result = default;
            for (int i = 0; i < iterations; i++)
            {
                bool advancing = (i & 1) == 0;
                bool hasEnemy = (i & 2) == 0;
                float cavRatio = (i % 5) / 5f;
                bool heavyRanged = (i & 8) == 0;
                float enemyCasualty = (i % 11) / 11f;
                float holdSeconds = i % 40;
                float selfCasualty = (i % 13) / 13f;
                float selfPower = (i % 3 == 0) ? 0.5f : 1.5f;
                bool defensive = (i & 4) == 0;
                result = TacticalSituationAssessor.AssessInfantryStance(
                    advancing, hasEnemy, cavRatio, heavyRanged, enemyCasualty, holdSeconds,
                    selfCasualty, selfPower, defensive);
            }
            if ((int)result == -1) Console.WriteLine("unreachable");
        }

        private static void BenchShieldWall(int iterations)
        {
            bool result = false;
            for (int i = 0; i < iterations; i++)
            {
                bool hasEnemy = (i & 1) == 0;
                float cavRatio = (i % 5) / 5f;
                bool heavyRanged = (i & 2) == 0;
                result ^= TacticalSituationAssessor.ShouldFormShieldWall(hasEnemy, cavRatio, heavyRanged);
            }
            if (result && iterations < 0) Console.WriteLine("unreachable");
        }

        private static void BenchDowngrade(int iterations)
        {
            bool result = false;
            for (int i = 0; i < iterations; i++)
            {
                float casualtyRatio = (i % 17) / 17f;
                result ^= TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(casualtyRatio);
            }
            if (result && iterations < 0) Console.WriteLine("unreachable");
        }
    }
}
