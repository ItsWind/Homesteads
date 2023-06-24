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
        private List<HomesteadScenePlaceable> validPlaceables = new();

        private Homestead homestead;
        // 0 = no editing, 1 = building, 2 = destroying
        private int editModeType = 0;
        private GameEntity? gameEntityLookingAt;
        private Vec3 positionLookingAt;
        private int currentPlaceableIndex = 0;
        private GameEntity? dummyEntity;
        private Mat3 buildingModeSavedRotation = Mat3.Identity;
        private HomesteadScenePlaceable currentPlaceable => validPlaceables[currentPlaceableIndex];
        
        public HomesteadSceneEditingMissionLogic(Homestead homestead) {
            this.homestead = homestead;
            validPlaceables = HomesteadScenePlaceable.GetTierGroup(homestead.Tier);
        }

        public override void AfterStart() {
            Utils.PrintDebugMessage("Press P to cycle through edit modes.");
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
                }
                return;
            }

            if (Input.IsKeyPressed(GlobalSettings<MCMConfig>.Instance.GetSetPlayerSpawnKey())) {
                Vec3 newPlayerSpawnPosition = new Vec3(Agent.Main.Position.X, Agent.Main.Position.Y, Mission.Scene.GetGroundHeightAtPosition(Agent.Main.Position));
                homestead.GetHomesteadScene().PlayerSpawnPosition = newPlayerSpawnPosition;
                Utils.PrintDebugMessage("New player spawn position set!", 0, 201, 0);
                return;
            }

            if (dummyEntity == null)
                return;
            // Below this are keys only usable when the dummy entity is present

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
                    toPrint = "You are no longer making any changes.";
                    break;
                case 1:
                    toPrint = "You are now in building mode. Press Q to place. Press { and } to scroll through placeable prefabs. Use 1-2 3-4 5-6 to rotate the entity.";
                    break;
                case 2:
                    toPrint = "You are now in destroying mode. Press Q to delete the currently looked at entity.";
                    break;
            }
            Utils.PrintDebugMessage(toPrint, 201, 0, 0);
            if (editModeType == 1)
                Utils.PrintDebugMessage(currentPlaceable.Description);
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
                currentPlaceableIndex = validPlaceables.Count - 1;
            if (currentPlaceableIndex >= validPlaceables.Count)
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
}
