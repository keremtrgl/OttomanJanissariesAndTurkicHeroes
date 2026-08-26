using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Injects authentic historical greetings for Sultan Alp Arslan, Ertugrul Gazi, and
    /// Nizamulmulk. Each has 3 alternative greeting lines rather than one repeated verbatim on
    /// every conversation - see GetGreetingVariant's remarks for how the "current" one is picked.
    /// </summary>
    public class SeljukDialogueBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Transient dialog behavior
        }

        /// <summary>
        /// Bannerlord's ConversationManager sorts dialog lines by priority and, for an NPC line,
        /// shows the FIRST one whose condition matches (confirmed by decompiling
        /// ConversationManager.GetSentenceOptions) - it does not pick randomly among equal-priority
        /// matches. So a plain second AddDialogLine with the same tokens/priority would never be
        /// seen: the first-registered line would always win. To get real variety instead, each of a
        /// character's candidate lines gets a condition that is true only during a specific
        /// slice of in-game hours, so on any single conversation exactly one line's condition is
        /// true (the earlier ones are re-evaluated too, but only one matches). All three
        /// evaluations happen within the same conversation-open resolve pass, at the same
        /// CampaignTime.Now, so the line never changes mid-conversation - but a different one of
        /// the three can show up on a later visit, at a different in-game hour.
        /// </summary>
        private static int GetGreetingVariant(int salt, int variantCount)
        {
            return ((int)CampaignTime.Now.ToHours + salt) % variantCount;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // SULTAN ALP ARSLAN DIALOGUE (salt 0)
                bool IsAlpArslan() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_seljuk_alp_arslan";

                starter.AddDialogLine("seljuk_alp_arslan_greeting_1", "start", "lord_pretalk",
                    "{=seljuk_alp_arslan_greet}Valiant alp of the Ghazavat fields! Welcome, may your blade remain sharp and your order steadfast. The House of Seljuk bestows its blessing upon you. What do you seek?",
                    () => IsAlpArslan() && GetGreetingVariant(0, 3) == 0, null, 200);
                starter.AddDialogLine("seljuk_alp_arslan_greeting_2", "start", "lord_pretalk",
                    "{=seljuk_alp_arslan_greet_2}The steppe wind carries word of your deeds before you even arrive. Sit, and tell me - does Rum still resist, or does its resolve finally crack?",
                    () => IsAlpArslan() && GetGreetingVariant(0, 3) == 1, null, 200);
                starter.AddDialogLine("seljuk_alp_arslan_greeting_3", "start", "lord_pretalk",
                    "{=seljuk_alp_arslan_greet_3}Every alp who serves the Sultanate honestly is worth ten who serve only for gold. I judge you have not yet decided which you are. Speak, and let us find out.",
                    () => IsAlpArslan() && GetGreetingVariant(0, 3) == 2, null, 200);

                // ERTUGRUL GAZI DIALOGUE (salt 1)
                bool IsErtugrul() => Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.StringId == "ertugrul_gazi" || Hero.OneToOneConversationHero.StringId == "lord_seljuk_ertugrul_gazi");

                starter.AddDialogLine("seljuk_ertugrul_greeting_1", "start", "lord_pretalk",
                    "{=seljuk_ertugrul_greet}O companion of Gaza! A thousand greetings from the frontier of Sogut and the Kayi encampment. May your steel shine forever, what brings you to my tent?",
                    () => IsErtugrul() && GetGreetingVariant(1, 3) == 0, null, 200);
                starter.AddDialogLine("seljuk_ertugrul_greeting_2", "start", "lord_pretalk",
                    "{=seljuk_ertugrul_greet_2}Sit by the fire, traveler. Out here on the marches, a man is judged by his sword-arm and his word, not his lineage. Which have you brought me today?",
                    () => IsErtugrul() && GetGreetingVariant(1, 3) == 1, null, 200);
                starter.AddDialogLine("seljuk_ertugrul_greeting_3", "start", "lord_pretalk",
                    "{=seljuk_ertugrul_greet_3}The Kayi do not forget a friend, nor forgive an enemy. I have not yet decided which you are - so speak carefully.",
                    () => IsErtugrul() && GetGreetingVariant(1, 3) == 2, null, 200);

                // NIZAMULMULK DIALOGUE (salt 2)
                bool IsNizam() => Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.StringId == "lord_seljuk_nizamulmulk" || Hero.OneToOneConversationHero.StringId == "lord_seljuk_nizam_al_mulk");

                starter.AddDialogLine("seljuk_nizam_greeting_1", "start", "lord_pretalk",
                    "{=seljuk_nizam_greet}The blade of justice and state order is sharp, my child. May your service to the Grand Realm endure. Speak, I am listening.",
                    () => IsNizam() && GetGreetingVariant(2, 3) == 0, null, 200);
                starter.AddDialogLine("seljuk_nizam_greeting_2", "start", "lord_pretalk",
                    "{=seljuk_nizam_greet_2}Order is a garden, my child - left untended even for a season, it reverts to wilderness. What matter brings you to disturb my accounts today?",
                    () => IsNizam() && GetGreetingVariant(2, 3) == 1, null, 200);
                starter.AddDialogLine("seljuk_nizam_greeting_3", "start", "lord_pretalk",
                    "{=seljuk_nizam_greet_3}I have buried three Sultans' worth of correspondence beneath this desk, and still Baghdad and Isfahan write to me as if I have nothing else to do. What is it?",
                    () => IsNizam() && GetGreetingVariant(2, 3) == 2, null, 200);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
