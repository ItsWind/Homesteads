using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

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