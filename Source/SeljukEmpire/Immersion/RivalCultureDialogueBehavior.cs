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
    /// (see SeljukDialogueBehavior.GetGreetingVariant's remarks for why this needs an in-game-hour
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

        private static int GetGreetingVariant(int salt, int variantCount)
        {
            return ((int)CampaignTime.Now.ToHours + salt) % variantCount;
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // BYZANTINE EMPEROR ROMANOS IV DIOGENES (salt 3)
                bool IsRomanos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_14";

                starter.AddDialogLine("byz_romanos_greeting_1", "start", "lord_pretalk",
                    "{=byz_romanos_greet}Stranger who dares approach the Purple! Rome endures though her eagle's wings are clipped at Manzikert - I will see her risen again, whatever the cost to my own name. Speak your business, and be brief.",
                    () => IsRomanos() && GetGreetingVariant(3, 3) == 0, null, 200);
                starter.AddDialogLine("byz_romanos_greeting_2", "start", "lord_pretalk",
                    "{=byz_romanos_greet_2}History will judge me for one field outside Manzikert and forget the twenty I held before it. I do not care - only Rome's survival concerns me now. What do you want?",
                    () => IsRomanos() && GetGreetingVariant(3, 3) == 1, null, 200);
                starter.AddDialogLine("byz_romanos_greeting_3", "start", "lord_pretalk",
                    "{=byz_romanos_greet_3}Every general in this camp believes he could have done better against the Turks. Perhaps. None of them are wearing the Purple, however. Speak.",
                    () => IsRomanos() && GetGreetingVariant(3, 3) == 2, null, 200);

                // BYZANTINE GENERAL ALEXIOS KOMNENOS (salt 4)
                bool IsAlexios() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_30";

                starter.AddDialogLine("byz_alexios_greeting_1", "start", "lord_pretalk",
                    "{=byz_alexios_greet_1}Well met. I have fought under three emperors already and buried the ambitions of men greater than either of us - so choose your words to me with some care.",
                    () => IsAlexios() && GetGreetingVariant(4, 3) == 0, null, 200);
                starter.AddDialogLine("byz_alexios_greeting_2", "start", "lord_pretalk",
                    "{=byz_alexios_greet_2}Rome does not need another hero, stranger - it needs someone willing to do what heroes will not. I have made my peace with that. Have you?",
                    () => IsAlexios() && GetGreetingVariant(4, 3) == 1, null, 200);
                starter.AddDialogLine("byz_alexios_greeting_3", "start", "lord_pretalk",
                    "{=byz_alexios_greet_3}The court whispers that I look at the throne too often. The court is not wrong. What brings you to me?",
                    () => IsAlexios() && GetGreetingVariant(4, 3) == 2, null, 200);

                // ABBASID CALIPH AL-QA'IM BI-AMR ALLAH (salt 5)
                bool IsAlQaim() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_1";

                starter.AddDialogLine("abb_alqaim_greeting_1", "start", "lord_pretalk",
                    "{=abb_alqaim_greet}Peace be upon you, traveler, in the name of the Commander of the Faithful. Baghdad stands as it has for three centuries, its light undimmed though the Sultan's sword, not mine, now guards its gates. What brings you before the seat of the Caliphate?",
                    () => IsAlQaim() && GetGreetingVariant(5, 3) == 0, null, 200);
                starter.AddDialogLine("abb_alqaim_greeting_2", "start", "lord_pretalk",
                    "{=abb_alqaim_greet_2}A Caliph without an army is still a Caliph, traveler - the Friday sermon is still read in my name from Cairo to Bukhara. Power is not the only throne. What do you seek?",
                    () => IsAlQaim() && GetGreetingVariant(5, 3) == 1, null, 200);
                starter.AddDialogLine("abb_alqaim_greeting_3", "start", "lord_pretalk",
                    "{=abb_alqaim_greet_3}The Sultan protects my palace; I protect his legitimacy. Neither of us says this aloud, but both of us know it. Now - what is your business?",
                    () => IsAlQaim() && GetGreetingVariant(5, 3) == 2, null, 200);

                // ABBASID VIZIER FAKHR AL-DAWLA IBN JAHIR (salt 6)
                bool IsFakhrAlDawla() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_3";

                starter.AddDialogLine("abb_fakhraldawla_greeting_1", "start", "lord_pretalk",
                    "{=abb_fakhraldawla_greet_1}Sit, if you have patience - I have petitions from three provinces on this desk and a Caliph who wants answers by evening. What is your business?",
                    () => IsFakhrAlDawla() && GetGreetingVariant(6, 3) == 0, null, 200);
                starter.AddDialogLine("abb_fakhraldawla_greeting_2", "start", "lord_pretalk",
                    "{=abb_fakhraldawla_greet_2}In this court, a well-placed word outlasts a well-placed sword. I have seen both used, and I know which one I trust more. Speak.",
                    () => IsFakhrAlDawla() && GetGreetingVariant(6, 3) == 1, null, 200);
                starter.AddDialogLine("abb_fakhraldawla_greeting_3", "start", "lord_pretalk",
                    "{=abb_fakhraldawla_greet_3}Baghdad's treasury does not balance itself, and neither does its politics. I manage both, badly some days. What do you need from me?",
                    () => IsFakhrAlDawla() && GetGreetingVariant(6, 3) == 2, null, 200);

                // GEORGIAN KING DAVID IV THE BUILDER (salt 7)
                bool IsDavid() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_1";

                starter.AddDialogLine("geo_david_greeting_1", "start", "lord_pretalk",
                    "{=geo_david_greet}Well met beneath the mountains of Kartli! Georgia will not remain a vassal to any invader's whim - I am building an army, a church, and a nation that will outlast us both. Speak plainly, what do you want?",
                    () => IsDavid() && GetGreetingVariant(7, 3) == 0, null, 200);
                starter.AddDialogLine("geo_david_greeting_2", "start", "lord_pretalk",
                    "{=geo_david_greet_2}Every noble house in this kingdom believes it should rule instead of me. I intend to prove, one reform at a time, that they are wrong. What do you want?",
                    () => IsDavid() && GetGreetingVariant(7, 3) == 1, null, 200);
                starter.AddDialogLine("geo_david_greeting_3", "start", "lord_pretalk",
                    "{=geo_david_greet_3}A kingdom is not defended by walls alone, but by the loyalty of the men who guard them. I am still building both. What brings you to me?",
                    () => IsDavid() && GetGreetingVariant(7, 3) == 2, null, 200);

                // GEORGIAN NOBLE LIPARIT BAGHVASHI (salt 8)
                bool IsLiparit() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_3";

                starter.AddDialogLine("geo_liparit_greeting_1", "start", "lord_pretalk",
                    "{=geo_liparit_greet_1}You stand in the lands of House Baghvashi, traveler - remember that, whatever crown you answer to. What do you want?",
                    () => IsLiparit() && GetGreetingVariant(8, 3) == 0, null, 200);
                starter.AddDialogLine("geo_liparit_greeting_2", "start", "lord_pretalk",
                    "{=geo_liparit_greet_2}Kings come and go, but the great houses of Kartli endure. I have outlasted more royal tempers than you might guess. Speak your business.",
                    () => IsLiparit() && GetGreetingVariant(8, 3) == 1, null, 200);
                starter.AddDialogLine("geo_liparit_greeting_3", "start", "lord_pretalk",
                    "{=geo_liparit_greet_3}I bow to no throne that forgets whose swords hold it up. State your business, and mind your tone.",
                    () => IsLiparit() && GetGreetingVariant(8, 3) == 2, null, 200);

                // ============================================================================
                // SECOND WAVE: 13 additional named lords across the three rival kingdoms (none
                // added to the Seljuk side here - it already has its own 3 named figures above).
                // Lighter treatment than the flagship six above: 1-2 lines each instead of 3, since
                // these are secondary court/battlefield figures rather than ruler-tier characters.
                // ============================================================================

                // BYZANTINE GENERAL NIKEPHOROS BOTANEIATES (later Emperor) (salt 9)
                bool IsBotaneiates() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_15";

                starter.AddDialogLine("byz_botaneiates_greeting_1", "start", "lord_pretalk",
                    "{=byz_botaneiates_greet_1}They call me old, and they are not wrong - but a long life spent watching younger men fail has taught me exactly how empires actually change hands. What do you want?",
                    () => IsBotaneiates() && GetGreetingVariant(9, 2) == 0, null, 200);
                starter.AddDialogLine("byz_botaneiates_greeting_2", "start", "lord_pretalk",
                    "{=byz_botaneiates_greet_2}Every ambitious man in Constantinople is patient until the moment he isn't. I have simply been more patient than most. Speak your business.",
                    () => IsBotaneiates() && GetGreetingVariant(9, 2) == 1, null, 200);

                // BYZANTINE GENERAL ANDRONIKOS DOUKAS (withdrew his troops at Manzikert) (salt 10)
                bool IsAndronikosDoukas() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_17";

                starter.AddDialogLine("byz_andronikos_greeting_1", "start", "lord_pretalk",
                    "{=byz_andronikos_greet_1}You will hear it said that I turned my back on the field at Manzikert. I turned my back on a battle already lost, and saved men who would otherwise be corpses. Judge me as you like - now, what brings you here?",
                    () => IsAndronikosDoukas() && GetGreetingVariant(10, 2) == 0, null, 200);
                starter.AddDialogLine("byz_andronikos_greeting_2", "start", "lord_pretalk",
                    "{=byz_andronikos_greet_2}The Doukas name carries weight in this Empire whether the mob approves of me or not. State your business.",
                    () => IsAndronikosDoukas() && GetGreetingVariant(10, 2) == 1, null, 200);

                // BYZANTINE GENERAL NIKEPHOROS BRYENNIOS (salt: none needed, 1 line)
                bool IsBryennios() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_63";

                starter.AddDialogLine("byz_bryennios_greeting_1", "start", "lord_pretalk",
                    "{=byz_bryennios_greet_1}A soldier who has not yet decided whether the throne is worth the risk of reaching for it is still, for now, a loyal one. Speak plainly, what do you need?",
                    IsBryennios, null, 200);

                // BYZANTINE ROMANOS ARGYROS (1 line)
                bool IsArgyros() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_1_55";

                starter.AddDialogLine("byz_argyros_greeting_1", "start", "lord_pretalk",
                    "{=byz_argyros_greet_1}The Argyros line has given Rome an emperor before, and may yet again. Until then, I serve as I am asked. What is it?",
                    IsArgyros, null, 200);

                // BYZANTINE GENERAL KATAKALON KEKAUMENOS (military writer) (salt 11)
                bool IsKekaumenos() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_SE9_l";

                starter.AddDialogLine("byz_kekaumenos_greeting_1", "start", "lord_pretalk",
                    "{=byz_kekaumenos_greet_1}I have written down everything I have learned commanding men on this frontier, so that fools who come after me need not learn it the hard way. What do you want to know?",
                    () => IsKekaumenos() && GetGreetingVariant(11, 2) == 0, null, 200);
                starter.AddDialogLine("byz_kekaumenos_greeting_2", "start", "lord_pretalk",
                    "{=byz_kekaumenos_greet_2}Trust the man who tells you war is glorious least of all. I have seen enough of it to know better. Speak your business.",
                    () => IsKekaumenos() && GetGreetingVariant(11, 2) == 1, null, 200);

                // ABBASID ARSLAN KHATUN (salt 12)
                bool IsArslanKhatun() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_2";

                starter.AddDialogLine("abb_arslankhatun_greeting_1", "start", "lord_pretalk",
                    "{=abb_arslankhatun_greet_1}A woman's name carries as much weight at this court as any man's title, whatever the scribes choose to write down. What brings you before me?",
                    () => IsArslanKhatun() && GetGreetingVariant(12, 2) == 0, null, 200);
                starter.AddDialogLine("abb_arslankhatun_greeting_2", "start", "lord_pretalk",
                    "{=abb_arslankhatun_greet_2}I have outlived the political schemes of men who thought a marriage alliance made me powerless. It did not. Speak your business.",
                    () => IsArslanKhatun() && GetGreetingVariant(12, 2) == 1, null, 200);

                // ABBASID VIZIER IBN AL-MUSLIMA (salt 13)
                bool IsIbnAlMuslima() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_5";

                starter.AddDialogLine("abb_ibnalmuslima_greeting_1", "start", "lord_pretalk",
                    "{=abb_ibnalmuslima_greet_1}Baghdad has enemies within its own walls as often as beyond them, and I have made it my life's work to know the difference. What is your business?",
                    () => IsIbnAlMuslima() && GetGreetingVariant(13, 2) == 0, null, 200);
                starter.AddDialogLine("abb_ibnalmuslima_greeting_2", "start", "lord_pretalk",
                    "{=abb_ibnalmuslima_greet_2}Faith and statecraft are not two different arts in this city, traveler - they are one and the same. Speak plainly.",
                    () => IsIbnAlMuslima() && GetGreetingVariant(13, 2) == 1, null, 200);

                // ABBASID CHRONICLER HILAL AL-SABI (1 line)
                bool IsHilalAlSabi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_18";

                starter.AddDialogLine("abb_hilalalsabi_greeting_1", "start", "lord_pretalk",
                    "{=abb_hilalalsabi_greet_1}Every event in this court, I write down for those who come after us - so mind what you say to me, it may well outlive us both.",
                    IsHilalAlSabi, null, 200);

                // ABBASID EMIR QURAYSH IBN BADRAN (1 line)
                bool IsQuraysh() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_3_19";

                starter.AddDialogLine("abb_quraysh_greeting_1", "start", "lord_pretalk",
                    "{=abb_quraysh_greet_1}My tribe held these lands before the Sultan's horsemen arrived, and will hold them after, God willing. What do you want?",
                    IsQuraysh, null, 200);

                // GEORGIAN QUEEN GURANDUKHT (salt 14)
                bool IsGurandukht() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_2";

                starter.AddDialogLine("geo_gurandukht_greeting_1", "start", "lord_pretalk",
                    "{=geo_gurandukht_greet_1}A kingdom raised by a boy-king needs a steady hand behind the throne, traveler, and I have provided it more than once. What do you want?",
                    () => IsGurandukht() && GetGreetingVariant(14, 2) == 0, null, 200);
                starter.AddDialogLine("geo_gurandukht_greeting_2", "start", "lord_pretalk",
                    "{=geo_gurandukht_greet_2}I have buried a husband and crowned a son, and neither task was as simple as the chroniclers make it sound. Speak your business.",
                    () => IsGurandukht() && GetGreetingVariant(14, 2) == 1, null, 200);

                // GEORGIAN NOBLE IVANE ORBELI (salt 15)
                bool IsIvaneOrbeli() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_5";

                starter.AddDialogLine("geo_orbeli_greeting_1", "start", "lord_pretalk",
                    "{=geo_orbeli_greet_1}House Orbeli has commanded armies for this kingdom since before the current dynasty could hold a sword. Remember whose loyalty a crown actually depends on. What do you want?",
                    () => IsIvaneOrbeli() && GetGreetingVariant(15, 2) == 0, null, 200);
                starter.AddDialogLine("geo_orbeli_greeting_2", "start", "lord_pretalk",
                    "{=geo_orbeli_greet_2}A king who forgets which houses hold up his throne rarely keeps it for long. I am simply reminding you of that fact. Speak.",
                    () => IsIvaneOrbeli() && GetGreetingVariant(15, 2) == 1, null, 200);

                // GEORGIAN CATHOLICOS GIORGI OF CHQONDIDI (1 line)
                bool IsGiorgiChqondideli() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_2_18";

                starter.AddDialogLine("geo_giorgi_greeting_1", "start", "lord_pretalk",
                    "{=geo_giorgi_greet_1}The Church and the Crown walk the same road in this kingdom, traveler, however uneasily. What brings you to me?",
                    IsGiorgiChqondideli, null, 200);

                // GEORGIAN NOBLE GRIGOL ERISTAVI (1 line)
                bool IsGrigolEristavi() => Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.StringId == "lord_S9_l";

                starter.AddDialogLine("geo_grigol_greeting_1", "start", "lord_pretalk",
                    "{=geo_grigol_greet_1}An Eristavi answers to the King, not to every wandering stranger who rides through his lands. State your business plainly.",
                    IsGrigolEristavi, null, 200);
            }
            catch (Exception)
            {
                // Safety
            }
        }
    }
}
