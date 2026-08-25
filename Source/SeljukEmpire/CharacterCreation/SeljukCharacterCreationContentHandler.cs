using System;
using System.Reflection;
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
    public class SeljukCharacterCreationContentHandler : ICharacterCreationContentHandler
    {
        private static readonly PropertyInfo GoldProp = typeof(NarrativeMenuOptionArgs).GetProperty("GoldToAdd", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private bool _isOptionsInjected;

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
                    AddOption(parentMenu, "seljuk_opt_bey", "{=cc_opt_bey_name}Oğuz Boyu Beyzadesi", "{=cc_opt_bey_desc}Bozkırın soylu Türkmen beylerinin soyundan geliyorsun. Çocukluğun beylik otağında töre, at ve kılıç eğitimiyle geçti.",
                        new[] { DefaultSkills.Riding, DefaultSkills.Leadership }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 1000,
                        mgr => SetParentOccupation(mgr, "Retainer"));

                    AddOption(parentMenu, "seljuk_opt_scholar", "{=cc_opt_scholar_name}Nizamiye Müderrisi Evladı", "{=cc_opt_scholar_desc}Ailen Nizamiye medreselerinde fıkıh, idare ve devlet nizamı öğreten saygın alimlerdendir.",
                        new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                        mgr => SetParentOccupation(mgr, "Merchant"));

                    AddOption(parentMenu, "seljuk_opt_ahi", "{=cc_opt_ahi_name}Ahi Demir Ustası Çırağı", "{=cc_opt_ahi_desc}Kayseri ve Konya'nın usta Ahi demircileri arasında, çeliğe su vermeyi ve helal rızkı öğrenerek büyüdün.",
                        new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Endurance, 1, 10, 800,
                        mgr => SetParentOccupation(mgr, "Farmer"));

                    AddOption(parentMenu, "seljuk_opt_ghulam", "{=cc_opt_ghulam_name}Sultan Hassa Gulamı Soyu", "{=cc_opt_ghulam_desc}Baban saray kışlasında Sultanın sadık Demir Maskeli Hassa süvarilerindendi. Kanında askeri disiplin var.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 600,
                        mgr => SetParentOccupation(mgr, "Retainer"));

                    AddOption(parentMenu, "seljuk_opt_nomad", "{=cc_opt_nomad_name}Uç Boyu Türkmen Göçeri", "{=cc_opt_nomad_desc}Toroslar ve Erzurum yaylalarında keçi güdüp şahin uçurarak doğanın içinde çetin şartlarda yetiştin.",
                        new[] { DefaultSkills.Scouting, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                        mgr => SetParentOccupation(mgr, "Herder"));
                }

                // STAGE 1: CHILDHOOD
                if (menus.Count > 1)
                {
                    var childMenu = menus[1];
                    AddOption(childMenu, "seljuk_child_steppes", "{=cc_child_steppes_name}Bozkırda At Üstünde Büyüdün", "{=cc_child_steppes_desc}Henüz yürümeyi öğrenmeden ata bindin; bozkır rüzgarı sana eyer üstünde dengeyi ve yay çekmeyi öğretti.",
                        new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_ikta", "{=cc_child_ikta_name}İkta Kışlasında Güreş ve Kılıç Eğitimi", "{=cc_child_ikta_desc}Köydeki İkta askerleriyle talim alanlarında güreştin, tahta kılıçlarla ilk vuruş tekniklerini kavradın.",
                        new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_caravan", "{=cc_child_caravan_name}İpek Yolu Kervanlarında Yıldızları İzledin", "{=cc_child_caravan_desc}Kervansaraylarda konaklayan tüccarların hikayelerini dinledin, çöl ve bozkır yollarını ezberledin.",
                        new[] { DefaultSkills.Scouting, DefaultSkills.Tactics }, 15, null, 0, 0, 0, null);

                    AddOption(childMenu, "seljuk_child_ahi", "{=cc_child_ahi_name}Ahi Ocağında Körük Çektin", "{=cc_child_ahi_desc}Ocağın ateşini harladın, demirci ustalarının elinde şekillenen çeliğin sırrına vakıf oldun.",
                        new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, null, 0, 0, 0, null);
                }

                // STAGE 2: YOUTH / EDUCATION
                if (menus.Count > 2)
                {
                    var youthMenu = menus[2];
                    AddOption(youthMenu, "seljuk_youth_nizamiye", "{=cc_youth_nizamiye_name}Nizamiye Medresesi'nde İlim Tahsil Ettin", "{=cc_youth_nizamiye_desc}Bağdat ve İsfahan'daki Nizamiye medreselerinde Siyasetname, fıkıh ve diplomasi okudun.",
                        new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_akinji", "{=cc_youth_akinji_name}Uç Beyliği Akıncı Çerisine Katıldın", "{=cc_youth_akinji_desc}Danişmend ve Artuk beylerinin sınır akınlarında düşman karakollarını yıpratan öncü süvarisi oldun.",
                        new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Scouting }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_subasi", "{=cc_youth_subasi_name}Şehir Subaşısı Emrinde Asayişi Korudun", "{=cc_youth_subasi_desc}Konya ve Sivas sokaklarında nizamı sağladın, mızrak ve kalkan talimlerinde öne çıktın.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, null);

                    AddOption(youthMenu, "seljuk_youth_caravan", "{=cc_youth_caravan_name}İpek Yolu Kervan Muhafızlığı Yaptın", "{=cc_youth_caravan_desc}Diyarbekir'den Alâiye limanına uzanan güzergahta haydutlara karşı kervanları korudun.",
                        new[] { DefaultSkills.Trade, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 0, 0, null);
                }

                // STAGE 3: CAREER / EARLY ADULTHOOD
                if (menus.Count > 3)
                {
                    var careerMenu = menus[3];
                    AddOption(careerMenu, "seljuk_car_ghulam", "{=cc_car_ghulam_name}Sultanın Hassa Gulam Kıtasına Seçildin", "{=cc_car_ghulam_desc}Üstün savaş yeteneklerin sayesinde Sultan Alp Arslan'ın şahsi muhafız alayına kabul edildin.",
                        new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 20, 0, null);

                    AddOption(careerMenu, "seljuk_car_danismend", "{=cc_car_danismend_name}Danişmend Gazi Emrinde Uç Alpi Oldun", "{=cc_car_danismend_desc}Kayseri ve Sivas dağlarında Bizans sınır kalelerine yapılan gaza seferlerinde ön safta çarpıştın.",
                        new[] { DefaultSkills.Bow, DefaultSkills.Tactics, DefaultSkills.Riding }, 15, null, 0, 15, 0, null);

                    AddOption(careerMenu, "seljuk_car_ahi", "{=cc_car_ahi_name}Ahi Evran Ocağı Yiğitbaşısı Oldun", "{=cc_car_ahi_desc}Esnafın hakkını savunan, şehir kuşatmasında halkı teşkilatlandıran yiğitbaşı kuşağını bağladın.",
                        new[] { DefaultSkills.TwoHanded, DefaultSkills.Leadership, DefaultSkills.Crafting }, 15, null, 0, 15, 0, null);

                    AddOption(careerMenu, "seljuk_car_caka", "{=cc_car_caka_name}Çaka Bey'in Deniz Akıncısı Oldun", "{=cc_car_caka_desc}Alâiye tersanesinde inşa edilen gemilerle Ege ve Akdeniz sahillerinde düşman donanmalarını vurdun.",
                        new[] { DefaultSkills.Crossbow, DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 15, 0, null);
                }

                // STAGE 4: DEFINING DEED
                if (menus.Count > 4)
                {
                    var deedMenu = menus[4];
                    AddOption(deedMenu, "seljuk_deed_siege", "{=cc_deed_siege_name}Kuşatmayı Yarıp Orduya Haber Ulaştırdın", "{=cc_deed_siege_desc}Düşmanın etrafını sardığı kaleden gece karanlığında gizlice sızıp Sultanın ordusuna imdat çağrısı götürdün.",
                        new[] { DefaultSkills.Tactics, DefaultSkills.Scouting }, 15, null, 0, 25, 0, null);

                    AddOption(deedMenu, "seljuk_deed_banner", "{=cc_deed_banner_name}Meydan Savaşında Düşman Sancağını Devirdin", "{=cc_deed_banner_desc}Ok yağmuru altında düşman başkumandanının muhafızlarını yarıp sancağını yere serdin.",
                        new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, null);

                    AddOption(deedMenu, "seljuk_deed_innocent", "{=cc_deed_innocent_name}Mazlumları Zulümden Kurtardın", "{=cc_deed_innocent_desc}Harami çetesinin bastığı bir Türkmen obasını tek başına cansiperane müdafaa ettin.",
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

        private static void AddOption(
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
                    if (goldToAdd > 0)
                    {
                        try
                        {
                            if (GoldProp != null && GoldProp.CanWrite)
                            {
                                GoldProp.SetValue(args, goldToAdd, null);
                            }
                        }
                        catch (Exception) { }
                    }
                },
                IsSeljukCultureSelected, // Only visible when the player has picked Culture.seljuk
                onSelect,
                null); // Let Native's ApplyFinalEffects apply Args cleanly without duplicate crash

            menu.AddNarrativeMenuOption(option);
        }

        public void OnStageCompleted(CharacterCreationStageBase stage) { }
        public void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager) { }
    }
}
