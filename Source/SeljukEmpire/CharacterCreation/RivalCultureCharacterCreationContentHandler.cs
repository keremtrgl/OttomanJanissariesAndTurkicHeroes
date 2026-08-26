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
    /// Injects authentic Byzantine, Abbasid, Georgian, Crusader (Culture.vlandia), Cilician Armenian
    /// (Culture.battania) &amp; Kara-Khanid (Culture.khuzait) character creation narrative options into
    /// Mount &amp; Blade II: Bannerlord, mirroring SeljukCharacterCreationContentHandler's approach
    /// for the rival-kingdom cultures introduced by this mod. Latin Empire has no separate entry here:
    /// it shares Culture.empire with Byzantium, and narrative visibility is gated by culture, not
    /// kingdom, so the Byzantine options already cover it. Safely integrated with Native
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
                // Native keys handlers by this int internally - must differ from SeljukCharacterCreationContentHandler's 100 or registration throws and this handler never runs.
                characterCreationManager.RegisterCharacterCreationContentHandler(this, 101);
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
                InjectCrusaderNarratives(menus);
                InjectArmenianNarratives(menus);
                InjectKaraKhanidNarratives(menus);

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

        // ============================== CRUSADER STATES (Culture.vlandia) ==============================

        private void InjectCrusaderNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "crus_opt_norman_knight", "{=crus_cc_norman_knight_name}Norman Knightly Family", "{=crus_cc_norman_knight_desc}Your family descends from Norman knights who followed the First Crusade east; you were raised with lance and warhorse from childhood.",
                    new[] { DefaultSkills.Riding, DefaultSkills.OneHanded }, 15, DefaultCharacterAttributes.Vigor, 1, 10, 1200,
                    IsCrusaderCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "crus_opt_antiochene_noble", "{=crus_cc_antiochene_noble_name}Antiochene Frontier Lord's Family", "{=crus_cc_antiochene_noble_desc}Your family holds a frontier fief near Antioch, its lords accustomed to skirmishing against Turkoman raiders and Muslim neighbors alike.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 1000,
                    IsCrusaderCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "crus_opt_genoese_merchant", "{=crus_cc_genoese_merchant_name}Genoese Trading Family", "{=crus_cc_genoese_merchant_desc}Your family are Genoese merchants who settled in Antioch's harbor quarter, growing wealthy on the trade between Outremer and Italy.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsCrusaderCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "crus_opt_hospitaller", "{=crus_cc_hospitaller_name}Line of a Hospitaller Serving Brother", "{=crus_cc_hospitaller_desc}Your father served the Knights Hospitaller as an armsman, tending the sick and fighting the Crusader states' many enemies.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 700,
                    IsCrusaderCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "crus_opt_pilgrim_settler", "{=crus_cc_pilgrim_settler_name}Poor Pilgrim Settler Family", "{=crus_cc_pilgrim_settler_desc}Your family walked to the Holy Land on the First Crusade and stayed, scraping out a modest living on the Levantine frontier.",
                    new[] { DefaultSkills.Scouting, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                    IsCrusaderCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "crus_child_squire_training", "{=crus_cc_squire_training_name}You Trained with Wooden Swords in a Norman Bailey", "{=crus_cc_squire_training_desc}You sparred with wooden swords in your family's bailey, drilled by an old man-at-arms in the Norman style.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);

                AddOption(childMenu, "crus_child_levantine", "{=crus_cc_levantine_name}You Grew Up Speaking Both French and Arabic", "{=crus_cc_levantine_desc}Raised among Levantine servants and neighbors, you grew up fluent in Arabic as well as your family's own French.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Trade }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);

                AddOption(childMenu, "crus_child_church_school", "{=crus_cc_church_school_name}You Learned Latin and Scripture at a Church School", "{=crus_cc_church_school_desc}At a parish church school you learned Latin letters and scripture from a Levantine-born priest.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);

                AddOption(childMenu, "crus_child_market", "{=crus_cc_market_name}You Learned Trade in the Markets of Antioch", "{=crus_cc_market_desc}In the crowded markets of Antioch you learned to haggle among Franks, Armenians, and Greeks alike.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "crus_youth_squire", "{=crus_cc_squire_name}You Served as a Squire to a Landed Knight", "{=crus_cc_squire_desc}You served as a squire to a landed knight, tending his horse and arms and learning the ways of mounted war.",
                    new[] { DefaultSkills.Riding, DefaultSkills.OneHanded, DefaultSkills.Polearm }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);

                AddOption(youthMenu, "crus_youth_garrison", "{=crus_cc_garrison_name}You Stood Garrison Duty in a Frontier Castle", "{=crus_cc_garrison_desc}You stood long watches on a frontier castle's walls, crossbow in hand, wary of raids from the surrounding hills.",
                    new[] { DefaultSkills.Athletics, DefaultSkills.Crossbow, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);

                AddOption(youthMenu, "crus_youth_scribe", "{=crus_cc_scribe_name}You Studied as a Scribe in a Cathedral Chapter", "{=crus_cc_scribe_desc}You studied under the canons of a cathedral chapter, learning to read, write, and keep accounts.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 0, 0, IsCrusaderCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "crus_car_knighted", "{=crus_cc_knighted_name}You Were Knighted and Joined a Lord's Retinue", "{=crus_cc_knighted_desc}You were girded with a sword and knighted, taking your place in a lord's mounted retinue.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsCrusaderCultureSelected, null);

                AddOption(careerMenu, "crus_car_templar", "{=crus_cc_templar_name}You Joined the Templars as a Serving Brother", "{=crus_cc_templar_desc}You took vows as a serving brother of the Templar order, garrisoning its castles and escorting pilgrims.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Crossbow, DefaultSkills.Athletics }, 15, null, 0, 15, 0, IsCrusaderCultureSelected, null);

                AddOption(careerMenu, "crus_car_italian_trade", "{=crus_cc_italian_trade_name}You Traded Alongside the Italian Merchant Fleets", "{=crus_cc_italian_trade_desc}You worked alongside Genoese and Venetian merchant fleets, learning the business that keeps the Crusader states alive.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 15, 0, IsCrusaderCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "crus_deed_siege_held", "{=crus_cc_siege_held_name}You Held a Wall During a Great Siege", "{=crus_cc_siege_held_desc}You held your section of the wall through a great siege, fighting off assault after assault.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics }, 15, null, 0, 30, 0, IsCrusaderCultureSelected, null);

                AddOption(deedMenu, "crus_deed_charge", "{=crus_cc_charge_name}You Led a Decisive Cavalry Charge", "{=crus_cc_charge_desc}You led a decisive cavalry charge that broke the enemy line at the moment it mattered most.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Riding }, 15, null, 0, 25, 0, IsCrusaderCultureSelected, null);

                AddOption(deedMenu, "crus_deed_ransom", "{=crus_cc_ransom_name}You Negotiated a Captured Lord's Ransom", "{=crus_cc_ransom_desc}You negotiated the ransom of a captured lord, earning both gratitude and a reputation for shrewdness.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsCrusaderCultureSelected, null);
            }
        }

        // ============================== CILICIAN ARMENIA (Culture.battania) ==============================

        private void InjectArmenianNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "arm_opt_naxarar", "{=arm_cc_naxarar_name}Naxarar Noble Family", "{=arm_cc_naxarar_desc}You come from an ancient Naxarar house, Armenia's hereditary feudal nobility, holding your own mountain fief and vassal warriors.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Polearm }, 15, DefaultCharacterAttributes.Vigor, 1, 10, 1300,
                    IsArmenianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "arm_opt_azat", "{=arm_cc_azat_name}Azat Landholding Warrior Family", "{=arm_cc_azat_desc}Your family are azat, free landholding warriors who owe cavalry service to their Naxarar lord in exchange for their mountain lands.",
                    new[] { DefaultSkills.Riding, DefaultSkills.OneHanded }, 15, DefaultCharacterAttributes.Control, 1, 15, 800,
                    IsArmenianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "arm_opt_silk_merchant", "{=arm_cc_silk_merchant_name}Armenian Silk Road Merchant Family", "{=arm_cc_silk_merchant_desc}Your family trades silk and goods along routes stretching from Cilicia to Persia, part of the great medieval Armenian merchant network.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                    IsArmenianCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "arm_opt_church", "{=arm_cc_church_name}Armenian Church Clergy Family", "{=arm_cc_church_desc}You were raised in a family devoted to the Armenian Apostolic Church, copying scripture and keeping the faith alive in the mountains.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 900,
                    IsArmenianCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));

                AddOption(parentMenu, "arm_opt_herder", "{=arm_cc_herder_name}Mountain Herder Family", "{=arm_cc_herder_desc}You grew up herding goats along the steep slopes of the Taurus mountains, learning to survive where the land offers little.",
                    new[] { DefaultSkills.Athletics, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Endurance, 1, 10, 500,
                    IsArmenianCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "arm_child_mountain_ride", "{=arm_cc_mountain_ride_name}You Rode Mountain Trails from a Young Age", "{=arm_cc_mountain_ride_desc}You rode narrow mountain trails from a young age, learning balance and nerve along the cliff edges of the Taurus.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);

                AddOption(childMenu, "arm_child_falconry", "{=arm_cc_falconry_name}You Learned Falconry in the Highlands", "{=arm_cc_falconry_desc}You learned to fly falcons and track game in the highlands above your family's home.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);

                AddOption(childMenu, "arm_child_monastery", "{=arm_cc_monastery_name}You Learned Armenian Script at a Monastery", "{=arm_cc_monastery_desc}You learned to read and write the Armenian script from monks at a nearby monastery.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);

                AddOption(childMenu, "arm_child_trade", "{=arm_cc_trade_name}You Learned Trade Along the Mountain Roads", "{=arm_cc_trade_desc}You learned to bargain and weigh goods along the mountain trade roads with your family's caravans.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "arm_youth_princely_guard", "{=arm_cc_princely_guard_name}You Joined the Prince's Household Guard", "{=arm_cc_princely_guard_desc}You joined the Cilician prince's household guard, training in spear and sword alongside his sworn men.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);

                AddOption(youthMenu, "arm_youth_border_watch", "{=arm_cc_border_watch_name}You Watched the Mountain Passes as a Border Scout", "{=arm_cc_border_watch_desc}You watched the mountain passes as a scout, ready to warn of Byzantine or Seljuk incursions.",
                    new[] { DefaultSkills.Scouting, DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);

                AddOption(youthMenu, "arm_youth_monastery_school", "{=arm_cc_monastery_school_name}You Studied at a Renowned Monastery School", "{=arm_cc_monastery_school_desc}You studied theology and letters at one of Cilicia's renowned monastery schools.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsArmenianCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "arm_car_ayrudzi", "{=arm_cc_ayrudzi_name}You Were Chosen for the Elite Ayrudzi Cavalry", "{=arm_cc_ayrudzi_desc}Your skill in the saddle earned you a place among the Ayrudzi, Cilicia's elite cavalry guard.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsArmenianCultureSelected, null);

                AddOption(careerMenu, "arm_car_naxarar_retainer", "{=arm_cc_naxarar_retainer_name}You Became a Naxarar Lord's Trusted Retainer", "{=arm_cc_naxarar_retainer_desc}You became a trusted retainer to a Naxarar lord, advising him and leading his men in the field.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Charm, DefaultSkills.Tactics }, 15, null, 0, 15, 0, IsArmenianCultureSelected, null);

                AddOption(careerMenu, "arm_car_intermediary", "{=arm_cc_intermediary_name}You Served as an Interpreter Between Christian and Muslim Courts", "{=arm_cc_intermediary_desc}Fluent in several tongues, you served as an interpreter between Christian and Muslim courts, a role Armenians were often trusted with.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 15, 0, IsArmenianCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "arm_deed_fortress_defense", "{=arm_cc_fortress_defense_name}You Held a Mountain Fortress Against Siege", "{=arm_cc_fortress_defense_desc}You held a mountain fortress against a determined siege until the besiegers finally gave up and withdrew.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics }, 15, null, 0, 30, 0, IsArmenianCultureSelected, null);

                AddOption(deedMenu, "arm_deed_battle", "{=arm_cc_battle_name}You Fought with Distinction Against Byzantine or Seljuk Forces", "{=arm_cc_battle_desc}You fought with distinction in a pitched battle against encroaching Byzantine or Seljuk forces.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 25, 0, IsArmenianCultureSelected, null);

                AddOption(deedMenu, "arm_deed_negotiation", "{=arm_cc_negotiation_name}You Negotiated a Vital Truce for Your People", "{=arm_cc_negotiation_desc}You negotiated a vital truce that spared your people from a war they could not have won.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsArmenianCultureSelected, null);
            }
        }

        // ============================== KARA-KHANID KHANATE (Culture.khuzait) ==============================

        private void InjectKaraKhanidNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "krkh_opt_khan_lineage", "{=krkh_cc_khan_lineage_name}Distant Branch of a Khanal Lineage", "{=krkh_cc_khan_lineage_desc}You descend from a minor branch of a Kara-Khanid ruling house, raised amid the customs and rivalries of the khan's court.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Riding }, 15, DefaultCharacterAttributes.Vigor, 1, 10, 1300,
                    IsKaraKhanidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "krkh_opt_tarkhan", "{=krkh_cc_tarkhan_name}Tarkhan Noble Family", "{=krkh_cc_tarkhan_desc}Your family holds Tarkhan rank, a tax-exempt warrior nobility owing horsemen to the Khan in exchange for their steppe privileges.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Control, 1, 15, 1000,
                    IsKaraKhanidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "krkh_opt_silk_merchant", "{=krkh_cc_silk_merchant_name}Samarkand Silk Road Merchant Family", "{=krkh_cc_silk_merchant_desc}Your family trades in the bazaars of Samarkand and Bukhara, at the very heart of the Silk Road's wealth.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsKaraKhanidCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "krkh_opt_madrasa", "{=krkh_cc_madrasa_name}Madrasa Scholar Family", "{=krkh_cc_madrasa_desc}Your family are scholars at one of Samarkand's madrasas, part of the wave of learning that followed the Kara-Khanids' conversion to Islam.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 900,
                    IsKaraKhanidCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));

                AddOption(parentMenu, "krkh_opt_nomad", "{=krkh_cc_nomad_name}Steppe Nomad Family", "{=krkh_cc_nomad_desc}You grew up following the herds across the open steppe, in the same way of life your Karluk and Yaghma ancestors once lived.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                    IsKaraKhanidCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "krkh_child_horseback", "{=krkh_cc_horseback_name}You Rode Before You Could Properly Walk", "{=krkh_cc_horseback_desc}You rode before you could properly walk, as is the way of every steppe-born child.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);

                AddOption(childMenu, "krkh_child_archery", "{=krkh_cc_archery_name}You Trained in Horse-Archery from Childhood", "{=krkh_cc_archery_desc}You trained in horse-archery from childhood, shooting at targets from a moving pony.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);

                AddOption(childMenu, "krkh_child_madrasa", "{=krkh_cc_madrasa_child_name}You Studied Quran and Arabic at a Mosque School", "{=krkh_cc_madrasa_child_desc}You studied the Quran and Arabic letters at a mosque school in one of the Khanate's settled cities.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);

                AddOption(childMenu, "krkh_child_bazaar", "{=krkh_cc_bazaar_name}You Learned Trade in the Bazaars of Samarkand", "{=krkh_cc_bazaar_desc}You learned to weigh silk and haggle over prices in the crowded bazaars of Samarkand.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "krkh_youth_khan_guard", "{=krkh_cc_khan_guard_name}You Joined the Khan's Household Guard", "{=krkh_cc_khan_guard_desc}You joined the Khan's household guard, training with spear and sword among his sworn horsemen.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);

                AddOption(youthMenu, "krkh_youth_horse_archer", "{=krkh_cc_horse_archer_name}You Rode as a Horse-Archer Skirmisher", "{=krkh_cc_horse_archer_desc}You rode as a horse-archer skirmisher, harassing rivals with volleys before wheeling away.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);

                AddOption(youthMenu, "krkh_youth_madrasa_student", "{=krkh_cc_madrasa_student_name}You Studied at Ibrahim Tamgach Khan's Madrasa in Samarkand", "{=krkh_cc_madrasa_student_desc}You studied law and statecraft at the great madrasa Ibrahim Tamgach Khan founded in Samarkand.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsKaraKhanidCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "krkh_car_tarkhan_cavalry", "{=krkh_cc_tarkhan_cavalry_name}You Joined the Elite Tarkhan Heavy Cavalry", "{=krkh_cc_tarkhan_cavalry_desc}Your prowess in the saddle earned you a place among the Khanate's elite Tarkhan heavy cavalry.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsKaraKhanidCultureSelected, null);

                AddOption(careerMenu, "krkh_car_caravan_trader", "{=krkh_cc_caravan_trader_name}You Led Caravans Along the Silk Road", "{=krkh_cc_caravan_trader_desc}You led trade caravans along the Silk Road, learning to survive both bandits and hard bargains.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Riding, DefaultSkills.Steward }, 15, null, 0, 15, 0, IsKaraKhanidCultureSelected, null);

                AddOption(careerMenu, "krkh_car_court_scribe", "{=krkh_cc_court_scribe_name}You Served as a Scribe at the Khan's Court", "{=krkh_cc_court_scribe_desc}You served as a scribe at the Khan's court, drafting decrees and keeping the treasury's accounts.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsKaraKhanidCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "krkh_deed_skirmish_won", "{=krkh_cc_skirmish_won_name}You Won a Decisive Horse-Archery Skirmish", "{=krkh_cc_skirmish_won_desc}You won a decisive skirmish, your arrows breaking the enemy's nerve before they ever closed to melee.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 30, 0, IsKaraKhanidCultureSelected, null);

                AddOption(deedMenu, "krkh_deed_caravan_defense", "{=krkh_cc_caravan_defense_name}You Defended a Caravan from Raiders", "{=krkh_cc_caravan_defense_desc}You defended a Silk Road caravan from raiders, saving its goods and the lives of its merchants.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Bow }, 15, null, 0, 25, 0, IsKaraKhanidCultureSelected, null);

                AddOption(deedMenu, "krkh_deed_seljuk_rival", "{=krkh_cc_seljuk_rival_name}You Distinguished Yourself Against Seljuk Rivals", "{=krkh_cc_seljuk_rival_desc}You distinguished yourself in a clash against Seljuk rivals contesting the Khanate's borders.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsKaraKhanidCultureSelected, null);
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

        private static bool IsCrusaderCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "vlandia";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsArmenianCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "battania";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsKaraKhanidCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "khuzait";
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
