using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Homesteads.Models {
    public class HomesteadMissionView : MissionView {
        public static HomesteadMissionView? Instance;

        private HomesteadScene homesteadScene;
        private GauntletLayer layer;
        private IGauntletMovie movie;
        private HomesteadVM dataSource;

        public HomesteadMissionView(Homestead homestead) {
            Instance = this;

            homesteadScene = homestead.GetHomesteadScene();
        }

        public static void TriggerSceneChanges() {
            if (Instance == null)
                return;

            Instance.dataSource.BuildPointsLeft = Instance.homesteadScene.BuildPointsLeftToUse;
            Instance.dataSource.TotalProductivity = Instance.homesteadScene.TotalProductivity;
            Instance.dataSource.ExtraSpace = Instance.homesteadScene.TotalSpace;
            Instance.dataSource.TotalLeisure = Instance.homesteadScene.TotalLeisure;
        }

        public static void SetStatVisibility(bool visible) {
            if (Instance == null)
                return;

            Instance.dataSource.AreStatsVisible = visible;
        }

        public static void SetPlaceableBoxVisibility(bool visible) {
            if (Instance == null)
                return;

            Instance.dataSource.CurrentPlaceableBoxVisible = visible;
        }

        public static void ChangeCurrentPlaceable(string displayName, string description) {
            if (Instance == null)
                return;

            Instance.dataSource.CurrentPlaceableName = displayName;
            Instance.dataSource.CurrentPlaceableDesc = description;
        }

        public override void OnMissionScreenInitialize() {
            dataSource = new HomesteadVM(homesteadScene);
            layer = new GauntletLayer(1);
            movie = layer.LoadMovie("HomesteadEditorHUD", dataSource);
            MissionScreen.AddLayer(layer);
        }

        public override void OnMissionScreenFinalize() {
            MissionScreen.RemoveLayer(layer);
            Instance = null;
        }
    }

    public class HomesteadVM : ViewModel {
        private HomesteadScene homesteadScene;

        private bool _areStatsVisible;
        private bool _currentPlaceableBoxVisible;

        private int _buildPointsLeft;
        private int _productivity;
        private int _space;
        private int _leisure;

        private string _placeableName;
        private string _placeableDesc;

        public HomesteadVM(HomesteadScene homesteadScene) {
            this.homesteadScene = homesteadScene;

            _areStatsVisible = false;
            _currentPlaceableBoxVisible = false;

            _buildPointsLeft = homesteadScene.BuildPointsLeftToUse;
            _productivity = homesteadScene.TotalProductivity;
            _space = homesteadScene.TotalSpace;
            _leisure = homesteadScene.TotalLeisure;

            _placeableName = "";
            _placeableDesc = "";
        }

        [DataSourceProperty]
        public string BuildPointsLocalization => Utils.GetLocalizedString("{=homestead_gui_build_points}Build Points: ");
        [DataSourceProperty]
        public string ProductivityLocalization => Utils.GetLocalizedString("{=homestead_gui_productivity}Productivity: ");
        [DataSourceProperty]
        public string SpaceLocalization => Utils.GetLocalizedString("{=homestead_gui_space}Extra Space: ");
        [DataSourceProperty]
        public string LeisureLocalization => Utils.GetLocalizedString("{=homestead_gui_leisure}Leisure: ");

        [DataSourceProperty]
        public bool AreStatsVisible {
            get {
                return _areStatsVisible;
            }
            set {
                if (value != _areStatsVisible) {
                    _areStatsVisible = value;
                    OnPropertyChangedWithValue(value, "AreStatsVisible");
                }
            }
        }

        [DataSourceProperty]
        public bool CurrentPlaceableBoxVisible {
            get {
                return _currentPlaceableBoxVisible;
            }
            set {
                if (value != _currentPlaceableBoxVisible) {
                    _currentPlaceableBoxVisible = value;
                    OnPropertyChangedWithValue(value, "CurrentPlaceableBoxVisible");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentPlaceableName {
            get {
                return _placeableName;
            }
            set {
                if (value != _placeableName) {
                    _placeableName = value;
                    OnPropertyChangedWithValue(value, "CurrentPlaceableName");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentPlaceableDesc {
            get {
                return _placeableDesc;
            }
            set {
                if (value != _placeableDesc) {
                    _placeableDesc = value;
                    OnPropertyChangedWithValue(value, "CurrentPlaceableDesc");
                }
            }
        }

        [DataSourceProperty]
        public int BuildPointsLeft {
            get {
                return _buildPointsLeft;
            }
            set {
                if (value != _buildPointsLeft) {
                    _buildPointsLeft = value;
                    OnPropertyChangedWithValue(value, "BuildPointsLeft");
                }
            }
        }

        [DataSourceProperty]
        public int TotalProductivity {
            get {
                return _productivity;
            }
            set {
                if (value != _productivity) {
                    _productivity = value;
                    OnPropertyChangedWithValue(value, "TotalProductivity");
                }
            }
        }

        [DataSourceProperty]
        public int ExtraSpace {
            get {
                return _space;
            }
            set {
                if (value != _space) {
                    _space = value;
                    OnPropertyChangedWithValue(value, "ExtraSpace");
                }
            }
        }

        [DataSourceProperty]
        public int TotalLeisure {
            get {
                return _leisure;
            }
            set {
                if (value != _leisure) {
                    _leisure = value;
                    OnPropertyChangedWithValue(value, "TotalLeisure");
                }
            }
        }
    }
}
