using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SeljukEmpire.Recruitment;
using Xunit;

namespace SeljukEmpire.Tests
{
    public class VolunteerSlotPolicyTests
    {
        // --- Which slots get replaced ----------------------------------------------------------

        [Fact]
        public void EmptySlot_IsNeverRefilled_Seljuk()
        {
            // An empty slot is a volunteer the player (or an AI lord) already recruited. Refilling
            // it here made every Seljuk settlement's roster infinite: recruit, leave, re-enter, repeat.
            Assert.False(VolunteerSlotPolicy.ShouldReplaceSeljukVolunteer(null));
        }

        [Fact]
        public void EmptySlot_IsNeverRefilled_Latin()
        {
            Assert.False(VolunteerSlotPolicy.ShouldReplaceLatinVolunteer(null));
        }

        [Theory]
        [InlineData("byz2_recruit")]
        [InlineData("imperial_recruit")]
        [InlineData("aserai_recruit")]
        public void ForeignTroop_IsReplaced_Seljuk(string troopId)
        {
            Assert.True(VolunteerSlotPolicy.ShouldReplaceSeljukVolunteer(troopId));
        }

        [Theory]
        [InlineData("seljuk_peasant")]
        [InlineData("seljuk_ghulam_recruit")]
        [InlineData("acemi_janissary")]
        [InlineData("azap_recruit")]
        [InlineData("ottoman_scout")]
        public void OwnTreeTroop_IsKept_Seljuk(string troopId)
        {
            // Includes Native-upgraded volunteers: an upgraded seljuk_ troop must not be knocked
            // back down to a tier-1 recruit.
            Assert.False(VolunteerSlotPolicy.ShouldReplaceSeljukVolunteer(troopId));
        }

        [Theory]
        [InlineData("byz2_recruit", true)]
        [InlineData("lat2_recruit", false)]
        [InlineData("lat2_squire", false)]
        public void LatinReplacementRule(string troopId, bool expected)
        {
            Assert.Equal(expected, VolunteerSlotPolicy.ShouldReplaceLatinVolunteer(troopId));
        }

        // --- Which troop a slot gets --------------------------------------------------------------

        [Fact]
        public void SeljukVillage_LowSlots_GetPeasants()
        {
            for (int slot = 0; slot < VolunteerSlotPolicy.FirstHighTierSlot; slot++)
            {
                Assert.Equal(VolunteerSlotPolicy.SeljukPeasant,
                    VolunteerSlotPolicy.GetSeljukCandidates(isVillage: true, isTownOrCastle: false, slot, notablePower: 500f, isArtisan: false)[0]);
            }
        }

        [Theory]
        [InlineData(149f, VolunteerSlotPolicy.SeljukPeasant)]
        [InlineData(150f, VolunteerSlotPolicy.SeljukGhulamRecruit)]
        public void SeljukVillage_HighSlots_DependOnNotablePower(float power, string expectedFirstChoice)
        {
            Assert.Equal(expectedFirstChoice,
                VolunteerSlotPolicy.GetSeljukCandidates(isVillage: true, isTownOrCastle: false, slotIndex: 4, power, isArtisan: false)[0]);
        }

        [Theory]
        [InlineData(0, false, VolunteerSlotPolicy.SeljukPeasant)]
        [InlineData(1, false, VolunteerSlotPolicy.AzapRecruit)]
        [InlineData(2, false, VolunteerSlotPolicy.SeljukPeasant)]
        [InlineData(3, false, VolunteerSlotPolicy.OttomanScout)]
        [InlineData(4, false, VolunteerSlotPolicy.SeljukGhulamRecruit)]
        [InlineData(5, true, VolunteerSlotPolicy.AcemiJanissary)]
        public void SeljukTown_SlotLayout(int slot, bool isArtisan, string expectedFirstChoice)
        {
            Assert.Equal(expectedFirstChoice,
                VolunteerSlotPolicy.GetSeljukCandidates(isVillage: false, isTownOrCastle: true, slot, notablePower: 0f, isArtisan)[0]);
        }

        [Theory]
        [InlineData(0, false, VolunteerSlotPolicy.LatinRecruit)]
        [InlineData(3, false, VolunteerSlotPolicy.LatinCrossbowman)]
        [InlineData(4, false, VolunteerSlotPolicy.LatinSquire)]
        [InlineData(4, true, VolunteerSlotPolicy.LatinCrossbowman)]
        public void LatinTown_SlotLayout(int slot, bool isArtisan, string expectedFirstChoice)
        {
            Assert.Equal(expectedFirstChoice,
                VolunteerSlotPolicy.GetLatinCandidates(isVillage: false, isTownOrCastle: true, slot, notablePower: 0f, isArtisan)[0]);
        }

        [Fact]
        public void NonVillageNonTown_IsLeftAlone()
        {
            Assert.Null(VolunteerSlotPolicy.GetSeljukCandidates(false, false, 0, 0f, false));
            Assert.Null(VolunteerSlotPolicy.GetLatinCandidates(false, false, 0, 0f, false));
        }

        [Fact]
        public void EveryChain_EndsInTheTier1Recruit_SoASlotIsNeverLeftForeign()
        {
            // If a better troop is ever missing from the XML, the slot must still degrade to the
            // tree's own tier-1 recruit rather than stay with another kingdom's troop.
            for (int slot = 0; slot < 6; slot++)
            {
                foreach (bool artisan in new[] { false, true })
                {
                    foreach (float power in new[] { 0f, 500f })
                    {
                        foreach (bool village in new[] { false, true })
                        {
                            Assert.Equal(VolunteerSlotPolicy.SeljukPeasant,
                                VolunteerSlotPolicy.GetSeljukCandidates(village, !village, slot, power, artisan).Last());
                            Assert.Equal(VolunteerSlotPolicy.LatinRecruit,
                                VolunteerSlotPolicy.GetLatinCandidates(village, !village, slot, power, artisan).Last());
                        }
                    }
                }
            }
        }

        [Fact]
        public void EveryTroopIdThePolicyUses_IsDefinedInModuleData()
        {
            // A typo'd id here has no error in-game: that slot just silently falls through to the
            // next candidate. Cross-check every id constant against the mod's own NPCCharacters.
            var definedIds = Directory.GetFiles(Path.Combine(FindRepoRoot(), "ModuleData"), "*.xml")
                .SelectMany(file => XDocument.Load(file).Descendants("NPCCharacter"))
                .Select(element => (string)element.Attribute("id"))
                .Where(id => id != null)
                .ToHashSet();

            var policyTroopIds = typeof(VolunteerSlotPolicy)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue())
                .ToList();

            Assert.NotEmpty(policyTroopIds);
            Assert.All(policyTroopIds, id => Assert.Contains(id, definedIds));
        }

        private static string FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "SubModule.xml")))
                {
                    return dir.FullName;
                }
            }
            throw new InvalidOperationException("Could not locate the repository root (SubModule.xml) above " + AppContext.BaseDirectory);
        }
    }
}
