using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace SeljukEmpire.Immersion
{
    /// <summary>
    /// Injects authentic historical Seljuk dialogues and greetings for Sultan Alp Arslan, Ertugrul Gazi, and Nizamulmulk.
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

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // SULTAN ALP ARSLAN DIALOGUE
                starter.AddDialogLine(
                    "seljuk_alp_arslan_greeting",
                    "start",
                    "lord_pretalk",
                    "{=seljuk_alp_arslan_greet}Valiant alp of the Ghazavat fields! Welcome, may your blade remain sharp and your order steadfast. The House of Seljuk bestows its blessing upon you. What do you seek?",
                    () => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_seljuk_alp_arslan",
                    null,
                    200);

                // ERTUGRUL GAZI DIALOGUE
                starter.AddDialogLine(
                    "seljuk_ertugrul_greeting",
                    "start",
                    "lord_pretalk",
                    "{=seljuk_ertugrul_greet}O companion of Gaza! A thousand greetings from the frontier of Sogut and the Kayi encampment. May your steel shine forever, what brings you to my tent?",
                    () => Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.StringId == "ertugrul_gazi" || Hero.OneToOneConversationHero.StringId == "lord_seljuk_ertugrul_gazi"),
                    null,
                    200);

                // NIZAMULMULK DIALOGUE
                starter.AddDialogLine(
                    "seljuk_nizam_greeting",
                    "start",
                    "lord_pretalk",
                    "{=seljuk_nizam_greet}The blade of justice and state order is sharp, my child. May your service to the Grand Realm endure. Speak, I am listening.",
                    () => Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.StringId == "lord_seljuk_nizamulmulk" || Hero.OneToOneConversationHero.StringId == "lord_seljuk_nizam_al_mulk"),
                    null,
                    200);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
