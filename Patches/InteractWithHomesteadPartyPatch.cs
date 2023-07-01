using HarmonyLib;
using Homesteads.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches {
    [HarmonyPatch(typeof(EncounterManager), nameof(EncounterManager.StartPartyEncounter))]
    internal class InteractWithHomesteadPartyPatch {
        [HarmonyPrefix]
        private static bool Prefix(PartyBase attackerParty, PartyBase defenderParty) {
            if (attackerParty != PartyBase.MainParty)
                return true;

            Homestead? homestead = Homestead.GetFor(defenderParty.MobileParty);
            if (homestead == null)
                return true;

            if (homestead.MobileParty.MapEvent != null)
                return true;

            // do homestead things
            HomesteadBehavior.Instance.CurrentHomestead = homestead;

            PlayerEncounter.Start();
            PlayerEncounter.Current.SetupFields(attackerParty, defenderParty);

            GameMenu.ActivateGameMenu("homestead_menu_main");
            HomesteadTutorial.LaunchedMenu();

            // skip treating the party like an actual party
            return false;
        }
    }
}
