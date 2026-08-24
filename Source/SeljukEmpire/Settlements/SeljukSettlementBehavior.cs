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
                SetupTown("town_K1", "clan_seljuk_royal", "{=seljuk_town_rey}Rey-i Saltanat", 8200f);
                SetupTown("town_K4", "clan_danismend", "{=seljuk_town_kayseri}Kayseriyye", 7100f);
                SetupTown("town_ES4", "clan_artuk", "{=seljuk_town_diyarbekir}Diyarbekir", 6800f);
                SetupTown("town_ES2", "clan_mengucek", "{=seljuk_town_divrigi}Divriği", 6200f);
                SetupTown("town_K6", "clan_caka", "{=seljuk_town_alaiye}Alâiye", 7400f);
                SetupTown("town_A4", "clan_karaman", "{=seljuk_town_larenede}Lârende", 6900f);

                // =====================================================================
                // 2. CASTLES (KALELER)
                // =====================================================================
                SetupCastle("castle_K2", "clan_seljuk_royal", "{=seljuk_castle_alamut}Deylem Hisarı");
                SetupCastle("castle_K5", "clan_danismend", "{=seljuk_castle_niksar}Niksar Kalesi");
                SetupCastle("castle_K1", "clan_artuk", "{=seljuk_castle_hasankeyf}Hasankeyf Kalesi");
                SetupCastle("castle_A8", "clan_saltuk", "{=seljuk_castle_erzurum}Erzurum Hisarı");
                SetupCastle("castle_ES3", "clan_kayi_oguz", "{=seljuk_castle_sogut}Söğüt Uç Kalesi");

                // =====================================================================
                // 3. VILLAGES (KÖYLER)
                // =====================================================================
                // Rey Villages
                SetupVillage("village_K1_1", "{=seljuk_vil_damgan}Damğan");
                SetupVillage("village_K1_2", "{=seljuk_vil_varamin}Varamin");
                SetupVillage("village_K1_3", "{=seljuk_vil_harakani}Harakani");

                // Kayseriyye Villages
                SetupVillage("village_K4_1", "{=seljuk_vil_talas}Talas");
                SetupVillage("village_K4_2", "{=seljuk_vil_develi}Develi");
                SetupVillage("village_K4_3", "{=seljuk_vil_incesu}İncesu");

                // Diyarbekir Villages
                SetupVillage("village_ES4_1", "{=seljuk_vil_silvan}Meyyafarikin");
                SetupVillage("village_ES4_2", "{=seljuk_vil_ergani}Ergani");
                SetupVillage("village_ES4_3", "{=seljuk_vil_cermik}Çermik");

                // Divriği Villages
                SetupVillage("village_ES2_1", "{=seljuk_vil_kemah}Kemah");
                SetupVillage("village_ES2_2", "{=seljuk_vil_ilisik}İliç");
                SetupVillage("village_ES2_3", "{=seljuk_vil_arapgir}Arapgir");

                // Alâiye Villages
                SetupVillage("village_K6_1", "{=seljuk_vil_anamur}Anamur");
                SetupVillage("village_K6_2", "{=seljuk_vil_gazipasa}Selinti");
                SetupVillage("village_K6_3", "{=seljuk_vil_manavgat}Manavgat");

                // Lârende Villages
                SetupVillage("village_A4_1", "{=seljuk_vil_mut}Mut");
                SetupVillage("village_A4_2", "{=seljuk_vil_ermenek}Ermenek");
                SetupVillage("village_A4_3", "{=seljuk_vil_eregli}Ereğli");

                // Castle Villages
                SetupVillage("castle_village_K2_1", "{=seljuk_vil_rudbar}Rudbar");
                SetupVillage("castle_village_K5_1", "{=seljuk_vil_erbaa}Erbaa");
                SetupVillage("castle_village_K1_1", "{=seljuk_vil_cizre}Cizre");
                SetupVillage("castle_village_A8_1", "{=seljuk_vil_tortum}Tortum");
                SetupVillage("castle_village_ES3_1", "{=seljuk_vil_domanic}Domaniç");
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
