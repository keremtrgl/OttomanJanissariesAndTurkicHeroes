namespace SeljukEmpire.Tactics
{
    /// <summary>
    /// Multi-doctrine Byzantine tactical AI - the Byzantine counterpart to
    /// <see cref="TuranTacticMissionBehavior"/>, running the same shared phase engine
    /// (<see cref="DoctrineTacticMissionBehaviorBase"/>) with Byzantine-flavored doctrine selection
    /// and battle messages.
    ///
    /// Historical grounding: Byzantine field doctrine (as described in the Strategikon and later
    /// Taktika military manuals) emphasized disciplined combined-arms coordination - a steady
    /// Tagma (professional regular army) infantry line holding the center while Toxotai archers
    /// provide missile support, with the decisive blow delivered by armored Kataphraktoi shock
    /// cavalry once the enemy line is fixed or has broken cohesion. Byzantine commanders also
    /// explicitly adopted steppe-style feigned-retreat and encirclement tactics from centuries of
    /// warfare against Avars, Huns, and Turkic peoples, which is why this doctrine set can share
    /// the Seljuk phase structure without being historically dishonest about it - the two
    /// traditions converged on similar battlefield mechanics from different origins.
    ///
    /// Doctrines, by <see cref="DoctrineArchetype"/>:
    ///   - CavalryEnvelopment -> Kataphraktoi Hammer and Anvil (needs armored shock cavalry).
    ///   - ShieldWall         -> Tagma Shield Wall: disciplined Skutatoi line at the choke point.
    ///   - Crossfire          -> Toxotai Crossfire: composite bows from a skirmish anchor.
    ///   - OutnumberedDefense -> Thematic Last Stand: high-ground defense against a superior force.
    ///
    /// This behavior and TuranTacticMissionBehavior can both be active in the same battle (e.g. a
    /// Seljuk army fighting a Byzantine one): each only ever issues orders to the single team it
    /// identifies as its own culture's, so the two never touch the same Formation objects.
    /// </summary>
    public class ByzantineTacticMissionBehavior : DoctrineTacticMissionBehaviorBase
    {
        private const string ByzantineCultureId = "empire";
        private const string ByzantineKingdomId = "empire_s";
        private const int MinimumShockCavalryForHammerAndAnvil = 6;

        protected override string CultureId => ByzantineCultureId;

        protected override string KingdomId => ByzantineKingdomId;

        protected override bool IsCavalryEnvelopmentViable(int horseArcherCount, int shockCavalryCount)
        {
            return shockCavalryCount >= MinimumShockCavalryForHammerAndAnvil;
        }

        protected override string GetDoctrineAnnouncement(DoctrineArchetype doctrine)
        {
            switch (doctrine)
            {
                case DoctrineArchetype.OutnumberedDefense:
                    return "{=byz_tactic_last_stand}[Byzantine Tactical Command] Thematic High-Ground Defense against a superior force!";
                case DoctrineArchetype.CavalryEnvelopment:
                    return "{=byz_tactic_hammer_anvil}[Byzantine Tactical Command] Kataphraktoi Hammer and Anvil deployed!";
                case DoctrineArchetype.ShieldWall:
                    return "{=byz_tactic_shield_wall}[Byzantine Tactical Command] Tagma Shield Wall formed at the choke point!";
                case DoctrineArchetype.Crossfire:
                    return "{=byz_tactic_crossfire}[Byzantine Tactical Command] Toxotai Crossfire formation engaged!";
                default:
                    return null;
            }
        }

        protected override string LinesBrokenAnnouncement =>
            "{=byz_tactic_lines_broken}[Kataphraktoi] The enemy line has broken cohesion! Dual-flank envelopment begins!";

        protected override string FullAssaultAnnouncement =>
            "{=byz_tactic_full_assault}[Tagma Advance] Hammer and Anvil complete! All lines advance!";
    }
}
