using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SeljukEmpire.CharacterCreation
{
    /// <summary>
    /// Injects authentic Seljuk & Turkic character creation narrative options into Mount & Blade II: Bannerlord.
    /// Safely integrated with Native CharacterCreationManager, 3D parent model equipment, and effect pipelines.
    /// </summary>
    /// <remarks>
    /// Was registered by polling GameStateManager.Current.ActiveState on SubModule.OnApplicationTick and
    /// calling CharacterCreationManager.RegisterCharacterCreationContentHandler one frame after
    /// CharacterCreationState activated. But CharacterCreationState's constructor builds
    /// CharacterCreationManager synchronously, and THAT constructor already iterates every registered
    /// handler's InitializeContent/AfterInitializeContent before the object even exists for the SubModule
    /// tick to find - so the tick-based registration always ran one frame too late, after every stage
    /// (culture select, family background, ...) had already been built without our content. Native's own
    /// TaleWorlds.CampaignSystem.CampaignBehaviors.CharacterCreationCampaignBehavior avoids this by being a
    /// CampaignBehaviorBase that self-registers inside CampaignEvents.OnCharacterCreationInitializedEvent,
    /// which fires mid-constructor before those loops run - this class now follows the same pattern.
    /// </remarks>
    public class SeljukCharacterCreationContentHandler : CampaignBehaviorBase, ICharacterCreationContentHandler
    {
        // NarrativeMenuOptionArgs.GoldToAdd has no public setter (only AffectedSkills/RenownToAdd/
        // Attribute/etc. have SetXxx methods) - Native itself never grants gold through a narrative
        // option, it's only ever read back into the option's own preview tooltip text. Setting it via
        // reflection from the per-option "preview args" callback (invoked on every hover/click while
        // browsing, not just on the option the player ends up keeping) was firing prematurely and
        // showing an unexpected gold notification before the player had actually chosen anything.
        // Gold bonuses are now tracked here and granted exactly once, for the option actually still
        // selected when character creation finishes (OnCharacterCreationFinalize).
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

                // Native's CharacterCreationCampaignBehavior only ever adds its own hardcoded 6 cultures
                // (aserai/battania/empire/khuzait/sturgia/vlandia) to the culture-select stage - Culture.seljuk
                // was never in that list, so it could never appear there no matter what is_main_culture is
                // set to in seljuk_culture.xml. Add it explicitly here.
                var seljukCulture = Game.Current?.ObjectManager?.GetObject<CultureObject>("seljuk");
                if (seljukCulture != null)
                {
                    characterCreationManager.CharacterCreationContent.AddCharacterCreationCulture(seljukCulture, 1, 10);
                }
            }
            catch (Exception)
            {
                // Safety
            }
        }

        public void InitializeContent(CharacterCreationManager characterCreationManager)
        {
            InjectSeljukNarratives(characterCreationManager);
        }

        public void AfterInitializeContent(CharacterCreationManager characterCreationManager)
        {
            InjectSeljukNarratives(characterCreationManager);
        }

        public void InjectSeljukNarratives(CharacterCreationManager characterCreationManager)
        {
            if (characterCreationManager == null || characterCreationManager.NarrativeMenus == null || _isOptionsInjected) return;

            try
            {
                var menus = characterCreationManager.NarrativeMenus;
                if (menus.Count == 0) return;

                // STAGE 0: PARENTS / HERITAGE (Family: You were born as...)
                if (menus.Count > 0)
                {
                    var parentMenu = menus[0];
                    AddOption(parentMenu, "seljuk_opt_bey", "{=cc_opt_bey_name}Son of an Oghuz Steppe Chieftain", "{=cc_opt_bey_desc}Born into noble Turcoman lineage. Your youth was spent in the tribal tent learning horseback riding, leadership, and archery.",
                        new[] { DefaultSkills.Riding, DefaultSkills.Leadership }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 1000,
                        mgr => SetParentOccupation(mgr, "Retainer"));

                    AddOption(parentMenu, "seljuk_opt_scholar", "{=cc_opt_scholar_name}Child of a Nizamiyya Scholar", "{=cc_opt_scholar_desc}Raised among respected professors and viziers, mastering law, economics, and imperial statecraft.",
                        new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                        mgr => SetParentOccupation(mgr, "Merchant"));

                    AddOption(parentMenu, "seljuk_opt_ahi", "{=cc_opt_ahi_name}Ahi Master Blacksmith Apprentice", "{=cc_opt_ahi_desc}Grew up in the bustling guilds of Konya and Kayseri, learning the secrets of forged steel and ethical trade.",
                        new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Endurance, 1, 10, 800,
                        mgr => SetParentOccupation(mgr, "Farmer"));

                    AddOption(parentMenu, "seljuk_opt_ghulam", "{=cc_opt_ghulam_name}Scion of an Imperial Ghulam", "{=cc_opt_ghulam_desc}Your father was an elite Iron-Masked bodyguard to the Sultan. Rigorous military discipline runs in your blood.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 600,
                        mgr => SetParentOccupation(mgr, "Retainer"));

                    AddOption(parentMenu, "seljuk_opt_nomad", "{=cc_opt_nomad_name}Border Turcoman Nomad", "{=cc_opt_nomad_desc}Survived the rugged mountains and vast steppes of Erzurum, learning the cunning ways of survival and scouting.",
                        new[] { DefaultSkills.Scouting, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                        mgr => SetParentOccupation(mgr, "Herder"));
                }

                // STAGE 1: CHILDHOOD
                if (menus.Count > 1)
                {
                    var childMenu = menus[1];
                    AddOption(childMenu, "seljuk_child_steppes", "{=cc_child_steppes_name}Rode on Horseback in the Open Steppes", "{=cc_child_steppes_desc}You rode before you could properly walk. The steppe winds taught you perfect balance and composite bow shooting.",
                        new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_ikta", "{=cc_child_ikta_name}Wrestled in the Iqta Garrison Grounds", "{=cc_child_ikta_desc}Sparred with provincial garrison soldiers and learned swordplay fundamentals from veterans.",
                        new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_caravan", "{=cc_child_caravan_name}Watched the Stars along the Silk Road", "{=cc_child_caravan_desc}Listened to merchant tales at fortified caravanserais, memorizing secret passes and desert routes.",
                        new[] { DefaultSkills.Scouting, DefaultSkills.Tactics }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_ahi", "{=cc_child_ahi_name}Pumped the Bellows at the Ahi Forge", "{=cc_child_ahi_desc}Stoked the blazing embers of the anvil, learning trade integrity and metal forging techniques.",
                        new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, null, 0, 0, 0, null);
                }

                // STAGE 2: YOUTH / EDUCATION
                if (menus.Count > 2)
                {
                    var youthMenu = menus[2];
                    AddOption(youthMenu, "seljuk_youth_nizamiye", "{=cc_youth_nizamiye_name}Studied Governance at the Nizamiyya Madrasa", "{=cc_youth_nizamiye_desc}Read Nizam al-Mulk's Siyasatnama, mastering rhetoric, logistics, and diplomatic negotiation.",
                        new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_akinji", "{=cc_youth_akinji_name}Joined the Border Akinji Light Cavalry", "{=cc_youth_akinji_desc}Rode in dangerous frontier skirmishes, demoralizing hostile outposts with swift arrow volleys.",
                        new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Scouting }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_subasi", "{=cc_youth_subasi_name}Maintained Law and Order with City Subashis", "{=cc_youth_subasi_desc}Patrolled city streets, drilling with heavy spears and organized infantry shield formations.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_caravan", "{=cc_youth_caravan_name}Escorted Silk Road Caravans", "{=cc_youth_caravan_desc}Guarded trade convoys across perilous mountain gorges from bandit ambushes.",
                        new[] { DefaultSkills.Trade, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 0, 0, null);
                }

                // STAGE 3: CAREER / EARLY ADULTHOOD
                if (menus.Count > 3)
                {
                    var careerMenu = menus[3];
                    AddOption(careerMenu, "seljuk_car_ghulam", "{=cc_car_ghulam_name}Inducted into the Sultan's Imperial Ghulam Guard", "{=cc_car_ghulam_desc}Recognized for exceptional combat prowess and enlisted directly into Sultan Alp Arslan's elite retinue.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 20, 0, null);

                    AddOption(careerMenu, "seljuk_car_danismend", "{=cc_car_danismend_name}Rode as a Frontier Gazi with Danishmend Bey", "{=cc_car_danismend_desc}Spearheaded holy campaigns in the rugged highlands, capturing key border forts.",
                        new[] { DefaultSkills.Bow, DefaultSkills.Tactics, DefaultSkills.Riding }, 15, null, 0, 15, 0, null);

                    AddOption(careerMenu, "seljuk_car_ahi", "{=cc_car_ahi_name}Appointed as Yigitbashi in the Ahi Order", "{=cc_car_ahi_desc}Led urban volunteer watches and safeguarded fair trade across Anatolian markets.",
                        new[] { DefaultSkills.TwoHanded, DefaultSkills.Leadership, DefaultSkills.Crafting }, 15, null, 0, 15, 0, null);

                    AddOption(careerMenu, "seljuk_car_caka", "{=cc_car_caka_name}Sailed as a Naval Raider with Chaka Bey", "{=cc_car_caka_desc}Boarded hostile warships across the Aegean and Mediterranean under the First Turkish Admiral.",
                        new[] { DefaultSkills.Crossbow, DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 15, 0, null);
                }

                // STAGE 4: DEFINING DEED
                if (menus.Count > 4)
                {
                    var deedMenu = menus[4];
                    AddOption(deedMenu, "seljuk_deed_siege", "{=cc_deed_siege_name}Broke through a Siege to Deliver Crucial Intelligence", "{=cc_deed_siege_desc}Slipped past hostile perimeter sentries in the dead of night to rally the Sultan's relief army.",
                        new[] { DefaultSkills.Tactics, DefaultSkills.Scouting }, 15, null, 0, 25, 0, null);

                    AddOption(deedMenu, "seljuk_deed_banner", "{=cc_deed_banner_name}Captured the Enemy Commander's Standard", "{=cc_deed_banner_desc}Charged through arrow fire, cut down the elite bodyguard, and brought down the hostile banner.",
                        new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, null);

                    AddOption(deedMenu, "seljuk_deed_innocent", "{=cc_deed_innocent_name}Defended Defenseless Nomads from Brigands", "{=cc_deed_innocent_desc}Single-handedly held a narrow pass against raiders, saving innocent camp families from slaughter.",
                        new[] { DefaultSkills.Charm, DefaultSkills.Athletics }, 15, null, 0, 20, 0, null);
                }

                _isOptionsInjected = true;
            }
            catch (Exception)
            {
                // Safe degradation
            }
        }

        private static bool IsSeljukCultureSelected(CharacterCreationManager manager)
        {
            try
            {
                return manager?.CharacterCreationContent?.SelectedCulture?.StringId == "seljuk";
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
                IsSeljukCultureSelected, // Only visible when the player has picked Culture.seljuk
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
