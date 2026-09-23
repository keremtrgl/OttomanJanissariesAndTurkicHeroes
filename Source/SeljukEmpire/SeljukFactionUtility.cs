using TaleWorlds.CampaignSystem.Settlements;

namespace SeljukEmpire
{
    /// <summary>
    /// Shared "does this settlement currently belong to kingdom X" checks, used by every behavior
    /// that grants kingdom-flavored menus, recruits, or rewards in that kingdom's settlements.
    /// Settlement ownership is dynamic (conquest moves fiefs between kingdoms during play), so this
    /// is always computed live from OwnerClan.Kingdom rather than a hardcoded settlement id list.
    /// Several behaviors used to each keep their own such list, and those lists had already gone
    /// stale after the mod's territory was rewritten (leftover "town_K1"/"castle_K2"-style ids that
    /// don't match any settlement this mod actually owns).
    /// </summary>
    public static class SeljukFactionUtility
    {
        public const string SeljukKingdomId = "kingdom_seljuks";
        public const string SeljukCultureId = "seljuk";

        public static bool IsSeljukSettlement(Settlement settlement)
        {
            return IsSettlementOfKingdom(settlement, SeljukKingdomId);
        }

        public static bool IsSettlementOfKingdom(Settlement settlement, string kingdomId)
        {
            // kingdomId null would otherwise match every kingdom-less (e.g. rebel/neutral) settlement.
            if (settlement == null || kingdomId == null) return false;

            if (settlement.OwnerClan?.Kingdom?.StringId == kingdomId)
            {
                return true;
            }

            // A village's own OwnerClan can differ from its bound town/castle's; fall back to the
            // bound settlement's owner so village-specific behaviors (recruitment, etc.) still
            // recognize the kingdom's villages correctly.
            return settlement.Village?.Bound?.OwnerClan?.Kingdom?.StringId == kingdomId;
        }
    }
}
