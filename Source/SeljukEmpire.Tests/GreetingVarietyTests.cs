using SeljukEmpire.Immersion;
using Xunit;

namespace SeljukEmpire.Tests
{
    public class GreetingVarietyTests
    {
        [Fact]
        public void MatchesTheOriginalPerBehaviorFormula()
        {
            // The four dialogue behaviors each used ((int)hours + salt) % count. The shared helper
            // must pick exactly the same line for every character at every hour, or players would
            // see a different greeting than before for no reason.
            for (int hour = 0; hour < 500; hour++)
            {
                for (int salt = 0; salt < 70; salt++)
                {
                    for (int count = 1; count <= 3; count++)
                    {
                        Assert.Equal((hour + salt) % count, GreetingVariety.SelectVariant(hour + 0.75, salt, count));
                    }
                }
            }
        }

        [Fact]
        public void EachVariantIsReachable()
        {
            var seen = new bool[3];
            for (int hour = 0; hour < 3; hour++)
            {
                seen[GreetingVariety.SelectVariant(hour, salt: 5, variantCount: 3)] = true;
            }
            Assert.All(seen, Assert.True);
        }

        [Fact]
        public void StableWithinTheSameHour()
        {
            Assert.Equal(GreetingVariety.SelectVariant(1234.01, 7, 3), GreetingVariety.SelectVariant(1234.99, 7, 3));
        }

        [Fact]
        public void SingleVariant_AlwaysZero()
        {
            Assert.Equal(0, GreetingVariety.SelectVariant(987654.0, 42, 1));
        }

        [Fact]
        public void NeverOverflowsOnLongCampaigns()
        {
            // (int)hours + salt used to be able to wrap negative; the result must stay in range.
            int variant = GreetingVariety.SelectVariant(int.MaxValue, int.MaxValue, 3);
            Assert.InRange(variant, 0, 2);
        }
    }
}
