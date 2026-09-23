namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// Multi-doctrine Seljuk tactical AI. Takes over a field-battle team that is meaningfully
    /// Seljuk (Culture.seljuk general or majority troops, or a Kingdom.kingdom_seljuks general) and
    /// runs it through the shared phase engine in <see cref="DoctrineTacticMissionBehaviorBase"/>.
    ///
    /// Doctrines, by <see cref="DoctrineArchetype"/>:
    ///   - CavalryEnvelopment -> Turan Wolf-Trap: horse-archer skirmish, feigned retreat
    ///     (sahte geri çekilme) and a dual-flank crescent encirclement (iki kanattan hilal
    ///     kuşatması). Needs horse archers - the steppe tradition's signature arm.
    ///   - ShieldWall         -> Nizamiye Shield Wall: infantry choke hold on the high ground.
    ///   - Crossfire          -> Steppe Crossfire: composite-bow crossfire from a skirmish anchor.
    ///   - OutnumberedDefense -> High-Ground Ambush: last stand and counter-charge from the heights.
    /// </summary>
    public class TuranTacticMissionBehavior : DoctrineTacticMissionBehaviorBase
    {
        private const int MinimumHorseArchersForWolfTrap = 6;

        protected override string CultureId => SeljukFactionUtility.SeljukCultureId;

        protected override string KingdomId => SeljukFactionUtility.SeljukKingdomId;

        protected override bool IsCavalryEnvelopmentViable(int horseArcherCount, int shockCavalryCount)
        {
            return horseArcherCount >= MinimumHorseArchersForWolfTrap;
        }

        protected override string GetDoctrineAnnouncement(DoctrineArchetype doctrine)
        {
            switch (doctrine)
            {
                case DoctrineArchetype.OutnumberedDefense:
                    return "{=seljuk_tactic_high_ground}[Seljuk Tactical Command] High-Ground Defense initiated against a numerically superior enemy!";
                case DoctrineArchetype.CavalryEnvelopment:
                    return "{=seljuk_tactic_wolf_trap}[Seljuk Tactical Command] Wolf-Trap and Turan Crescent tactic underway!";
                case DoctrineArchetype.ShieldWall:
                    return "{=seljuk_tactic_shield_wall}[Seljuk Tactical Command] Nizamiye Shield Wall and choke-point defense formed!";
                case DoctrineArchetype.Crossfire:
                    return "{=seljuk_tactic_crossfire}[Seljuk Tactical Command] Steppe Crescent Crossfire formation engaged!";
                default:
                    return null;
            }
        }

        protected override string LinesBrokenAnnouncement =>
            "{=seljuk_tactic_lines_broken}[Wolf-Trap] The enemy line has broken! Dual-flank crescent encirclement begins!";

        protected override string FullAssaultAnnouncement =>
            "{=seljuk_tactic_full_assault}[Nizamiye Advance] Hammer and Anvil assault! All lines advance!";
    }
}
