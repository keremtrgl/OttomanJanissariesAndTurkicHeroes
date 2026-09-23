using TaleWorlds.CampaignSystem;

namespace SeljukEmpire.Immersion
{
    public static partial class GreetingVariety
    {
        /// <summary>
        /// Index of the greeting line to show right now for a character with
        /// <paramref name="variantCount"/> alternative lines - see the class remarks.
        /// </summary>
        public static int Current(int salt, int variantCount)
        {
            return SelectVariant(CampaignTime.Now.ToHours, salt, variantCount);
        }
    }
}
