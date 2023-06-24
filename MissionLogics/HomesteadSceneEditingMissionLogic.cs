﻿using System;
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
        // 0 = no editing, 1 = building, 2 = destroying
        private int editModeType = 0;
        private GameEntity? gameEntityLookingAt;
        private Vec3 positionLookingAt;
        private int currentPlaceableIndex = 0;
        private GameEntity? dummyEntity;
        private Mat3 buildingModeSavedRotation = Mat3.Identity;
        private HomesteadScenePlaceable currentPlaceable => validPlaceablesInCurrentCategory[currentPlaceableIndex];
        private string currentCategoryString = "Misc";
        
        public HomesteadSceneEditingMissionLogic(Homestead homestead) {
            this.homestead = homestead;
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
                if (editModeType == 1 && dummyEntity != null) {
                    homestead.GetHomesteadScene().AddPlaceableEntityToCurrentScene(currentPlaceable, dummyEntity.GlobalPosition, dummyEntity.GetFrame().rotation);
                    RemoveDummyEntity();
                } else if (editModeType == 2 && gameEntityLookingAt != null) {
                    homestead.GetHomesteadScene().RemovePlaceableEntityFromCurrentScene(gameEntityLookingAt);
                }/* else if (editModeType == 3) {
                    homestead.GetHomesteadScene().PlayerToggleNavMeshAtCurrentPosition();
                }*/
                return;
            }

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetSetPlayerSpawnKey())) {
                Vec3 newPlayerSpawnPosition = new Vec3(Agent.Main.Position.X, Agent.Main.Position.Y, Mission.Scene.GetGroundHeightAtPosition(Agent.Main.Position));
                homestead.GetHomesteadScene().PlayerSpawnPosition = newPlayerSpawnPosition;
                Utils.PrintLocalizedMessage("homestead_new_player_spawn_set", "New player spawn position set!", 0, 201, 0);
                return;
            }

            if (dummyEntity == null)
                return;
            // Below this are keys only usable when the dummy entity is present

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetSwitchBuilderModeCategoryKey())) {
                SwitchBuilderMenuCategory();
                return;
            }

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetResetRotationKey())) {
                buildingModeSavedRotation = Mat3.Identity;
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
        }

        private void SwitchEditMode() {
            editModeType++;
            if (editModeType > 2)
                editModeType = 0;
            string toPrint = "";
            switch (editModeType) {
                case 0:
                    toPrint = Utils.GetLocalizedString("{=homestead_cancelled_edit_mode}You are no longer making any changes.");
                    //homestead.GetHomesteadScene().ToggleNavMeshDisablersVisibility(false);
                    break;
                case 1:
                    toPrint = Utils.GetLocalizedString("{=homestead_entered_building_mode}You are now in building mode. The keys listed are default keys. Press Q, by default, to place. Press { and } to scroll through placeable prefabs. Press \" to switch categories. Press 1-2 3-4 5-6 to rotate the entity.");
                    break;
                case 2:
                    toPrint = Utils.GetLocalizedString("{=homestead_entered_delete_mode}You are now in delete mode. Press Q, by default, to delete the currently looked at entity.");
                    break;
                /*case 3:
                    toPrint = "You are now in navmesh mode. Press Q to disable/enable the currently stood on navmesh.";
                    homestead.GetHomesteadScene().ToggleNavMeshDisablersVisibility(true);
                    break;*/
            }
            Utils.PrintDebugMessage(toPrint, 201, 0, 0);
            if (editModeType == 1)
                Utils.PrintDebugMessage(currentPlaceable.Description);
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
            if (editModeType == 1) {
                if (!positionLookingAt.IsValid) {
                    RemoveDummyEntity();
                    return;
                }
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
            Utils.PrintDebugMessage(currentPlaceable.Description);
        }

        private void CreateBuildingModeDummyEntity() {
            if (dummyEntity != null)
                return;

            dummyEntity = Utils.CreateGameEntityWithPrefab(currentPlaceable.PrefabName, positionLookingAt, buildingModeSavedRotation);
            List<GameEntity> allEntitiesInDummy = dummyEntity.GetEntityAndChildren().ToList();
            foreach (GameEntity entity in allEntitiesInDummy)
                entity.SetPhysicsState(false, true);
            SetBuildingModeDummyEntityColor(allEntitiesInDummy);
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
                    entityMesh.SetMaterial(currentPlaceable.BuildPointsRequired <= homestead.GetHomesteadScene().BuildPointsLeftToUse ? Utils.DoesItemRosterHaveItems(homestead.Stash, currentPlaceable.ItemRequirements) ? "plain_green" : "plain_red" : "plain_red");
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
