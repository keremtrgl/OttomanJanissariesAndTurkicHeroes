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

                // Byzantine, Abbasid and Georgian settlements/villages are declared purely via
                // XML overrides (byzantine_settlements.xml / abbasid_settlements.xml /
                // georgian_settlements.xml) with no equivalent C# reinforcement. Settlement.Name
                // is not part of the campaign save graph either way, so reasserting these too,
                // every session, closes the same gap for them rather than leaving them dependent
                // on the XML override alone.

                // --- Byzantine (35 settlements/villages) ---
                RenameSettlement(Settlement.Find("town_ES4"), "{=byz_s_ankara}Ankara");
                RenameSettlement(Settlement.Find("village_ES4_1"), "{=byz_v_juliopolis}Iuliopolis");
                RenameSettlement(Settlement.Find("village_ES4_3"), "{=byz_v_germa}Germa");
                RenameSettlement(Settlement.Find("town_ES5"), "{=byz_s_amaseia}Amaseia");
                RenameSettlement(Settlement.Find("village_ES5_1"), "{=byz_v_andrapa}Andrapa");
                RenameSettlement(Settlement.Find("village_ES5_2"), "{=byz_v_ibora}Ibora");
                RenameSettlement(Settlement.Find("village_ES5_3"), "{=byz_v_sebastopolis}Sebastopolis");
                RenameSettlement(Settlement.Find("town_ES3"), "{=byz_s_nicaea}Nicaea");
                RenameSettlement(Settlement.Find("village_ES3_1"), "{=byz_v_prusa}Prusa");
                RenameSettlement(Settlement.Find("village_ES3_2"), "{=byz_v_apollonia}Apollonia");
                RenameSettlement(Settlement.Find("village_ES3_3"), "{=byz_v_lopadion}Lopadion");
                RenameSettlement(Settlement.Find("town_ES6"), "{=byz_s_sebasteia}Sebasteia");
                RenameSettlement(Settlement.Find("village_ES6_1"), "{=byz_v_nicopolis}Nicopolis");
                RenameSettlement(Settlement.Find("village_ES6_2"), "{=byz_v_koloneia}Koloneia");
                RenameSettlement(Settlement.Find("castle_village_ES6_2"), "{=byz_v_amycon2}Dazimon");
                RenameSettlement(Settlement.Find("town_ES7"), "{=byz_s_trebizond}Trebizond");
                RenameSettlement(Settlement.Find("village_ES7_1"), "{=byz_v_rhizaion}Rhizaion");
                RenameSettlement(Settlement.Find("village_ES7_2"), "{=byz_v_kerasous}Kerasous");
                RenameSettlement(Settlement.Find("castle_village_ES7_2"), "{=byz_v_eunalica2}Susurmena");
                RenameSettlement(Settlement.Find("castle_ES1"), "{=byz_c_dorylaeum}Dorylaeum Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES1_1"), "{=byz_v_nakoleia}Nakoleia");
                RenameSettlement(Settlement.Find("castle_village_ES1_2"), "{=byz_v_midaion}Midaion");
                RenameSettlement(Settlement.Find("castle_ES2"), "{=byz_c_nicomedia}Nicomedia Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES2_1"), "{=byz_v_chalcedon}Chalcedon");
                RenameSettlement(Settlement.Find("castle_village_ES2_2"), "{=byz_v_prainetos}Prainetos");
                RenameSettlement(Settlement.Find("castle_ES3"), "{=byz_c_chonae}Chonae Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES3_1"), "{=byz_v_laodicea}Laodicea");
                RenameSettlement(Settlement.Find("castle_village_ES3_2"), "{=byz_v_colossae}Colossae");
                RenameSettlement(Settlement.Find("castle_ES6"), "{=byz_c_claudiopolis}Claudiopolis Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES6_1"), "{=byz_v_gangra}Gangra");
                RenameSettlement(Settlement.Find("castle_ES7"), "{=byz_c_neocaesarea}Neocaesarea Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES7_1"), "{=byz_v_comana}Comana");
                RenameSettlement(Settlement.Find("castle_ES8"), "{=byz_c_caesarea}Caesarea Kalesi");
                RenameSettlement(Settlement.Find("castle_village_ES8_1"), "{=byz_v_tyana}Tyana");
                RenameSettlement(Settlement.Find("castle_village_ES8_2"), "{=byz_v_nazianzus}Nazianzus");

                // --- Abbasid (44 settlements/villages) ---
                RenameSettlement(Settlement.Find("town_A1"), "{=abb_s_baghdad}Bağdat");
                RenameSettlement(Settlement.Find("village_A1_1"), "{=abb_v_qutrabbul}Kutrabbul");
                RenameSettlement(Settlement.Find("village_A1_2"), "{=abb_v_babalsham}Bâbüşşâm");
                RenameSettlement(Settlement.Find("village_A1_4"), "{=abb_v_awana}Avâna");
                RenameSettlement(Settlement.Find("town_A3"), "{=abb_s_basra}Basra");
                RenameSettlement(Settlement.Find("village_A3_1"), "{=abb_v_ubulla}Übülle");
                RenameSettlement(Settlement.Find("village_A3_3"), "{=abb_v_abbadan}Abbâdân");
                RenameSettlement(Settlement.Find("town_A5"), "{=abb_s_kufa}Küfe");
                RenameSettlement(Settlement.Find("village_A5_1"), "{=abb_v_hira}Hîre");
                RenameSettlement(Settlement.Find("village_A5_2"), "{=abb_v_najaf}Necef");
                RenameSettlement(Settlement.Find("village_A5_3"), "{=abb_v_qadisiyyah}Kâdisiye");
                RenameSettlement(Settlement.Find("town_A6"), "{=abb_s_mosul}Musul");
                RenameSettlement(Settlement.Find("village_A6_1"), "{=abb_v_sinjar}Sincar");
                RenameSettlement(Settlement.Find("village_A6_2"), "{=abb_v_balad}Balad");
                RenameSettlement(Settlement.Find("village_A6_3"), "{=abb_v_tellafar}Tell Afer");
                RenameSettlement(Settlement.Find("village_A6_4"), "{=abb_v_nineveh}Ninova");
                RenameSettlement(Settlement.Find("town_A7"), "{=abb_s_wasit}Vâsıt");
                RenameSettlement(Settlement.Find("village_A7_2"), "{=abb_v_numaniyya}Nu'mâniye");
                RenameSettlement(Settlement.Find("village_A7_3"), "{=abb_v_jarjaraya}Cercerâyâ");
                RenameSettlement(Settlement.Find("village_A7_4"), "{=abb_v_dayralaqul}Deyrü'l-Akûl");
                RenameSettlement(Settlement.Find("town_A8"), "{=abb_s_samarra}Sâmerrâ");
                RenameSettlement(Settlement.Find("village_A8_1"), "{=abb_v_daquqa}Dakûka");
                RenameSettlement(Settlement.Find("village_A8_2"), "{=abb_v_harba}Harbâ");
                RenameSettlement(Settlement.Find("castle_A1"), "{=abb_c_anbar}Enbâr Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A1_1"), "{=abb_v_falluja}Fellûce");
                RenameSettlement(Settlement.Find("castle_village_A1_2"), "{=abb_v_sura}Sûrâ");
                RenameSettlement(Settlement.Find("castle_A2"), "{=abb_c_hit}Hît Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A2_1"), "{=abb_v_hadithah}Hadîse");
                RenameSettlement(Settlement.Find("castle_village_A2_2"), "{=abb_v_alus}Âlûs");
                RenameSettlement(Settlement.Find("castle_A3"), "{=abb_c_tikrit}Tikrît Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A3_1"), "{=abb_v_dur}Ed-Devr");
                RenameSettlement(Settlement.Find("castle_village_A3_2"), "{=abb_v_sinn}Sinn Barîmmâ");
                RenameSettlement(Settlement.Find("castle_A4"), "{=abb_c_rahba}Rahbe Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A4_1"), "{=abb_v_raqqa}Rakka");
                RenameSettlement(Settlement.Find("castle_village_A4_2"), "{=abb_v_qarqisiya}Karkîsiyâ");
                RenameSettlement(Settlement.Find("castle_A5"), "{=abb_c_ana}Âne Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A5_1"), "{=abb_v_rawa}Râve");
                RenameSettlement(Settlement.Find("castle_village_A5_2"), "{=abb_v_baqubah}Bakuba");
                RenameSettlement(Settlement.Find("castle_A7"), "{=abb_c_nahrawan}Nehrevân Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A7_1"), "{=abb_v_jalula}Celûlâ");
                RenameSettlement(Settlement.Find("castle_village_A7_2"), "{=abb_v_khaniqin}Hanikin");
                RenameSettlement(Settlement.Find("castle_A9"), "{=abb_c_ukbara}Ukbera Kalesi");
                RenameSettlement(Settlement.Find("castle_village_A9_1"), "{=abb_v_dujayl}Dûceyl");
                RenameSettlement(Settlement.Find("castle_village_A9_2"), "{=abb_v_maskin}Maskin");

                // --- Georgian (47 settlements/villages) ---
                RenameSettlement(Settlement.Find("town_S1"), "{=geo_s_kutaisi}Kutaisi");
                RenameSettlement(Settlement.Find("village_S1_1"), "{=geo_v_nokalakevi}Nokalakevi");
                RenameSettlement(Settlement.Find("village_S1_3"), "{=geo_v_vardtsikhe}Vardtsihe");
                RenameSettlement(Settlement.Find("town_S2"), "{=geo_s_tbilisi}Tiflis");
                RenameSettlement(Settlement.Find("village_S2_1"), "{=geo_v_mtskheta}Mtsheta");
                RenameSettlement(Settlement.Find("village_S2_2"), "{=geo_v_rustavi}Rustavi");
                RenameSettlement(Settlement.Find("town_S3"), "{=geo_s_kldekari}Kldekari");
                RenameSettlement(Settlement.Find("village_S3_1"), "{=geo_v_trialeti}Trialeti");
                RenameSettlement(Settlement.Find("village_S3_2"), "{=geo_v_manglisi}Manglisi");
                RenameSettlement(Settlement.Find("town_S4"), "{=geo_s_telavi}Telavi");
                RenameSettlement(Settlement.Find("village_S4_1"), "{=geo_v_ikalto}İkalto");
                RenameSettlement(Settlement.Find("village_S4_3"), "{=geo_v_alaverdi}Alaverdi");
                RenameSettlement(Settlement.Find("village_S4_4"), "{=geo_v_nekresi}Nekresi");
                RenameSettlement(Settlement.Find("town_S5"), "{=geo_s_lore}Lore");
                RenameSettlement(Settlement.Find("village_S5_1"), "{=geo_v_kaladzori}Kaladzori");
                RenameSettlement(Settlement.Find("village_S5_2"), "{=geo_v_tashir}Taşir");
                RenameSettlement(Settlement.Find("town_S6"), "{=geo_s_chqondidi}Çkondidi");
                RenameSettlement(Settlement.Find("village_S6_1"), "{=geo_v_anakopia}Anakopya");
                RenameSettlement(Settlement.Find("village_S6_2"), "{=geo_v_bichvinta}Bıçvinta");
                RenameSettlement(Settlement.Find("village_S6_3"), "{=geo_v_sokhumi}Sohumi");
                RenameSettlement(Settlement.Find("town_S7"), "{=geo_s_khornabuji}Hornabuci");
                RenameSettlement(Settlement.Find("village_S7_1"), "{=geo_v_bodbe}Bodbe");
                RenameSettlement(Settlement.Find("village_S7_2"), "{=geo_v_vejini}Vejini");
                RenameSettlement(Settlement.Find("castle_S1"), "{=geo_c_gori}Gori Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S1_1"), "{=geo_v_ateni}Ateni");
                RenameSettlement(Settlement.Find("castle_village_S1_2"), "{=geo_v_uplistsikhe}Uplistsihe");
                RenameSettlement(Settlement.Find("castle_S2"), "{=geo_c_dmanisi}Dmanisi Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S2_1"), "{=geo_v_bolnisi}Bolnisi");
                RenameSettlement(Settlement.Find("castle_village_S2_2"), "{=geo_v_tsalka}Tsalka");
                RenameSettlement(Settlement.Find("castle_S3"), "{=geo_c_samshvilde}Samşvilde Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S3_1"), "{=geo_v_orbeti}Orbeti");
                RenameSettlement(Settlement.Find("castle_village_S3_2"), "{=geo_v_tsintskaro}Tsintskaro");
                RenameSettlement(Settlement.Find("castle_S4"), "{=geo_c_zedazeni}Zedazeni Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S4_1"), "{=geo_v_mukhrani}Muhrani");
                RenameSettlement(Settlement.Find("castle_village_S4_2"), "{=geo_v_tsromi}Tsromi");
                RenameSettlement(Settlement.Find("castle_S5"), "{=geo_c_manavi}Manavi Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S5_1"), "{=geo_v_kvareli}Kvareli");
                RenameSettlement(Settlement.Find("castle_village_S5_2"), "{=geo_v_akhmeta}Ahmeta");
                RenameSettlement(Settlement.Find("castle_S6"), "{=geo_c_kojori}Kojori Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S6_1"), "{=geo_v_betania}Betania");
                RenameSettlement(Settlement.Find("castle_village_S6_2"), "{=geo_v_tabakhmela}Tabahmela");
                RenameSettlement(Settlement.Find("castle_S7"), "{=geo_c_artanuji}Artanuci Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S7_1"), "{=geo_v_oltisi}Oltisi");
                RenameSettlement(Settlement.Find("castle_village_S7_2"), "{=geo_v_shavsheti}Şavşeti");
                RenameSettlement(Settlement.Find("castle_S8"), "{=geo_c_tmogvi}Tmogvi Kalesi");
                RenameSettlement(Settlement.Find("castle_village_S8_1"), "{=geo_v_akhaltsikhe}Ahaltsihe");
                RenameSettlement(Settlement.Find("castle_village_S8_2"), "{=geo_v_khertvisi}Hertvisi");
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
