using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.GameComponents;

namespace Homesteads.Patches {
    [HarmonyPatch(typeof(DesertionCampaignBehavior), nameof(DesertionCampaignBehavior.DailyTickParty))]
    internal class HomesteadDesertionPatch {
        [HarmonyPostfix]
        private static void Postfix(DesertionCampaignBehavior __instance, MobileParty mobileParty) {
			Homestead? homestead = Homestead.GetFor(mobileParty);
			if (homestead == null)
				return;

			if (!Campaign.Current.DesertionEnabled) {
				return;
			}
			if (mobileParty.IsActive && !mobileParty.IsDisbanding && mobileParty.Party.MapEvent == null) {
				// troop roster intended to be null
				TroopRoster troopRoster = null;
				if (mobileParty.MemberRoster.TotalRegulars > 0) {
					//this.PartiesCheckDesertionDueToMorale(mobileParty, ref troopRoster);
					//this.PartiesCheckDesertionDueToPartySizeExceedsPaymentRatio(mobileParty, ref troopRoster);
					AccessTools.Method(typeof(DesertionCampaignBehavior), "PartiesCheckDesertionDueToMorale").Invoke(__instance, BindingFlags.NonPublic, null, new object[] { mobileParty, troopRoster }, null);
					AccessTools.Method(typeof(DesertionCampaignBehavior), "PartiesCheckDesertionDueToPartySizeExceedsPaymentRatio").Invoke(__instance, BindingFlags.NonPublic, null, new object[] { mobileParty, troopRoster }, null);
				}
				if (troopRoster != null && troopRoster.Count > 0) {
					CampaignEventDispatcher.Instance.OnTroopsDeserted(mobileParty, troopRoster);
				}
				if (mobileParty.Party.NumberOfAllMembers <= 0) {
					DestroyPartyAction.Apply(null, mobileParty);
				}
			}
		}
	}

	[HarmonyPatch(typeof(DefaultPartySizeLimitModel))]
	internal class HomesteadPartySizePatch {
		[HarmonyPatch(nameof(DefaultPartySizeLimitModel.GetPartyMemberSizeLimit))]
		[HarmonyPrefix]
		private static bool MemberPrefix(PartyBase party, bool includeDescriptions, ref ExplainedNumber __result) {
			if (party == null || party.MobileParty == null)
				return true;

			Homestead? homestead = Homestead.GetFor(party.MobileParty);
			if (homestead != null) {
				__result = new ExplainedNumber(homestead.GetTroopLimit(), includeDescriptions);
				return false;
            }

			return true;
		}

		[HarmonyPatch(nameof(DefaultPartySizeLimitModel.GetPartyPrisonerSizeLimit))]
		[HarmonyPrefix]
		private static bool PrisonerPrefix(PartyBase party, bool includeDescriptions, ref ExplainedNumber __result) {
			if (party == null || party.MobileParty == null)
				return true;

			Homestead? homestead = Homestead.GetFor(party.MobileParty);
			if (homestead != null) {
				__result = new ExplainedNumber(homestead.GetPrisonerLimit(), includeDescriptions);
				return false;
			}

			return true;
		}
	}
}
