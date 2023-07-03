using Homesteads.MissionLogics;
using SandBox.Objects.AnimationPoints;
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
        [SaveableField(11)]
        public Mat3 PlayerSpawnRotation = Mat3.Identity;

        public int MaxBuildPoints => Homestead.Tier == 0 ? 15 : (Homestead.Tier * 30) + (Homestead.Tier * 15);
        public int BuildPointsLeftToUse => MaxBuildPoints - CurrentlyUsedBuildPoints;

        private Dictionary<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntities = new();

        public HomesteadScene(string sceneName, Homestead homestead) {
            SceneName = sceneName;
            Homestead = homestead;
        }

        public void AddPlaceableEntityToCurrentScene(HomesteadScenePlaceable placeable, Vec3 position, Mat3 rotation, bool skipItemCheck = false) {
            if (placeable.BuildPointsRequired > BuildPointsLeftToUse) {
                Utils.PrintLocalizedMessage("homestead_cannot_place_no_build_points", "You cannot place this object! You need to upgrade your tier or remove other objects.", 255, 80, 80);
                return;
            }

            // Check for item requirements
            if (!skipItemCheck && !Utils.DoesItemRosterHaveItems(Homestead.Stash, placeable.ItemRequirements, true)) {
                Utils.PrintLocalizedMessage("homestead_cannot_place_lacking_items", "You do not have the items required in your homestead's stash for this object!", 255, 80, 80);
                return;
            }

            GameEntity entity = Utils.CreateGameEntityWithPrefab(placeable.PrefabName, position, rotation);
            AddPlaceableEntityValues(placeable);

            HomesteadSceneSavedEntity savedEntity = new HomesteadSceneSavedEntity(placeable, entity.GlobalPosition, entity.GetFrame().rotation);
            SavedEntities.Add(savedEntity);
            loadedSavedEntities.Add(entity, savedEntity);
        }

        public void RemovePlaceableEntityFromCurrentScene(GameEntity entity) {
            // Remove productivity and space from entity
            GameEntity? prefabParentEntity;
            HomesteadScenePlaceable? placeableData = GetHomesteadSceneEntityPlaceable(entity, out prefabParentEntity);
            if (placeableData == null || prefabParentEntity == null)
                return;

            RemovePlaceableEntityValues(placeableData);

            SavedEntities.Remove(loadedSavedEntities[prefabParentEntity]);
            prefabParentEntity.Remove(0);
        }

        public HomesteadScenePlaceable? GetHomesteadSceneEntityPlaceable(GameEntity entity, out GameEntity? prefabParentEntity) {
            GameEntity entityToCheck = entity;
            if (!loadedSavedEntities.ContainsKey(entityToCheck)) {
                GameEntity? parentEntity = entityToCheck.Parent;
                if (parentEntity == null || !loadedSavedEntities.ContainsKey(parentEntity)) {
                    prefabParentEntity = null;
                    return null;
                }
                else
                    entityToCheck = parentEntity;
            }

            prefabParentEntity = entityToCheck;
            return loadedSavedEntities[entityToCheck].Placeable;
        }

        public void AddAllSavedEntitiesToCurrentScene() {
            loadedSavedEntities = new();

            foreach (HomesteadSceneSavedEntity savedEntity in SavedEntities.ToList())
                AddSavedEntityToCurrentScene(savedEntity);
        }

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

            HomesteadMissionView.TriggerSceneChanges();

            // Give back items required based on engineering skill
            if (placeable.ItemRequirements == null || placeable.ItemRequirements.Count == 0)
                return;

            int engineeringSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Engineering);

            foreach (KeyValuePair<string, int> pair in placeable.ItemRequirements) {
                string[] itemIDs = pair.Key.Split('|');
                string itemID = itemIDs.GetRandomElementInefficiently();
                ItemObject? itemToGive = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemID);
                if (itemToGive == null)
                    continue;

                float percentageToGiveBack = MathF.Lerp(0.05f, 0.5f, engineeringSkill / 300f);
                int amountToGiveBack = (int)Math.Round(pair.Value * percentageToGiveBack);
                if (amountToGiveBack <= 0)
                    continue;

                Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, percentageToGiveBack * 100);
                Homestead.Stash.AddToCounts(itemToGive, amountToGiveBack);
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

            HomesteadMissionView.TriggerSceneChanges();
        }
    }
}
