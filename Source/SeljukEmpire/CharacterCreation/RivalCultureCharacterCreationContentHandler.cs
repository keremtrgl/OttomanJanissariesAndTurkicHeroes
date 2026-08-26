using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SeljukEmpire.CharacterCreation
{
    /// <summary>
    /// Injects authentic Byzantine, Abbasid &amp; Georgian character creation narrative options into
    /// Mount &amp; Blade II: Bannerlord, mirroring SeljukCharacterCreationContentHandler's approach
    /// for the three rival-kingdom cultures introduced by this mod. Safely integrated with Native
    /// CharacterCreationManager, 3D parent model equipment, and effect pipelines.
    /// </summary>
    /// <remarks>
    /// See SeljukCharacterCreationContentHandler's remarks: this used to be registered a frame too late
    /// via SubModule.OnApplicationTick polling, after CharacterCreationManager's constructor had already
    /// run its handler-invoking loops on an empty handler list. Now self-registers via
    /// CampaignEvents.OnCharacterCreationInitializedEvent, same as Native's own
    /// CharacterCreationCampaignBehavior.
    /// </remarks>
    public class RivalCultureCharacterCreationContentHandler : CampaignBehaviorBase, ICharacterCreationContentHandler
    {
        // See SeljukCharacterCreationContentHandler's comment on this field: NarrativeMenuOptionArgs.
        // GoldToAdd has no public setter, and setting it via reflection from the per-option preview
        // callback (fired on every hover/click while browsing, not just the option kept) was granting
        // gold and popping a notification before the player had actually chosen anything. Gold bonuses
        // are tracked here and granted once, for whichever option is still selected when character
        // creation finishes (OnCharacterCreationFinalize).
        private readonly Dictionary<string, int> _goldBonusByOptionId = new Dictionary<string, int>();
        private bool _isOptionsInjected;

        public override void RegisterEvents()
        {
            CampaignEvents.OnCharacterCreationInitializedEvent.AddNonSerializedListener(this, OnCharacterCreationInitialized);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient character-creation behavior, nothing to persist
        }

        private void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
        {
            try
            {
                characterCreationManager.RegisterCharacterCreationContentHandler(this, 100);
            }
            catch (Exception)
            {
                // Safety
            }
        }

        public void InitializeContent(CharacterCreationManager characterCreationManager)
        {
            InjectRivalNarratives(characterCreationManager);
        }

        public void AfterInitializeContent(CharacterCreationManager characterCreationManager)
        {
            InjectRivalNarratives(characterCreationManager);
        }

        public void InjectRivalNarratives(CharacterCreationManager characterCreationManager)
        {
            if (characterCreationManager == null || characterCreationManager.NarrativeMenus == null || _isOptionsInjected) return;

            try
            {
                var menus = characterCreationManager.NarrativeMenus;
                if (menus.Count == 0) return;

                InjectByzantineNarratives(menus);
                InjectAbbasidNarratives(menus);
                InjectGeorgianNarratives(menus);

                _isOptionsInjected = true;
            }
            catch (Exception)
            {
                // Safe degradation
            }
        }

        // ============================== BYZANTINE (Culture.empire) ==============================

        private void InjectByzantineNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "byz_opt_senatorial", "{=byz_cc_senatorial_name}Constantinopolitan Senatorial Family", "{=byz_cc_senatorial_desc}Your family belongs to the landed dynatoi class of the imperial capital; you learned the intrigue and protocol of the palace from the cradle.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "byz_opt_akritas", "{=byz_cc_akritas_name}Anatolian Akritai Border Family", "{=byz_cc_akritas_desc}Your family are akritai warriors who guard the Taurus and Euphrates frontier against Turkoman raids.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 600,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "byz_opt_thematic", "{=byz_cc_thematic_name}Anatolian Thematic Soldier-Landowner Family", "{=byz_cc_thematic_desc}Your father was a stratiotes, a military landowner who served the thematic army with his own horse and armor.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 700,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "byz_opt_merchant", "{=byz_cc_merchant_name}Thessaloniki Harbor Merchant Family", "{=byz_cc_merchant_desc}You grew up in a wealthy family trading silk and spices in the port of Thessaloniki, the Empire's second city.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1000,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "byz_opt_monastic", "{=byz_cc_monastic_name}Monastic Library Scribe Family", "{=byz_cc_monastic_desc}Your family were scribes who copied ancient Greek and Roman works in monastery libraries; learning was your cradle.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 900,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "byz_child_hippodrome", "{=byz_cc_hippodrome_name}You Grew Up Watching the Hippodrome Races", "{=byz_cc_hippodrome_desc}You watched the chariot races at Constantinople's famous Hippodrome and felt the passion of the rival factions.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_akritic_songs", "{=byz_cc_akritic_songs_name}You Grew Up on Border Songs of the Akritai", "{=byz_cc_akritic_songs_desc}You grew up on the akritic epics told by the elders of the border villages, and learned to draw a bow early.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_church_school", "{=byz_cc_church_school_name}You Learned Greek and Scripture at the Church School", "{=byz_cc_church_school_desc}At the parish church school you memorized the Greek alphabet, sacred texts, and hymns.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_market", "{=byz_cc_market_name}You Learned Trade in the Mese Street Market", "{=byz_cc_market_desc}In the shops along the Mese, Constantinople's main avenue, you learned the finer points of trade from merchants.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "byz_youth_varangian", "{=byz_cc_varangian_name}You Trained Alongside the Varangian Guard", "{=byz_cc_varangian_desc}You trained in axe and sword alongside the Empire's famed Varangian Guard at the imperial palace.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(youthMenu, "byz_youth_thematic_army", "{=byz_cc_thematic_army_name}You Joined the Anatolian Thematic Army", "{=byz_cc_thematic_army_desc}You became a hardened soldier in Anatolia's frontier themes, trained in mounted archery and the spear.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Polearm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(youthMenu, "byz_youth_university", "{=byz_cc_university_name}You Studied Rhetoric at the Mangana University", "{=byz_cc_university_desc}You took lessons in philosophy, law and rhetoric at Constantinople's famed Mangana University.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "byz_car_kataphraktos", "{=byz_cc_kataphraktos_name}You Were Chosen for a Kataphraktos Heavy Cavalry Regiment", "{=byz_cc_kataphraktos_desc}You were accepted into the elite Kataphraktos corps of armored horses and armored riders.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsByzantineCultureSelected, null);

                AddOption(careerMenu, "byz_car_akritas_officer", "{=byz_cc_akritas_officer_name}You Became a Border Akritai Commander", "{=byz_cc_akritas_officer_desc}You commanded akritai units defending the Anatolian frontier against Turkoman raids.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 15, 0, IsByzantineCultureSelected, null);

                AddOption(careerMenu, "byz_car_bureaucrat", "{=byz_cc_bureaucrat_name}You Became a Clerk in the Imperial Chancery", "{=byz_cc_bureaucrat_desc}You became a trusted clerk in the palace bureaucracy, keeping tax and land records.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "byz_deed_rally", "{=byz_cc_rally_name}You Survived a Battlefield Rout and Rallied the Army", "{=byz_cc_rally_desc}Amid a great rout you rallied the scattering units and secured an orderly retreat.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Tactics }, 15, null, 0, 30, 0, IsByzantineCultureSelected, null);

                AddOption(deedMenu, "byz_deed_fortress_defense", "{=byz_cc_fortress_defense_name}You Held a Besieged Fortress to the Very End", "{=byz_cc_fortress_defense_desc}You defended a besieged frontier fortress with resolve until relief finally arrived.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics }, 15, null, 0, 25, 0, IsByzantineCultureSelected, null);

                AddOption(deedMenu, "byz_deed_diplomat", "{=byz_cc_diplomat_name}You Completed a Perilous Embassy", "{=byz_cc_diplomat_desc}You skillfully and bravely completed a dangerous peace embassy to an enemy court.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsByzantineCultureSelected, null);
            }
        }

        // ============================== ABBASID (Culture.aserai) ==============================

        private void InjectAbbasidNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "abb_opt_hashimite", "{=abb_cc_hashimite_name}Sharifian Hashimite Family", "{=abb_cc_hashimite_desc}You are the child of a sharifian Hashimite family descended from the Prophet's line, respected and influential in Baghdad.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "abb_opt_qadi", "{=abb_cc_qadi_name}Baghdad Qadi and Ulema Family", "{=abb_cc_qadi_desc}Your family is a respected line of ulema who produced expert qadis learned in fiqh and sharia.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1100,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "abb_opt_bedouin", "{=abb_cc_bedouin_name}Desert Bedouin Tribe", "{=abb_cc_bedouin_desc}You were born into a Bedouin tribe deep in the Iraqi desert, herding camels and finding your way through sandstorms.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));

                AddOption(parentMenu, "abb_opt_basra_merchant", "{=abb_cc_basra_merchant_name}Basra Harbor Merchant Family", "{=abb_cc_basra_merchant_desc}You come from a wealthy merchant family trading spices and pearls on ships sailing from Basra into the Indian Ocean.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "abb_opt_ghilman", "{=abb_cc_ghilman_name}Line of a Caliphal Ghulam Guardsman", "{=abb_cc_ghilman_desc}Your father was one of the Turkish-born ghulam guardsmen who stood watch at the Caliph's Dar al-Khilafa.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 700,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "abb_child_desert", "{=abb_cc_desert_name}You Learned to Ride Camel and Horse in the Desert", "{=abb_cc_desert_desc}Among your Bedouin kin you learned to ride camels under the merciless desert sun and navigate by the stars.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_madrasa", "{=abb_cc_madrasa_name}You Memorized the Quran and Fiqh at the Mosque School", "{=abb_cc_madrasa_desc}From a young age you attended lessons in Quran, hadith and fiqh at Baghdad's great mosques.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_bazaar", "{=abb_cc_bazaar_name}You Learned Trade in the Bazaars of Baghdad", "{=abb_cc_bazaar_desc}In the labyrinthine streets of the Karkh bazaar you learned bargaining and weighing goods from the merchants.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_wrestling", "{=abb_cc_wrestling_name}You Trained in Wrestling and Swordplay in the Squares", "{=abb_cc_wrestling_desc}You wrestled with other children in Baghdad's squares and played at swordplay with wooden blades.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "abb_youth_nizamiyya", "{=abb_cc_nizamiyya_name}You Studied at the Nizamiyya Madrasa of Baghdad", "{=abb_cc_nizamiyya_desc}At the famous Nizamiyya madrasa founded by Nizam al-Mulk, you studied fiqh, logic, and statecraft.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(youthMenu, "abb_youth_caravan_guard", "{=abb_cc_caravan_guard_name}You Served as a Hajj Caravan Guard", "{=abb_cc_caravan_guard_desc}You protected pilgrim caravans traveling from Baghdad to Mecca against desert bandits.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow, DefaultSkills.Trade }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(youthMenu, "abb_youth_palace_guard", "{=abb_cc_palace_guard_name}You Joined the Dar al-Khilafa Guard Regiment", "{=abb_cc_palace_guard_desc}You received weapons training among the Caliph's disciplined ghulam guardsmen at the palace.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "abb_car_ghulam_elite", "{=abb_cc_ghulam_elite_name}You Were Chosen for the Caliph's Elite Guard", "{=abb_cc_ghulam_elite_desc}Your outstanding skill earned you a place in the Caliph's personal Dar al-Khilafa guard unit.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 20, 0, IsAbbasidCultureSelected, null);

                AddOption(careerMenu, "abb_car_scholar", "{=abb_cc_scholar_name}You Served as a Clerk in the Court of Grievances", "{=abb_cc_scholar_desc}You became a trusted clerk recording cases in the Caliph's court of grievances.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsAbbasidCultureSelected, null);

                AddOption(careerMenu, "abb_car_desert_raider", "{=abb_cc_desert_raider_name}You Became a Raider in Desert Campaigns", "{=abb_cc_desert_raider_desc}You fought on the front line in campaigns to keep order among the desert tribes.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Tactics }, 15, null, 0, 15, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "abb_deed_palace_defense", "{=abb_cc_palace_defense_name}You Defended the Caliph During a Siege of the Palace", "{=abb_cc_palace_defense_desc}During a raid on the Dar al-Khilafa you risked your life and stood out among the Caliph's guard.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, IsAbbasidCultureSelected, null);

                AddOption(deedMenu, "abb_deed_caravan_save", "{=abb_cc_caravan_save_name}You Saved a Hajj Caravan from a Raid", "{=abb_cc_caravan_save_desc}You single-handedly defended a great pilgrim caravan struck by desert bandits, saving the pilgrims.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Athletics }, 15, null, 0, 25, 0, IsAbbasidCultureSelected, null);

                AddOption(deedMenu, "abb_deed_justice", "{=abb_cc_justice_name}You Defended the Wronged Before the Qadi", "{=abb_cc_justice_desc}You courageously defended the case of a wronged tradesman before the qadi, securing justice.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsAbbasidCultureSelected, null);
            }
        }

        // ============================== GEORGIAN (Culture.sturgia) ==============================

        private void InjectGeorgianNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "geo_opt_bagrationi", "{=geo_cc_bagrationi_name}Distant Branch of the Bagrationi Dynasty", "{=geo_cc_bagrationi_desc}You come from a distant branch of the royal Bagrationi line; you grew up amid the court's customs and intrigues.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "geo_opt_aznauri", "{=geo_cc_aznauri_name}Aznauri Petty Noble Family", "{=geo_cc_aznauri_desc}You are the child of a landed petty-noble aznauri family in the Caucasus mountains, raised to ride and wield the spear.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 700,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "geo_opt_mountain", "{=geo_cc_mountain_name}Mountain Valley Warrior Family", "{=geo_cc_mountain_desc}You grew up in an unreachable Caucasus mountain valley, hardened by wrestling snow and rock.",
                    new[] { DefaultSkills.Athletics, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Endurance, 1, 10, 500,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));

                AddOption(parentMenu, "geo_opt_tbilisi_merchant", "{=geo_cc_tbilisi_merchant_name}Tbilisi Silk Road Merchant Family", "{=geo_cc_tbilisi_merchant_desc}You are the child of a family trading silk and spices in the markets of Tbilisi, where East meets West.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1000,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "geo_opt_monastery", "{=geo_cc_monastery_name}Monastery Academy Scribe Family", "{=geo_cc_monastery_desc}You were raised in a family of scribes copying theology and philosophy at a renowned monastery academy.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Crafting }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 800,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "geo_child_caucasus", "{=geo_cc_caucasus_name}You Rode Through Caucasus Mountain Passes", "{=geo_cc_caucasus_desc}In narrow mountain passes, along the edge of cliffs, you learned to ride and balance at a young age.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_falconry", "{=geo_cc_falconry_name}You Learned Falconry and Archery", "{=geo_cc_falconry_desc}You learned to hunt in the Caucasus forests by flying falcons and drawing the bow.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_church", "{=geo_cc_church_name}You Sang in the Church Choir and Studied at the Monastery School", "{=geo_cc_church_desc}You sang hymns in the Orthodox church choir and learned to read and write from prayer books at the monastery school.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_smithy", "{=geo_cc_smithy_name}You Forged Steel at a Mountain Village Smithy", "{=geo_cc_smithy_desc}At the village smithy you learned to forge steel and repair armor alongside the master smiths.",
                    new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "geo_youth_royal_guard", "{=geo_cc_royal_guard_name}You Joined the Royal Guard Regiment", "{=geo_cc_royal_guard_desc}You received disciplined weapons and cavalry training among the king's palace guard.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(youthMenu, "geo_youth_mountain_scout", "{=geo_cc_mountain_scout_name}You Became a Border Scout in the Mountain Passes", "{=geo_cc_mountain_scout_desc}You joined the vanguard units watching the mountain passes against enemy raids.",
                    new[] { DefaultSkills.Scouting, DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(youthMenu, "geo_youth_academy", "{=geo_cc_academy_name}You Studied Philosophy and Rhetoric at the Monastery Academy", "{=geo_cc_academy_desc}At the Caucasus's foremost monastery academy you studied theology, philosophy, and statecraft.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "geo_car_royal_cavalry", "{=geo_cc_royal_cavalry_name}You Were Chosen for the Royal Heavy Cavalry", "{=geo_cc_royal_cavalry_desc}You joined the elite royal cavalry regiment that fought in campaigns along the Caucasus frontier.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsGeorgianCultureSelected, null);

                AddOption(careerMenu, "geo_car_border_gazi", "{=geo_cc_border_gazi_name}You Gained Experience in Border Fortresses", "{=geo_cc_border_gazi_desc}You stood watch for years at Caucasus border fortresses, gaining experience through many sieges.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics, DefaultSkills.Bow }, 15, null, 0, 15, 0, IsGeorgianCultureSelected, null);

                AddOption(careerMenu, "geo_car_court", "{=geo_cc_court_name}You Served at the Royal Court", "{=geo_cc_court_desc}You served as an advisor close to the royal family at the court in Tbilisi.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "geo_deed_battle_hero", "{=geo_cc_battle_hero_name}You Showed Heroism in a Great Pitched Battle", "{=geo_cc_battle_hero_desc}In a great pitched battle you cut through enemy ranks and struck down the enemy standard-bearer.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, IsGeorgianCultureSelected, null);

                AddOption(deedMenu, "geo_deed_rescue", "{=geo_cc_rescue_name}You Saved a Besieged Mountain Fortress", "{=geo_cc_rescue_desc}You slipped through a secret pass to bring relief to a besieged mountain fortress, saving its people.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Scouting }, 15, null, 0, 25, 0, IsGeorgianCultureSelected, null);

                AddOption(deedMenu, "geo_deed_pilgrim", "{=geo_cc_pilgrim_name}You Became a Shield for Pilgrims and the Wronged", "{=geo_cc_pilgrim_desc}You earned renown by protecting pilgrim caravans bound for holy sites from bandits.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Athletics }, 15, null, 0, 20, 0, IsGeorgianCultureSelected, null);
            }
        }

        // ============================== SHARED HELPERS ==============================

        private static bool IsByzantineCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "empire";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsAbbasidCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "aserai";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsGeorgianCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "sturgia";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void SetParentOccupation(CharacterCreationManager manager, string occupation)
        {
            try
            {
                if (manager?.CharacterCreationContent != null)
                {
                    manager.CharacterCreationContent.SetParentOccupation(occupation);
                }
            }
            catch (Exception) { }
        }

        private void AddOption(
            NarrativeMenu menu,
            string stringId,
            string titleKey,
            string descKey,
            SkillObject[] skills,
            int skillLevelToAdd,
            CharacterAttribute attribute,
            int attributeLevelToAdd,
            int renownToAdd,
            int goldToAdd,
            Func<CharacterCreationManager, bool> visibilityPredicate,
            NarrativeMenuOptionOnSelectDelegate onSelect)
        {
            if (menu == null) return;

            if (goldToAdd > 0)
            {
                _goldBonusByOptionId[stringId] = goldToAdd;
            }

            var option = new NarrativeMenuOption(
                stringId,
                new TextObject(titleKey),
                new TextObject(descKey),
                args =>
                {
                    if (args == null) return;
                    if (skills != null && skills.Length > 0)
                    {
                        args.SetAffectedSkills(skills);
                        args.SetLevelToSkills(skillLevelToAdd);
                        args.SetFocusToSkills(1);
                    }
                    if (attribute != null && attributeLevelToAdd > 0)
                    {
                        args.SetLevelToAttribute(attribute, attributeLevelToAdd);
                    }
                    if (renownToAdd > 0)
                    {
                        args.SetRenownToAdd(renownToAdd);
                    }
                },
                mgr => visibilityPredicate(mgr),
                onSelect,
                null); // Let Native's ApplyFinalEffects apply Args cleanly without duplicate crash

            menu.AddNarrativeMenuOption(option);
        }

        public void OnStageCompleted(CharacterCreationStageBase stage) { }

        public void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
        {
            try
            {
                if (Hero.MainHero == null || characterCreationManager?.SelectedOptions == null) return;

                int totalGold = 0;
                foreach (var selectedOption in characterCreationManager.SelectedOptions.Values)
                {
                    if (selectedOption != null && _goldBonusByOptionId.TryGetValue(selectedOption.StringId, out int gold))
                    {
                        totalGold += gold;
                    }
                }

                if (totalGold > 0)
                {
                    Hero.MainHero.Gold += totalGold;
                }
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
