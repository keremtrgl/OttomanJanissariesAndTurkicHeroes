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
                    "{=seljuk_alp_arslan_greet}Gaza meydanlarının yiğit alpi! Hoş geldin, kılıcın keskin, nizamın daim olsun. Âl-i Selçuk'un fermanı ve himmeti üzerinedir. Ne dilersin?",
                    () => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_seljuk_alp_arslan",
                    null,
                    200);

                // ERTUGRUL GAZI DIALOGUE
                starter.AddDialogLine(
                    "seljuk_ertugrul_greeting",
                    "start",
                    "lord_pretalk",
                    "{=seljuk_ertugrul_greet}Ey gaza yoldaşım! Söğüt uç boyundan ve Kayı otağından sana bin selam olsun. Pusatın daim parlasın, ne murat edersin?",
                    () => Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.StringId == "ertugrul_gazi" || Hero.OneToOneConversationHero.StringId == "lord_seljuk_ertugrul_gazi"),
                    null,
                    200);

                // NIZAMULMULK DIALOGUE
                starter.AddDialogLine(
                    "seljuk_nizam_greeting",
                    "start",
                    "lord_pretalk",
                    "{=seljuk_nizam_greet}Adaletin ve nizamın kılıcı keskindir evlat. Devlet-i Âliyye'ye hizmetin daim olsun. Buyur, seni dinlerim.",
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
