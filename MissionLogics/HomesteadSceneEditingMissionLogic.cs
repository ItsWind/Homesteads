using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.InputSystem;
using Homesteads.Models;
using TaleWorlds.Engine;
using MCM.Abstractions.Base.Global;

namespace Homesteads.MissionLogics {
    public class HomesteadSceneEditingMissionLogic : MissionLogic {
        private List<HomesteadScenePlaceable> allPlaceables = new();
        private List<HomesteadScenePlaceable> validPlaceablesInCurrentCategory => allPlaceables.Where(x => x.BuilderMenuCategoryString == currentCategoryString).ToList();

        private Homestead homestead;
        private HomesteadScene homesteadScene;
        // 0 = no editing, 1 = building, 2 = destroying
        private int editModeType = 0;
        private GameEntity? gameEntityLookingAt;
        private Vec3 positionLookingAt;
        private int currentPlaceableIndex = 0;
        private GameEntity? dummyEntity;
        private Mat3 buildingModeSavedRotation = Mat3.Identity;
        private HomesteadScenePlaceable? currentPlaceableOverride = null;
        private HomesteadScenePlaceable currentPlaceable => currentPlaceableOverride == null ? validPlaceablesInCurrentCategory[currentPlaceableIndex] : currentPlaceableOverride;
        private string currentCategoryString = "Misc";
        
        public HomesteadSceneEditingMissionLogic(Homestead homestead) {
            this.homestead = homestead;
            homesteadScene = homestead.GetHomesteadScene();
            allPlaceables = HomesteadScenePlaceable.GetTierGroup(homestead.Tier);
        }

        public override void AfterStart() {
            Utils.PrintLocalizedMessage("homestead_mission_start_reminder", "Press P, by default, to cycle through edit modes.", 0, 201, 0);
        }

        public override void OnMissionTick(float dt) {
            if (Agent.Main == null)
                return;

            HandleLookingAtOnTick(dt);
            HandleInputOnTick(dt);
            BuildingModeDummyEntityTick(dt);
        }

        private void HandleLookingAtOnTick(float dt) {
            if (editModeType == 0)
                return;

            Vec3 eyeGlobalPos = Agent.Main.GetEyeGlobalPosition();
            float maximumPlaceDistance = 30f;
            Vec3 maximumPos = eyeGlobalPos + (Agent.Main.LookDirection * maximumPlaceDistance);

            float collisionDistance = 0f;
            Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPos, maximumPos, out collisionDistance, out positionLookingAt, out gameEntityLookingAt);
            if (collisionDistance > maximumPlaceDistance) {
                positionLookingAt = Vec3.Invalid;
                gameEntityLookingAt = null;
            }
        }

