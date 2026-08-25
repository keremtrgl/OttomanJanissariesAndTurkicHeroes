using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SeljukEmpire.Settlements
{
    /// <summary>
    /// Manages historical Great Seljuk Empire territorial ownership, kingdom-clan hierarchy, city renaming, village renaming, and prosperity.
    /// Safe runtime initialization: Preserves 100% of Calradia's 3D navigation nodes, siege scenes,
    /// and map meshes without destructive XML overrides.
    /// </summary>
    public class SeljukSettlementBehavior : CampaignBehaviorBase
    {
        private bool _isSettlementOwnershipInitialized;
        private static readonly FieldInfo NameField = typeof(Settlement).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);

        public SeljukSettlementBehavior()
        {
            _isSettlementOwnershipInitialized = false;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_isSettlementOwnershipInitialized", ref _isSettlementOwnershipInitialized);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            InitializeSeljukKingdomHierarchy();

            if (!_isSettlementOwnershipInitialized)
            {
                InitializeSeljukTerritories();
                _isSettlementOwnershipInitialized = true;
            }

            // Renaming is done via reflection (see RenameSettlement) and is not part of the
            // campaign save graph, so - unlike ownership/prosperity above - it must be
            // reasserted every session, not just the first one, or a save/reload cycle
            // shows the settlement's original Native name again.
            ReapplySeljukSettlementNames();
        }

        private void ReapplySeljukSettlementNames()
        {
            try
            {
                // Towns
                RenameSettlement(Settlement.Find("town_ES1"), "{=seljuk_town_danustica}Konya");
                RenameSettlement(Settlement.Find("town_A2"), "{=seljuk_town_husnfulq}Söğüt");
                RenameSettlement(Settlement.Find("town_ES2"), "{=seljuk_town_isfahan}İsfahan");
                RenameSettlement(Settlement.Find("town_A4"), "{=seljuk_town_nisabur}Nişabur");

                // Castles
                RenameSettlement(Settlement.Find("castle_ES4"), "{=seljuk_castle_lavenia}Lavenia Kalesi");
                RenameSettlement(Settlement.Find("castle_A6"), "{=seljuk_castle_shibalzumr}Şibal Zümr Kalesi");
                RenameSettlement(Settlement.Find("castle_ES5"), "{=seljuk_castle_morenia}Morenia Kalesi");
                RenameSettlement(Settlement.Find("castle_A8"), "{=seljuk_castle_rey}Rey Kalesi");

                // Konya villages
                RenameSettlement(Settlement.Find("village_ES1_2"), "{=seljuk_vil_polisia}Meram");
                RenameSettlement(Settlement.Find("village_ES1_3"), "{=seljuk_vil_tegresos}Sille");
                RenameSettlement(Settlement.Find("village_ES1_4"), "{=seljuk_vil_erebulos}Karatay");

                // Söğüt villages
                RenameSettlement(Settlement.Find("village_A2_2"), "{=seljuk_vil_abukhih}Domaniç");
                RenameSettlement(Settlement.Find("village_A2_3"), "{=seljuk_vil_hoqqa}Bozüyük");

                // Castle villages
                RenameSettlement(Settlement.Find("castle_village_ES4_1"), "{=seljuk_vil_lavenia}Lavenia");
                RenameSettlement(Settlement.Find("castle_village_A6_1"), "{=seljuk_vil_shibalzumr}Şibal Zümr");
                RenameSettlement(Settlement.Find("castle_village_ES5_1"), "{=seljuk_vil_morenia}Morenia");

                // İsfahan villages
                RenameSettlement(Settlement.Find("village_ES2_2"), "{=seljuk_vil_cuybare}Cûybâre");
                RenameSettlement(Settlement.Find("village_ES2_3"), "{=seljuk_vil_lenban}Lenban");
                RenameSettlement(Settlement.Find("village_ES2_4"), "{=seljuk_vil_hasanabad}Hasanabad");

                // Nişabur villages
                RenameSettlement(Settlement.Find("village_A4_1"), "{=seljuk_vil_bostanabad}Bostanabad");
                RenameSettlement(Settlement.Find("village_A4_2"), "{=seljuk_vil_sadyah}Şadyah");
                RenameSettlement(Settlement.Find("village_A4_4"), "{=seljuk_vil_kohandiz}Kohandiz");

                // Rey Kalesi villages
                RenameSettlement(Settlement.Find("castle_village_A8_1"), "{=seljuk_vil_cesmedeh}Çeşmedeh");
                RenameSettlement(Settlement.Find("castle_village_A8_2"), "{=seljuk_vil_veramin}Veramin");
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        private void InitializeSeljukKingdomHierarchy()
        {
            try
            {
                Kingdom seljukKingdom = Kingdom.All.Find(k => k.StringId == "kingdom_seljuks");
                Clan royalClan = Clan.FindFirst(c => c.StringId == "clan_seljuk_royal");
                Hero alpArslan = Hero.FindFirst(h => h.StringId == "lord_seljuk_alp_arslan");

                if (seljukKingdom != null)
                {
                    if (alpArslan != null && royalClan != null)
                    {
                        if (royalClan.Leader != alpArslan)
                        {
                            ChangeClanLeaderAction.ApplyWithSelectedNewLeader(royalClan, alpArslan);
                        }

                        if (seljukKingdom.RulingClan != royalClan)
                        {
                            ChangeRulingClanAction.Apply(seljukKingdom, royalClan);
                        }
                    }

                    // Map of clan to leader StringId
                    var clanLeaders = new Dictionary<string, string>
                    {
                        { "clan_seljuk_royal", "lord_seljuk_alp_arslan" },
                        { "clan_nizamiye", "lord_seljuk_nizamulmulk" },
                        { "clan_danismend", "lord_seljuk_danismend_gazi" },
                        { "clan_artuk", "lord_seljuk_artuk_bey" },
                        { "clan_mengucek", "lord_seljuk_mengucek_gazi" },
                        { "clan_saltuk", "lord_seljuk_emir_saltuk" },
                        { "clan_caka", "lord_seljuk_caka_bey" },
                        { "clan_ahlatsah", "lord_seljuk_sokmen_bey" },
                        { "clan_karaman", "lord_seljuk_karaman_bey" },
                        { "clan_kayi_oguz", "ertugrul_gazi" },
                        { "clan_ahi_order", "lord_seljuk_ahi_evran" }
                    };

                    foreach (var pair in clanLeaders)
                    {
                        Clan clan = Clan.FindFirst(c => c.StringId == pair.Key);
                        Hero leader = Hero.FindFirst(h => h.StringId == pair.Value);

                        if (clan != null)
                        {
                            if (leader != null && clan.Leader != leader)
                            {
                                ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, leader);
                            }

                            if (clan.Kingdom != seljukKingdom)
                            {
                                ChangeKingdomAction.ApplyByJoinToKingdom(clan, seljukKingdom, CampaignTime.Never, false);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Kingdom hierarchy safety catch
            }
        }

        private void InitializeSeljukTerritories()
        {
            try
            {
                // =====================================================================
                // 1. TOWNS (ŞEHİRLER)
                // =====================================================================
                SetupTown("town_ES1", "clan_seljuk_royal", "{=seljuk_town_danustica}Konya", 7800f);
                SetupTown("town_A2", "clan_kayi_oguz", "{=seljuk_town_husnfulq}Söğüt", 6600f);
                SetupTown("town_ES2", "clan_seljuk_royal", "{=seljuk_town_isfahan}İsfahan", 7200f);
                SetupTown("town_A4", "clan_seljuk_royal", "{=seljuk_town_nisabur}Nişabur", 6800f);

                // =====================================================================
                // 2. CASTLES (KALELER)
                // =====================================================================
                SetupCastle("castle_ES4", "clan_danismend", "{=seljuk_castle_lavenia}Lavenia Kalesi");
                SetupCastle("castle_A6", "clan_artuk", "{=seljuk_castle_shibalzumr}Şibal Zümr Kalesi");
                SetupCastle("castle_ES5", "clan_ahlatsah", "{=seljuk_castle_morenia}Morenia Kalesi");
                SetupCastle("castle_A8", "clan_seljuk_royal", "{=seljuk_castle_rey}Rey Kalesi");

                // =====================================================================
                // 3. VILLAGES (KÖYLER)
                // =====================================================================
                // Konya Villages
                SetupVillage("village_ES1_2", "{=seljuk_vil_polisia}Meram");
                SetupVillage("village_ES1_3", "{=seljuk_vil_tegresos}Sille");
                SetupVillage("village_ES1_4", "{=seljuk_vil_erebulos}Karatay");

                // Söğüt Villages
                SetupVillage("village_A2_2", "{=seljuk_vil_abukhih}Domaniç");
                SetupVillage("village_A2_3", "{=seljuk_vil_hoqqa}Bozüyük");

                // Castle Villages
                SetupVillage("castle_village_ES4_1", "{=seljuk_vil_lavenia}Lavenia");
                SetupVillage("castle_village_A6_1", "{=seljuk_vil_shibalzumr}Şibal Zümr");
                SetupVillage("castle_village_ES5_1", "{=seljuk_vil_morenia}Morenia");

                // İsfahan Villages
                SetupVillage("village_ES2_2", "{=seljuk_vil_cuybare}Cûybâre");
                SetupVillage("village_ES2_3", "{=seljuk_vil_lenban}Lenban");
                SetupVillage("village_ES2_4", "{=seljuk_vil_hasanabad}Hasanabad");

                // Nişabur Villages
                SetupVillage("village_A4_1", "{=seljuk_vil_bostanabad}Bostanabad");
                SetupVillage("village_A4_2", "{=seljuk_vil_sadyah}Şadyah");
                SetupVillage("village_A4_4", "{=seljuk_vil_kohandiz}Kohandiz");

                // Rey Kalesi Villages
                SetupVillage("castle_village_A8_1", "{=seljuk_vil_cesmedeh}Çeşmedeh");
                SetupVillage("castle_village_A8_2", "{=seljuk_vil_veramin}Veramin");
            }
            catch (Exception)
            {
                // Engine safety catch
            }
        }

        private void SetupTown(string settlementId, string clanId, string newNameTextKey, float prosperity)
        {
            try
            {
                Settlement settlement = Settlement.Find(settlementId);
                Clan clan = Clan.FindFirst(c => c.StringId == clanId);

                if (settlement != null && clan != null)
                {
                    Hero owner = clan.Leader ?? clan.Heroes.Find(h => h.IsAlive);
                    if (owner != null && settlement.OwnerClan != clan)
                    {
                        ChangeOwnerOfSettlementAction.ApplyByDefault(owner, settlement);
                    }

                    if (settlement.Town != null)
                    {
                        settlement.Town.Prosperity = prosperity;
                        settlement.Town.Security = 85f;
                        settlement.Town.Loyalty = 90f;
                    }

                    RenameSettlement(settlement, newNameTextKey);
                }
            }
            catch (Exception) { }
        }

        private void SetupCastle(string settlementId, string clanId, string newNameTextKey)
        {
            try
            {
                Settlement settlement = Settlement.Find(settlementId);
                Clan clan = Clan.FindFirst(c => c.StringId == clanId);

                if (settlement != null && clan != null)
                {
                    Hero owner = clan.Leader ?? clan.Heroes.Find(h => h.IsAlive);
                    if (owner != null && settlement.OwnerClan != clan)
                    {
                        ChangeOwnerOfSettlementAction.ApplyByDefault(owner, settlement);
                    }

                    if (settlement.Town != null)
                    {
                        settlement.Town.Prosperity = 4500f;
                        settlement.Town.Security = 95f;
                        settlement.Town.Loyalty = 95f;
                    }

                    RenameSettlement(settlement, newNameTextKey);
                }
            }
            catch (Exception) { }
        }

        private void SetupVillage(string villageId, string newNameTextKey)
        {
            try
            {
                Settlement village = Settlement.Find(villageId);
                if (village != null && village.IsVillage)
                {
                    if (village.Village != null)
                    {
                        village.Village.Hearth = 650f;
                    }
                    RenameSettlement(village, newNameTextKey);
                }
            }
            catch (Exception) { }
        }

        private void RenameSettlement(Settlement settlement, string textKey)
        {
            if (settlement == null || string.IsNullOrEmpty(textKey) || NameField == null) return;
            try
            {
                TextObject localizedName = new TextObject(textKey);
                NameField.SetValue(settlement, localizedName);
            }
            catch (Exception) { }
        }
    }
}
