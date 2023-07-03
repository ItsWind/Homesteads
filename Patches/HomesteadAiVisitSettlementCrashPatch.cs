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
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
    internal class HomesteadAiVisitSettlementCrashPatch {
        [HarmonyPrefix]
        private static bool Prefix(MobileParty mobileParty) {
            if (Homestead.GetFor(mobileParty) != null)
                return false;
            return true;
        }
    }
}
