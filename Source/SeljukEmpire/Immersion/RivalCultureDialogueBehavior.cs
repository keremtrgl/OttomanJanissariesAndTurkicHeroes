using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Injects authentic historical greetings for six flagship figures of the three rival
    /// kingdoms - Byzantine Emperor Romanos IV Diogenes and general Alexios Komnenos, Abbasid
    /// Caliph Al-Qa'im bi-Amr Allah and vizier Fakhr al-Dawla ibn Jahir, and Georgian King David
    /// IV the Builder and noble Liparit Baghvashi - mirroring SeljukDialogueBehavior's pattern for
    /// the Seljuk side's own named figures, including its 3-line-per-character variety mechanism
    /// (see GreetingVariety's remarks for why this needs an in-game-hour
    /// condition rather than a plain duplicate AddDialogLine).
    /// </summary>
    public class RivalCultureDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient dialog behavior
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // BYZANTINE EMPEROR ROMANOS IV DIOGENES (salt 3)
                bool IsRomanos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_14";

                starter.AddDialogLine("byz_romanos_greeting_1", "start", "lord_start",
                    "{=byz_romanos_greet}Stranger who dares approach the Purple! Rome endures though her eagle's wings are clipped at Manzikert - I will see her risen again, whatever the cost to my own name. Speak your business, and be brief.",
                    () => IsRomanos() && GreetingVariety.Current(3, 3) == 0, null, 200);
                starter.AddDialogLine("byz_romanos_greeting_2", "start", "lord_start",
                    "{=byz_romanos_greet_2}History will judge me for one field outside Manzikert and forget the twenty I held before it. I do not care - only Rome's survival concerns me now. What do you want?",
                    () => IsRomanos() && GreetingVariety.Current(3, 3) == 1, null, 200);
                starter.AddDialogLine("byz_romanos_greeting_3", "start", "lord_start",
                    "{=byz_romanos_greet_3}Every general in this camp believes he could have done better against the Turks. Perhaps. None of them are wearing the Purple, however. Speak.",
                    () => IsRomanos() && GreetingVariety.Current(3, 3) == 2, null, 200);

                // BYZANTINE GENERAL ALEXIOS KOMNENOS (salt 4)
                bool IsAlexios() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_30";

                starter.AddDialogLine("byz_alexios_greeting_1", "start", "lord_start",
                    "{=byz_alexios_greet_1}Well met. I have fought under three emperors already and buried the ambitions of men greater than either of us - so choose your words to me with some care.",
                    () => IsAlexios() && GreetingVariety.Current(4, 3) == 0, null, 200);
                starter.AddDialogLine("byz_alexios_greeting_2", "start", "lord_start",
                    "{=byz_alexios_greet_2}Rome does not need another hero, stranger - it needs someone willing to do what heroes will not. I have made my peace with that. Have you?",
                    () => IsAlexios() && GreetingVariety.Current(4, 3) == 1, null, 200);
                starter.AddDialogLine("byz_alexios_greeting_3", "start", "lord_start",
                    "{=byz_alexios_greet_3}The court whispers that I look at the throne too often. The court is not wrong. What brings you to me?",
                    () => IsAlexios() && GreetingVariety.Current(4, 3) == 2, null, 200);

                // ABBASID CALIPH AL-QA'IM BI-AMR ALLAH (salt 5)
                bool IsAlQaim() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_1";

                starter.AddDialogLine("abb_alqaim_greeting_1", "start", "lord_start",
                    "{=abb_alqaim_greet}Peace be upon you, traveler, in the name of the Commander of the Faithful. Baghdad stands as it has for three centuries, its light undimmed though the Sultan's sword, not mine, now guards its gates. What brings you before the seat of the Caliphate?",
                    () => IsAlQaim() && GreetingVariety.Current(5, 3) == 0, null, 200);
                starter.AddDialogLine("abb_alqaim_greeting_2", "start", "lord_start",
                    "{=abb_alqaim_greet_2}A Caliph without an army is still a Caliph, traveler - the Friday sermon is still read in my name from Cairo to Bukhara. Power is not the only throne. What do you seek?",
                    () => IsAlQaim() && GreetingVariety.Current(5, 3) == 1, null, 200);
                starter.AddDialogLine("abb_alqaim_greeting_3", "start", "lord_start",
                    "{=abb_alqaim_greet_3}The Sultan protects my palace; I protect his legitimacy. Neither of us says this aloud, but both of us know it. Now - what is your business?",
                    () => IsAlQaim() && GreetingVariety.Current(5, 3) == 2, null, 200);

                // ABBASID VIZIER FAKHR AL-DAWLA IBN JAHIR (salt 6)
                bool IsFakhrAlDawla() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_3";

                starter.AddDialogLine("abb_fakhraldawla_greeting_1", "start", "lord_start",
                    "{=abb_fakhraldawla_greet_1}Sit, if you have patience - I have petitions from three provinces on this desk and a Caliph who wants answers by evening. What is your business?",
                    () => IsFakhrAlDawla() && GreetingVariety.Current(6, 3) == 0, null, 200);
                starter.AddDialogLine("abb_fakhraldawla_greeting_2", "start", "lord_start",
                    "{=abb_fakhraldawla_greet_2}In this court, a well-placed word outlasts a well-placed sword. I have seen both used, and I know which one I trust more. Speak.",
                    () => IsFakhrAlDawla() && GreetingVariety.Current(6, 3) == 1, null, 200);
                starter.AddDialogLine("abb_fakhraldawla_greeting_3", "start", "lord_start",
                    "{=abb_fakhraldawla_greet_3}Baghdad's treasury does not balance itself, and neither does its politics. I manage both, badly some days. What do you need from me?",
                    () => IsFakhrAlDawla() && GreetingVariety.Current(6, 3) == 2, null, 200);

                // GEORGIAN KING DAVID IV THE BUILDER (salt 7)
                bool IsDavid() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_1";

                starter.AddDialogLine("geo_david_greeting_1", "start", "lord_start",
                    "{=geo_david_greet}Well met beneath the mountains of Kartli! Georgia will not remain a vassal to any invader's whim - I am building an army, a church, and a nation that will outlast us both. Speak plainly, what do you want?",
                    () => IsDavid() && GreetingVariety.Current(7, 3) == 0, null, 200);
                starter.AddDialogLine("geo_david_greeting_2", "start", "lord_start",
                    "{=geo_david_greet_2}Every noble house in this kingdom believes it should rule instead of me. I intend to prove, one reform at a time, that they are wrong. What do you want?",
                    () => IsDavid() && GreetingVariety.Current(7, 3) == 1, null, 200);
                starter.AddDialogLine("geo_david_greeting_3", "start", "lord_start",
                    "{=geo_david_greet_3}A kingdom is not defended by walls alone, but by the loyalty of the men who guard them. I am still building both. What brings you to me?",
                    () => IsDavid() && GreetingVariety.Current(7, 3) == 2, null, 200);

                // GEORGIAN NOBLE LIPARIT BAGHVASHI (salt 8)
                bool IsLiparit() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_3";

                starter.AddDialogLine("geo_liparit_greeting_1", "start", "lord_start",
                    "{=geo_liparit_greet_1}You stand in the lands of House Baghvashi, traveler - remember that, whatever crown you answer to. What do you want?",
                    () => IsLiparit() && GreetingVariety.Current(8, 3) == 0, null, 200);
                starter.AddDialogLine("geo_liparit_greeting_2", "start", "lord_start",
                    "{=geo_liparit_greet_2}Kings come and go, but the great houses of Kartli endure. I have outlasted more royal tempers than you might guess. Speak your business.",
                    () => IsLiparit() && GreetingVariety.Current(8, 3) == 1, null, 200);
                starter.AddDialogLine("geo_liparit_greeting_3", "start", "lord_start",
                    "{=geo_liparit_greet_3}I bow to no throne that forgets whose swords hold it up. State your business, and mind your tone.",
                    () => IsLiparit() && GreetingVariety.Current(8, 3) == 2, null, 200);

                // ============================================================================
                // SECOND WAVE: 13 additional named lords across the three rival kingdoms (none
                // added to the Seljuk side here - it already has its own 3 named figures above).
                // Lighter treatment than the flagship six above: 1-2 lines each instead of 3, since
                // these are secondary court/battlefield figures rather than ruler-tier characters.
                // ============================================================================

                // BYZANTINE GENERAL NIKEPHOROS BOTANEIATES (later Emperor) (salt 9)
                bool IsBotaneiates() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_15";

                starter.AddDialogLine("byz_botaneiates_greeting_1", "start", "lord_start",
                    "{=byz_botaneiates_greet_1}They call me old, and they are not wrong - but a long life spent watching younger men fail has taught me exactly how empires actually change hands. What do you want?",
                    () => IsBotaneiates() && GreetingVariety.Current(9, 2) == 0, null, 200);
                starter.AddDialogLine("byz_botaneiates_greeting_2", "start", "lord_start",
                    "{=byz_botaneiates_greet_2}Every ambitious man in Constantinople is patient until the moment he isn't. I have simply been more patient than most. Speak your business.",
                    () => IsBotaneiates() && GreetingVariety.Current(9, 2) == 1, null, 200);

                // BYZANTINE GENERAL ANDRONIKOS DOUKAS (withdrew his troops at Manzikert) (salt 10)
                bool IsAndronikosDoukas() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_17";

                starter.AddDialogLine("byz_andronikos_greeting_1", "start", "lord_start",
                    "{=byz_andronikos_greet_1}You will hear it said that I turned my back on the field at Manzikert. I turned my back on a battle already lost, and saved men who would otherwise be corpses. Judge me as you like - now, what brings you here?",
                    () => IsAndronikosDoukas() && GreetingVariety.Current(10, 2) == 0, null, 200);
                starter.AddDialogLine("byz_andronikos_greeting_2", "start", "lord_start",
                    "{=byz_andronikos_greet_2}The Doukas name carries weight in this Empire whether the mob approves of me or not. State your business.",
                    () => IsAndronikosDoukas() && GreetingVariety.Current(10, 2) == 1, null, 200);

                // BYZANTINE GENERAL NIKEPHOROS BRYENNIOS (salt: none needed, 1 line)
                bool IsBryennios() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_63";

                starter.AddDialogLine("byz_bryennios_greeting_1", "start", "lord_start",
                    "{=byz_bryennios_greet_1}A soldier who has not yet decided whether the throne is worth the risk of reaching for it is still, for now, a loyal one. Speak plainly, what do you need?",
                    IsBryennios, null, 200);

                // BYZANTINE ROMANOS ARGYROS (1 line)
                bool IsArgyros() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_55";

                starter.AddDialogLine("byz_argyros_greeting_1", "start", "lord_start",
                    "{=byz_argyros_greet_1}The Argyros line has given Rome an emperor before, and may yet again. Until then, I serve as I am asked. What is it?",
                    IsArgyros, null, 200);

                // BYZANTINE GENERAL KATAKALON KEKAUMENOS (military writer) (salt 11)
                bool IsKekaumenos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_SE9_l";

                starter.AddDialogLine("byz_kekaumenos_greeting_1", "start", "lord_start",
                    "{=byz_kekaumenos_greet_1}I have written down everything I have learned commanding men on this frontier, so that fools who come after me need not learn it the hard way. What do you want to know?",
                    () => IsKekaumenos() && GreetingVariety.Current(11, 2) == 0, null, 200);
                starter.AddDialogLine("byz_kekaumenos_greeting_2", "start", "lord_start",
                    "{=byz_kekaumenos_greet_2}Trust the man who tells you war is glorious least of all. I have seen enough of it to know better. Speak your business.",
                    () => IsKekaumenos() && GreetingVariety.Current(11, 2) == 1, null, 200);

                // ABBASID ARSLAN KHATUN (salt 12)
                bool IsArslanKhatun() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_2";

                starter.AddDialogLine("abb_arslankhatun_greeting_1", "start", "lord_start",
                    "{=abb_arslankhatun_greet_1}A woman's name carries as much weight at this court as any man's title, whatever the scribes choose to write down. What brings you before me?",
                    () => IsArslanKhatun() && GreetingVariety.Current(12, 2) == 0, null, 200);
                starter.AddDialogLine("abb_arslankhatun_greeting_2", "start", "lord_start",
                    "{=abb_arslankhatun_greet_2}I have outlived the political schemes of men who thought a marriage alliance made me powerless. It did not. Speak your business.",
                    () => IsArslanKhatun() && GreetingVariety.Current(12, 2) == 1, null, 200);

                // ABBASID VIZIER IBN AL-MUSLIMA (salt 13)
                bool IsIbnAlMuslima() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_5";

                starter.AddDialogLine("abb_ibnalmuslima_greeting_1", "start", "lord_start",
                    "{=abb_ibnalmuslima_greet_1}Baghdad has enemies within its own walls as often as beyond them, and I have made it my life's work to know the difference. What is your business?",
                    () => IsIbnAlMuslima() && GreetingVariety.Current(13, 2) == 0, null, 200);
                starter.AddDialogLine("abb_ibnalmuslima_greeting_2", "start", "lord_start",
                    "{=abb_ibnalmuslima_greet_2}Faith and statecraft are not two different arts in this city, traveler - they are one and the same. Speak plainly.",
                    () => IsIbnAlMuslima() && GreetingVariety.Current(13, 2) == 1, null, 200);

                // ABBASID CHRONICLER HILAL AL-SABI (1 line)
                bool IsHilalAlSabi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_18";

                starter.AddDialogLine("abb_hilalalsabi_greeting_1", "start", "lord_start",
                    "{=abb_hilalalsabi_greet_1}Every event in this court, I write down for those who come after us - so mind what you say to me, it may well outlive us both.",
                    IsHilalAlSabi, null, 200);

                // ABBASID EMIR QURAYSH IBN BADRAN (1 line)
                bool IsQuraysh() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_19";

                starter.AddDialogLine("abb_quraysh_greeting_1", "start", "lord_start",
                    "{=abb_quraysh_greet_1}My tribe held these lands before the Sultan's horsemen arrived, and will hold them after, God willing. What do you want?",
                    IsQuraysh, null, 200);

                // GEORGIAN QUEEN GURANDUKHT (salt 14)
                bool IsGurandukht() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_2";

                starter.AddDialogLine("geo_gurandukht_greeting_1", "start", "lord_start",
                    "{=geo_gurandukht_greet_1}A kingdom raised by a boy-king needs a steady hand behind the throne, traveler, and I have provided it more than once. What do you want?",
                    () => IsGurandukht() && GreetingVariety.Current(14, 2) == 0, null, 200);
                starter.AddDialogLine("geo_gurandukht_greeting_2", "start", "lord_start",
                    "{=geo_gurandukht_greet_2}I have buried a husband and crowned a son, and neither task was as simple as the chroniclers make it sound. Speak your business.",
                    () => IsGurandukht() && GreetingVariety.Current(14, 2) == 1, null, 200);

                // GEORGIAN NOBLE IVANE ORBELI (salt 15)
                bool IsIvaneOrbeli() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_5";

                starter.AddDialogLine("geo_orbeli_greeting_1", "start", "lord_start",
                    "{=geo_orbeli_greet_1}House Orbeli has commanded armies for this kingdom since before the current dynasty could hold a sword. Remember whose loyalty a crown actually depends on. What do you want?",
                    () => IsIvaneOrbeli() && GreetingVariety.Current(15, 2) == 0, null, 200);
                starter.AddDialogLine("geo_orbeli_greeting_2", "start", "lord_start",
                    "{=geo_orbeli_greet_2}A king who forgets which houses hold up his throne rarely keeps it for long. I am simply reminding you of that fact. Speak.",
                    () => IsIvaneOrbeli() && GreetingVariety.Current(15, 2) == 1, null, 200);

                // GEORGIAN CATHOLICOS GIORGI OF CHQONDIDI (1 line)
                bool IsGiorgiChqondideli() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_18";

                starter.AddDialogLine("geo_giorgi_greeting_1", "start", "lord_start",
                    "{=geo_giorgi_greet_1}The Church and the Crown walk the same road in this kingdom, traveler, however uneasily. What brings you to me?",
                    IsGiorgiChqondideli, null, 200);

                // GEORGIAN NOBLE GRIGOL ERISTAVI (1 line)
                bool IsGrigolEristavi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_S9_l";

                starter.AddDialogLine("geo_grigol_greeting_1", "start", "lord_start",
                    "{=geo_grigol_greet_1}An Eristavi answers to the King, not to every wandering stranger who rides through his lands. State your business plainly.",
                    IsGrigolEristavi, null, 200);

                // ============================================================================
                // THIRD WAVE (v1.8.3): the last 7 named lords across Abbasid/Georgian left with
                // no custom line after the second wave above - closes out both kingdoms' full
                // named-lord rosters. Same lighter 1-2-line treatment as the second wave.
                // ============================================================================

                // ABBASID NAQIB AL-ASHRAF TIRAD AL-ZAYNABI (marshal of the Prophet's descendants
                // in Baghdad, a real hereditary Abbasid-era court office) (salt 16)
                bool IsTiradAlZaynabi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_16";

                starter.AddDialogLine("abb_tirad_greeting_1", "start", "lord_start",
                    "{=abb_tirad_greet_1}Every descendant of the Prophet in this city answers to my register before he answers to any emir's summons. What is your business with House Zaynabi?",
                    () => IsTiradAlZaynabi() && GreetingVariety.Current(16, 2) == 0, null, 200);
                starter.AddDialogLine("abb_tirad_greeting_2", "start", "lord_start",
                    "{=abb_tirad_greet_2}Lineage is its own kind of army in Baghdad, traveler - mine has outlasted several Sultans already. Speak your business.",
                    () => IsTiradAlZaynabi() && GreetingVariety.Current(16, 2) == 1, null, 200);

                // ABBASID CHIEF QADI ABU ABDALLAH AL-DAMAGHANI (salt 17)
                bool IsAlDamaghani() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_17";

                starter.AddDialogLine("abb_damaghani_greeting_1", "start", "lord_start",
                    "{=abb_damaghani_greet_1}I have sat in judgment over men far greater than you, and the law did not bend for any of them either. State your business.",
                    () => IsAlDamaghani() && GreetingVariety.Current(17, 2) == 0, null, 200);
                starter.AddDialogLine("abb_damaghani_greeting_2", "start", "lord_start",
                    "{=abb_damaghani_greet_2}A city this old needs a judge more than it needs another sword - Baghdad has had enough of the latter. What do you seek?",
                    () => IsAlDamaghani() && GreetingVariety.Current(17, 2) == 1, null, 200);

                // ABBASID EMIR IBN AYYUB (1 line)
                bool IsIbnAyyub() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_22";

                starter.AddDialogLine("abb_ibnayyub_greeting_1", "start", "lord_start",
                    "{=abb_ibnayyub_greet_1}My sword has served the Sultanate longer than most men at this court have been alive. Speak your business, and be quick about it.",
                    IsIbnAyyub, null, 200);

                // GEORGIAN KING AGHSARTAN II OF KAKHETI-HERETI (a rival eastern Georgian kingdom,
                // not yet absorbed into David IV's unified crown) (salt 18)
                bool IsAghsartan() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_16";

                starter.AddDialogLine("geo_aghsartan_greeting_1", "start", "lord_start",
                    "{=geo_aghsartan_greet_1}Kakheti bent its knee to Tbilisi's crown once already in my family's memory - I do not intend to make a habit of it. What do you want?",
                    () => IsAghsartan() && GreetingVariety.Current(18, 2) == 0, null, 200);
                starter.AddDialogLine("geo_aghsartan_greeting_2", "start", "lord_start",
                    "{=geo_aghsartan_greet_2}A mountain kingdom survives by never quite trusting its larger neighbors, even the Georgian ones. Speak your business.",
                    () => IsAghsartan() && GreetingVariety.Current(18, 2) == 1, null, 200);

                // GEORGIAN NOBLE ARISHIANI (1 line)
                bool IsArishiani() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_17";

                starter.AddDialogLine("geo_arishiani_greeting_1", "start", "lord_start",
                    "{=geo_arishiani_greet_1}I have ridden every pass between here and the Kartli border more times than I can count. State your business plainly.",
                    IsArishiani, null, 200);

                // GEORGIAN NOBLE KAVTAR BARAMISDZE (1 line)
                bool IsKavtarBaramisdze() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_19";

                starter.AddDialogLine("geo_kavtar_greeting_1", "start", "lord_start",
                    "{=geo_kavtar_greet_1}House Baramisdze holds its lands by the sword as much as by the King's favor, traveler. What brings you to me?",
                    IsKavtarBaramisdze, null, 200);

                // GEORGIAN DUKE KAKHABER KAKHABERISDZE OF KARTLI (salt 19)
                bool IsKakhaber() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_20";

                starter.AddDialogLine("geo_kakhaber_greeting_1", "start", "lord_start",
                    "{=geo_kakhaber_greet_1}Kartli is the heart of this kingdom, and I have governed it long enough to know every noble house within it by name. Speak your business.",
                    () => IsKakhaber() && GreetingVariety.Current(19, 2) == 0, null, 200);
                starter.AddDialogLine("geo_kakhaber_greeting_2", "start", "lord_start",
                    "{=geo_kakhaber_greet_2}A duke who cannot hold his own province has no business advising a king on how to hold a kingdom. I hold mine well. What do you want?",
                    () => IsKakhaber() && GreetingVariety.Current(19, 2) == 1, null, 200);

                // ============================================================================
                // FOURTH WAVE (v1.8.4): the last 15 named Byzantine lords with no custom line -
                // all 9 from byzantine_north_lords.xml (zero of the North roster had a line
                // before this wave) plus the 6 remaining from byzantine_lords.xml (South). West
                // (byzantine_west_lords.xml) was already fully covered in NewKingdomsDialogueBehavior.
                // Same lighter 1-2-line treatment as the second/third waves.
                // ============================================================================

                // BYZANTINE NORTH GENERAL GEORGIOS PALAIOLOGOS (Alexios I's brother-in-law,
                // key general in the 1081 coup and the defense of Dyrrhachium) (salt 20)
                bool IsPalaiologos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_1";

                starter.AddDialogLine("byz_n_palaiologos_greeting_1", "start", "lord_start",
                    "{=byz_n_palaiologos_greet_1}I have helped raise an emperor to his throne before, stranger, and I did not do it for gratitude - I did it because Rome needed a steady hand more than it needed the man already wearing the crown. What do you want?",
                    () => IsPalaiologos() && GreetingVariety.Current(20, 2) == 0, null, 200);
                starter.AddDialogLine("byz_n_palaiologos_greeting_2", "start", "lord_start",
                    "{=byz_n_palaiologos_greet_2}I have held Dyrrhachium's walls against men who thought Rome too weak to defend her own coastline. They were wrong then, and they would be wrong now. Speak your business.",
                    () => IsPalaiologos() && GreetingVariety.Current(20, 2) == 1, null, 200);

                // BYZANTINE NORTH KONSTANTINOS ANGELOS (married into the Komnenos line; founder
                // of the Angelos house's later rise to the throne) (1 line)
                bool IsKAngelos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_3";

                starter.AddDialogLine("byz_n_angelos_greeting_1", "start", "lord_start",
                    "{=byz_n_angelos_greet_1}My marriage bound the Angelos name to the blood of emperors, traveler - what my grandsons make of that is not yet written. What is your business with me?",
                    IsKAngelos, null, 200);

                // BYZANTINE NORTH IOANNES KANTAKOUZENOS (doux/general; ancestor of the later
                // Kantakouzenos imperial line) (1 line)
                bool IsKantakouzenos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_5";

                starter.AddDialogLine("byz_n_kantakouzenos_greeting_1", "start", "lord_start",
                    "{=byz_n_kantakouzenos_greet_1}The Kantakouzenos name commands soldiers on this frontier today, whatever it commands in Constantinople tomorrow. State your business plainly.",
                    IsKantakouzenos, null, 200);

                // BYZANTINE NORTH BARDAS PHOKAS (led the 989 revolt against Basil II; scion of
                // the great rival military dynasty to the Skleroi) (salt 21)
                bool IsPhokas() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_20";

                starter.AddDialogLine("byz_n_phokas_greeting_1", "start", "lord_start",
                    "{=byz_n_phokas_greet_1}The Phokas name has worn the Purple before and buried more rivals than I care to count. Mind that, whatever business brings you before me.",
                    () => IsPhokas() && GreetingVariety.Current(21, 2) == 0, null, 200);
                starter.AddDialogLine("byz_n_phokas_greeting_2", "start", "lord_start",
                    "{=byz_n_phokas_greet_2}Every emperor in Constantinople watches this family a little too closely, and for good reason. Speak, and be quick about it.",
                    () => IsPhokas() && GreetingVariety.Current(21, 2) == 1, null, 200);

                // BYZANTINE NORTH ROMANOS SKLEROS (Skleros house, the Phokas family's great
                // rivals, own revolt against Basil II in 976-979) (1 line)
                bool IsRSkleros() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_50";

                starter.AddDialogLine("byz_n_skleros_greeting_1", "start", "lord_start",
                    "{=byz_n_skleros_greet_1}The Skleros house has raised its banner against an emperor once already within living memory - remember that before you mistake me for an easy man to command. What do you want?",
                    IsRSkleros, null, 200);

                // BYZANTINE NORTH LEON KOURKOUAS (descendant of John Kourkouas, the great 10th
                // century general who pushed Rome's eastern border further than any before him) (1 line)
                bool IsLKourkouas() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_51";

                starter.AddDialogLine("byz_n_kourkouas_greeting_1", "start", "lord_start",
                    "{=byz_n_kourkouas_greet_1}My grandfather's grandfather carried Rome's eagles further east than any general before or since. I intend to hold what he won. Speak your business.",
                    IsLKourkouas, null, 200);

                // BYZANTINE NORTH MICHAEL XIPHILINOS (Xiphilinos house, which produced a
                // Patriarch of Constantinople - a family of the law and the Church, not the sword) (1 line)
                bool IsXiphilinos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_58";

                starter.AddDialogLine("byz_n_xiphilinos_greeting_1", "start", "lord_start",
                    "{=byz_n_xiphilinos_greet_1}My family has served the Church and the law as faithfully as others have served the sword, traveler. State your business, and mind your words.",
                    IsXiphilinos, null, 200);

                // BYZANTINE NORTH NIKEPHOROS VATATZES (general; ancestor line of the Vatatzes
                // emperors who would one day rule from Nicaea) (1 line)
                bool IsVatatzes() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_NE8_l";

                starter.AddDialogLine("byz_n_vatatzes_greeting_1", "start", "lord_start",
                    "{=byz_n_vatatzes_greet_1}The Vatatzes name is not yet a great one in Constantinople, but give it a century or two. What brings you to me today?",
                    IsVatatzes, null, 200);

                // BYZANTINE NORTH THEODOROS GABRAS (real semi-independent doux of Trebizond who
                // held the frontier against the Turkoman tribes until his death in battle against
                // them) (salt 22)
                bool IsGabras() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_NE9_l";

                starter.AddDialogLine("byz_n_gabras_greeting_1", "start", "lord_start",
                    "{=byz_n_gabras_greet_1}Trebizond answers to Constantinople in name and to me in practice, and I have held this frontier against the Turkoman tribes longer than any garrison sent from the capital could manage alone. What is your business?",
                    () => IsGabras() && GreetingVariety.Current(22, 2) == 0, null, 200);
                starter.AddDialogLine("byz_n_gabras_greeting_2", "start", "lord_start",
                    "{=byz_n_gabras_greet_2}Every raid out of the Turkoman hills tests whether Trebizond still has teeth. It does. Speak plainly, what do you want?",
                    () => IsGabras() && GreetingVariety.Current(22, 2) == 1, null, 200);

                // BYZANTINE SOUTH MARIA OF ALANIA (Georgian princess, empress twice over under
                // two different husbands - Michael VII Doukas, then Nikephoros III Botaneiates) (salt 23)
                bool IsMariaAlania() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_16";

                starter.AddDialogLine("byz_mariaalania_greeting_1", "start", "lord_start",
                    "{=byz_mariaalania_greet_1}I came from Georgia's mountains to wear Rome's crown - twice, in fact, under two different husbands. Neither throne surprised me as much as you might expect. What do you want?",
                    () => IsMariaAlania() && GreetingVariety.Current(23, 2) == 0, null, 200);
                starter.AddDialogLine("byz_mariaalania_greeting_2", "start", "lord_start",
                    "{=byz_mariaalania_greet_2}An empress learns quickly that a crown outlasts the man wearing it, if she is patient enough. I have been very patient. Speak your business.",
                    () => IsMariaAlania() && GreetingVariety.Current(23, 2) == 1, null, 200);

                // BYZANTINE SOUTH MARIA OF BULGARIA (Bulgarian-origin noblewoman at the
                // Byzantine court, from the generations absorbed after Bulgaria's 1018 annexation) (1 line)
                bool IsMariaBulgaria() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_18";

                starter.AddDialogLine("byz_mariabulgaria_greeting_1", "start", "lord_start",
                    "{=byz_mariabulgaria_greet_1}My family knelt to the Purple when Bulgaria fell, traveler, and I have made my peace with a Constantinople that still forgets we were ever a kingdom of our own. What is your business?",
                    IsMariaBulgaria, null, 200);

                // BYZANTINE SOUTH IRENE DOUKAINA (Alexios I's empress, mother of the historian
                // Anna Komnene) (salt 24)
                bool IsIreneDoukaina() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_30_1";

                starter.AddDialogLine("byz_irenedoukaina_greeting_1", "start", "lord_start",
                    "{=byz_irenedoukaina_greet_1}My husband did not want me at his side when he first took the throne. He learned otherwise soon enough, and so will you if you underestimate me. Speak.",
                    () => IsIreneDoukaina() && GreetingVariety.Current(24, 2) == 0, null, 200);
                starter.AddDialogLine("byz_irenedoukaina_greeting_2", "start", "lord_start",
                    "{=byz_irenedoukaina_greet_2}My daughter writes down everything her father does, for history to judge. I make sure she has the full truth to write, not merely the flattering half. What do you want?",
                    () => IsIreneDoukaina() && GreetingVariety.Current(24, 2) == 1, null, 200);

                // BYZANTINE SOUTH NIKEPHOROS MELISSENOS (rival claimant to the throne in 1081,
                // stood down when Alexios moved first and was made Caesar instead) (salt 25)
                bool IsMelissenos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_54";

                starter.AddDialogLine("byz_melissenos_greeting_1", "start", "lord_start",
                    "{=byz_melissenos_greet_1}I raised my own claim to the throne the same year Alexios raised his. I chose to stand down rather than tear the Empire apart over it - a choice that still costs me less sleep than you might think. What do you want?",
                    () => IsMelissenos() && GreetingVariety.Current(25, 2) == 0, null, 200);
                starter.AddDialogLine("byz_melissenos_greeting_2", "start", "lord_start",
                    "{=byz_melissenos_greet_2}A Caesar's title is a fine consolation for a throne I never had to bloody myself winning. Speak your business.",
                    () => IsMelissenos() && GreetingVariety.Current(25, 2) == 1, null, 200);

                // BYZANTINE SOUTH JOSEPH TARCHANEIOTES (his detachment left the field at
                // Manzikert before the main battle - one more figure this court still quietly
                // blames for 1071, alongside Andronikos Doukas above) (1 line)
                bool IsTarchaneiotes() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_72";

                starter.AddDialogLine("byz_tarchaneiotes_greeting_1", "start", "lord_start",
                    "{=byz_tarchaneiotes_greet_1}They still whisper that my men left the field at Manzikert before the battle was lost. I have stopped correcting them - the whispering does the fighting for me now. What brings you here?",
                    IsTarchaneiotes, null, 200);

                // BYZANTINE SOUTH ANNA DIOGENISSA (Romanos IV Diogenes's daughter, per this
                // mod's own renaming of Native's "Ira" - see byzantine_lords.xml) (1 line)
                bool IsAnnaDiogenissa() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_37";

                starter.AddDialogLine("byz_annadiogenissa_greeting_1", "start", "lord_start",
                    "{=byz_annadiogenissa_greet_1}My father lost an empire at Manzikert, and I have spent my life since watching lesser men pretend they would have done better in his place. What do you want?",
                    IsAnnaDiogenissa, null, 200);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
