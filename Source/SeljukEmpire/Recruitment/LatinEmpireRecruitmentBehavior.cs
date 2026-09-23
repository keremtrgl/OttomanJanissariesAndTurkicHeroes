namespace SeljukEmpire.Recruitment
{
    /// <summary>
    /// Kingdom.empire_w (Latin Empire) shares Culture.empire with Kingdom.empire_s (Bizans), so
    /// Culture.basic_troop (byz2_recruit) is claimed by Bizans and notables in Latin Empire
    /// settlements would otherwise recruit Byzantine troops. This puts the lat2_ tree in their
    /// volunteer slots instead - the same "one Culture, several kingdoms" fix as
    /// SeljukRecruitmentBehavior. Slot layout: <see cref="VolunteerSlotPolicy.GetLatinCandidates"/>.
    /// </summary>
    public class LatinEmpireRecruitmentBehavior : KingdomVolunteerRecruitmentBehaviorBase
    {
        private const string LatinEmpireKingdomId = "empire_w";

        protected override string KingdomId => LatinEmpireKingdomId;

        protected override bool ShouldReplaceVolunteer(string currentTroopId)
        {
            return VolunteerSlotPolicy.ShouldReplaceLatinVolunteer(currentTroopId);
        }

        protected override string[] GetCandidates(bool isVillage, bool isTownOrCastle, int slotIndex, float notablePower, bool isArtisan)
        {
            return VolunteerSlotPolicy.GetLatinCandidates(isVillage, isTownOrCastle, slotIndex, notablePower, isArtisan);
        }
    }
}
