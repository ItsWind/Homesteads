using HarmonyLib;
using Homesteads.Models;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using SandBox.ViewModelCollection.Map;

namespace Homesteads.Patches {
    [HarmonyPatch(typeof(PartyVisual), "AddMobileIconComponents")]
    internal class HomesteadMapAppearanceModelPatch {
        [HarmonyPrefix]
        private static bool Prefix(PartyVisual __instance, PartyBase party) {
            Homestead? homestead = Homestead.GetFor(party.MobileParty);
            if (homestead == null)
                return true;

            homestead.BuildMapIcon(__instance);

            return false;
        }
    }

    [HarmonyPatch(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    internal class HomesteadMapAppearanceNameplatePatch {
        [HarmonyPostfix]
        private static void Postfix(PartyNameplateVM __instance) {
            Homestead? homestead = Homestead.GetFor(__instance.Party);
            if (homestead == null)
                return;

            FieldInfo fieldInfo = AccessTools.Field(typeof(PartyNameplateVM), "_latestNameTextObject");
            FieldInfo fieldInfo2 = AccessTools.Field(typeof(PartyNameplateVM), "_fullNameBind");
            TextObject? textObject = (TextObject)fieldInfo.GetValue(__instance);
            string text = (string)fieldInfo2.GetValue(__instance);
            if (textObject == null || text == null)
                return;

            fieldInfo.SetValue(__instance, homestead.Name, BindingFlags.NonPublic, null, null);
            fieldInfo2.SetValue(__instance, homestead.Name.ToString(), BindingFlags.NonPublic, null, null);
        }
    }

    [HarmonyPatch(typeof(MapMobilePartyTrackerVM), "InitList")]
    internal class HomesteadMapAppearancePartyTrackerPatch {
        [HarmonyPostfix]
        private static void Postfix(MapMobilePartyTrackerVM __instance) {
            Camera? camera = (Camera)AccessTools.Field(typeof(MapMobilePartyTrackerVM), "_mapCamera").GetValue(__instance);
            Action<Vec2>? fastMoveCameraToPosition = (Action<Vec2>)AccessTools.Field(typeof(MapMobilePartyTrackerVM), "_fastMoveCameraToPosition").GetValue(__instance);
            if (camera == null || fastMoveCameraToPosition == null)
                return;

            foreach (KeyValuePair<MobileParty, Homestead> pair in HomesteadBehavior.Instance.HomesteadMobileParties)
                __instance.Trackers.Add(new MobilePartyTrackItemVM(pair.Key, camera, fastMoveCameraToPosition));
        }
    }
}
