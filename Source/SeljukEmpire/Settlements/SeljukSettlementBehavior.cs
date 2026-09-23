using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
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
        private static readonly FieldInfo NameField = typeof(Settlement).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly struct SettlementName
        {
            public readonly string SettlementId;
            public readonly string NameKey;

            public SettlementName(string settlementId, string nameKey)
            {
                SettlementId = settlementId;
                NameKey = nameKey;
            }
        }

        /// <summary>
        /// Every settlement this mod renames, and its localized name ("{=...}" string id + English fallback). The single
        /// source of truth for names: reapplied on every session launch (see OnSessionLaunched),
        /// so the one-time territory setup below no longer carries its own second copy of the
        /// Seljuk names that could drift out of sync with this list.
        /// </summary>
        private static readonly SettlementName[] SettlementNames =
        {
            // Towns
            new SettlementName("town_ES1", "{=seljuk_town_danustica}Konya"),
            new SettlementName("town_A2", "{=seljuk_town_husnfulq}Sogut"),
            new SettlementName("town_ES2", "{=seljuk_town_isfahan}Isfahan"),
            new SettlementName("town_A4", "{=seljuk_town_nisabur}Nishapur"),

            // Castles
            new SettlementName("castle_ES4", "{=seljuk_castle_lavenia}Lavenia Castle"),
            new SettlementName("castle_A6", "{=seljuk_castle_shibalzumr}Shibal Zumr Castle"),
            new SettlementName("castle_ES5", "{=seljuk_castle_morenia}Morenia Castle"),
            new SettlementName("castle_A8", "{=seljuk_castle_rey}Rey Castle"),

            // Konya villages
            new SettlementName("village_ES1_2", "{=seljuk_vil_polisia}Meram"),
            new SettlementName("village_ES1_3", "{=seljuk_vil_tegresos}Sille"),
            new SettlementName("village_ES1_4", "{=seljuk_vil_erebulos}Karatay"),

            // Söğüt villages
            new SettlementName("village_A2_2", "{=seljuk_vil_abukhih}Domanic"),
            new SettlementName("village_A2_3", "{=seljuk_vil_hoqqa}Bozuyuk"),

            // Castle villages
            new SettlementName("castle_village_ES4_1", "{=seljuk_vil_lavenia}Lavenia"),
            new SettlementName("castle_village_ES4_2", "{=seljuk_v_niksar}Niksar"),
            new SettlementName("castle_village_A6_1", "{=seljuk_vil_shibalzumr}Shibal Zumr"),
            new SettlementName("castle_village_ES5_1", "{=seljuk_vil_morenia}Morenia"),
            new SettlementName("castle_village_ES5_2", "{=seljuk_v_adilcevaz}Adilcevaz"),

            // İsfahan villages
            new SettlementName("village_ES2_2", "{=seljuk_vil_cuybare}Juybareh"),
            new SettlementName("village_ES2_3", "{=seljuk_vil_lenban}Lenban"),
            new SettlementName("village_ES2_4", "{=seljuk_vil_hasanabad}Hasanabad"),

            // Nişabur villages
            new SettlementName("village_A4_1", "{=seljuk_vil_bostanabad}Bostanabad"),
            new SettlementName("village_A4_2", "{=seljuk_vil_sadyah}Shadyakh"),
            new SettlementName("village_A4_4", "{=seljuk_vil_kohandiz}Kohandezh"),

            // Rey Kalesi villages
            new SettlementName("castle_village_A8_1", "{=seljuk_vil_cesmedeh}Cheshmedeh"),
            new SettlementName("castle_village_A8_2", "{=seljuk_vil_veramin}Varamin"),

            // Byzantine, Abbasid and Georgian settlements/villages are declared purely via
            // XML overrides (byzantine_settlements.xml / abbasid_settlements.xml /
            // georgian_settlements.xml) with no equivalent C# reinforcement. Settlement.Name
            // is not part of the campaign save graph either way, so reasserting these too,
            // every session, closes the same gap for them rather than leaving them dependent
            // on the XML override alone.

            // --- Byzantine (28 settlements/villages, matches byzantine_settlements.xml) ---
            new SettlementName("town_ES4", "{=byz_s_ankara}Ancyra"),
            new SettlementName("village_ES4_1", "{=byz_v_juliopolis}Iuliopolis"),
            new SettlementName("village_ES4_3", "{=byz_v_germa}Germa"),
            // town_ES5 + villages are Seljuk-owned (settlements.xml), not Byzantine - reasserting
            // the correct Seljuk name here instead of the Byzantine one it used to silently revert to.
            new SettlementName("town_ES5", "{=seljuk_town_amasya}Amasya (Amaseia)"),
            new SettlementName("village_ES5_1", "{=seljuk_vil_merzifon}Merzifon"),
            new SettlementName("village_ES5_2", "{=seljuk_vil_tasova}Tasova"),
            new SettlementName("village_ES5_3", "{=seljuk_vil_gumushacikoy}Gumushacikoy"),
            new SettlementName("town_ES3", "{=byz_s_nicaea}Nicaea"),
            new SettlementName("village_ES3_1", "{=byz_v_prusa}Prusa"),
            new SettlementName("village_ES3_2", "{=byz_v_apollonia}Apollonia"),
            new SettlementName("village_ES3_3", "{=byz_v_lopadion}Lopadion"),
            new SettlementName("town_ES6", "{=byz_s_sebasteia}Sebasteia"),
            new SettlementName("village_ES6_1", "{=byz_v_nicopolis}Nicopolis"),
            new SettlementName("village_ES6_2", "{=byz_v_koloneia}Koloneia"),
            new SettlementName("castle_village_ES6_2", "{=byz_v_amycon2}Dazimon"),
            new SettlementName("town_ES7", "{=byz_s_trebizond}Trebizond"),
            new SettlementName("village_ES7_1", "{=byz_v_rhizaion}Rhizaion"),
            new SettlementName("village_ES7_2", "{=byz_v_kerasous}Kerasous"),
            new SettlementName("castle_village_ES7_2", "{=byz_v_eunalica2}Susurmena"),
            // castle_ES1 + villages are Seljuk-owned (settlements.xml, Kayı Boyu) - same fix as
            // town_ES5 above, reasserting the correct Seljuk name instead of the Byzantine one.
            new SettlementName("castle_ES1", "{=seljuk_castle_dorylaeum}Eskisehir (Dorylaeum)"),
            new SettlementName("castle_village_ES1_1", "{=seljuk_vil_sivrihisar}Sivrihisar"),
            new SettlementName("castle_village_ES1_2", "{=seljuk_vil_mihaliccik}Mihaliccik"),
            new SettlementName("castle_ES2", "{=byz_c_nicomedia}Nicomedia Castle"),
            new SettlementName("castle_village_ES2_1", "{=byz_v_chalcedon}Chalcedon"),
            new SettlementName("castle_village_ES2_2", "{=byz_v_prainetos}Prainetos"),
            new SettlementName("castle_ES3", "{=byz_c_chonae}Chonae Castle"),
            new SettlementName("castle_village_ES3_1", "{=byz_v_laodicea}Laodicea"),
            new SettlementName("castle_village_ES3_2", "{=byz_v_colossae}Colossae"),
            new SettlementName("castle_ES6", "{=byz_c_claudiopolis}Claudiopolis Castle"),
            new SettlementName("castle_village_ES6_1", "{=byz_v_gangra}Gangra"),
            new SettlementName("castle_ES7", "{=byz_c_neocaesarea}Neocaesarea Castle"),
            new SettlementName("castle_village_ES7_1", "{=byz_v_comana}Comana"),
            new SettlementName("castle_ES8", "{=byz_c_caesarea}Caesarea Castle"),
            new SettlementName("castle_village_ES8_1", "{=byz_v_tyana}Tyana"),
            new SettlementName("castle_village_ES8_2", "{=byz_v_nazianzus}Nazianzus"),

            // --- Abbasid (44 settlements/villages) ---
            new SettlementName("town_A1", "{=abb_s_baghdad}Baghdad"),
            new SettlementName("village_A1_1", "{=abb_v_qutrabbul}Qutrabbul"),
            new SettlementName("village_A1_2", "{=abb_v_babalsham}Bab al-Sham"),
            new SettlementName("village_A1_4", "{=abb_v_awana}Awana"),
            new SettlementName("town_A3", "{=abb_s_basra}Basra"),
            new SettlementName("village_A3_1", "{=abb_v_ubulla}Ubulla"),
            new SettlementName("village_A3_3", "{=abb_v_abbadan}Abbadan"),
            new SettlementName("town_A5", "{=abb_s_kufa}Kufa"),
            new SettlementName("village_A5_1", "{=abb_v_hira}Hira"),
            new SettlementName("village_A5_2", "{=abb_v_najaf}Najaf"),
            new SettlementName("village_A5_3", "{=abb_v_qadisiyyah}Qadisiyyah"),
            new SettlementName("town_A6", "{=abb_s_mosul}Mosul"),
            new SettlementName("village_A6_1", "{=abb_v_sinjar}Sinjar"),
            new SettlementName("village_A6_2", "{=abb_v_balad}Balad"),
            new SettlementName("village_A6_3", "{=abb_v_tellafar}Tell Afar"),
            new SettlementName("village_A6_4", "{=abb_v_nineveh}Nineveh"),
            new SettlementName("town_A7", "{=abb_s_wasit}Wasit"),
            new SettlementName("village_A7_2", "{=abb_v_numaniyya}Nu'maniyya"),
            new SettlementName("village_A7_3", "{=abb_v_jarjaraya}Jarjaraya"),
            new SettlementName("village_A7_4", "{=abb_v_dayralaqul}Dayr al-Aqul"),
            new SettlementName("town_A8", "{=abb_s_samarra}Samarra"),
            new SettlementName("village_A8_1", "{=abb_v_daquqa}Daquqa"),
            new SettlementName("village_A8_2", "{=abb_v_harba}Harba"),
            new SettlementName("castle_A1", "{=abb_c_anbar}Anbar Castle"),
            new SettlementName("castle_village_A1_1", "{=abb_v_falluja}Falluja"),
            new SettlementName("castle_village_A1_2", "{=abb_v_sura}Sura"),
            new SettlementName("castle_A2", "{=abb_c_hit}Hit Castle"),
            new SettlementName("castle_village_A2_1", "{=abb_v_hadithah}Hadithah"),
            new SettlementName("castle_village_A2_2", "{=abb_v_alus}Alus"),
            new SettlementName("castle_A3", "{=abb_c_tikrit}Tikrit Castle"),
            new SettlementName("castle_village_A3_1", "{=abb_v_dur}Ad-Dur"),
            new SettlementName("castle_village_A3_2", "{=abb_v_sinn}Sinn Barimma"),
            new SettlementName("castle_A4", "{=abb_c_rahba}Rahba Castle"),
            new SettlementName("castle_village_A4_1", "{=abb_v_raqqa}Raqqa"),
            new SettlementName("castle_village_A4_2", "{=abb_v_qarqisiya}Qarqisiya"),
            new SettlementName("castle_A5", "{=abb_c_ana}Ana Castle"),
            new SettlementName("castle_village_A5_1", "{=abb_v_rawa}Rawa"),
            new SettlementName("castle_village_A5_2", "{=abb_v_baqubah}Baqubah"),
            new SettlementName("castle_A7", "{=abb_c_nahrawan}Nahrawan Castle"),
            new SettlementName("castle_village_A7_1", "{=abb_v_jalula}Jalula"),
            new SettlementName("castle_village_A7_2", "{=abb_v_khaniqin}Khaniqin"),
            new SettlementName("castle_A9", "{=abb_c_ukbara}Ukbara Castle"),
            new SettlementName("castle_village_A9_1", "{=abb_v_dujayl}Dujayl"),
            new SettlementName("castle_village_A9_2", "{=abb_v_maskin}Maskin"),

            // --- Georgian (47 settlements/villages) ---
            new SettlementName("town_S1", "{=geo_s_kutaisi}Kutaisi"),
            new SettlementName("village_S1_1", "{=geo_v_nokalakevi}Nokalakevi"),
            new SettlementName("village_S1_3", "{=geo_v_vardtsikhe}Vardtsikhe"),
            new SettlementName("town_S2", "{=geo_s_tbilisi}Tbilisi"),
            new SettlementName("village_S2_1", "{=geo_v_mtskheta}Mtskheta"),
            new SettlementName("village_S2_2", "{=geo_v_rustavi}Rustavi"),
            new SettlementName("town_S3", "{=geo_s_kldekari}Kldekari"),
            new SettlementName("village_S3_1", "{=geo_v_trialeti}Trialeti"),
            new SettlementName("village_S3_2", "{=geo_v_manglisi}Manglisi"),
            new SettlementName("town_S4", "{=geo_s_telavi}Telavi"),
            new SettlementName("village_S4_1", "{=geo_v_ikalto}Ikalto"),
            new SettlementName("village_S4_3", "{=geo_v_alaverdi}Alaverdi"),
            new SettlementName("village_S4_4", "{=geo_v_nekresi}Nekresi"),
            new SettlementName("town_S5", "{=geo_s_lore}Lore"),
            new SettlementName("village_S5_1", "{=geo_v_kaladzori}Kaladzori"),
            new SettlementName("village_S5_2", "{=geo_v_tashir}Tashir"),
            new SettlementName("town_S6", "{=geo_s_chqondidi}Chqondidi"),
            new SettlementName("village_S6_1", "{=geo_v_anakopia}Anakopia"),
            new SettlementName("village_S6_2", "{=geo_v_bichvinta}Bichvinta"),
            new SettlementName("village_S6_3", "{=geo_v_sokhumi}Sokhumi"),
            new SettlementName("town_S7", "{=geo_s_khornabuji}Khornabuji"),
            new SettlementName("village_S7_1", "{=geo_v_bodbe}Bodbe"),
            new SettlementName("village_S7_2", "{=geo_v_vejini}Vejini"),
            new SettlementName("castle_S1", "{=geo_c_gori}Gori Castle"),
            new SettlementName("castle_village_S1_1", "{=geo_v_ateni}Ateni"),
            new SettlementName("castle_village_S1_2", "{=geo_v_uplistsikhe}Uplistsikhe"),
            new SettlementName("castle_S2", "{=geo_c_dmanisi}Dmanisi Castle"),
            new SettlementName("castle_village_S2_1", "{=geo_v_bolnisi}Bolnisi"),
            new SettlementName("castle_village_S2_2", "{=geo_v_tsalka}Tsalka"),
            new SettlementName("castle_S3", "{=geo_c_samshvilde}Samshvilde Castle"),
            new SettlementName("castle_village_S3_1", "{=geo_v_orbeti}Orbeti"),
            new SettlementName("castle_village_S3_2", "{=geo_v_tsintskaro}Tsintskaro"),
            new SettlementName("castle_S4", "{=geo_c_zedazeni}Zedazeni Castle"),
            new SettlementName("castle_village_S4_1", "{=geo_v_mukhrani}Mukhrani"),
            new SettlementName("castle_village_S4_2", "{=geo_v_tsromi}Tsromi"),
            new SettlementName("castle_S5", "{=geo_c_manavi}Manavi Castle"),
            new SettlementName("castle_village_S5_1", "{=geo_v_kvareli}Kvareli"),
            new SettlementName("castle_village_S5_2", "{=geo_v_akhmeta}Akhmeta"),
            new SettlementName("castle_S6", "{=geo_c_kojori}Kojori Castle"),
            new SettlementName("castle_village_S6_1", "{=geo_v_betania}Betania"),
            new SettlementName("castle_village_S6_2", "{=geo_v_tabakhmela}Tabakhmela"),
            new SettlementName("castle_S7", "{=geo_c_artanuji}Artanuji Castle"),
            new SettlementName("castle_village_S7_1", "{=geo_v_oltisi}Oltisi"),
            new SettlementName("castle_village_S7_2", "{=geo_v_shavsheti}Shavsheti"),
            new SettlementName("castle_S8", "{=geo_c_tmogvi}Tmogvi Castle"),
            new SettlementName("castle_village_S8_1", "{=geo_v_akhaltsikhe}Akhaltsikhe"),
            new SettlementName("castle_village_S8_2", "{=geo_v_khertvisi}Khertvisi"),
        };

        private bool _isSettlementOwnershipInitialized;

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
            ReapplySettlementNames();
        }

        private static void ReapplySettlementNames()
        {
            if (NameField == null)
            {
                // Settlement._name is private engine state; if a game update renames it, every
                // custom settlement name silently reverts to Native's. Say so once per session
                // instead of failing invisibly.
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Seljuk Empire] Settlement renaming is unavailable on this game version (Settlement._name not found).",
                    Colors.Red));
                return;
            }

            // Each rename is independent: one missing or failing settlement must not stop the
            // other ~150 from being applied (the old single try/catch around the whole list did).
            foreach (SettlementName entry in SettlementNames)
            {
                RenameSettlement(Settlement.Find(entry.SettlementId), entry.NameKey);
            }
        }

        private static void InitializeSeljukKingdomHierarchy()
        {
            try
            {
                Kingdom seljukKingdom = Kingdom.All.Find(k => k.StringId == SeljukFactionUtility.SeljukKingdomId);
                Clan royalClan = Clan.FindFirst(c => c.StringId == "clan_seljuk_royal");
                Hero alpArslan = Hero.Find("lord_seljuk_alp_arslan");

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
                        Hero leader = Hero.Find(pair.Value);

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

        private static void InitializeSeljukTerritories()
        {
            try
            {
                // =====================================================================
                // 1. TOWNS (ŞEHİRLER)
                // =====================================================================
                SetupTown("town_ES1", "clan_seljuk_royal", 7800f);
                SetupTown("town_A2", "clan_kayi_oguz", 6600f);
                SetupTown("town_ES2", "clan_seljuk_royal", 7200f);
                SetupTown("town_A4", "clan_seljuk_royal", 6800f);

                // =====================================================================
                // 2. CASTLES (KALELER)
                // =====================================================================
                SetupCastle("castle_ES4", "clan_danismend");
                SetupCastle("castle_A6", "clan_artuk");
                SetupCastle("castle_ES5", "clan_ahlatsah");
                SetupCastle("castle_A8", "clan_seljuk_royal");

                // =====================================================================
                // 3. VILLAGES (KÖYLER)
                // =====================================================================
                // Konya Villages
                SetupVillage("village_ES1_2");
                SetupVillage("village_ES1_3");
                SetupVillage("village_ES1_4");

                // Söğüt Villages
                SetupVillage("village_A2_2");
                SetupVillage("village_A2_3");

                // Castle Villages
                SetupVillage("castle_village_ES4_1");
                SetupVillage("castle_village_A6_1");
                SetupVillage("castle_village_ES5_1");

                // İsfahan Villages
                SetupVillage("village_ES2_2");
                SetupVillage("village_ES2_3");
                SetupVillage("village_ES2_4");

                // Nişabur Villages
                SetupVillage("village_A4_1");
                SetupVillage("village_A4_2");
                SetupVillage("village_A4_4");

                // Rey Kalesi Villages
                SetupVillage("castle_village_A8_1");
                SetupVillage("castle_village_A8_2");

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

        private static void SetupTown(string settlementId, string clanId, float prosperity)
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
                }
            }
            catch (Exception) { }
        }

        private static void SetupCastle(string settlementId, string clanId)
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
                }
            }
            catch (Exception) { }
        }

        private static void SetupVillage(string villageId)
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
                }
            }
            catch (Exception) { }
        }

        private static void AssignThinClanVillage(string villageId, string clanId)
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

        private static void RenameSettlement(Settlement settlement, string textKey)
        {
            if (settlement == null || string.IsNullOrEmpty(textKey) || NameField == null) return;
            try
            {
                NameField.SetValue(settlement, new TextObject(textKey));
            }
            catch (Exception) { }
        }
    }
}
