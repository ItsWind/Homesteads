using HarmonyLib;
using Homesteads.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches {
    [HarmonyPatch(typeof(MobilePartyAi), nameof(MobilePartyAi.DefaultBehavior), MethodType.Setter)]
    internal class StopHomesteadPartyMovingDefaultBehaviorPatch {
        [HarmonyPrefix]
        private static bool Prefix(MobilePartyAi __instance) {
            MobileParty party = (MobileParty)AccessTools.Field(typeof(MobilePartyAi), "_mobileParty").GetValue(__instance);
            Homestead? homestead = Homestead.GetFor(party);
            return homestead == null;
        }
    }

    [HarmonyPatch(typeof(MobileParty), nameof(MobileParty.ShortTermBehavior), MethodType.Setter)]
    internal class StopHomesteadPartyMovingShortTermBehaviorPatch {
        [HarmonyPrefix]
        private static bool Prefix(MobileParty __instance) {
            Homestead? homestead = Homestead.GetFor(__instance);
            return homestead == null;
        }
    }
}
