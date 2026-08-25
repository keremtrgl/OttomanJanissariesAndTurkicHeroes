using System;
using System.Reflection;
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
        private static readonly PropertyInfo GoldProp = typeof(NarrativeMenuOptionArgs).GetProperty("GoldToAdd", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                AddOption(parentMenu, "byz_opt_senatorial", "{=byz_cc_senatorial_name}Konstantinopolis Senatocu Ailesi", "{=byz_cc_senatorial_desc}Ailen İmparatorluk başkentinde toprak sahibi dynatoi sınıfındandır; sarayın entrika ve protokolünü beşikten öğrendin.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "byz_opt_akritas", "{=byz_cc_akritas_name}Anadolu Akritas Sınır Ailesi", "{=byz_cc_akritas_desc}Toros ve Fırat sınırında İmparatorluğu Türkmen akınlarına karşı koruyan akritai savaşçı ailesindensin.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 600,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "byz_opt_thematic", "{=byz_cc_thematic_name}Anadolu Thema Askeri Toprak Sahibi Ailesi", "{=byz_cc_thematic_desc}Baban stratiotes olarak thema ordusuna kendi atı ve zırhıyla hizmet eden askeri çiftlik sahibiydi.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 700,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "byz_opt_merchant", "{=byz_cc_merchant_name}Selanik Liman Tüccarı Ailesi", "{=byz_cc_merchant_desc}İmparatorluğun ikinci büyük şehri Selanik'in limanında ipek ve baharat ticareti yapan zengin bir ailede büyüdün.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1000,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "byz_opt_monastic", "{=byz_cc_monastic_name}Manastır Kütüphanesi Kâtip Ailesi", "{=byz_cc_monastic_desc}Ailen manastır kütüphanelerinde eski Yunan ve Roma eserlerini istinsah eden kâtiplerdendi; ilim beşiğindi.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 900,
                    IsByzantineCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "byz_child_hippodrome", "{=byz_cc_hippodrome_name}Hipodrom Yarışlarını İzleyerek Büyüdün", "{=byz_cc_hippodrome_desc}Konstantinopolis'in ünlü Hipodrom'unda at yarışlarını izleyip mahalle takımlarının tutkusunu içinde hissettin.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_akritic_songs", "{=byz_cc_akritic_songs_name}Sınırda Akritik Destanlar Dinleyerek Büyüdün", "{=byz_cc_akritic_songs_desc}Sınır köylerinde yaşlıların anlattığı akritai destanlarıyla büyüdün, ok atmayı erkenden öğrendin.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_church_school", "{=byz_cc_church_school_name}Kilise Okulunda Yunanca ve İlahiyat Öğrendin", "{=byz_cc_church_school_desc}Mahalle kilisesinin okulunda Yunan alfabesini, kutsal metinleri ve ilahileri ezberledin.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(childMenu, "byz_child_market", "{=byz_cc_market_name}Mese Caddesi Pazarında Ticareti Öğrendin", "{=byz_cc_market_desc}Konstantinopolis'in ana caddesi Mese'deki dükkanlarda tüccarların yanında ticaret inceliklerini öğrendin.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "byz_youth_varangian", "{=byz_cc_varangian_name}Varanjyalı Muhafızlarla Talim Yaptın", "{=byz_cc_varangian_desc}İmparatorluk sarayının ünlü Varanjyalı Muhafızları arasında balta ve kılıç talimi gördün.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(youthMenu, "byz_youth_thematic_army", "{=byz_cc_thematic_army_name}Anadolu Thema Ordusuna Katıldın", "{=byz_cc_thematic_army_desc}Anadolu'nun sınır themalarında atlı okçuluk ve mızrak talimiyle sertleşen bir asker oldun.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Polearm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);

                AddOption(youthMenu, "byz_youth_university", "{=byz_cc_university_name}Manganalar Üniversitesi'nde Retorik Okudun", "{=byz_cc_university_desc}Konstantinopolis'in ünlü Manganalar Üniversitesi'nde felsefe, hukuk ve retorik dersleri aldın.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "byz_car_kataphraktos", "{=byz_cc_kataphraktos_name}Kataphraktos Ağır Süvari Alayına Seçildin", "{=byz_cc_kataphraktos_desc}Zırhlı atlardan ve zırhlı süvarilerden oluşan seçkin Kataphraktos birliğine kabul edildin.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsByzantineCultureSelected, null);

                AddOption(careerMenu, "byz_car_akritas_officer", "{=byz_cc_akritas_officer_name}Sınır Akritas Kumandanı Oldun", "{=byz_cc_akritas_officer_desc}Türkmen akınlarına karşı Anadolu sınırını savunan akritai birliklerine komuta ettin.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 15, 0, IsByzantineCultureSelected, null);

                AddOption(careerMenu, "byz_car_bureaucrat", "{=byz_cc_bureaucrat_name}İmparatorluk Divanında Kâtip Oldun", "{=byz_cc_bureaucrat_desc}Sarayın bürokrasisinde vergi ve arazi kayıtlarını tutan güvenilir bir kâtip oldun.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsByzantineCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "byz_deed_rally", "{=byz_cc_rally_name}Bir Meydan Savaşından Sağ Çıkıp Orduyu Topladın", "{=byz_cc_rally_desc}Büyük bir bozgunun ortasında dağılan birlikleri yeniden topladın ve düzenli bir geri çekilme sağladın.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Tactics }, 15, null, 0, 30, 0, IsByzantineCultureSelected, null);

                AddOption(deedMenu, "byz_deed_fortress_defense", "{=byz_cc_fortress_defense_name}Kuşatılan Bir Kaleyi Son Ana Kadar Savundun", "{=byz_cc_fortress_defense_desc}Düşman kuşatması altındaki bir sınır kalesini takviye gelene kadar azimle savundun.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics }, 15, null, 0, 25, 0, IsByzantineCultureSelected, null);

                AddOption(deedMenu, "byz_deed_diplomat", "{=byz_cc_diplomat_name}Tehlikeli Bir Elçilik Görevini Başarıyla Tamamladın", "{=byz_cc_diplomat_desc}Düşman sarayına gönderilen tehlikeli bir barış elçiliğini ustalıkla ve cesaretle tamamladın.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsByzantineCultureSelected, null);
            }
        }

        // ============================== ABBASID (Culture.aserai) ==============================

        private void InjectAbbasidNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "abb_opt_hashimite", "{=abb_cc_hashimite_name}Haşimî Şerif Ailesi", "{=abb_cc_hashimite_desc}Peygamber soyundan gelen şerif bir Haşimî ailesinin çocuğusun; Bağdat'ta saygı ve nüfuz sahibisin.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "abb_opt_qadi", "{=abb_cc_qadi_name}Bağdat Kadı ve Ulema Ailesi", "{=abb_cc_qadi_desc}Ailen fıkıh ve şeriat ilminde uzman kadılar yetiştiren saygın bir ulema hanedanıdır.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Trade }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1100,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "abb_opt_bedouin", "{=abb_cc_bedouin_name}Sahra Bedevi Kabilesi", "{=abb_cc_bedouin_desc}Irak çölünün derinliklerinde deve sürüleri güden, kum fırtınalarında yol bulmayı bilen bir Bedevi kabilesinde doğdun.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Cunning, 1, 10, 500,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));

                AddOption(parentMenu, "abb_opt_basra_merchant", "{=abb_cc_basra_merchant_name}Basra Liman Tüccarı Ailesi", "{=abb_cc_basra_merchant_desc}Basra limanından Hint Okyanusu'na açılan gemilerle baharat ve inci ticareti yapan zengin bir tüccar ailesindensin.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1200,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "abb_opt_ghilman", "{=abb_cc_ghilman_name}Halife Muhafız Gulamı Soyu", "{=abb_cc_ghilman_desc}Baban Halifenin Dar-ül Hilafe'sinde nöbet tutan Türk asıllı gulam muhafızlarındandı.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.Athletics }, 15, DefaultCharacterAttributes.Control, 1, 15, 700,
                    IsAbbasidCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "abb_child_desert", "{=abb_cc_desert_name}Çölde Deve ve At Sürmeyi Öğrendin", "{=abb_cc_desert_desc}Bedevi akrabalarının yanında çölün acımasız güneşinde deve sürmeyi ve yıldızlarla yön bulmayı öğrendin.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_madrasa", "{=abb_cc_madrasa_name}Cami Medresesinde Kur'an ve Fıkıh Ezberledin", "{=abb_cc_madrasa_desc}Bağdat'ın büyük camilerinde küçük yaşta Kur'an, hadis ve fıkıh derslerine katıldın.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_bazaar", "{=abb_cc_bazaar_name}Bağdat Çarşısında Ticareti Öğrendin", "{=abb_cc_bazaar_desc}Kerh çarşısının labirent gibi sokaklarında tüccarların yanında pazarlık ve mal tartmayı öğrendin.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Steward }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(childMenu, "abb_child_wrestling", "{=abb_cc_wrestling_name}Meydanlarda Güreş ve Kılıç Talimi Yaptın", "{=abb_cc_wrestling_desc}Bağdat meydanlarında yaşıtlarınla güreşip tahta kılıçlarla dövüş oyunları oynadın.",
                    new[] { DefaultSkills.OneHanded, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "abb_youth_nizamiyya", "{=abb_cc_nizamiyya_name}Bağdat Nizamiye Medresesi'nde Okudun", "{=abb_cc_nizamiyya_desc}Nizam-ül Mülk'ün kurduğu ünlü Nizamiye medresesinde fıkıh, mantık ve devlet idaresi okudun.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(youthMenu, "abb_youth_caravan_guard", "{=abb_cc_caravan_guard_name}Hac Kervanı Muhafızlığı Yaptın", "{=abb_cc_caravan_guard_desc}Bağdat'tan Mekke'ye giden hac kervanlarını çöl haydutlarına karşı korudun.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Bow, DefaultSkills.Trade }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);

                AddOption(youthMenu, "abb_youth_palace_guard", "{=abb_cc_palace_guard_name}Dar-ül Hilafe Muhafız Alayına Katıldın", "{=abb_cc_palace_guard_desc}Halifenin sarayında disiplinli gulam muhafızları arasında silah eğitimi aldın.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "abb_car_ghulam_elite", "{=abb_cc_ghulam_elite_name}Halife Hassa Muhafızına Seçildin", "{=abb_cc_ghulam_elite_desc}Üstün yeteneğinle Halifenin şahsi Dar-ül Hilafe muhafız birliğine kabul edildin.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Riding }, 15, null, 0, 20, 0, IsAbbasidCultureSelected, null);

                AddOption(careerMenu, "abb_car_scholar", "{=abb_cc_scholar_name}Divan-ı Mezalim'de Kâtiplik Yaptın", "{=abb_cc_scholar_desc}Halifenin adalet divanında davaları kayda geçiren güvenilir bir kâtip oldun.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsAbbasidCultureSelected, null);

                AddOption(careerMenu, "abb_car_desert_raider", "{=abb_cc_desert_raider_name}Sahra Seferlerinde Akıncı Oldun", "{=abb_cc_desert_raider_desc}Çöl kabilelerine karşı düzeni sağlayan seferlerde ön safta çarpıştın.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Riding, DefaultSkills.Tactics }, 15, null, 0, 15, 0, IsAbbasidCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "abb_deed_palace_defense", "{=abb_cc_palace_defense_name}Saray Kuşatmasında Halifeyi Korudun", "{=abb_cc_palace_defense_desc}Dar-ül Hilafe'ye yapılan bir baskında canını hiçe sayarak Halifenin muhafızları arasında öne çıktın.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, IsAbbasidCultureSelected, null);

                AddOption(deedMenu, "abb_deed_caravan_save", "{=abb_cc_caravan_save_name}Hac Kervanını Baskından Kurtardın", "{=abb_cc_caravan_save_desc}Çöl haydutlarının bastığı büyük bir hac kervanını tek başına savunarak hacıları kurtardın.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Athletics }, 15, null, 0, 25, 0, IsAbbasidCultureSelected, null);

                AddOption(deedMenu, "abb_deed_justice", "{=abb_cc_justice_name}Mazlumun Hakkını Kadı Önünde Savundun", "{=abb_cc_justice_desc}Haksızlığa uğrayan bir esnafın davasını kadı huzurunda cesaretle savunarak adalet sağladın.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward }, 15, null, 0, 20, 0, IsAbbasidCultureSelected, null);
            }
        }

        // ============================== GEORGIAN (Culture.sturgia) ==============================

        private void InjectGeorgianNarratives(MBReadOnlyList<NarrativeMenu> menus)
        {
            if (menus.Count > 0)
            {
                var parentMenu = menus[0];
                AddOption(parentMenu, "geo_opt_bagrationi", "{=geo_cc_bagrationi_name}Bagrationi Hanedanı Yan Kolu", "{=geo_cc_bagrationi_desc}Kraliyet Bagrationi soyunun uzak bir kolundan geliyorsun; sarayın töre ve entrikalarıyla büyüdün.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1300,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "geo_opt_aznauri", "{=geo_cc_aznauri_name}Aznauri Küçük Asilzade Ailesi", "{=geo_cc_aznauri_desc}Kafkas dağlarında toprak sahibi küçük asilzade sınıfından bir ailenin çocuğusun; ata biner, mızrak kullanırdın.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm }, 15, DefaultCharacterAttributes.Vigor, 1, 15, 700,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Retainer"));

                AddOption(parentMenu, "geo_opt_mountain", "{=geo_cc_mountain_name}Dağlık Vadi Savaşçısı Ailesi", "{=geo_cc_mountain_desc}Kafkasların ulaşılmaz dağ vadilerinde, kar ve kayalarla boğuşarak sertleşen bir dağ ailesinde büyüdün.",
                    new[] { DefaultSkills.Athletics, DefaultSkills.Bow }, 15, DefaultCharacterAttributes.Endurance, 1, 10, 500,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Herder"));

                AddOption(parentMenu, "geo_opt_tbilisi_merchant", "{=geo_cc_tbilisi_merchant_name}Tiflis İpek Yolu Tüccarı Ailesi", "{=geo_cc_tbilisi_merchant_desc}Doğu ile Batı'yı birbirine bağlayan Tiflis pazarlarında ipek ve baharat ticareti yapan bir ailenin evladısın.",
                    new[] { DefaultSkills.Trade, DefaultSkills.Charm }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 1000,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Merchant"));

                AddOption(parentMenu, "geo_opt_monastery", "{=geo_cc_monastery_name}Manastır Akademisi Kâtip Ailesi", "{=geo_cc_monastery_desc}Ünlü bir manastır akademisinde teoloji ve felsefe kopyalayan kâtip bir ailede yetiştin.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Crafting }, 15, DefaultCharacterAttributes.Intelligence, 1, 10, 800,
                    IsGeorgianCultureSelected, mgr => SetParentOccupation(mgr, "Farmer"));
            }

            if (menus.Count > 1)
            {
                var childMenu = menus[1];
                AddOption(childMenu, "geo_child_caucasus", "{=geo_cc_caucasus_name}Kafkas Geçitlerinde At Sürdün", "{=geo_cc_caucasus_desc}Dar dağ geçitlerinde, uçurumların kenarında ata binmeyi ve dengeyi küçük yaşta öğrendin.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Athletics }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_falconry", "{=geo_cc_falconry_name}Şahin Eğitimi ve Ok Atışı Öğrendin", "{=geo_cc_falconry_desc}Kafkas ormanlarında şahin uçurup yay çekerek avlanmayı öğrendin.",
                    new[] { DefaultSkills.Bow, DefaultSkills.Scouting }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_church", "{=geo_cc_church_name}Kilise Korosunda ve Manastır Okulunda Okudun", "{=geo_cc_church_desc}Ortodoks kilise korosunda ilahiler söyledin, manastır okulunda okuma yazma ve dua kitaplarını öğrendin.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(childMenu, "geo_child_smithy", "{=geo_cc_smithy_name}Dağ Köyü Demirci Ocağında Çelik Dövdün", "{=geo_cc_smithy_desc}Dağ köyünün demirci ocağında ustaların yanında çelik dövmeyi ve zırh onarmayı öğrendin.",
                    new[] { DefaultSkills.Crafting, DefaultSkills.Trade }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 2)
            {
                var youthMenu = menus[2];
                AddOption(youthMenu, "geo_youth_royal_guard", "{=geo_cc_royal_guard_name}Kraliyet Muhafız Alayına Katıldın", "{=geo_cc_royal_guard_desc}Kralın saray muhafızları arasında disiplinli silah ve süvari eğitimi gördün.",
                    new[] { DefaultSkills.Polearm, DefaultSkills.OneHanded, DefaultSkills.Tactics }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(youthMenu, "geo_youth_mountain_scout", "{=geo_cc_mountain_scout_name}Dağ Geçitlerinde Sınır Gözcüsü Oldun", "{=geo_cc_mountain_scout_desc}Düşman akınlarına karşı dağ geçitlerini gözetleyen öncü birliklere katıldın.",
                    new[] { DefaultSkills.Scouting, DefaultSkills.Bow, DefaultSkills.Riding }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);

                AddOption(youthMenu, "geo_youth_academy", "{=geo_cc_academy_name}Manastır Akademisi'nde Felsefe ve Retorik Okudun", "{=geo_cc_academy_desc}Kafkasların önde gelen manastır akademisinde teoloji, felsefe ve devlet yönetimi üzerine eğitim aldın.",
                    new[] { DefaultSkills.Steward, DefaultSkills.Leadership, DefaultSkills.Charm }, 15, null, 0, 0, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 3)
            {
                var careerMenu = menus[3];
                AddOption(careerMenu, "geo_car_royal_cavalry", "{=geo_cc_royal_cavalry_name}Kraliyet Ağır Süvarisine Seçildin", "{=geo_cc_royal_cavalry_desc}Kafkas sınırındaki seferlerde savaşan seçkin kraliyet süvari alayına katıldın.",
                    new[] { DefaultSkills.Riding, DefaultSkills.Polearm, DefaultSkills.OneHanded }, 15, null, 0, 20, 0, IsGeorgianCultureSelected, null);

                AddOption(careerMenu, "geo_car_border_gazi", "{=geo_cc_border_gazi_name}Sınır Kalelerinde Deneyim Kazandın", "{=geo_cc_border_gazi_desc}Kafkas sınır kalelerinde yıllarca düşmana karşı nöbet tuttun, kuşatmalarda tecrübe kazandın.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Athletics, DefaultSkills.Bow }, 15, null, 0, 15, 0, IsGeorgianCultureSelected, null);

                AddOption(careerMenu, "geo_car_court", "{=geo_cc_court_name}Kraliyet Sarayında Hizmet Ettin", "{=geo_cc_court_desc}Tiflis sarayında kraliyet ailesine yakın bir danışman olarak görev aldın.",
                    new[] { DefaultSkills.Charm, DefaultSkills.Steward, DefaultSkills.Leadership }, 15, null, 0, 15, 0, IsGeorgianCultureSelected, null);
            }

            if (menus.Count > 4)
            {
                var deedMenu = menus[4];
                AddOption(deedMenu, "geo_deed_battle_hero", "{=geo_cc_battle_hero_name}Büyük Bir Meydan Savaşında Kahramanlık Gösterdin", "{=geo_cc_battle_hero_desc}Büyük bir meydan savaşında düşman saflarını yararak sancaktarı devirdin.",
                    new[] { DefaultSkills.Leadership, DefaultSkills.OneHanded }, 15, null, 0, 30, 0, IsGeorgianCultureSelected, null);

                AddOption(deedMenu, "geo_deed_rescue", "{=geo_cc_rescue_name}Kuşatılan Dağ Kalesini Kurtardın", "{=geo_cc_rescue_desc}Muhasara altındaki bir dağ kalesine gizli bir geçitten yardım ulaştırıp halkı kurtardın.",
                    new[] { DefaultSkills.Tactics, DefaultSkills.Scouting }, 15, null, 0, 25, 0, IsGeorgianCultureSelected, null);

                AddOption(deedMenu, "geo_deed_pilgrim", "{=geo_cc_pilgrim_name}Hacılara ve Mazlumlara Kalkan Oldun", "{=geo_cc_pilgrim_desc}Kutsal topraklara giden hacı kervanlarını haydutlardan koruyarak ün kazandın.",
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
            Func<CharacterCreationManager, bool> visibilityPredicate,
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
                mgr => visibilityPredicate(mgr),
                onSelect,
                null); // Let Native's ApplyFinalEffects apply Args cleanly without duplicate crash

            menu.AddNarrativeMenuOption(option);
        }

        public void OnStageCompleted(CharacterCreationStageBase stage) { }
        public void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager) { }
    }
}
