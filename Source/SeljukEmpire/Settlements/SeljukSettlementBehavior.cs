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
            if (!_isSettlementOwnershipInitialized)
            {
                // Was called unconditionally on every session launch (not just the first), with
                // no IsAlive check on the hardcoded leader ids - unlike SetupTown's own
                // `clan.Leader ?? clan.Heroes.Find(h => h.IsAlive)` fallback used elsewhere in
                // this same file. On a long campaign a named lord (e.g. Ertugrul Gazi) can die,
                // the game hands leadership to a successor, and the player reloads - at which
                // point this was forcing the dead hero back onto clan.Leader and unconditionally
                // pulling every clan back into kingdom_seljuks even if it had legitimately left.
                // Gated the same one-time-only way as InitializeSeljukTerritories() just below:
                // establish the authored hierarchy once at campaign start, then let Native's own
                // succession/diplomacy systems govern it from there.
                InitializeSeljukKingdomHierarchy();
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
                RenameSettlement(Settlement.Find("town_A2"), "{=seljuk_town_husnfulq}Sogut");
                RenameSettlement(Settlement.Find("town_ES2"), "{=seljuk_town_isfahan}Isfahan");
                RenameSettlement(Settlement.Find("town_A4"), "{=seljuk_town_nisabur}Nishapur");

                // Castles
                RenameSettlement(Settlement.Find("castle_ES4"), "{=seljuk_castle_lavenia}Lavenia Castle");
                RenameSettlement(Settlement.Find("castle_A6"), "{=seljuk_castle_shibalzumr}Shibal Zumr Castle");
                RenameSettlement(Settlement.Find("castle_ES5"), "{=seljuk_castle_morenia}Morenia Castle");
                RenameSettlement(Settlement.Find("castle_A8"), "{=seljuk_castle_rey}Rey Castle");

                // Konya villages
                RenameSettlement(Settlement.Find("village_ES1_2"), "{=seljuk_vil_polisia}Meram");
                RenameSettlement(Settlement.Find("village_ES1_3"), "{=seljuk_vil_tegresos}Sille");
                RenameSettlement(Settlement.Find("village_ES1_4"), "{=seljuk_vil_erebulos}Karatay");

                // Söğüt villages
                RenameSettlement(Settlement.Find("village_A2_2"), "{=seljuk_vil_abukhih}Domanic");
                RenameSettlement(Settlement.Find("village_A2_3"), "{=seljuk_vil_hoqqa}Bozuyuk");

                // Castle villages
                RenameSettlement(Settlement.Find("castle_village_ES4_1"), "{=seljuk_vil_lavenia}Lavenia");
                RenameSettlement(Settlement.Find("castle_village_ES4_2"), "{=seljuk_v_niksar}Niksar");
                RenameSettlement(Settlement.Find("castle_village_A6_1"), "{=seljuk_vil_shibalzumr}Shibal Zumr");
                RenameSettlement(Settlement.Find("castle_village_ES5_1"), "{=seljuk_vil_morenia}Morenia");
                RenameSettlement(Settlement.Find("castle_village_ES5_2"), "{=seljuk_v_adilcevaz}Adilcevaz");

                // İsfahan villages
                RenameSettlement(Settlement.Find("village_ES2_2"), "{=seljuk_vil_cuybare}Juybareh");
                RenameSettlement(Settlement.Find("village_ES2_3"), "{=seljuk_vil_lenban}Lenban");
                RenameSettlement(Settlement.Find("village_ES2_4"), "{=seljuk_vil_hasanabad}Hasanabad");

                // Nişabur villages
                RenameSettlement(Settlement.Find("village_A4_1"), "{=seljuk_vil_bostanabad}Bostanabad");
                RenameSettlement(Settlement.Find("village_A4_2"), "{=seljuk_vil_sadyah}Shadyakh");
                RenameSettlement(Settlement.Find("village_A4_4"), "{=seljuk_vil_kohandiz}Kohandezh");

                // Rey Kalesi villages
                RenameSettlement(Settlement.Find("castle_village_A8_1"), "{=seljuk_vil_cesmedeh}Cheshmedeh");
                RenameSettlement(Settlement.Find("castle_village_A8_2"), "{=seljuk_vil_veramin}Varamin");

                // Byzantine, Abbasid and Georgian settlements/villages are declared purely via
                // XML overrides (byzantine_settlements.xml / abbasid_settlements.xml /
                // georgian_settlements.xml) with no equivalent C# reinforcement. Settlement.Name
                // is not part of the campaign save graph either way, so reasserting these too,
                // every session, closes the same gap for them rather than leaving them dependent
                // on the XML override alone.

                // --- Byzantine (28 settlements/villages, matches byzantine_settlements.xml) ---
                RenameSettlement(Settlement.Find("town_ES4"), "{=byz_s_ankara}Ancyra");
                RenameSettlement(Settlement.Find("village_ES4_1"), "{=byz_v_juliopolis}Iuliopolis");
                RenameSettlement(Settlement.Find("village_ES4_3"), "{=byz_v_germa}Germa");
                // town_ES5 + villages are Seljuk-owned (settlements.xml), not Byzantine - reasserting
                // the correct Seljuk name here instead of the Byzantine one it used to silently revert to.
                RenameSettlement(Settlement.Find("town_ES5"), "{=seljuk_town_amasya}Amasya (Amaseia)");
                RenameSettlement(Settlement.Find("village_ES5_1"), "{=seljuk_vil_merzifon}Merzifon");
                RenameSettlement(Settlement.Find("village_ES5_2"), "{=seljuk_vil_tasova}Tasova");
                RenameSettlement(Settlement.Find("village_ES5_3"), "{=seljuk_vil_gumushacikoy}Gumushacikoy");
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
                // castle_ES1 + villages are Seljuk-owned (settlements.xml, Kayı Boyu) - same fix as
                // town_ES5 above, reasserting the correct Seljuk name instead of the Byzantine one.
                RenameSettlement(Settlement.Find("castle_ES1"), "{=seljuk_castle_dorylaeum}Eskisehir (Dorylaeum)");
                RenameSettlement(Settlement.Find("castle_village_ES1_1"), "{=seljuk_vil_sivrihisar}Sivrihisar");
                RenameSettlement(Settlement.Find("castle_village_ES1_2"), "{=seljuk_vil_mihaliccik}Mihaliccik");
                RenameSettlement(Settlement.Find("castle_ES2"), "{=byz_c_nicomedia}Nicomedia Castle");
                RenameSettlement(Settlement.Find("castle_village_ES2_1"), "{=byz_v_chalcedon}Chalcedon");
                RenameSettlement(Settlement.Find("castle_village_ES2_2"), "{=byz_v_prainetos}Prainetos");
                RenameSettlement(Settlement.Find("castle_ES3"), "{=byz_c_chonae}Chonae Castle");
                RenameSettlement(Settlement.Find("castle_village_ES3_1"), "{=byz_v_laodicea}Laodicea");
                RenameSettlement(Settlement.Find("castle_village_ES3_2"), "{=byz_v_colossae}Colossae");
                RenameSettlement(Settlement.Find("castle_ES6"), "{=byz_c_claudiopolis}Claudiopolis Castle");
                RenameSettlement(Settlement.Find("castle_village_ES6_1"), "{=byz_v_gangra}Gangra");
                RenameSettlement(Settlement.Find("castle_ES7"), "{=byz_c_neocaesarea}Neocaesarea Castle");
                RenameSettlement(Settlement.Find("castle_village_ES7_1"), "{=byz_v_comana}Comana");
                RenameSettlement(Settlement.Find("castle_ES8"), "{=byz_c_caesarea}Caesarea Castle");
                RenameSettlement(Settlement.Find("castle_village_ES8_1"), "{=byz_v_tyana}Tyana");
                RenameSettlement(Settlement.Find("castle_village_ES8_2"), "{=byz_v_nazianzus}Nazianzus");

                // --- Abbasid (44 settlements/villages) ---
                RenameSettlement(Settlement.Find("town_A1"), "{=abb_s_baghdad}Baghdad");
                RenameSettlement(Settlement.Find("village_A1_1"), "{=abb_v_qutrabbul}Qutrabbul");
                RenameSettlement(Settlement.Find("village_A1_2"), "{=abb_v_babalsham}Bab al-Sham");
                RenameSettlement(Settlement.Find("village_A1_4"), "{=abb_v_awana}Awana");
                RenameSettlement(Settlement.Find("town_A3"), "{=abb_s_basra}Basra");
                RenameSettlement(Settlement.Find("village_A3_1"), "{=abb_v_ubulla}Ubulla");
                RenameSettlement(Settlement.Find("village_A3_3"), "{=abb_v_abbadan}Abbadan");
                RenameSettlement(Settlement.Find("town_A5"), "{=abb_s_kufa}Kufa");
                RenameSettlement(Settlement.Find("village_A5_1"), "{=abb_v_hira}Hira");
                RenameSettlement(Settlement.Find("village_A5_2"), "{=abb_v_najaf}Najaf");
                RenameSettlement(Settlement.Find("village_A5_3"), "{=abb_v_qadisiyyah}Qadisiyyah");
                RenameSettlement(Settlement.Find("town_A6"), "{=abb_s_mosul}Mosul");
                RenameSettlement(Settlement.Find("village_A6_1"), "{=abb_v_sinjar}Sinjar");
                RenameSettlement(Settlement.Find("village_A6_2"), "{=abb_v_balad}Balad");
                RenameSettlement(Settlement.Find("village_A6_3"), "{=abb_v_tellafar}Tell Afar");
                RenameSettlement(Settlement.Find("village_A6_4"), "{=abb_v_nineveh}Nineveh");
                RenameSettlement(Settlement.Find("town_A7"), "{=abb_s_wasit}Wasit");
                RenameSettlement(Settlement.Find("village_A7_2"), "{=abb_v_numaniyya}Nu'maniyya");
                RenameSettlement(Settlement.Find("village_A7_3"), "{=abb_v_jarjaraya}Jarjaraya");
                RenameSettlement(Settlement.Find("village_A7_4"), "{=abb_v_dayralaqul}Dayr al-Aqul");
                RenameSettlement(Settlement.Find("town_A8"), "{=abb_s_samarra}Samarra");
                RenameSettlement(Settlement.Find("village_A8_1"), "{=abb_v_daquqa}Daquqa");
                RenameSettlement(Settlement.Find("village_A8_2"), "{=abb_v_harba}Harba");
                RenameSettlement(Settlement.Find("castle_A1"), "{=abb_c_anbar}Anbar Castle");
                RenameSettlement(Settlement.Find("castle_village_A1_1"), "{=abb_v_falluja}Falluja");
                RenameSettlement(Settlement.Find("castle_village_A1_2"), "{=abb_v_sura}Sura");
                RenameSettlement(Settlement.Find("castle_A2"), "{=abb_c_hit}Hit Castle");
                RenameSettlement(Settlement.Find("castle_village_A2_1"), "{=abb_v_hadithah}Hadithah");
                RenameSettlement(Settlement.Find("castle_village_A2_2"), "{=abb_v_alus}Alus");
                RenameSettlement(Settlement.Find("castle_A3"), "{=abb_c_tikrit}Tikrit Castle");
                RenameSettlement(Settlement.Find("castle_village_A3_1"), "{=abb_v_dur}Ad-Dur");
                RenameSettlement(Settlement.Find("castle_village_A3_2"), "{=abb_v_sinn}Sinn Barimma");
                RenameSettlement(Settlement.Find("castle_A4"), "{=abb_c_rahba}Rahba Castle");
                RenameSettlement(Settlement.Find("castle_village_A4_1"), "{=abb_v_raqqa}Raqqa");
                RenameSettlement(Settlement.Find("castle_village_A4_2"), "{=abb_v_qarqisiya}Qarqisiya");
                RenameSettlement(Settlement.Find("castle_A5"), "{=abb_c_ana}Ana Castle");
                RenameSettlement(Settlement.Find("castle_village_A5_1"), "{=abb_v_rawa}Rawa");
                RenameSettlement(Settlement.Find("castle_village_A5_2"), "{=abb_v_baqubah}Baqubah");
                RenameSettlement(Settlement.Find("castle_A7"), "{=abb_c_nahrawan}Nahrawan Castle");
                RenameSettlement(Settlement.Find("castle_village_A7_1"), "{=abb_v_jalula}Jalula");
                RenameSettlement(Settlement.Find("castle_village_A7_2"), "{=abb_v_khaniqin}Khaniqin");
                RenameSettlement(Settlement.Find("castle_A9"), "{=abb_c_ukbara}Ukbara Castle");
                RenameSettlement(Settlement.Find("castle_village_A9_1"), "{=abb_v_dujayl}Dujayl");
                RenameSettlement(Settlement.Find("castle_village_A9_2"), "{=abb_v_maskin}Maskin");

                // --- Georgian (47 settlements/villages) ---
                RenameSettlement(Settlement.Find("town_S1"), "{=geo_s_kutaisi}Kutaisi");
                RenameSettlement(Settlement.Find("village_S1_1"), "{=geo_v_nokalakevi}Nokalakevi");
                RenameSettlement(Settlement.Find("village_S1_3"), "{=geo_v_vardtsikhe}Vardtsikhe");
                RenameSettlement(Settlement.Find("town_S2"), "{=geo_s_tbilisi}Tbilisi");
                RenameSettlement(Settlement.Find("village_S2_1"), "{=geo_v_mtskheta}Mtskheta");
                RenameSettlement(Settlement.Find("village_S2_2"), "{=geo_v_rustavi}Rustavi");
                RenameSettlement(Settlement.Find("town_S3"), "{=geo_s_kldekari}Kldekari");
                RenameSettlement(Settlement.Find("village_S3_1"), "{=geo_v_trialeti}Trialeti");
                RenameSettlement(Settlement.Find("village_S3_2"), "{=geo_v_manglisi}Manglisi");
                RenameSettlement(Settlement.Find("town_S4"), "{=geo_s_telavi}Telavi");
                RenameSettlement(Settlement.Find("village_S4_1"), "{=geo_v_ikalto}Ikalto");
                RenameSettlement(Settlement.Find("village_S4_3"), "{=geo_v_alaverdi}Alaverdi");
                RenameSettlement(Settlement.Find("village_S4_4"), "{=geo_v_nekresi}Nekresi");
                RenameSettlement(Settlement.Find("town_S5"), "{=geo_s_lore}Lore");
                RenameSettlement(Settlement.Find("village_S5_1"), "{=geo_v_kaladzori}Kaladzori");
                RenameSettlement(Settlement.Find("village_S5_2"), "{=geo_v_tashir}Tashir");
                RenameSettlement(Settlement.Find("town_S6"), "{=geo_s_chqondidi}Chqondidi");
                RenameSettlement(Settlement.Find("village_S6_1"), "{=geo_v_anakopia}Anakopia");
                RenameSettlement(Settlement.Find("village_S6_2"), "{=geo_v_bichvinta}Bichvinta");
                RenameSettlement(Settlement.Find("village_S6_3"), "{=geo_v_sokhumi}Sokhumi");
                RenameSettlement(Settlement.Find("town_S7"), "{=geo_s_khornabuji}Khornabuji");
                RenameSettlement(Settlement.Find("village_S7_1"), "{=geo_v_bodbe}Bodbe");
                RenameSettlement(Settlement.Find("village_S7_2"), "{=geo_v_vejini}Vejini");
                RenameSettlement(Settlement.Find("castle_S1"), "{=geo_c_gori}Gori Castle");
                RenameSettlement(Settlement.Find("castle_village_S1_1"), "{=geo_v_ateni}Ateni");
                RenameSettlement(Settlement.Find("castle_village_S1_2"), "{=geo_v_uplistsikhe}Uplistsikhe");
                RenameSettlement(Settlement.Find("castle_S2"), "{=geo_c_dmanisi}Dmanisi Castle");
                RenameSettlement(Settlement.Find("castle_village_S2_1"), "{=geo_v_bolnisi}Bolnisi");
                RenameSettlement(Settlement.Find("castle_village_S2_2"), "{=geo_v_tsalka}Tsalka");
                RenameSettlement(Settlement.Find("castle_S3"), "{=geo_c_samshvilde}Samshvilde Castle");
                RenameSettlement(Settlement.Find("castle_village_S3_1"), "{=geo_v_orbeti}Orbeti");
                RenameSettlement(Settlement.Find("castle_village_S3_2"), "{=geo_v_tsintskaro}Tsintskaro");
                RenameSettlement(Settlement.Find("castle_S4"), "{=geo_c_zedazeni}Zedazeni Castle");
                RenameSettlement(Settlement.Find("castle_village_S4_1"), "{=geo_v_mukhrani}Mukhrani");
                RenameSettlement(Settlement.Find("castle_village_S4_2"), "{=geo_v_tsromi}Tsromi");
                RenameSettlement(Settlement.Find("castle_S5"), "{=geo_c_manavi}Manavi Castle");
                RenameSettlement(Settlement.Find("castle_village_S5_1"), "{=geo_v_kvareli}Kvareli");
                RenameSettlement(Settlement.Find("castle_village_S5_2"), "{=geo_v_akhmeta}Akhmeta");
                RenameSettlement(Settlement.Find("castle_S6"), "{=geo_c_kojori}Kojori Castle");
                RenameSettlement(Settlement.Find("castle_village_S6_1"), "{=geo_v_betania}Betania");
                RenameSettlement(Settlement.Find("castle_village_S6_2"), "{=geo_v_tabakhmela}Tabakhmela");
                RenameSettlement(Settlement.Find("castle_S7"), "{=geo_c_artanuji}Artanuji Castle");
                RenameSettlement(Settlement.Find("castle_village_S7_1"), "{=geo_v_oltisi}Oltisi");
                RenameSettlement(Settlement.Find("castle_village_S7_2"), "{=geo_v_shavsheti}Shavsheti");
                RenameSettlement(Settlement.Find("castle_S8"), "{=geo_c_tmogvi}Tmogvi Castle");
                RenameSettlement(Settlement.Find("castle_village_S8_1"), "{=geo_v_akhaltsikhe}Akhaltsikhe");
                RenameSettlement(Settlement.Find("castle_village_S8_2"), "{=geo_v_khertvisi}Khertvisi");
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
                SetupTown("town_A2", "clan_kayi_oguz", "{=seljuk_town_husnfulq}Sogut", 6600f);
                SetupTown("town_ES2", "clan_seljuk_royal", "{=seljuk_town_isfahan}Isfahan", 7200f);
                SetupTown("town_A4", "clan_seljuk_royal", "{=seljuk_town_nisabur}Nishapur", 6800f);

                // =====================================================================
                // 2. CASTLES (KALELER)
                // =====================================================================
                SetupCastle("castle_ES4", "clan_danismend", "{=seljuk_castle_lavenia}Lavenia Castle");
                SetupCastle("castle_A6", "clan_artuk", "{=seljuk_castle_shibalzumr}Shibal Zumr Castle");
                SetupCastle("castle_ES5", "clan_ahlatsah", "{=seljuk_castle_morenia}Morenia Castle");
                SetupCastle("castle_A8", "clan_seljuk_royal", "{=seljuk_castle_rey}Rey Castle");

                // =====================================================================
                // 3. VILLAGES (KÖYLER)
                // =====================================================================
                // Konya Villages
                SetupVillage("village_ES1_2", "{=seljuk_vil_polisia}Meram");
                SetupVillage("village_ES1_3", "{=seljuk_vil_tegresos}Sille");
                SetupVillage("village_ES1_4", "{=seljuk_vil_erebulos}Karatay");

                // Söğüt Villages
                SetupVillage("village_A2_2", "{=seljuk_vil_abukhih}Domanic");
                SetupVillage("village_A2_3", "{=seljuk_vil_hoqqa}Bozuyuk");

                // Castle Villages
                SetupVillage("castle_village_ES4_1", "{=seljuk_vil_lavenia}Lavenia");
                SetupVillage("castle_village_A6_1", "{=seljuk_vil_shibalzumr}Shibal Zumr");
                SetupVillage("castle_village_ES5_1", "{=seljuk_vil_morenia}Morenia");

                // İsfahan Villages
                SetupVillage("village_ES2_2", "{=seljuk_vil_cuybare}Juybareh");
                SetupVillage("village_ES2_3", "{=seljuk_vil_lenban}Lenban");
                SetupVillage("village_ES2_4", "{=seljuk_vil_hasanabad}Hasanabad");

                // Nişabur Villages
                SetupVillage("village_A4_1", "{=seljuk_vil_bostanabad}Bostanabad");
                SetupVillage("village_A4_2", "{=seljuk_vil_sadyah}Shadyakh");
                SetupVillage("village_A4_4", "{=seljuk_vil_kohandiz}Kohandezh");

                // Rey Kalesi Villages
                SetupVillage("castle_village_A8_1", "{=seljuk_vil_cesmedeh}Cheshmedeh");
                SetupVillage("castle_village_A8_2", "{=seljuk_vil_veramin}Varamin");

                // =====================================================================
                // 4. THIN-CLAN İKTÂ FIEFS (5 landless frontier beyliks each get 1 village)
                // =====================================================================
                // clan_mengucek/saltuk/caka/karaman/ahi_order were tier-3 clans with a lord and
                // an initial_home_settlement pointing at Konya but no owned settlement at all -
                // "own no actual settlement" per this mod's own dev roadmap (Work stream C).
                // clan_seljuk_royal alone held all 11 villages across its 4 towns/castles; this
                // grants one village each to the five landless beys from that royal domain,
                // mirroring the real Seljuk iqta/timar practice of the Sultan rewarding loyal
                // frontier commanders with crown-land revenue grants (not necessarily their own
                // beylik's home region - iqta assignment usually wasn't tied to a bey's ancestral
                // territory). clan_seljuk_royal keeps 6 of its 11 villages, still the largest
                // holder by a wide margin.
                AssignThinClanVillage("village_ES1_3", "clan_ahi_order");   // Sille (Konya)
                AssignThinClanVillage("village_ES1_4", "clan_karaman");    // Karatay (Konya) - Karamanids later succeeded the Seljuks in this exact region
                AssignThinClanVillage("village_ES2_4", "clan_mengucek");   // Hasanabad (İsfahan)
                AssignThinClanVillage("village_A4_2", "clan_saltuk");      // Şadyah (Nişabur)
                AssignThinClanVillage("castle_village_A8_2", "clan_caka"); // Veramin (Rey Kalesi)
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

        private void AssignThinClanVillage(string villageId, string clanId)
        {
            try
            {
                Settlement village = Settlement.Find(villageId);
                Clan clan = Clan.FindFirst(c => c.StringId == clanId);

                if (village != null && village.IsVillage && clan != null)
                {
                    Hero owner = clan.Leader ?? clan.Heroes.Find(h => h.IsAlive);
                    if (owner != null && village.OwnerClan != clan)
                    {
                        ChangeOwnerOfSettlementAction.ApplyByDefault(owner, village);
                    }
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
