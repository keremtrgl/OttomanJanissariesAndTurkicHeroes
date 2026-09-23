using SeljukEmpire.Economy;
using Xunit;

namespace SeljukEmpire.Tests
{
    public class CaravanInsurancePolicyTests
    {
        private static bool Eligible(
            bool policyActive = true,
            bool isPlayerOwnedCaravan = true,
            bool wasDisbanding = false,
            double daysSinceLastClaim = 30.0,
            int crewAtRisk = 20)
        {
            return CaravanInsurancePolicy.IsClaimEligible(policyActive, isPlayerOwnedCaravan, wasDisbanding, daysSinceLastClaim, crewAtRisk);
        }

        [Fact]
        public void GenuineLoss_WithPolicy_IsPaid()
        {
            Assert.True(Eligible());
        }

        [Fact]
        public void NoPolicy_IsNotPaid()
        {
            Assert.False(Eligible(policyActive: false));
        }

        [Fact]
        public void SomeoneElsesCaravan_IsNotPaid()
        {
            Assert.False(Eligible(isPlayerOwnedCaravan: false));
        }

        [Fact]
        public void DisbandedCaravan_IsNotPaid()
        {
            Assert.False(Eligible(wasDisbanding: true));
        }

        [Theory]
        [InlineData(6.99, false)]
        [InlineData(7.0, true)]
        public void WeeklyCooldown(double daysSinceLastClaim, bool expected)
        {
            Assert.Equal(expected, Eligible(daysSinceLastClaim: daysSinceLastClaim));
        }

        [Theory]
        [InlineData(4, false)]
        [InlineData(5, true)]
        public void ThrowawayCaravans_AreNotPaid(int crew, bool expected)
        {
            Assert.Equal(expected, Eligible(crewAtRisk: crew));
        }

        [Fact]
        public void EmptiedRosterAtDestruction_IsNotWhatTheCrewRuleMeasures()
        {
            // The behavior passes the crew recorded when the fatal battle began. A caravan beaten
            // down to 0 men is exactly the case insurance exists for; judged by its roster at the
            // moment of destruction it would be (wrongly) rejected as a "throwaway".
            Assert.False(Eligible(crewAtRisk: 0));
            Assert.True(Eligible(crewAtRisk: 25));
        }
    }
}