        private void HandleInputOnTick(float dt) {
            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetEditModeKey())) {
                SwitchEditMode();
                return;
            }

            if (editModeType == 0)
                return;
            // Below this are edit mode keys

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetPlaceKey())) {
                // Press Q in building mode
                if (editModeType == 1 && dummyEntity != null) {
                    homesteadScene.AddPlaceableEntityToCurrentScene(currentPlaceable, dummyEntity.GlobalPosition, dummyEntity.GetFrame().rotation);
                    RemoveDummyEntity();
                }
                // Press Q in delete mode
                else if (editModeType == 2 && gameEntityLookingAt != null) {
                    homesteadScene.RemovePlaceableEntityFromCurrentScene(gameEntityLookingAt);
                }
                // Press Q in edit mode
                else if (editModeType == 3) {
                    // If placeable is picked up
                    if (currentPlaceableOverride != null && dummyEntity != null) {
                        homesteadScene.AddPlaceableEntityToCurrentScene(currentPlaceable, dummyEntity.GlobalPosition, dummyEntity.GetFrame().rotation, true);
                        currentPlaceableOverride = null;
                        RemoveDummyEntity();
                    }
                    // If NO placeable is picked up
                    else if (currentPlaceableOverride == null && gameEntityLookingAt != null) {
                        GameEntity? prefabParent;
                        HomesteadScenePlaceable? placeableToCopy = homesteadScene.GetHomesteadSceneEntityPlaceable(gameEntityLookingAt, out prefabParent);
                        if (placeableToCopy == null || prefabParent == null)
                            return;

                        currentPlaceableOverride = placeableToCopy;
                        homesteadScene.RemovePlaceableEntityFromCurrentScene(prefabParent);
                    }
                }
                return;
            }

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetSetPlayerSpawnKey())) {
                Vec3 newPlayerSpawnPosition = new Vec3(Agent.Main.Position.X, Agent.Main.Position.Y, Mission.Scene.GetGroundHeightAtPosition(Agent.Main.Position));
                Mat3 newPlayerSpawnRotation = Agent.Main.Frame.rotation;
                homesteadScene.PlayerSpawnRotation = newPlayerSpawnRotation;
                homesteadScene.PlayerSpawnPosition = newPlayerSpawnPosition;
                Utils.PrintLocalizedMessage("homestead_new_player_spawn_set", "New player spawn position set!", 0, 201, 0);
                return;
            }

            if (dummyEntity == null)
                return;
            // Below this are keys only usable when the dummy entity is present

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetResetRotationKey())) {
                buildingModeSavedRotation = Mat3.Identity;
                return;
            }

            // rotate on side (x)
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateUpKey())) {
                RotateDummyEntity(dt, "x");
                return;
            }
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateDownKey())) {
                RotateDummyEntity(dt, "x", false);
                return;
            }
            // rotate forward (y)
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateTiltLeftKey())) {
                RotateDummyEntity(dt, "y");
                return;
            }
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateTiltRightKey())) {
                RotateDummyEntity(dt, "y", false);
                return;
            }
            // rotate up (z)
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateTurnLeftKey())) {
                RotateDummyEntity(dt, "z");
                return;
            }
            if (Input.IsKeyDown(GlobalSettings<MCMConfig>.Instance.GetRotateTurnRightKey())) {
                RotateDummyEntity(dt, "z", false);
                return;
            }

            if (editModeType != 1)
                return;

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetSwitchBuilderModeCategoryKey())) {
                SwitchBuilderMenuCategory();
                return;
            }

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetCycleRightKey())) {
                ChangeCurrentPlaceableIndex(1);
                return;
            }
            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetCycleLeftKey())) {
                ChangeCurrentPlaceableIndex(-1);
                return;
            }
        }

        private void SwitchEditMode() {
            // If placeable is picked up in edit mode
            if (currentPlaceableOverride != null) {
                Utils.PrintLocalizedMessage("homestead_edit_mode_switch_when_building_picked_up", "You can't switch your edit mode until you place the currently picked up placeable.", 255, 80, 80);
                return;
            }

            editModeType++;
            if (editModeType > 3)
                editModeType = 0;
            string toPrint = "";
            switch (editModeType) {
                case 0:
                    toPrint = Utils.GetLocalizedString("{=homestead_cancelled_edit_mode}You are no longer making any changes.");
                    break;
                case 1:
                    toPrint = Utils.GetLocalizedString("{=homestead_entered_building_mode}You are now in building mode. The keys listed are default keys. Press Q, by default, to place. Press { and } to scroll through placeable prefabs. Press \" to switch categories. Press 1-2 3-4 5-6 to rotate the entity.");
                    HomesteadMissionView.SetPlaceableBoxVisibility(true);
                    HomesteadMissionView.SetStatVisibility(true);
                    break;
                case 2:
                    toPrint = Utils.GetLocalizedString("{=homestead_entered_delete_mode}You are now in delete mode. Press Q, by default, to delete the currently looked at entity.");
                    HomesteadMissionView.SetPlaceableBoxVisibility(false);
                    break;
                case 3:
                    toPrint = Utils.GetLocalizedString("{=homestead_entered_edit_mode}You are now in edit mode. Press Q, by default, to pick up the currently looked at entity.");
                    HomesteadMissionView.SetStatVisibility(false);
                    break;
            }
            Utils.PrintDebugMessage(toPrint, 201, 0, 0);
        }

        private void SwitchBuilderMenuCategory() {
            currentPlaceableIndex = 0;
            BuilderMenuCategory currentCategory = (BuilderMenuCategory)Enum.Parse(typeof(BuilderMenuCategory), currentCategoryString);
            int currentCategoryIndex = (int)currentCategory;
            currentCategoryIndex++;
            if (currentCategoryIndex >= Enum.GetNames(typeof(BuilderMenuCategory)).Length)
                currentCategoryIndex = 0;
            BuilderMenuCategory newCategory = (BuilderMenuCategory)currentCategoryIndex;
            currentCategoryString = newCategory.ToString();

            Utils.PrintLocalizedMessage("homestead_switched_building_mode_category", "BUILD CATEGORY SWITCHED TO: {NEW_CATEGORY_NAME}", 0, 201, 0,
                ("NEW_CATEGORY_NAME", currentCategoryString));

            RemoveDummyEntity();
        }

        private void RotateDummyEntity(float dt, string typeOfRotation, bool isAdding = true) {
            if (dummyEntity == null)
                return;

            float change = dt * (isAdding ? 1 : -1);

            switch (typeOfRotation) {
                case "x":
                    buildingModeSavedRotation.RotateAboutSide(change);
                    break;
                case "y":
                    buildingModeSavedRotation.RotateAboutForward(change);
                    break;
                case "z":
                    buildingModeSavedRotation.RotateAboutUp(change);
                    break;
            }
        }

        private void BuildingModeDummyEntityTick(float dt) {
            if (editModeType == 1 || editModeType == 3) {
                if (!positionLookingAt.IsValid) {
                    RemoveDummyEntity();
                    return;
                }

                // Check if in edit mode and haven't picked up a building
                if (editModeType == 3 && currentPlaceableOverride == null)
                    return;

                CreateBuildingModeDummyEntity();
                dummyEntity.SetLocalPosition(positionLookingAt);
                MatrixFrame frame = dummyEntity.GetFrame();
                frame.rotation = buildingModeSavedRotation;
                dummyEntity.SetFrame(ref frame);
            }
            else {
                RemoveDummyEntity();
            }
        }

        private void ChangeCurrentPlaceableIndex(int change) {
            currentPlaceableIndex += change;
            if (currentPlaceableIndex < 0)
                currentPlaceableIndex = validPlaceablesInCurrentCategory.Count - 1;
            if (currentPlaceableIndex >= validPlaceablesInCurrentCategory.Count)
                currentPlaceableIndex = 0;

            RemoveDummyEntity();
        }

        private void CreateBuildingModeDummyEntity() {
            if (dummyEntity != null)
                return;

            dummyEntity = Utils.CreateGameEntityWithPrefab(currentPlaceable.PrefabName, positionLookingAt, buildingModeSavedRotation);
            List<GameEntity> allEntitiesInDummy = dummyEntity.GetEntityAndChildren().ToList();
            foreach (GameEntity entity in allEntitiesInDummy)
                entity.SetPhysicsState(false, true);
            SetBuildingModeDummyEntityColor(allEntitiesInDummy);

            HomesteadMissionView.ChangeCurrentPlaceable(currentPlaceable.DisplayName, currentPlaceable.Description);
        }

        private void SetBuildingModeDummyEntityColor(List<GameEntity>? allEntitiesInDummy = null) {
            if (dummyEntity == null)
                return;

            if (allEntitiesInDummy == null)
                allEntitiesInDummy = dummyEntity.GetEntityAndChildren().ToList();

            foreach (GameEntity entity in allEntitiesInDummy) {
                MetaMesh? dummyMetaMesh = entity.GetMetaMesh(0);
                if (dummyMetaMesh == null)
                    continue;
                for (int i = 0; i < dummyMetaMesh.MeshCount; i++) {
                    Mesh entityMesh = dummyMetaMesh.GetMeshAtIndex(i);
                    entityMesh.SetMaterial(currentPlaceableOverride != null ? "plain_green" : currentPlaceable.BuildPointsRequired <= homestead.GetHomesteadScene().BuildPointsLeftToUse ? Utils.DoesItemRosterHaveItems(homestead.Stash, currentPlaceable.ItemRequirements) ? "plain_green" : "plain_red" : "plain_red");
                }
            }
        }

        private void RemoveDummyEntity() {
            if (dummyEntity == null)
                return;

            dummyEntity.RemoveAllChildren();
            dummyEntity.Remove(0);
            dummyEntity = null;
        }
    }

    enum BuilderMenuCategory {
        Misc,
        Light,
        Housing,
        Productivity,
        Leisure
    }
}
