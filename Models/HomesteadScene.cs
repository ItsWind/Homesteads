using Homesteads.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Objects;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models {
    public class HomesteadScene {
        [SaveableField(1)]
        public Homestead Homestead;
        [SaveableField(2)]
        public string SceneName;
        [SaveableField(3)]
        public List<HomesteadSceneSavedEntity> SavedEntities = new();
        [SaveableField(4)]
        public int CurrentlyUsedBuildPoints = 0;
        [SaveableField(5)]
        public int TotalProductivity = 0;
        [SaveableField(6)]
        public int TotalSpace = 0;
        [SaveableField(7)]
        public int TotalLeisure = 0;
        // DO NOT USE 8
        [SaveableField(9)]
        public Vec3 PlayerSpawnPosition = Vec3.Invalid;
        [SaveableField(10)]
        public List<HomesteadScenePlaceableProducedItem> ProduceItems = new();
        //[SaveableField(11)]
        //public List<int> NavMeshFacesDisabled = new();

        public int MaxBuildPoints => Homestead.Tier == 0 ? 15 : (Homestead.Tier * 30) + (Homestead.Tier * 15);
        public int BuildPointsLeftToUse => MaxBuildPoints - CurrentlyUsedBuildPoints;

        private Dictionary<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntities = new();
        //private Dictionary<int, GameEntity> loadedNavDisablers = new();

        public HomesteadScene(string sceneName, Homestead homestead) {
            SceneName = sceneName;
            Homestead = homestead;
        }

        /*public void ToggleNavMeshDisablersVisibility(bool visible) {
            foreach (KeyValuePair<int, GameEntity> pair in loadedNavDisablers)
                pair.Value.SetVisibilityExcludeParents(visible);
        }

        public void AddAllSavedNavMeshDisables() {
            loadedNavDisablers = new();

            try {
                foreach (int faceIndex in NavMeshFacesDisabled)
                    DisableNavMeshFaceInCurrentScene(faceIndex, false);
            }
            catch (NullReferenceException) {
                if (NavMeshFacesDisabled == null)
                    NavMeshFacesDisabled = new();
            }
        }

        public void PlayerToggleNavMeshAtCurrentPosition() {
            int navMeshFaceIndex = Utils.GetNavMeshFaceIndexForPosition(Agent.Main.Position);
            // if already disabled
            if (NavMeshFacesDisabled.Contains(navMeshFaceIndex))
                EnableNavMeshFaceInCurrentScene(navMeshFaceIndex);
            // if not disabled yet
            else
                DisableNavMeshFaceInCurrentScene(navMeshFaceIndex);
        }*/

        public void AddPlaceableEntityToCurrentScene(HomesteadScenePlaceable placeable, Vec3 position, Mat3 rotation) {
            if (placeable.BuildPointsRequired > BuildPointsLeftToUse) {
                Utils.PrintLocalizedMessage("homestead_cannot_place_no_build_points", "You cannot place this object! You need to upgrade your tier or remove other objects.", 255, 80, 80);
                return;
            }

            // Check for item requirements
            if (!Utils.DoesItemRosterHaveItems(Homestead.Stash, placeable.ItemRequirements, true)) {
                Utils.PrintLocalizedMessage("homestead_cannot_place_lacking_items", "You do not have the items required in your homestead's stash for this object!", 255, 80, 80);
                return;
            }

            GameEntity entity = Utils.CreateGameEntityWithPrefab(placeable.PrefabName, position, rotation);
            AddPlaceableEntityValues(placeable);

            HomesteadSceneSavedEntity savedEntity = new HomesteadSceneSavedEntity(placeable, entity.GlobalPosition, entity.GetFrame().rotation);
            SavedEntities.Add(savedEntity);
            loadedSavedEntities.Add(entity, savedEntity);

            Utils.PrintLocalizedMessage("homestead_show_build_points_left_to_use", "{AMOUNT_OF_BP_LEFT} BUILD POINTS LEFT TO USE.", 255, 255, 255,
                ("AMOUNT_OF_BP_LEFT", BuildPointsLeftToUse.ToString()));
        }

        public void RemovePlaceableEntityFromCurrentScene(GameEntity entity) {
            GameEntity entityToCheck = entity;
            if (!loadedSavedEntities.ContainsKey(entityToCheck)) {
                GameEntity? parentEntity = entityToCheck.Parent;
                if (parentEntity == null || !loadedSavedEntities.ContainsKey(parentEntity))
                    return;
                else
                    entityToCheck = parentEntity;
            }

            // Remove productivity and space from entity
            HomesteadScenePlaceable placeableData = loadedSavedEntities[entityToCheck].Placeable;
            RemovePlaceableEntityValues(placeableData);

            SavedEntities.Remove(loadedSavedEntities[entityToCheck]);
            entityToCheck.RemoveAllChildren();
            entityToCheck.Remove(0);

            Utils.PrintLocalizedMessage("homestead_show_build_points_left_to_use", "{AMOUNT_OF_BP_LEFT} BUILD POINTS LEFT TO USE.", 255, 255, 255,
                ("AMOUNT_OF_BP_LEFT", BuildPointsLeftToUse.ToString()));
        }

        public void AddAllSavedEntitiesToCurrentScene() {
            loadedSavedEntities = new();

            foreach (HomesteadSceneSavedEntity savedEntity in SavedEntities.ToList())
                AddSavedEntityToCurrentScene(savedEntity);

            HomesteadSpawningMissionLogic.Instance.HandleSpawning();
        }

        /*private void DisableNavMeshFaceInCurrentScene(int faceIndex, bool playerAdded = true) {
            Vec3 navmeshCenterPos = Vec3.Invalid;
            Mission.Current.Scene.GetNavMeshCenterPosition(faceIndex, ref navmeshCenterPos);
            navmeshCenterPos = new Vec3(navmeshCenterPos.X, navmeshCenterPos.Y, Mission.Current.Scene.GetGroundHeightAtPosition(navmeshCenterPos));

            GameEntity navMeshDisablerEntity = Utils.CreateGameEntityWithPrefab("homestead_navmesh_deactivator", navmeshCenterPos, Mat3.Identity);
            NavigationMeshDeactivator noNav = navMeshDisablerEntity.GetFirstScriptOfType<NavigationMeshDeactivator>();

            noNav.DisableFaceWithId = faceIndex;
            noNav.DisableFaceWithIdForAnimals = faceIndex;

            if (playerAdded)
                NavMeshFacesDisabled.Add(faceIndex);
            else
                navMeshDisablerEntity.SetVisibilityExcludeParents(false);

            loadedNavDisablers[faceIndex] = navMeshDisablerEntity;
        }

        private void EnableNavMeshFaceInCurrentScene(int faceIndex) {
            NavMeshFacesDisabled.Remove(faceIndex);
            loadedNavDisablers[faceIndex].Remove(0);
            loadedNavDisablers.Remove(faceIndex);
        }*/

        private void AddSavedEntityToCurrentScene(HomesteadSceneSavedEntity savedEntity) {
            if (!GameEntity.PrefabExists(savedEntity.Placeable.PrefabName)) {
                Utils.PrintDebugMessage("CAUGHT NON EXISTING PREFAB NAME " + savedEntity.Placeable.PrefabName, 255, 0, 0);
                RemovePlaceableEntityValues(savedEntity.Placeable);
                SavedEntities.Remove(savedEntity);
                return;
            }

            Vec3 sVec = new Vec3(savedEntity.rotSx, savedEntity.rotSy, savedEntity.rotSz);
            Vec3 fVec = new Vec3(savedEntity.rotFx, savedEntity.rotFy, savedEntity.rotFz);
            Vec3 uVec = new Vec3(savedEntity.rotUx, savedEntity.rotUy, savedEntity.rotUz);
            Mat3 rotation = new Mat3(sVec, fVec, uVec);

            Vec3 position = new Vec3(savedEntity.posX, savedEntity.posY, savedEntity.posZ);

            GameEntity entity = Utils.CreateGameEntityWithPrefab(savedEntity.Placeable.PrefabName, position, rotation);

            loadedSavedEntities[entity] = savedEntity;
        }

        private void RemovePlaceableEntityValues(HomesteadScenePlaceable placeable) {
            CurrentlyUsedBuildPoints -= placeable.BuildPointsRequired;

            TotalProductivity -= placeable.ProductivityIncrease;
            TotalSpace -= placeable.SpaceIncrease;
            TotalLeisure -= placeable.LeisureIncrease;

            // try/catch to fix update V2.3
            try {
                foreach (HomesteadScenePlaceableProducedItem produceItem in placeable.ProduceItems)
                    ProduceItems.Remove(produceItem);
            }
            catch (NullReferenceException) {
                if (ProduceItems == null)
                    ProduceItems = new();
            }
        }

        private void AddPlaceableEntityValues(HomesteadScenePlaceable placeable) {
            CurrentlyUsedBuildPoints += placeable.BuildPointsRequired;

            TotalProductivity += placeable.ProductivityIncrease;
            TotalSpace += placeable.SpaceIncrease;
            TotalLeisure += placeable.LeisureIncrease;

            // try/catch to fix update V2.3
            try {
                ProduceItems.AddRange(placeable.ProduceItems);
            }
            catch (NullReferenceException) {
                if (ProduceItems == null)
                    ProduceItems = new();
            }
        }
    }
}
