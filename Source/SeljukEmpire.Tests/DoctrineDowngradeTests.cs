using Xunit;
using SeljukEmpire.Tactics;

namespace SeljukEmpire.Tests
{
    public class DoctrineDowngradeTests
    {
        [Fact]
        public void BelowThreshold_DoesNotDowngrade()
        {
            Assert.False(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.39f));
        }

        [Fact]
        public void AtThreshold_DoesNotDowngrade()
        {
            Assert.False(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.40f));
        }

        [Fact]
        public void AboveThreshold_Downgrades()
        {
            Assert.True(TacticalSituationAssessor.ShouldDowngradeToDefensiveDoctrine(0.41f));
        }
    }
}
