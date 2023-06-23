using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Encounters;
using System.Linq;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.InputSystem;

namespace Homesteads {
    public class SubModule : MBSubModuleBase {
        protected override void OnSubModuleLoad() {
            new Harmony("Homesteads").PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter) {

            if (game.GameType is Campaign) {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;

                campaignStarter.AddBehavior(new HomesteadBehavior());
            }
		}
	}
}