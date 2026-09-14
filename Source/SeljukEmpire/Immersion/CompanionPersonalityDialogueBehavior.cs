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

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // Named historical companions (salts 47-60) go here in Step 3.
                // Generic Seljuk wanderers (salts 61-71) go here in Task 2.

                // NASIR KHUSRAW DIALOGUE (salt 47)
                bool IsKhusraw() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_seljuk_khusraw";

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
                bool IsKhayyam() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_seljuk_khayyam";

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
                bool IsPsellos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_empire_psellos";

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
                bool IsRoussel() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_empire_roussel";

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
                bool IsGhazali() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_aserai_ghazali";

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
                bool IsUsama() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_aserai_usama";

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
                bool IsPetritsi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_sturgia_petritsi";

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
                bool IsVardan() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_sturgia_vardan";

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
                bool IsPeterHermit() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_vlandia_peterhermit";

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
                bool IsBartholomew() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_vlandia_bartholomew";

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
                bool IsMatthew() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_battania_matthew";

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
                bool IsHeratsi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_battania_heratsi";

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
                bool IsKashgari() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_khuzait_kashgari";

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
                bool IsYasawi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "spc_wanderer_khuzait_yasawi";

                starter.AddDialogLine("companion_yasawi_greeting_1", "start", "lord_start",
                    "{=companion_yasawi_greet_1}A man does not need Arabic argument to find God, only a verse he can carry in his own tongue. What is it you carry tonight?",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 0, null, 200);
                starter.AddDialogLine("companion_yasawi_greeting_2", "start", "lord_start",
                    "{=companion_yasawi_greet_2}I lost my father young and found a teacher instead. Loss has a way of doing that, if you let it. What troubles you, {PLAYER.NAME}?",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 1, null, 200);
                starter.AddDialogLine("companion_yasawi_greeting_3", "start", "lord_start",
                    "{=companion_yasawi_greet_3}The scholars in the cities call my verses simple. The frightened men I've sat with call them enough. Speak, and I will listen the way I listen to them.",
                    () => IsYasawi() && GetGreetingVariant(60, 3) == 2, null, 200);
            }
            catch (System.Exception)
            {
                // Matches this mod's established defensive pattern for Immersion behaviors.
            }
        }
    }
}
