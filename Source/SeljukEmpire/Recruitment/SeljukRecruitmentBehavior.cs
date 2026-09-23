namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Notables across Seljuk-owned settlements (villages, castles, towns) offer authentic,
    /// affordable Seljuk and Ottoman recruits - peasants, azaps, scouts, ghulam recruits and
    /// janissary cadets - in place of whatever troop tree the settlement's own culture would hand
    /// out. Slot layout: <see cref="VolunteerSlotPolicy.GetSeljukCandidates"/>.
    /// </summary>
    public class SeljukRecruitmentBehavior : KingdomVolunteerRecruitmentBehaviorBase
    {
        protected override string KingdomId => SeljukFactionUtility.SeljukKingdomId;

        protected override bool ShouldReplaceVolunteer(string currentTroopId)
        {
            return VolunteerSlotPolicy.ShouldReplaceSeljukVolunteer(currentTroopId);
        }

        protected override string[] GetCandidates(bool isVillage, bool isTownOrCastle, int slotIndex, float notablePower, bool isArtisan)
        {
            return VolunteerSlotPolicy.GetSeljukCandidates(isVillage, isTownOrCastle, slotIndex, notablePower, isArtisan);
        }
    }
}
