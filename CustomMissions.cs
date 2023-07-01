using SandBox;
using SandBox.Missions.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using Homesteads.Models;
using Homesteads.MissionLogics;
using SandBox.Conversation.MissionLogics;
using SandBox.View;

namespace Homesteads {
    public static class CustomMissions {
        public static Mission StartHomesteadMission(Homestead homestead) {
            string sceneName = homestead.GetHomesteadScene().SceneName;
            return MissionState.OpenNew(sceneName,
                SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", false, DecalAtlasGroup.Battle),
                (Mission mission) => new MissionBehavior[] {
                    new MissionOptionsComponent(),
                    new CampaignMissionComponent(),
                    new MissionBasicTeamLogic(),
                    new BasicLeaveMissionLogic(),
                    new MissionAgentLookHandler(),
                    new HeroSkillHandler(),
                    new MissionFacialAnimationHandler(),
                    new BattleAgentLogic(),
                    new MountAgentLogic(),
                    new AgentHumanAILogic(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new EquipmentControllerLeaveLogic(),
                    new MissionConversationLogic(),
                    ViewCreator.CreateMissionLeaveView(),
                    ViewCreator.CreateMissionBoundaryCrossingView(),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreatePhotoModeView(),
                    SandBoxViewCreator.CreateMissionConversationView(mission),
                    new HomesteadSpawningMissionLogic(homestead),
                    new HomesteadSceneEditingMissionLogic(homestead),
                    new HomesteadConversationMissionLogic(),
                    new HomesteadMissionView(homestead),
                });
        }
    }
}
