using TaleWorlds.CampaignSystem.Settlements;

namespace SeljukEmpire
{
    /// <summary>
    /// Shared "does this settlement currently belong to the Seljuk kingdom" check, used by every
    /// behavior that grants Seljuk-flavored menus, recruits, or rewards in Seljuk towns.
    /// Settlement ownership is dynamic (conquest moves fiefs in and out of Kingdom.kingdom_seljuks
    /// during play), so this is always computed live from OwnerClan.Kingdom rather than a
    /// hardcoded settlement id list. Several behaviors used to each keep their own such list, and
    /// those lists had already gone stale after the mod's territory was rewritten (leftover
    /// "town_K1"/"castle_K2"-style ids that don't match any settlement this mod actually owns) -
    /// harmless only because every one of them also had this same Kingdom fallback.
    /// </summary>
    public static class SeljukFactionUtility
    {
        public const string SeljukKingdomId = "kingdom_seljuks";
        public const string SeljukCultureId = "seljuk";

        public static bool IsSeljukSettlement(Settlement settlement)
        {
            if (settlement == null) return false;

            if (settlement.OwnerClan?.Kingdom != null && settlement.OwnerClan.Kingdom.StringId == SeljukKingdomId)
            {
                return true;
            }

            // A village's own OwnerClan can differ from its bound town/castle's; fall back to the
            // bound settlement's owner so village-specific behaviors (recruitment, etc.) still
            // recognize Seljuk villages correctly.
            if (settlement.Village?.Bound?.OwnerClan?.Kingdom != null && settlement.Village.Bound.OwnerClan.Kingdom.StringId == SeljukKingdomId)
            {
                return true;
            }

            return false;
        }
    }
}
