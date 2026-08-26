using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Injects authentic historical greetings for 32 named lords across the four kingdoms added
    /// after RivalCultureDialogueBehavior was written - Crusader States, Cilician Armenia,
    /// Kara-Khanid Khanate, the Latin Empire - plus 7 Bizans (empire_s) "West" lords left over
    /// from the Latin Empire split. Same 1-3-line variety mechanism as
    /// SeljukDialogueBehavior/RivalCultureDialogueBehavior - see GetGreetingVariant's remarks
    /// there for why this needs an in-game-hour condition rather than a plain duplicate
    /// AddDialogLine. Salts continue from 16 (RivalCultureDialogueBehavior used 0-15).
    /// </summary>
    public class NewKingdomsDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient dialog behavior
        }

        private static int GetGreetingVariant(int salt, int variantCount)
        {
            return ((int)CampaignTime.Now.ToHours + salt) % variantCount;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // ========================= CRUSADER STATES (vlandia) =========================

                // BOHEMOND OF TARANTO (salt 16)
                bool IsBohemond() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_1";

                starter.AddDialogLine("crus_bohemond_greeting_1", "start", "lord_pretalk",
                    "{=crus_bohemond_greet_1}I took Antioch when older, wiser men said it could not be taken - my father Guiscard taught me that Byzantium's promises are worth exactly as much as the army standing behind them, and no more. What do you want?",
                    () => IsBohemond() && GetGreetingVariant(16, 3) == 0, null, 200);
                starter.AddDialogLine("crus_bohemond_greeting_2", "start", "lord_pretalk",
                    "{=crus_bohemond_greet_2}They call me a prince without patience, and they are right - I have carved out one principality with a broken oath already, and I see no reason to apologize for it now. Speak your business.",
                    () => IsBohemond() && GetGreetingVariant(16, 3) == 1, null, 200);
                starter.AddDialogLine("crus_bohemond_greeting_3", "start", "lord_pretalk",
                    "{=crus_bohemond_greet_3}I marched all the way to Illyria once to break the Emperor's power, and lost. I do not lose the same way twice. What brings you to Antioch?",
                    () => IsBohemond() && GetGreetingVariant(16, 3) == 2, null, 200);

                // BALDWIN OF LE BOURCQ (salt 17)
                bool IsBaldwin2() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_3";

                starter.AddDialogLine("crus_baldwin2_greeting_1", "start", "lord_pretalk",
                    "{=crus_baldwin2_greet_1}Edessa is mine by the sword and God's favor both, cousin's blood or no - I have been ransomed out of two captivities already, and I do not intend a third. Speak your business.",
                    () => IsBaldwin2() && GetGreetingVariant(17, 2) == 0, null, 200);
                starter.AddDialogLine("crus_baldwin2_greeting_2", "start", "lord_pretalk",
                    "{=crus_baldwin2_greet_2}A count on this frontier prays as often as he fights, traveler, and I have found little difference between the two. What do you seek from me?",
                    () => IsBaldwin2() && GetGreetingVariant(17, 2) == 1, null, 200);

                // MORPHIA OF MELITENE (salt 18)
                bool IsMorphia() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_6";

                starter.AddDialogLine("crus_morphia_greeting_1", "start", "lord_pretalk",
                    "{=crus_morphia_greet_1}My father ruled Melitene as an Armenian lord long before I wed a Latin count, and I have not forgotten which blood runs in these veins. What do you want of the Countess of Edessa?",
                    () => IsMorphia() && GetGreetingVariant(18, 2) == 0, null, 200);
                starter.AddDialogLine("crus_morphia_greeting_2", "start", "lord_pretalk",
                    "{=crus_morphia_greet_2}A husband twice taken captive teaches his wife to hold a fief alone, and hold it well. Speak your business, and do not mistake my patience for weakness.",
                    () => IsMorphia() && GetGreetingVariant(18, 2) == 1, null, 200);

                // TANCRED (salt 19)
                bool IsTancred() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_16";

                starter.AddDialogLine("crus_tancred_greeting_1", "start", "lord_pretalk",
                    "{=crus_tancred_greet_1}My uncle Bohemond taught me to take what Byzantium will not defend, and I have learned the lesson better than he ever expected. What do you want?",
                    () => IsTancred() && GetGreetingVariant(19, 2) == 0, null, 200);
                starter.AddDialogLine("crus_tancred_greeting_2", "start", "lord_pretalk",
                    "{=crus_tancred_greet_2}Baldwin of Boulogne and I have quarreled over Cilicia more than once, and I do not expect the last word has been said on it. Speak your business, and be quick about it.",
                    () => IsTancred() && GetGreetingVariant(19, 2) == 1, null, 200);

                // WALERAN OF LE PUISET (salt 20)
                bool IsWaleran() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_22";

                starter.AddDialogLine("crus_waleran_greeting_1", "start", "lord_pretalk",
                    "{=crus_waleran_greet_1}Bira Castle is a small charge on a dangerous frontier, but I hold it as faithfully as any count holds Edessa itself. What brings you to me?",
                    () => IsWaleran() && GetGreetingVariant(20, 2) == 0, null, 200);
                starter.AddDialogLine("crus_waleran_greeting_2", "start", "lord_pretalk",
                    "{=crus_waleran_greet_2}My family's name will mean more in this land before the century is out, mark me - though not always for reasons a man would choose. Speak your business.",
                    () => IsWaleran() && GetGreetingVariant(20, 2) == 1, null, 200);

                // BALDWIN OF BOULOGNE (salt 21)
                bool IsBaldwin1() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_23";

                starter.AddDialogLine("crus_baldwin1_greeting_1", "start", "lord_pretalk",
                    "{=crus_baldwin1_greet_1}My brother Godfrey took Jerusalem and refused its crown. I did not share his scruples - a kingdom needs a king, not a humble title and a dead man's caution. What do you want?",
                    () => IsBaldwin1() && GetGreetingVariant(21, 3) == 0, null, 200);
                starter.AddDialogLine("crus_baldwin1_greeting_2", "start", "lord_pretalk",
                    "{=crus_baldwin1_greet_2}I ruled Edessa first, as its very first Latin count, before Jerusalem's throne called me south. I do not forget where a man's fortune truly began. Speak your business.",
                    () => IsBaldwin1() && GetGreetingVariant(21, 3) == 1, null, 200);
                starter.AddDialogLine("crus_baldwin1_greeting_3", "start", "lord_pretalk",
                    "{=crus_baldwin1_greet_3}God gave the Franks this land twice over - once at Antioch, once at Jerusalem - and I intend to see that we keep both. What is your business here?",
                    () => IsBaldwin1() && GetGreetingVariant(21, 3) == 2, null, 200);

                // JOSCELIN OF COURTENAY (salt 22)
                bool IsJoscelin() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_4_24";

                starter.AddDialogLine("crus_joscelin_greeting_1", "start", "lord_pretalk",
                    "{=crus_joscelin_greet_1}Edessa's border touches Artuqid steel on every side, and I have learned to watch Balak's banners more closely than any other. What do you want?",
                    () => IsJoscelin() && GetGreetingVariant(22, 2) == 0, null, 200);
                starter.AddDialogLine("crus_joscelin_greeting_2", "start", "lord_pretalk",
                    "{=crus_joscelin_greet_2}A count who trusts too easily on this frontier ends up in an Artuqid dungeon - I intend to die free, whatever else God has planned for me. Speak your business.",
                    () => IsJoscelin() && GetGreetingVariant(22, 2) == 1, null, 200);

                // ========================= CILICIAN ARMENIA (battania) =========================

                // RUBEN I (salt 23)
                bool IsRuben1() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_1";

                starter.AddDialogLine("arm_ruben1_greeting_1", "start", "lord_pretalk",
                    "{=arm_ruben1_greet_1}I served the Bagratid crown honestly until Byzantium decided an Armenian's loyalty was worth less than his land - Kars taught me otherwise. What do you want in these mountains?",
                    () => IsRuben1() && GetGreetingVariant(23, 3) == 0, null, 200);
                starter.AddDialogLine("arm_ruben1_greeting_2", "start", "lord_pretalk",
                    "{=arm_ruben1_greet_2}Every fortress I hold, I built with men Byzantium abandoned and land Byzantium could not defend. Let them call it rebellion if they like. Speak your business.",
                    () => IsRuben1() && GetGreetingVariant(23, 3) == 1, null, 200);
                starter.AddDialogLine("arm_ruben1_greeting_3", "start", "lord_pretalk",
                    "{=arm_ruben1_greet_3}The Taurus mountains do not care whose flag flies over Constantinople - only who holds the passes. I hold them. What brings you to me?",
                    () => IsRuben1() && GetGreetingVariant(23, 3) == 2, null, 200);

                // THOROS I (salt 24)
                bool IsThoros1() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_3";

                starter.AddDialogLine("arm_thoros1_greeting_1", "start", "lord_pretalk",
                    "{=arm_thoros1_greet_1}My grandfather Ruben carved this principality from nothing but mountain rock and stubbornness. I intend to leave my own sons more than I inherited. What do you want?",
                    () => IsThoros1() && GetGreetingVariant(24, 2) == 0, null, 200);
                starter.AddDialogLine("arm_thoros1_greeting_2", "start", "lord_pretalk",
                    "{=arm_thoros1_greet_2}A cousin's ambition is a dangerous thing in this family, traveler - I have dealt with mine, and I do not regret it. Speak your business.",
                    () => IsThoros1() && GetGreetingVariant(24, 2) == 1, null, 200);

                // CONSTANTINE I (salt 25)
                bool IsConstantine1() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_5";

                starter.AddDialogLine("arm_constantine1_greeting_1", "start", "lord_pretalk",
                    "{=arm_constantine1_greet_1}My father held the mountains; I mean to hold the plain below them as well, whatever it costs in Byzantine or Crusader goodwill. What brings you to me?",
                    () => IsConstantine1() && GetGreetingVariant(25, 2) == 0, null, 200);
                starter.AddDialogLine("arm_constantine1_greeting_2", "start", "lord_pretalk",
                    "{=arm_constantine1_greet_2}An Armenian prince survives by choosing his allies as carefully as his enemies - today that may be Antioch, tomorrow it may not be. Speak your business.",
                    () => IsConstantine1() && GetGreetingVariant(25, 2) == 1, null, 200);

                // KOGH VASIL (salt 26)
                bool IsKoghVasil() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_14";

                starter.AddDialogLine("arm_koghvasil_greeting_1", "start", "lord_pretalk",
                    "{=arm_koghvasil_greet_1}They call me the Robber in Byzantine chronicles, and perhaps I have earned the name - Kaisun and Raban are mine because I took them, not because any emperor granted them. What do you want?",
                    () => IsKoghVasil() && GetGreetingVariant(26, 2) == 0, null, 200);
                starter.AddDialogLine("arm_koghvasil_greeting_2", "start", "lord_pretalk",
                    "{=arm_koghvasil_greet_2}I have no crown and no ancient house behind me, only a sword and the men who trust it. That has been enough so far. Speak your business.",
                    () => IsKoghVasil() && GetGreetingVariant(26, 2) == 1, null, 200);

                // VASIL DGHA (salt 27)
                bool IsVasilDgha() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_16";

                starter.AddDialogLine("arm_vasildgha_greeting_1", "start", "lord_pretalk",
                    "{=arm_vasildgha_greet_1}My father Vasil built this lordship from raw ambition alone, and left it to me though no blood tie bound us - I mean to prove his trust was not misplaced. What do you want?",
                    () => IsVasilDgha() && GetGreetingVariant(27, 2) == 0, null, 200);
                starter.AddDialogLine("arm_vasildgha_greeting_2", "start", "lord_pretalk",
                    "{=arm_vasildgha_greet_2}Kaisun and Raban sit between greater powers than mine, and I know it well. A wise heir knows when to bend and when to hold firm. Speak your business.",
                    () => IsVasilDgha() && GetGreetingVariant(27, 2) == 1, null, 200);

                // OSHIN OF LAMPRON (salt 28)
                bool IsOshinLampron() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_17";

                starter.AddDialogLine("arm_oshinlampron_greeting_1", "start", "lord_pretalk",
                    "{=arm_oshinlampron_greet_1}The Rubenids call themselves princes of all Armenians in Cilicia - Lampron answers to no house but its own. What brings you to my mountain?",
                    () => IsOshinLampron() && GetGreetingVariant(28, 2) == 0, null, 200);
                starter.AddDialogLine("arm_oshinlampron_greeting_2", "start", "lord_pretalk",
                    "{=arm_oshinlampron_greet_2}My family will hold this castle long after the current squabbles over Cilicia are forgotten chronicle entries, mark my words. What do you want?",
                    () => IsOshinLampron() && GetGreetingVariant(28, 2) == 1, null, 200);

                // GABRIEL OF MELITENE (salt 29)
                bool IsGabrielMelitene() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_5_15";

                starter.AddDialogLine("arm_gabrielmelitene_greeting_1", "start", "lord_pretalk",
                    "{=arm_gabrielmelitene_greet_1}My daughter Morphia sits beside a Latin count in Edessa now - stranger alliances have held longer than anyone expected. What do you want of Melitene?",
                    () => IsGabrielMelitene() && GetGreetingVariant(29, 2) == 0, null, 200);
                starter.AddDialogLine("arm_gabrielmelitene_greeting_2", "start", "lord_pretalk",
                    "{=arm_gabrielmelitene_greet_2}An Armenian lord this far from Cilicia's mountains survives on his wits and his walls alone. I have relied on both for years now. Speak your business.",
                    () => IsGabrielMelitene() && GetGreetingVariant(29, 2) == 1, null, 200);

                // ========================= KARA-KHANID KHANATE (khuzait) =========================

                // SHAMS AL-MULK NASR (salt 30)
                bool IsNasr() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_1";

                starter.AddDialogLine("krkh_nasr_greeting_1", "start", "lord_pretalk",
                    "{=krkh_nasr_greet_1}Transoxiana was Qarakhanid land before the Seljuks ever crossed the Oxus, and I have not forgotten it, whatever treaties my uncles signed. What do you want?",
                    () => IsNasr() && GetGreetingVariant(30, 2) == 0, null, 200);
                starter.AddDialogLine("krkh_nasr_greeting_2", "start", "lord_pretalk",
                    "{=krkh_nasr_greet_2}Bukhara and Samarkand both answer to my house, whatever the Seljuk sultans in Isfahan believe about the frontier. Speak your business.",
                    () => IsNasr() && GetGreetingVariant(30, 2) == 1, null, 200);

                // BUWI MARYAM (salt 31)
                bool IsBuwiMaryam() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_4";

                starter.AddDialogLine("krkh_buwimaryam_greeting_1", "start", "lord_pretalk",
                    "{=krkh_buwimaryam_greet_1}A khatun of this house has buried more husbands' ambitions than she has ever needed a sword to settle. What do you want of me?",
                    () => IsBuwiMaryam() && GetGreetingVariant(31, 2) == 0, null, 200);
                starter.AddDialogLine("krkh_buwimaryam_greeting_2", "start", "lord_pretalk",
                    "{=krkh_buwimaryam_greet_2}The steppe courts remember which women held a khanate together while the men rode off to lose it. I intend to be remembered well. Speak your business.",
                    () => IsBuwiMaryam() && GetGreetingVariant(31, 2) == 1, null, 200);

                // BUGHRA KHAN HARUN (salt 32)
                bool IsBughraHarun() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_5";

                starter.AddDialogLine("krkh_bughraharun_greeting_1", "start", "lord_pretalk",
                    "{=krkh_bughraharun_greet_1}I rode into Khorasan once when the Seljuks looked weak, and though I was pushed back, I proved the frontier is not as settled as Isfahan likes to believe. What do you want?",
                    () => IsBughraHarun() && GetGreetingVariant(32, 2) == 0, null, 200);
                starter.AddDialogLine("krkh_bughraharun_greeting_2", "start", "lord_pretalk",
                    "{=krkh_bughraharun_greet_2}A Bughra Khan does not forget an insult to his khanate, whatever the outcome of one campaign. Speak your business.",
                    () => IsBughraHarun() && GetGreetingVariant(32, 2) == 1, null, 200);

                // YUSUF TOGHRUL KHAN (salt 33)
                bool IsToghrulKhan() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_16";

                starter.AddDialogLine("krkh_toghrulkhan_greeting_1", "start", "lord_pretalk",
                    "{=krkh_toghrulkhan_greet_1}Toghrul was a great name among the Seljuks before it was ever mine - I intend to prove a Qarakhanid can wear it just as well. What do you want?",
                    () => IsToghrulKhan() && GetGreetingVariant(33, 2) == 0, null, 200);
                starter.AddDialogLine("krkh_toghrulkhan_greeting_2", "start", "lord_pretalk",
                    "{=krkh_toghrulkhan_greet_2}The steppe does not care whose ancestors won which battle first. It only cares who holds it now. Speak your business.",
                    () => IsToghrulKhan() && GetGreetingVariant(33, 2) == 1, null, 200);

                // HASAN IBN SULAYMAN (1 line)
                bool IsHasanIbnSulayman() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_17";

                starter.AddDialogLine("krkh_hasanibnsulayman_greeting_1", "start", "lord_pretalk",
                    "{=krkh_hasanibnsulayman_greet_1}Every prince of this house learns Bukhara's politics before he learns to hold a sword properly - I have mastered both by now. What do you want?",
                    IsHasanIbnSulayman, null, 200);

                // ALI-TEGIN (salt 34)
                bool IsAliTegin() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_18";

                starter.AddDialogLine("krkh_alitegin_greeting_1", "start", "lord_pretalk",
                    "{=krkh_alitegin_greet_1}I took Bukhara from under the noses of khans who thought themselves my betters, and I have held it against Ghaznavid and Qarakhanid alike ever since. What do you want?",
                    () => IsAliTegin() && GetGreetingVariant(34, 3) == 0, null, 200);
                starter.AddDialogLine("krkh_alitegin_greeting_2", "start", "lord_pretalk",
                    "{=krkh_alitegin_greet_2}They call me a rebel in every court from Kashgar to Ghazna, and I wear the title with more pride than any khan's blessing could give me. Speak your business.",
                    () => IsAliTegin() && GetGreetingVariant(34, 3) == 1, null, 200);
                starter.AddDialogLine("krkh_alitegin_greeting_3", "start", "lord_pretalk",
                    "{=krkh_alitegin_greet_3}A throne granted by inheritance is worth less than one taken and held by a man's own hand - I have proven that twice over now. What brings you to Bukhara?",
                    () => IsAliTegin() && GetGreetingVariant(34, 3) == 2, null, 200);

                // YUSUF QADIR KHAN (salt 35)
                bool IsQadirKhan() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_19";

                starter.AddDialogLine("krkh_qadirkhan_greeting_1", "start", "lord_pretalk",
                    "{=krkh_qadirkhan_greet_1}Kashgar and Khotan both bow to my khanate now, farther east than any Qarakhanid ruled before me. What do you want?",
                    () => IsQadirKhan() && GetGreetingVariant(35, 2) == 0, null, 200);
                starter.AddDialogLine("krkh_qadirkhan_greeting_2", "start", "lord_pretalk",
                    "{=krkh_qadirkhan_greet_2}I have brought scholars and poets to my court as readily as soldiers - a khanate remembered only for its swords does not last. Speak your business.",
                    () => IsQadirKhan() && GetGreetingVariant(35, 2) == 1, null, 200);

                // SATUQ BUGHRA KHAN (salt 36)
                bool IsSatukBughra() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_6_20";

                starter.AddDialogLine("krkh_satukbughra_greeting_1", "start", "lord_pretalk",
                    "{=krkh_satukbughra_greet_1}I was the first of my line to accept the Prophet's faith, against the wishes of my own uncle who held the throne before me - the whole steppe has followed where I led since. What do you want?",
                    () => IsSatukBughra() && GetGreetingVariant(36, 3) == 0, null, 200);
                starter.AddDialogLine("krkh_satukbughra_greeting_2", "start", "lord_pretalk",
                    "{=krkh_satukbughra_greet_2}A khan who changes his god changes his khanate's whole future, whether his court realizes it yet or not. I made that choice once, and I have never doubted it. Speak your business.",
                    () => IsSatukBughra() && GetGreetingVariant(36, 3) == 1, null, 200);
                starter.AddDialogLine("krkh_satukbughra_greeting_3", "start", "lord_pretalk",
                    "{=krkh_satukbughra_greet_3}Islam came to the Turkic steppe through me before it came through any sultan's army - remember that, whoever else claims the credit later. What brings you before me?",
                    () => IsSatukBughra() && GetGreetingVariant(36, 3) == 2, null, 200);

                // YUSUF BALASAGUNI (salt 37)
                bool IsBalasaguni() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_K9_l";

                starter.AddDialogLine("krkh_balasaguni_greeting_1", "start", "lord_pretalk",
                    "{=krkh_balasaguni_greet_1}I wrote a book of wisdom for princes who will rule long after my ink has dried, traveler - Kutadgu Bilig, I called it, the wisdom that brings fortune. What do you seek from me?",
                    () => IsBalasaguni() && GetGreetingVariant(37, 3) == 0, null, 200);
                starter.AddDialogLine("krkh_balasaguni_greeting_2", "start", "lord_pretalk",
                    "{=krkh_balasaguni_greet_2}A sword wins a khanate in an afternoon; a book of wise governance can hold one together for a hundred years. I have chosen to write the second kind. Speak your business.",
                    () => IsBalasaguni() && GetGreetingVariant(37, 3) == 1, null, 200);
                starter.AddDialogLine("krkh_balasaguni_greeting_3", "start", "lord_pretalk",
                    "{=krkh_balasaguni_greet_3}Kashgar's court asked me for poetry and I gave them a manual for ruling justly instead - some gifts outlast the giver's own lifetime. What do you want?",
                    () => IsBalasaguni() && GetGreetingVariant(37, 3) == 2, null, 200);

                // ========================= LATIN EMPIRE (empire_w) =========================

                // HENRY OF FLANDERS (salt 38)
                bool IsHenryFlanders() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_9";

                starter.AddDialogLine("lat_henryflanders_greeting_1", "start", "lord_pretalk",
                    "{=lat_henryflanders_greet_1}My brother Baldwin died in a Bulgarian dungeon believing Constantinople's Greeks would simply accept a Latin crown by conquest alone. I have found a gentler road holds an empire together far better than a spear. What do you want?",
                    () => IsHenryFlanders() && GetGreetingVariant(38, 3) == 0, null, 200);
                starter.AddDialogLine("lat_henryflanders_greeting_2", "start", "lord_pretalk",
                    "{=lat_henryflanders_greet_2}I married a Bulgarian princess for peace, not love, and I would do it again tomorrow if it kept the peace another year. An emperor's marriages belong to his empire, not himself. Speak your business.",
                    () => IsHenryFlanders() && GetGreetingVariant(38, 3) == 1, null, 200);
                starter.AddDialogLine("lat_henryflanders_greeting_3", "start", "lord_pretalk",
                    "{=lat_henryflanders_greet_3}Every Greek bishop and Latin baron in this empire expects me to favor his own side against the other. I have learned to disappoint both equally, and Romania is more stable for it. What brings you to Constantinople?",
                    () => IsHenryFlanders() && GetGreetingVariant(38, 3) == 2, null, 200);

                // MARCO SANUDO (salt 39)
                bool IsMarcoSanudo() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_53";

                starter.AddDialogLine("lat_marcosanudo_greeting_1", "start", "lord_pretalk",
                    "{=lat_marcosanudo_greet_1}I sailed from the fleet at Constantinople with a handful of ships and took Naxos for my own crown, not the Doge's - my uncle Dandolo would have done the same in my place. What do you want?",
                    () => IsMarcoSanudo() && GetGreetingVariant(39, 2) == 0, null, 200);
                starter.AddDialogLine("lat_marcosanudo_greeting_2", "start", "lord_pretalk",
                    "{=lat_marcosanudo_greet_2}The Cyclades answer to the Duke of the Archipelago now, and that duke answers to the Emperor in Constantinople, and to no one else in the Aegean. Speak your business.",
                    () => IsMarcoSanudo() && GetGreetingVariant(39, 2) == 1, null, 200);

                // ========================= BIZANS WEST BONUS (empire_s) =========================
                // 7 lords left in Kingdom.empire_s after the Latin Empire split (clan_empire_west_2
                // and _7 moved to empire_w) - real Byzantine noble house names, written from each
                // family's genuine historical reputation rather than an over-specific single
                // biography, since (unlike Bohemond or Alp Arslan) no one precisely-attested
                // individual matches each generated name.

                // THEODOROS LASKARIS (salt 40)
                bool IsLaskaris() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_7";

                starter.AddDialogLine("byz_laskaris_greeting_1", "start", "lord_pretalk",
                    "{=byz_laskaris_greet_1}The Laskaris name is not the oldest in this court, but I intend it will not be forgotten either, whatever the chroniclers choose to write of lesser men. What do you want?",
                    () => IsLaskaris() && GetGreetingVariant(40, 2) == 0, null, 200);
                starter.AddDialogLine("byz_laskaris_greeting_2", "start", "lord_pretalk",
                    "{=byz_laskaris_greet_2}Rome has weathered worse than a lost battle at Manzikert before, and my house means to be standing when it weathers this one too. Speak your business.",
                    () => IsLaskaris() && GetGreetingVariant(40, 2) == 1, null, 200);

                // ANDRONIKOS KONTOSTEPHANOS (salt 41)
                bool IsKontostephanos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_11";

                starter.AddDialogLine("byz_kontostephanos_greeting_1", "start", "lord_pretalk",
                    "{=byz_kontostephanos_greet_1}My family has served this Empire from the deck of a warship as often as from a palace hall, and I trust the sea's honesty more than any courtier's. What do you want?",
                    () => IsKontostephanos() && GetGreetingVariant(41, 2) == 0, null, 200);
                starter.AddDialogLine("byz_kontostephanos_greeting_2", "start", "lord_pretalk",
                    "{=byz_kontostephanos_greet_2}Lesbos was under my house's protection until the Latins took it by treaty and treachery both - I have not forgotten the debt. Speak your business.",
                    () => IsKontostephanos() && GetGreetingVariant(41, 2) == 1, null, 200);

                // IOANNES RAOUL (salt 42)
                bool IsRaoul() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_40";

                starter.AddDialogLine("byz_raoul_greeting_1", "start", "lord_pretalk",
                    "{=byz_raoul_greet_1}My grandfather's grandfather came to this Empire from Frankish lands and swore his sword to the Purple - I am Roman by oath if not entirely by blood, and I have never once regretted the trade. What do you want?",
                    () => IsRaoul() && GetGreetingVariant(42, 2) == 0, null, 200);
                starter.AddDialogLine("byz_raoul_greeting_2", "start", "lord_pretalk",
                    "{=byz_raoul_greet_2}A Raoul understands both Latin ambition and Greek pride better than most in this court - I was raised on both. Speak your business.",
                    () => IsRaoul() && GetGreetingVariant(42, 2) == 1, null, 200);

                // THEODOROS SYNADENOS (salt 43)
                bool IsSynadenos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_45";

                starter.AddDialogLine("byz_synadenos_greeting_1", "start", "lord_pretalk",
                    "{=byz_synadenos_greet_1}Synada gave its name to my house long before I was born to it, out in Phrygia where the frontier tests a man's loyalty more than any palace does. What do you want?",
                    () => IsSynadenos() && GetGreetingVariant(43, 2) == 0, null, 200);
                starter.AddDialogLine("byz_synadenos_greeting_2", "start", "lord_pretalk",
                    "{=byz_synadenos_greet_2}A provincial house like mine holds the Empire together in the places the capital forgets to look. Speak your business.",
                    () => IsSynadenos() && GetGreetingVariant(43, 2) == 1, null, 200);

                // ANDRONIKOS KAMATEROS (salt 44)
                bool IsKamateros() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_52";

                starter.AddDialogLine("byz_kamateros_greeting_1", "start", "lord_pretalk",
                    "{=byz_kamateros_greet_1}My house has produced more scribes and diplomats than soldiers, and I count that no shame - an Empire needs men who can write a treaty as much as men who can break one. What do you want?",
                    () => IsKamateros() && GetGreetingVariant(44, 2) == 0, null, 200);
                starter.AddDialogLine("byz_kamateros_greeting_2", "start", "lord_pretalk",
                    "{=byz_kamateros_greet_2}I have read enough Church councils' arguments to know that the sharpest blade in this Empire is sometimes just a well-chosen word. Speak your business.",
                    () => IsKamateros() && GetGreetingVariant(44, 2) == 1, null, 200);

                // NIKEPHOROS KOURTIKIOS (salt 45)
                bool IsKourtikios() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_71";

                starter.AddDialogLine("byz_kourtikios_greeting_1", "start", "lord_pretalk",
                    "{=byz_kourtikios_greet_1}The Kourtikios name has guarded the Anatolikon frontier since before any Turk crossed the Euphrates in force - we do not intend to stop now. What do you want?",
                    () => IsKourtikios() && GetGreetingVariant(45, 2) == 0, null, 200);
                starter.AddDialogLine("byz_kourtikios_greeting_2", "start", "lord_pretalk",
                    "{=byz_kourtikios_greet_2}A border family learns to read an enemy's intentions from dust on the horizon long before any messenger arrives. Speak your business.",
                    () => IsKourtikios() && GetGreetingVariant(45, 2) == 1, null, 200);

                // KONSTANTINOS MELIAS (salt 46)
                bool IsMelias() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_WE9_l";

                starter.AddDialogLine("byz_melias_greeting_1", "start", "lord_pretalk",
                    "{=byz_melias_greet_1}My ancestor Melias was an Armenian who carved out Lykandos for this Empire with his own sword and his own men - I carry an Armenian's blood and a Roman's oath both, and I see no contradiction in it. What do you want?",
                    () => IsMelias() && GetGreetingVariant(46, 2) == 0, null, 200);
                starter.AddDialogLine("byz_melias_greeting_2", "start", "lord_pretalk",
                    "{=byz_melias_greet_2}Armenian and Roman blood have mixed in my family for generations, and both halves have served this Empire well. Speak your business.",
                    () => IsMelias() && GetGreetingVariant(46, 2) == 1, null, 200);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
