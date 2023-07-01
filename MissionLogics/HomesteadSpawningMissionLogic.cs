using Homesteads.Models;
using SandBox;
using SandBox.Objects.Usables;
using System.Collections.Generic;
using System.Linq;
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
        private HomesteadScene homesteadScene;

        private List<SoundEvent> sounds = new();

        public HomesteadSpawningMissionLogic(Homestead homestead) {
            Instance = this;
            this.homestead = homestead;
        }

        public override void AfterStart() {
            homesteadScene = homestead.GetHomesteadScene();
            homesteadScene.AddAllSavedEntitiesToCurrentScene();
        }

        public override void OnEndMissionInternal() {
            foreach (SoundEvent sound in sounds)
                sound.Release();
        }

        public void HandleSpawning() {
            SpawnPlayer();

            SpawnTroops();
            SpawnPrisoners();

            SpawnAnimals("hog");
            SpawnAnimals("goose");
            SpawnAnimals("chicken");
            SpawnAnimals("cow");
            SpawnAnimals("sheep");
            SpawnAnimals("cat");
            SpawnAnimals("dog");

            HomesteadTutorial.WalkAround();
        }

        private void SpawnPlayer() {
            Vec3 playerSpawnPos = homesteadScene.PlayerSpawnPosition;
            Mat3 playerSpawnRot = homesteadScene.PlayerSpawnRotation;
            if (!playerSpawnPos.IsValid)
                Mission.Scene.GetNavMeshCenterPosition(0, ref playerSpawnPos);
            // Check if rotation is null for players that are updating
            if (playerSpawnRot == null)
                playerSpawnRot = Mat3.Identity;

            SpawnHomesteadAgent(null, playerSpawnPos, playerSpawnRot, PartyBase.MainParty, CharacterObject.PlayerCharacter, Agent.ControllerType.Player, "", false);
        }

        private void SpawnTroops() {
            TroopRoster homesteadLeaderRoster = TroopRoster.CreateDummyTroopRoster();
            homesteadLeaderRoster.Add(homestead.Troops.ToFlattenedRoster().Where(x => x.Troop.HeroObject == homestead.Leader));
            bool usedLeader = SpawnNPCs("spawnpoint_homestead_leader", homesteadLeaderRoster);

            TroopRoster spouseRoster = TroopRoster.CreateDummyTroopRoster();
            spouseRoster.Add(MobileParty.MainParty.MemberRoster.ToFlattenedRoster().Where(x => x.Troop.HeroObject != null && x.Troop.HeroObject.Spouse == Hero.MainHero));
            spouseRoster.Add(homestead.Troops.ToFlattenedRoster().Where(x => x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && x.Troop.HeroObject.Spouse == Hero.MainHero));
            bool usedSpouse = SpawnNPCs("spawnpoint_homestead_spouse", spouseRoster);

            TroopRoster companionRoster = TroopRoster.CreateDummyTroopRoster();
            companionRoster.Add(MobileParty.MainParty.MemberRoster.ToFlattenedRoster().Where(x => x.Troop.HeroObject != null && !x.Troop.IsPlayerCharacter && x.Troop.HeroObject.Spouse != Hero.MainHero));
            companionRoster.Add(homestead.Troops.ToFlattenedRoster().Where(x => x.Troop.HeroObject != null && x.Troop.HeroObject != homestead.Leader && !x.Troop.IsPlayerCharacter && x.Troop.HeroObject.Spouse != Hero.MainHero));
            bool usedCompanions = SpawnNPCs("spawnpoint_homestead_companion", companionRoster);

            TroopRoster generalRoster = TroopRoster.CreateDummyTroopRoster();
            generalRoster.Add(homestead.Troops.ToFlattenedRoster());
            if (usedLeader)
                generalRoster.RemoveIf(x => x.Character.HeroObject == homestead.Leader);
            if (usedSpouse)
                generalRoster.RemoveIf(x => x.Character.HeroObject != null && x.Character.HeroObject.Spouse == Hero.MainHero);
            if (usedCompanions)
                generalRoster.RemoveIf(x => x.Character.HeroObject != null && x.Character.HeroObject.Spouse != Hero.MainHero);
            SpawnNPCs("spawnpoint_homestead_npc", generalRoster);
        }

        private void SpawnPrisoners() {
            SpawnNPCs("spawnpoint_homestead_prisoner", homestead.Prisoners);
        }

        private bool SpawnNPCs(string spawnpointTag, TroopRoster roster) {
            List<GameEntity> entitiesWithNpcSpawnTag = Mission.Scene.FindEntitiesWithTag(spawnpointTag).ToList();
            if (entitiesWithNpcSpawnTag.Count == 0)
                return false;

            List<CharacterObject> npcs = roster.ToFlattenedRoster().Troops.ToList();

            foreach (CharacterObject npc in npcs) {
                if (entitiesWithNpcSpawnTag.Count <= 0)
                    break;

                GameEntity spawnEntity = entitiesWithNpcSpawnTag.GetRandomElementInefficiently();

                string actionSetCodeSuffix = ActionSetCode.Villager1ActionSetSuffix;
                bool shouldWearCivEquipment = true;
                HandleSpawnEntitySpecialTags(spawnEntity, ref actionSetCodeSuffix, ref shouldWearCivEquipment);

                Vec3 positionToSpawnAt = new Vec3(spawnEntity.GlobalPosition.X, spawnEntity.GlobalPosition.Y, Mission.Scene.GetGroundHeightAtPosition(spawnEntity.GlobalPosition));
                Mat3 rotationToSpawnWith = spawnEntity.GetFrame().rotation;

                SpawnHomesteadAgent(spawnEntity, positionToSpawnAt, rotationToSpawnWith, homestead.Party, npc, Agent.ControllerType.AI, actionSetCodeSuffix, shouldWearCivEquipment);

                entitiesWithNpcSpawnTag.Remove(spawnEntity);
            }

            return true;
        }

        private void HandleSpawnEntitySpecialTags(GameEntity entity, ref string actionSetCodeSuffix, ref bool shouldWearCivEquipment) {
            if (entity.HasTag("homestead_flute_musician")) {
                int randomFluteSoundIndex = MBRandom.RandomInt(1, 3); // max is exclusive
                int eventID = SoundEvent.GetEventIdFromString("homestead/music/flute" + randomFluteSoundIndex);
                SoundEvent sound = SoundEvent.CreateEvent(eventID, Mission.Scene);
                sound.PlayInPosition(entity.GlobalPosition);
                sounds.Add(sound);

                actionSetCodeSuffix = ActionSetCode.MusicianSuffix;
            }
            else if (entity.HasTag("homestead_guard")) {
                shouldWearCivEquipment = false;

                actionSetCodeSuffix = ActionSetCode.GuardSuffix;
            }
        }

        private Agent SpawnHomesteadAgent(GameEntity? spawnEntity, Vec3 positionToSpawnAt, Mat3 rotationToSpawnWith, PartyBase fromParty, CharacterObject characterObject, Agent.ControllerType controllerType, string actionSetCodeSuffix, bool civilianEquipment) {
            UsablePlace? usablePlace = spawnEntity == null ? null : spawnEntity.GetFirstScriptOfType<UsablePlace>();

            AgentBuildData buildData = new AgentBuildData(characterObject).Team(Mission.PlayerTeam).InitialPosition(positionToSpawnAt);

            Vec2 vec = rotationToSpawnWith.f.AsVec2;
            vec = vec.Normalized();

            AgentBuildData buildData2 = buildData.InitialDirection(vec).CivilianEquipment(civilianEquipment).NoHorses(civilianEquipment).NoWeapons(false).ClothingColor1(Mission.PlayerTeam.Color).ClothingColor2(Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(fromParty, characterObject)).Controller(controllerType);

            Hero? heroObject = characterObject.HeroObject;
            if (((heroObject != null) ? heroObject.ClanBanner : null) != null) {
                buildData2.Banner(characterObject.HeroObject.ClanBanner);
            }

            Agent agent = SpawnAgentAndTickAnimations(buildData2);
            if (usablePlace != null) {
                AnimationSystemData animData = agent.Monster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agent.Monster, agent.IsFemale, actionSetCodeSuffix), agent.Character.GetStepSize(), false);

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
