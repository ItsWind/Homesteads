using Homesteads.Models;
using SandBox;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics {
    public class HomesteadSpawningMissionLogic : MissionLogic {
        public static HomesteadSpawningMissionLogic Instance;

        private Homestead homestead;

        private CampaignAgentComponent? rightHand = null;

        public HomesteadSpawningMissionLogic(Homestead homestead) {
            Instance = this;
            this.homestead = homestead;
        }

        public override void AfterStart() {
            HomesteadScene homesteadScene = homestead.GetHomesteadScene();
            //homesteadScene.AddAllSavedNavMeshDisables();
            homesteadScene.AddAllSavedEntitiesToCurrentScene();
        }

        public override void OnMissionTick(float dt) {
            //if (rightHand == null)
                //return;
            //rightHand.AgentNavigator.SetTargetFrame(Agent.Main.Position.ToWorldPosition(), 0f);
        }

        public void HandleSpawning() {
            SpawnPlayer();
            //SpawnRightHandToPlayer();
            SpawnTroops();
            SpawnPrisoners();

            SpawnAnimals("hog");
            SpawnAnimals("goose");
            SpawnAnimals("chicken");
            SpawnAnimals("cow");
            SpawnAnimals("sheep");
            SpawnAnimals("cat");
            SpawnAnimals("dog");
        }

        private void SpawnPlayer() {
            Vec3 playerSpawnPos = homestead.GetHomesteadScene().PlayerSpawnPosition;
            if (!playerSpawnPos.IsValid)
                Mission.Scene.GetNavMeshCenterPosition(0, ref playerSpawnPos);

            SpawnHomesteadAgent(null, playerSpawnPos, PartyBase.MainParty, CharacterObject.PlayerCharacter, Agent.ControllerType.Player, false);
        }

        private void SpawnRightHandToPlayer() {
            CharacterObject? rightHandObject = null;
            try {
                rightHandObject = PartyBase.MainParty.MemberRoster.GetTroopRoster().Where(x => x.Character.HeroObject != null && x.Character != CharacterObject.PlayerCharacter).Select(x => x.Character).First();
            }
            catch (InvalidOperationException) {
                return;
            }
            Agent agent = SpawnHomesteadAgent(null, Agent.Main.Position, PartyBase.MainParty, rightHandObject, Agent.ControllerType.AI, false);
            rightHand = agent.GetComponent<CampaignAgentComponent>();
            rightHand.CreateAgentNavigator();

            /*foreach (int faceIndex in homestead.GetHomesteadScene().NavMeshFacesDisabled) {
                rightHand.SetAgentExcludeStateForFaceGroupId(faceIndex, false);
            }*/
        }

        private void SpawnTroops() {
            SpawnNPCs("spawnpoint_homestead_npc", HomesteadBehavior.Instance.CurrentHomestead.Troops);
        }

        private void SpawnPrisoners() {
            SpawnNPCs("spawnpoint_homestead_prisoner", HomesteadBehavior.Instance.CurrentHomestead.Prisoners);
        }

        private void SpawnNPCs(string spawnpointTag, TroopRoster roster) {
            List<GameEntity> entitiesWithNpcSpawnTag = Mission.Scene.FindEntitiesWithTag(spawnpointTag).ToList();
            List<CharacterObject> npcs = roster.ToFlattenedRoster().Troops.ToList();

            Dictionary<GameEntity, int> entityTimesUsed = new();

            foreach (CharacterObject npc in npcs) {
                if (entitiesWithNpcSpawnTag.Count <= 0)
                    break;

                GameEntity spawnEntity = entitiesWithNpcSpawnTag.GetRandomElementInefficiently();
                Vec3 positionToSpawnAt = new Vec3(spawnEntity.GlobalPosition.X, spawnEntity.GlobalPosition.Y, Mission.Scene.GetGroundHeightAtPosition(spawnEntity.GlobalPosition));

                SpawnHomesteadAgent(spawnEntity, positionToSpawnAt, HomesteadBehavior.Instance.CurrentHomestead.Party, npc, Agent.ControllerType.AI, true);

                entitiesWithNpcSpawnTag.Remove(spawnEntity);
            }
        }

        private Agent SpawnHomesteadAgent(GameEntity? spawnEntity, Vec3 positionToSpawnAt, PartyBase fromParty, CharacterObject characterObject, Agent.ControllerType controllerType, bool civilianEquipment) {
            MatrixFrame spawnFrame = spawnEntity == null ? MatrixFrame.Identity : spawnEntity.GetFrame();
            UsablePlace? usablePlace = spawnEntity == null ? null : spawnEntity.GetFirstScriptOfType<UsablePlace>();

            AgentBuildData buildData = new AgentBuildData(characterObject).Team(Mission.PlayerTeam).InitialPosition(positionToSpawnAt);

            Vec2 vec = spawnFrame.rotation.f.AsVec2;
            vec = vec.Normalized();

            AgentBuildData buildData2 = buildData.InitialDirection(vec).CivilianEquipment(civilianEquipment).NoHorses(civilianEquipment).NoWeapons(false).ClothingColor1(Mission.PlayerTeam.Color).ClothingColor2(Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(fromParty, characterObject)).Controller(controllerType);

            Hero? heroObject = characterObject.HeroObject;
            if (((heroObject != null) ? heroObject.ClanBanner : null) != null) {
                buildData2.Banner(characterObject.HeroObject.ClanBanner);
            }

            Agent agent = SpawnAgentAndTickAnimations(buildData2);
            if (usablePlace != null) {
                AnimationSystemData animData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agent.Monster, agent.IsFemale, ActionSetCode.MusicianSuffix), agent.Character.GetStepSize(), false);

                agent.SetActionSet(ref animData);
                agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator().SetTarget(usablePlace);
            }

            return agent;
        }

        private void SpawnAnimals(string animalNameId) {
            foreach (GameEntity entity in Mission.Scene.FindEntitiesWithTag("sp_" + animalNameId)) {
                MatrixFrame frame = entity.GetFrame();
                ItemObject spawnObject = Game.Current.ObjectManager.GetObject<ItemObject>(animalNameId);
                ItemRosterElement rosterElement = new ItemRosterElement(spawnObject);
                Vec3 positionToSpawn = new Vec3(entity.GlobalPosition.X, entity.GlobalPosition.Y, Mission.Scene.GetGroundHeightAtPosition(entity.GlobalPosition));
                Vec2 initialDirection = frame.rotation.f.AsVec2;
                Agent agent = Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in positionToSpawn, in initialDirection);
                TickAgentAnimations(agent);
            }
        }

        private Agent SpawnAgentAndTickAnimations(AgentBuildData buildData) {
            Agent agent = Mission.SpawnAgent(buildData);
            TickAgentAnimations(agent);
            return agent;
        }

        private void TickAgentAnimations(Agent agent) {
            for (int i = 0; i < 3; i++) {
                agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
            }
        }
    }
}
