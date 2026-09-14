using TaleWorlds.CampaignSystem;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Gives all 25 recruitable companions 3 alternative repeated-conversation greetings each,
    /// reflecting each one's documented profession/personality - same rotation mechanism as
    /// SeljukDialogueBehavior (see its GetGreetingVariant remarks). Companions already in the
    /// player's party route through the same "start"->"lord_start" flow as any other Hero
    /// conversation (confirmed by decompiling LordConversationsCampaignBehavior - the only
    /// companion-specific branch is the one-time "meet in main party" line, not the repeated path).
    /// </summary>
    public class CompanionPersonalityDialogueBehavior : CampaignBehaviorBase
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

        // Only fire on repeat conversations, so Native's own start_wanderer_unmet/introduction chain wins the first meeting.
        private static bool IsRepeatTalkWith(string stringId)
        {
            Hero h = Hero.OneToOneConversationHero;
            return h != null && h.HasMet && h.StringId == stringId;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // NASIR KHUSRAW DIALOGUE (salt 47)
                bool IsKhusraw() => IsRepeatTalkWith("spc_wanderer_seljuk_khusraw");

                starter.AddDialogLine("companion_khusraw_greeting_1", "start", "lord_start",
                    "{=companion_khusraw_greet_1}Seven years on the road taught me to trust a stranger's question more than a scholar's answer. So - what is it you actually want to know?",
                    () => IsKhusraw() && GetGreetingVariant(47, 3) == 0, null, 200);
                starter.AddDialogLine("companion_khusraw_greeting_2", "start", "lord_start",
                    "{=companion_khusraw_greet_2}I have not stopped walking since Khorasan, {PLAYER.NAME}, not in any way that matters. What troubles you tonight - and don't give me the answer you think I want.",
                    () => IsKhusraw() && GetGreetingVariant(47, 3) == 1, null, 200);
                starter.AddDialogLine("companion_khusraw_greeting_3", "start", "lord_start",
                    "{=companion_khusraw_greet_3}Every city I passed through had its own version of certainty, and every one of them was wrong about something. What's yours?",
                    () => IsKhusraw() && GetGreetingVariant(47, 3) == 2, null, 200);

                // OMAR KHAYYAM DIALOGUE (salt 48)
                bool IsKhayyam() => IsRepeatTalkWith("spc_wanderer_seljuk_khayyam");

                starter.AddDialogLine("companion_khayyam_greeting_1", "start", "lord_start",
                    "{=companion_khayyam_greet_1}Back so soon? I was in the middle of a calculation the tavern-keeper will never understand. What troubles you now?",
                    () => IsKhayyam() && GetGreetingVariant(48, 3) == 0, null, 200);
                starter.AddDialogLine("companion_khayyam_greeting_2", "start", "lord_start",
                    "{=companion_khayyam_greet_2}The stars keep their own counsel, {PLAYER.NAME}, same as I do. What is it you actually want to ask me?",
                    () => IsKhayyam() && GetGreetingVariant(48, 3) == 1, null, 200);
                starter.AddDialogLine("companion_khayyam_greeting_3", "start", "lord_start",
                    "{=companion_khayyam_greet_3}A wise man doubts twice before he answers once. Go on, then - ask, and let me doubt.",
                    () => IsKhayyam() && GetGreetingVariant(48, 3) == 2, null, 200);

                // MICHAEL PSELLOS DIALOGUE (salt 49)
                bool IsPsellos() => IsRepeatTalkWith("spc_wanderer_empire_psellos");

                starter.AddDialogLine("companion_psellos_greeting_1", "start", "lord_start",
                    "{=companion_psellos_greet_1}Ah, you return - and here I thought I'd exhausted my store of flattering things to say about your campaign. Sit, sit. Tell me what fresh crisis requires my particular talent for making bad news sound like strategy.",
                    () => IsPsellos() && GetGreetingVariant(49, 3) == 0, null, 200);
                starter.AddDialogLine("companion_psellos_greeting_2", "start", "lord_start",
                    "{=companion_psellos_greet_2}I have outlived four emperors' good opinion of me and most of their reigns besides, {PLAYER.NAME}. Whatever you're about to ask, I promise I've survived worse questions from far grander company.",
                    () => IsPsellos() && GetGreetingVariant(49, 3) == 1, null, 200);
                starter.AddDialogLine("companion_psellos_greeting_3", "start", "lord_start",
                    "{=companion_psellos_greet_3}A rhetorician's gift is knowing exactly how much truth a man can bear before dinner. Ask your question, and I shall calibrate my answer accordingly.",
                    () => IsPsellos() && GetGreetingVariant(49, 3) == 2, null, 200);

                // ROUSSEL DE BAILLEUL DIALOGUE (salt 50)
                bool IsRoussel() => IsRepeatTalkWith("spc_wanderer_empire_roussel");

                starter.AddDialogLine("companion_roussel_greeting_1", "start", "lord_start",
                    "{=companion_roussel_greet_1}Still here, still breathing - that's more than most men who followed me into Anatolia can say. What do you want?",
                    () => IsRoussel() && GetGreetingVariant(50, 3) == 0, null, 200);
                starter.AddDialogLine("companion_roussel_greeting_2", "start", "lord_start",
                    "{=companion_roussel_greet_2}I held a city once with nothing but my own lances and my own nerve. Speak plainly, {PLAYER.NAME} - I've no patience left for anything else.",
                    () => IsRoussel() && GetGreetingVariant(50, 3) == 1, null, 200);
                starter.AddDialogLine("companion_roussel_greeting_3", "start", "lord_start",
                    "{=companion_roussel_greet_3}Chains taught me more patience than Manzikert ever did. Get to the point.",
                    () => IsRoussel() && GetGreetingVariant(50, 3) == 2, null, 200);

                // AL-GHAZALI DIALOGUE (salt 51)
                bool IsGhazali() => IsRepeatTalkWith("spc_wanderer_aserai_ghazali");

                starter.AddDialogLine("companion_ghazali_greeting_1", "start", "lord_start",
                    "{=companion_ghazali_greet_1}I once taught certainty to men who paid well to hear it. I no longer trust a certainty that comes that easily - mine or yours. What is it you seek?",
                    () => IsGhazali() && GetGreetingVariant(51, 3) == 0, null, 200);
                starter.AddDialogLine("companion_ghazali_greeting_2", "start", "lord_start",
                    "{=companion_ghazali_greet_2}The robe I wear now cost me the finest teaching post in Baghdad, and I have never once regretted the trade. Speak, and I will listen as carefully as I once argued.",
                    () => IsGhazali() && GetGreetingVariant(51, 3) == 1, null, 200);
                starter.AddDialogLine("companion_ghazali_greeting_3", "start", "lord_start",
                    "{=companion_ghazali_greet_3}A man who has doubted everything he was taught learns to measure his words before he spends them. So - measure yours, and tell me what troubles you.",
                    () => IsGhazali() && GetGreetingVariant(51, 3) == 2, null, 200);

                // USAMA IBN MUNQIDH DIALOGUE (salt 52)
                bool IsUsama() => IsRepeatTalkWith("spc_wanderer_aserai_usama");

                starter.AddDialogLine("companion_usama_greeting_1", "start", "lord_start",
                    "{=companion_usama_greet_1}Ask any man in Shaizar - I have never once had to choose between the blade and the verse, and I'm not about to start now. What do you need, sword or word?",
                    () => IsUsama() && GetGreetingVariant(52, 3) == 0, null, 200);
                starter.AddDialogLine("companion_usama_greeting_2", "start", "lord_start",
                    "{=companion_usama_greet_2}I have hunted lions, crossed spears with Frankish knights, and written poetry about all three before the ink on my boots had dried. What's troubling you?",
                    () => IsUsama() && GetGreetingVariant(52, 3) == 1, null, 200);
                starter.AddDialogLine("companion_usama_greeting_3", "start", "lord_start",
                    "{=companion_usama_greet_3}A man who knows only the sword is half a man in my eyes, {PLAYER.NAME} - and I've shared enough tables with Franks to know a few of them agree with me. Speak.",
                    () => IsUsama() && GetGreetingVariant(52, 3) == 2, null, 200);

                // IOANE PETRITSI DIALOGUE (salt 53)
                bool IsPetritsi() => IsRepeatTalkWith("spc_wanderer_sturgia_petritsi");

                starter.AddDialogLine("companion_petritsi_greeting_1", "start", "lord_start",
                    "{=companion_petritsi_greet_1}I spent years translating a pagan philosopher our own priests still eye with suspicion, and I have made my peace with never fully finishing that argument. What is it you're searching for tonight?",
                    () => IsPetritsi() && GetGreetingVariant(53, 3) == 0, null, 200);
                starter.AddDialogLine("companion_petritsi_greeting_2", "start", "lord_start",
                    "{=companion_petritsi_greet_2}Some call what I brought back from Constantinople heresy, others call it wisdom. I have learned to hold both possibilities gently. Speak, and I will listen the same way.",
                    () => IsPetritsi() && GetGreetingVariant(53, 3) == 1, null, 200);
                starter.AddDialogLine("companion_petritsi_greeting_3", "start", "lord_start",
                    "{=companion_petritsi_greet_3}A philosopher learns patience from unfinished questions, {PLAYER.NAME} - I have a great many of those. What is yours?",
                    () => IsPetritsi() && GetGreetingVariant(53, 3) == 2, null, 200);

                // VARDAN OF SVANETI DIALOGUE (salt 54)
                bool IsVardan() => IsRepeatTalkWith("spc_wanderer_sturgia_vardan");

                starter.AddDialogLine("companion_vardan_greeting_1", "start", "lord_start",
                    "{=companion_vardan_greet_1}I raised the Svans against a king who wanted to swallow our valleys whole. I'd do it again, whatever it cost me. What do you want?",
                    () => IsVardan() && GetGreetingVariant(54, 3) == 0, null, 200);
                starter.AddDialogLine("companion_vardan_greeting_2", "start", "lord_start",
                    "{=companion_vardan_greet_2}The duchy is gone; the sword and the grudge remain. Speak your business and be quick about it.",
                    () => IsVardan() && GetGreetingVariant(54, 3) == 1, null, 200);
                starter.AddDialogLine("companion_vardan_greeting_3", "start", "lord_start",
                    "{=companion_vardan_greet_3}Svaneti still answers to itself in my heart, whatever the lowlands say. What is it?",
                    () => IsVardan() && GetGreetingVariant(54, 3) == 2, null, 200);

                // PETER THE HERMIT DIALOGUE (salt 55)
                bool IsPeterHermit() => IsRepeatTalkWith("spc_wanderer_vlandia_peterhermit");

                starter.AddDialogLine("companion_peterhermit_greeting_1", "start", "lord_start",
                    "{=companion_peterhermit_greet_1}I led thousands east on faith alone once, and buried most of them before we ever reached the Holy Land. I still believe, {PLAYER.NAME} - I have simply learned not to trust my own certainty to lead anyone but myself. What is it?",
                    () => IsPeterHermit() && GetGreetingVariant(55, 3) == 0, null, 200);
                starter.AddDialogLine("companion_peterhermit_greeting_2", "start", "lord_start",
                    "{=companion_peterhermit_greet_2}God did not clear our path at Nicaea, whatever I promised the faithful who followed me. I carry that every day I still draw breath. Speak, what troubles you?",
                    () => IsPeterHermit() && GetGreetingVariant(55, 3) == 1, null, 200);
                starter.AddDialogLine("companion_peterhermit_greeting_3", "start", "lord_start",
                    "{=companion_peterhermit_greet_3}I am not ashamed of the faith - only of what my faith cost the men who trusted me too completely. Tell me what you need, and I will not pretend to have easy answers.",
                    () => IsPeterHermit() && GetGreetingVariant(55, 3) == 2, null, 200);

                // PETER BARTHOLOMEW DIALOGUE (salt 56)
                bool IsBartholomew() => IsRepeatTalkWith("spc_wanderer_vlandia_bartholomew");

                starter.AddDialogLine("companion_bartholomew_greeting_1", "start", "lord_start",
                    "{=companion_bartholomew_greet_1}I held the Lance up before a starving army with nothing left, and the siege broke the very next day. Believe what you like about the vision - I know what I saw. What do you want?",
                    () => IsBartholomew() && GetGreetingVariant(56, 3) == 0, null, 200);
                starter.AddDialogLine("companion_bartholomew_greeting_2", "start", "lord_start",
                    "{=companion_bartholomew_greet_2}Saint Andrew came to me when no one else would listen, {PLAYER.NAME}. I walked through fire to prove it once. I won't do it twice, but I won't apologize for it either. Speak.",
                    () => IsBartholomew() && GetGreetingVariant(56, 3) == 1, null, 200);
                starter.AddDialogLine("companion_bartholomew_greeting_3", "start", "lord_start",
                    "{=companion_bartholomew_greet_3}Some men still whisper 'fraud' when they think I can't hear it. I dug where the vision told me to dig, and the Lance was there. What is it you need?",
                    () => IsBartholomew() && GetGreetingVariant(56, 3) == 2, null, 200);

                // MATTHEW OF EDESSA DIALOGUE (salt 57)
                bool IsMatthew() => IsRepeatTalkWith("spc_wanderer_battania_matthew");

                starter.AddDialogLine("companion_matthew_greeting_1", "start", "lord_start",
                    "{=companion_matthew_greet_1}I have recorded three different rulers' versions of this land in my own lifetime, and no two of them agreed on a single fact worth keeping. What would you like entered into the record tonight?",
                    () => IsMatthew() && GetGreetingVariant(57, 3) == 0, null, 200);
                starter.AddDialogLine("companion_matthew_greeting_2", "start", "lord_start",
                    "{=companion_matthew_greet_2}History written only by the victors is not history, {PLAYER.NAME} - it is flattery with better handwriting. Speak, and I will judge later which page to show which lord.",
                    () => IsMatthew() && GetGreetingVariant(57, 3) == 1, null, 200);
                starter.AddDialogLine("companion_matthew_greeting_3", "start", "lord_start",
                    "{=companion_matthew_greet_3}Edessa has changed hands so many times I keep a separate ledger just for its rulers. What is your business?",
                    () => IsMatthew() && GetGreetingVariant(57, 3) == 2, null, 200);

                // MKHITAR HERATSI DIALOGUE (salt 58)
                bool IsHeratsi() => IsRepeatTalkWith("spc_wanderer_battania_heratsi");

                starter.AddDialogLine("companion_heratsi_greeting_1", "start", "lord_start",
                    "{=companion_heratsi_greet_1}I have written down everything I know of fevers so it doesn't die with me in some tent no one remembers. What ails you tonight - or is it something a physician can't treat?",
                    () => IsHeratsi() && GetGreetingVariant(58, 3) == 0, null, 200);
                starter.AddDialogLine("companion_heratsi_greeting_2", "start", "lord_start",
                    "{=companion_heratsi_greet_2}A man who dies of a wound a trained hand could have caught is a death I take personally, {PLAYER.NAME}. Tell me what's wrong, and let me judge whether it's serious.",
                    () => IsHeratsi() && GetGreetingVariant(58, 3) == 1, null, 200);
                starter.AddDialogLine("companion_heratsi_greeting_3", "start", "lord_start",
                    "{=companion_heratsi_greet_3}My guild would rather I guarded my knowledge than gave it away. I disagree with them daily. What do you need?",
                    () => IsHeratsi() && GetGreetingVariant(58, 3) == 2, null, 200);

                // MAHMUD AL-KASHGARI DIALOGUE (salt 59)
                bool IsKashgari() => IsRepeatTalkWith("spc_wanderer_khuzait_kashgari");

                starter.AddDialogLine("companion_kashgari_greeting_1", "start", "lord_start",
                    "{=companion_kashgari_greet_1}Every dialect I ever recorded had its own word for 'home' that no other tribe quite shared. Tell me, {PLAYER.NAME} - where does your own tongue say you're from?",
                    () => IsKashgari() && GetGreetingVariant(59, 3) == 0, null, 200);
                starter.AddDialogLine("companion_kashgari_greeting_2", "start", "lord_start",
                    "{=companion_kashgari_greet_2}I gave up a comfortable life near Kashgar to prove that Oghuz and Kipchak and Karluk are all branches of one tree. I'm still collecting proof. What have you brought me today?",
                    () => IsKashgari() && GetGreetingVariant(59, 3) == 1, null, 200);
                starter.AddDialogLine("companion_kashgari_greeting_3", "start", "lord_start",
                    "{=companion_kashgari_greet_3}A man's speech tells me more about where he's from than he usually means to reveal. Speak plainly - I promise I'm only half listening for the dialect.",
                    () => IsKashgari() && GetGreetingVariant(59, 3) == 2, null, 200);

                // AHMAD YASAWI DIALOGUE (salt 60)
                bool IsYasawi() => IsRepeatTalkWith("spc_wanderer_khuzait_yasawi");

                starter.AddDialogLine("companion_yasawi_greeting_1", "start", "lord_start",
                    "{=companion_yasawi_greet_1}A man does not need Arabic argument to find God, only a verse he can carry in his own tongue. What is it you carry tonight?",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 0, null, 200);
                starter.AddDialogLine("companion_yasawi_greeting_2", "start", "lord_start",
                    "{=companion_yasawi_greet_2}I lost my father young and found a teacher instead. Loss has a way of doing that, if you let it. What troubles you, {PLAYER.NAME}?",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 1, null, 200);
                starter.AddDialogLine("companion_yasawi_greeting_3", "start", "lord_start",
                    "{=companion_yasawi_greet_3}The scholars in the cities call my verses simple. The frightened men I've sat with call them enough. Speak, and I will listen the way I listen to them.",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 2, null, 200);

                // WANDERER 0 "THE KNOWING" DIALOGUE (salt 61)
                bool IsWanderer0() => IsRepeatTalkWith("spc_wanderer_seljuk_0");

                starter.AddDialogLine("companion_wanderer0_greeting_1", "start", "lord_start",
                    "{=companion_wanderer0_greet_1}My father learned to break walls from Byzantine captives who once thought they had nothing to teach a Turkmen. I stopped being surprised by what a man learns when the alternative is dying. What is it you want broken, {PLAYER.NAME}?",
                    () => IsWanderer0() && GetGreetingVariant(61, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer0_greeting_2", "start", "lord_start",
                    "{=companion_wanderer0_greet_2}Every wall looks eternal until you know exactly where to strike it. My father taught me that lesson before he taught me my letters. Is there some wall of yours that needs reducing to rubble?",
                    () => IsWanderer0() && GetGreetingVariant(61, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer0_greeting_3", "start", "lord_start",
                    "{=companion_wanderer0_greet_3}The beys pay less for a siege-master than they once did, and pay it late besides. Perhaps that is its own kind of lesson too, about walls and about promises. What do you need?",
                    () => IsWanderer0() && GetGreetingVariant(61, 3) == 2, null, 200);

                // WANDERER 1 "THE HAWK" DIALOGUE (salt 62)
                bool IsWanderer1() => IsRepeatTalkWith("spc_wanderer_seljuk_1");

                starter.AddDialogLine("companion_wanderer1_greeting_1", "start", "lord_start",
                    "{=companion_wanderer1_greet_1}Fifty sheep was a fair bride-price. Her brother called it not enough, then had no answer when I asked what would be. Some questions get answered with a fist instead of a word.",
                    () => IsWanderer1() && GetGreetingVariant(62, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer1_greeting_2", "start", "lord_start",
                    "{=companion_wanderer1_greet_2}My own brothers struck a bargain to save their skins rather than stand behind mine. I watch men closely now, {PLAYER.NAME} - to see which sort would do the same.",
                    () => IsWanderer1() && GetGreetingVariant(62, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer1_greeting_3", "start", "lord_start",
                    "{=companion_wanderer1_greet_3}Speak plainly. I have little patience left for men who talk around a thing instead of through it.",
                    () => IsWanderer1() && GetGreetingVariant(62, 3) == 2, null, 200);

                // WANDERER 2 "THE FATHERLESS" DIALOGUE (salt 63)
                bool IsWanderer2() => IsRepeatTalkWith("spc_wanderer_seljuk_2");

                starter.AddDialogLine("companion_wanderer2_greeting_1", "start", "lord_start",
                    "{=companion_wanderer2_greet_1}Plague took the man who owed my father's blood before I ever raised a hand to him. My own kin wanted me to spend his brother's life to settle the account anyway. I chose otherwise.",
                    () => IsWanderer2() && GetGreetingVariant(63, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer2_greeting_2", "start", "lord_start",
                    "{=companion_wanderer2_greet_2}I have a gift for ending quarrels with words instead of knives, {PLAYER.NAME} - my family didn't thank me for it. Let's see what sort of man you are before I decide how much of that gift you're owed.",
                    () => IsWanderer2() && GetGreetingVariant(63, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer2_greeting_3", "start", "lord_start",
                    "{=companion_wanderer2_greet_3}Here, at least, I choose what I do and for whom I do it. That is worth more to me than any clan's good opinion. What do you want?",
                    () => IsWanderer2() && GetGreetingVariant(63, 3) == 2, null, 200);

                // WANDERER 3 "IRONEYE" DIALOGUE (salt 64)
                bool IsWanderer3() => IsRepeatTalkWith("spc_wanderer_seljuk_3");

                starter.AddDialogLine("companion_wanderer3_greeting_1", "start", "lord_start",
                    "{=companion_wanderer3_greet_1}Still breathing, I see. Good - means you haven't needed me yet.",
                    () => IsWanderer3() && GetGreetingVariant(64, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer3_greeting_2", "start", "lord_start",
                    "{=companion_wanderer3_greet_2}Keep your voice down. A man who talks too much in camp dies quietly later.",
                    () => IsWanderer3() && GetGreetingVariant(64, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer3_greeting_3", "start", "lord_start",
                    "{=companion_wanderer3_greet_3}I've been counting arrows. We have enough. For now.",
                    () => IsWanderer3() && GetGreetingVariant(64, 3) == 2, null, 200);

                // WANDERER 4 "THE OUTCAST" DIALOGUE (salt 65)
                bool IsWanderer4() => IsRepeatTalkWith("spc_wanderer_seljuk_4");

                starter.AddDialogLine("companion_wanderer4_greeting_1", "start", "lord_start",
                    "{=companion_wanderer4_greet_1}My own clan cast me out for killing a man who called us servants once too often. I don't regret the blow. I regret what it cost me - every fire I once called mine.",
                    () => IsWanderer4() && GetGreetingVariant(65, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer4_greeting_2", "start", "lord_start",
                    "{=companion_wanderer4_greet_2}There are merchants in these towns who ask no questions about where a man's coin comes from. I've dealt with enough of them to know exactly what silence like that is worth.",
                    () => IsWanderer4() && GetGreetingVariant(65, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer4_greeting_3", "start", "lord_start",
                    "{=companion_wanderer4_greet_3}We lost our flocks to a Byzantine raid, and our pride to the clan that sheltered us afterward. I got the pride back the hard way, {PLAYER.NAME}. What do you want?",
                    () => IsWanderer4() && GetGreetingVariant(65, 3) == 2, null, 200);

                // WANDERER 5 "THE MAD" DIALOGUE (salt 66)
                bool IsWanderer5() => IsRepeatTalkWith("spc_wanderer_seljuk_5");

                starter.AddDialogLine("companion_wanderer5_greeting_1", "start", "lord_start",
                    "{=companion_wanderer5_greet_1}My father taught me to work a drunk man's purse before I could grow a proper beard. He also taught me pride costs more than it's worth - he just learned that lesson on the gallows instead of before it.",
                    () => IsWanderer5() && GetGreetingVariant(66, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer5_greeting_2", "start", "lord_start",
                    "{=companion_wanderer5_greet_2}Mind how you speak to me tonight. I'm in one of my moods, and moods like mine have hurt better men than you. ...Sit anyway, if you like. I don't bite everyone.",
                    () => IsWanderer5() && GetGreetingVariant(66, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer5_greeting_3", "start", "lord_start",
                    "{=companion_wanderer5_greet_3}Every trade wants finesse, my father used to say - right before his pride got him hanged for refusing to pay a guard captain what he asked. I try to remember the finesse more than the pride.",
                    () => IsWanderer5() && GetGreetingVariant(66, 3) == 2, null, 200);

                // WANDERER 6 "THE GREY FALCON" DIALOGUE (salt 67)
                bool IsWanderer6() => IsRepeatTalkWith("spc_wanderer_seljuk_6");

                starter.AddDialogLine("companion_wanderer6_greeting_1", "start", "lord_start",
                    "{=companion_wanderer6_greet_1}My people refused the Sultan's decree to settle down and be counted like sheep for the tax rolls. We got hunted down and counted anyway - just with fewer flocks left over.",
                    () => IsWanderer6() && GetGreetingVariant(67, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer6_greeting_2", "start", "lord_start",
                    "{=companion_wanderer6_greet_2}I know little of the steppe these days, if I'm honest, {PLAYER.NAME}. Spend a generation in back alleys instead of open grass and see how much of it you remember.",
                    () => IsWanderer6() && GetGreetingVariant(67, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer6_greeting_3", "start", "lord_start",
                    "{=companion_wanderer6_greet_3}I mean to earn back what was taken from my people - new herds, and grass to graze them on. Slow work, buying freedom one coin at a time. Have you got some for me?",
                    () => IsWanderer6() && GetGreetingVariant(67, 3) == 2, null, 200);

                // WANDERER 7 "THE SHE-WOLF" DIALOGUE (salt 68)
                bool IsWanderer7() => IsRepeatTalkWith("spc_wanderer_seljuk_7");

                starter.AddDialogLine("companion_wanderer7_greeting_1", "start", "lord_start",
                    "{=companion_wanderer7_greet_1}More than a score of men have felt my blade in a rich man's private arena, and every one of them was trying to kill me first. Spare me the look - I've seen it before, and it's never once impressed me.",
                    () => IsWanderer7() && GetGreetingVariant(68, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer7_greeting_2", "start", "lord_start",
                    "{=companion_wanderer7_greet_2}I bought my own freedom with winnings from fights I never chose to be in. I didn't do it to be admired, and I don't need your permission to be proud of it.",
                    () => IsWanderer7() && GetGreetingVariant(68, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer7_greeting_3", "start", "lord_start",
                    "{=companion_wanderer7_greet_3}I fought for another man's amusement long enough to know exactly what condescension looks like on a face, {PLAYER.NAME}. Watch yours, if you mean to keep talking to me.",
                    () => IsWanderer7() && GetGreetingVariant(68, 3) == 2, null, 200);

                // WANDERER 8 "THE ALONE" DIALOGUE (salt 69)
                bool IsWanderer8() => IsRepeatTalkWith("spc_wanderer_seljuk_8");

                starter.AddDialogLine("companion_wanderer8_greeting_1", "start", "lord_start",
                    "{=companion_wanderer8_greet_1}My mother raised me as the son she never had. She's gone now. So is the clan that turned on us for it. I don't much want to talk about which came first.",
                    () => IsWanderer8() && GetGreetingVariant(69, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer8_greeting_2", "start", "lord_start",
                    "{=companion_wanderer8_greet_2}I took a sword and a horse off the man who helped end my mother's life. That's most of what you need to know about me.",
                    () => IsWanderer8() && GetGreetingVariant(69, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer8_greeting_3", "start", "lord_start",
                    "{=companion_wanderer8_greet_3}Say what you need. I've never been one for long talk, before or after.",
                    () => IsWanderer8() && GetGreetingVariant(69, 3) == 2, null, 200);

                // WANDERER 9 "THE SWIFT" DIALOGUE (salt 70)
                bool IsWanderer9() => IsRepeatTalkWith("spc_wanderer_seljuk_9");

                starter.AddDialogLine("companion_wanderer9_greeting_1", "start", "lord_start",
                    "{=companion_wanderer9_greet_1}I grew up running between camel legs on the trade road, and I've never quite stopped running since. Say your piece quickly - I'm no good at standing still.",
                    () => IsWanderer9() && GetGreetingVariant(70, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer9_greeting_2", "start", "lord_start",
                    "{=companion_wanderer9_greet_2}My father wanted a husband found for me. I told him I'd rather carry a blade and guard the goods myself, {PLAYER.NAME}. He grumbled, but here I am, and here's my blade.",
                    () => IsWanderer9() && GetGreetingVariant(70, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer9_greeting_3", "start", "lord_start",
                    "{=companion_wanderer9_greet_3}Even sitting still, some part of me is watching the door - old habit, from years guarding caravans on the road. What do you need?",
                    () => IsWanderer9() && GetGreetingVariant(70, 3) == 2, null, 200);

                // WANDERER 10 "THE RAGGED" DIALOGUE (salt 71)
                bool IsWanderer10() => IsRepeatTalkWith("spc_wanderer_seljuk_10");

                starter.AddDialogLine("companion_wanderer10_greeting_1", "start", "lord_start",
                    "{=companion_wanderer10_greet_1}I tried to steal a horse once, to run off and start a new life among the free clans. The wretched animal threw me before I'd gone a mile and trotted straight back to its owner. I've learned to laugh about it since - mostly.",
                    () => IsWanderer10() && GetGreetingVariant(71, 3) == 0, null, 200);
                starter.AddDialogLine("companion_wanderer10_greeting_2", "start", "lord_start",
                    "{=companion_wanderer10_greet_2}My father was a peddler, mending broken things door to door for a coin here, a coin there. I don't want that life, {PLAYER.NAME}. I don't rightly know what I want instead, but I'm grateful for any road that isn't his.",
                    () => IsWanderer10() && GetGreetingVariant(71, 3) == 1, null, 200);
                starter.AddDialogLine("companion_wanderer10_greeting_3", "start", "lord_start",
                    "{=companion_wanderer10_greet_3}I mean to be one of the hawks in this world someday, not one of the pigeons the horse-lords ride over. It's a small thing to hope for. Doesn't stop me hoping it.",
                    () => IsWanderer10() && GetGreetingVariant(71, 3) == 2, null, 200);
            }
            catch (System.Exception)
            {
                // Matches this mod's established defensive pattern for Immersion behaviors.
            }
        }
    }
}
