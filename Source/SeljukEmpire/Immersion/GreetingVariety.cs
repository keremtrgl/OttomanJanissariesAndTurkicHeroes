namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Picks which of a character's alternative greeting lines is "current".
    ///
    /// Bannerlord's ConversationManager sorts dialog lines by priority and, for an NPC line,
    /// shows the FIRST one whose condition matches (confirmed by decompiling
    /// ConversationManager.GetSentenceOptions) - it does not pick randomly among equal-priority
    /// matches. So a plain second AddDialogLine with the same tokens/priority would never be seen.
    /// Instead each of a character's candidate lines gets a condition that is true only during
    /// its own slice of in-game hours, so on any single conversation exactly one line's condition
    /// is true. Every candidate is evaluated in the same conversation-open pass, at the same
    /// in-game time, so the line never changes mid-conversation - but a different one can show
    /// up on a later visit, at a different in-game hour.
    ///
    /// This half has no TaleWorlds.* reference so it is unit tested directly; the CampaignTime
    /// overload used by the dialogue behaviors lives in GreetingVariety.Campaign.cs.
    /// </summary>
    public static partial class GreetingVariety
    {
        /// <param name="totalCampaignHours">In-game hours since campaign start.</param>
        /// <param name="salt">Per-character offset, so characters don't all rotate in lockstep.</param>
        /// <param name="variantCount">How many alternative lines the character has (1 or more).</param>
        /// <returns>The index, in [0, variantCount), of the line to show right now.</returns>
        public static int SelectVariant(double totalCampaignHours, int salt, int variantCount)
        {
            if (variantCount <= 1) return 0;

            // Same arithmetic the four dialogue behaviors each used to carry a private copy of;
            // the double modulo keeps the result in range even for a negative salt.
            int hour = (int)totalCampaignHours;
            int slot = (hour % variantCount + salt % variantCount) % variantCount;
            return slot < 0 ? slot + variantCount : slot;
        }
    }
}
