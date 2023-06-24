using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;

namespace Homesteads {
    internal sealed class MCMConfig : AttributeGlobalSettings<MCMConfig> {
        public override string Id => "Homesteads";
        public override string DisplayName => "Homesteads";
        public override string FolderName => "Homesteads";
        public override string FormatType => "xml";

        [SettingPropertyText("Toggle Edit Mode Types", Order = 1, HintText = "The button that is used to switch to an edit mode or turn it off.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindEditMode { get; set; } = "P";

        [SettingPropertyText("Switch Builder Mode Category", Order = 2, HintText = "The button that will switch your build menu's category of placeables to the next category.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindSwitchBuilderModeCategory { get; set; } = "Apostrophe";

        [SettingPropertyText("Cycle Placeables Left", Order = 3, HintText = "One of two buttons that are used to switch to a different placeable.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindCycleLeft { get; set; } = "OpenBraces";

        [SettingPropertyText("Cycle Placeables Right", Order = 4, HintText = "One of two buttons that are used to switch to a different placeable.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindCycleRight { get; set; } = "CloseBraces";

        [SettingPropertyText("Place Highlighted Placeable", Order = 5, HintText = "The button that is used to place the selected placeable.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindPlace { get; set; } = "Q";

        [SettingPropertyText("Set Player Spawn", Order = 6, HintText = "The button that is used to set the player's spawn.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindSetPlayerSpawn { get; set; } = "O";

        [SettingPropertyText("Reset Rotation", Order = 7, HintText = "The button that is used to reset the rotation of the dummy placeable while in edit mode.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindResetRotation { get; set; } = "I";

        [SettingPropertyText("Rotate Up", Order = 8, HintText = "The button that is used to rotate the highlighted placeable upwards.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateUp { get; set; } = "D1";

        [SettingPropertyText("Rotate Down", Order = 9, HintText = "The button that is used to rotate the highlighted placeable downwards.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateDown { get; set; } = "D2";

        [SettingPropertyText("Rotate Tilt Left", Order = 10, HintText = "The button that is used to tilt the highlighted placeable left.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateTiltLeft { get; set; } = "D3";

        [SettingPropertyText("Rotate Tilt Right", Order = 11, HintText = "The button that is used to tilt the highlighted placeable right.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateTiltRight { get; set; } = "D4";

        [SettingPropertyText("Rotate Turn Left", Order = 12, HintText = "The button that is used to turn the highlighted placeable left.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateTurnLeft { get; set; } = "D5";

        [SettingPropertyText("Rotate Turn Right", Order = 13, HintText = "The button that is used to turn the highlighted placeable right.", RequireRestart = false)]
        [SettingPropertyGroup("Change Key Binds")]
        public string KeyBindRotateTurnRight { get; set; } = "D6";

        public InputKey GetEditModeKey() {
            return GetKey(KeyBindEditMode, InputKey.P);
        }
        public InputKey GetSwitchBuilderModeCategoryKey() {
            return GetKey(KeyBindSwitchBuilderModeCategory, InputKey.Apostrophe);
        }
        public InputKey GetCycleLeftKey() {
            return GetKey(KeyBindCycleLeft, InputKey.OpenBraces);
        }
        public InputKey GetCycleRightKey() {
            return GetKey(KeyBindCycleRight, InputKey.CloseBraces);
        }
        public InputKey GetPlaceKey() {
            return GetKey(KeyBindPlace, InputKey.Q);
        }
        public InputKey GetSetPlayerSpawnKey() {
            return GetKey(KeyBindSetPlayerSpawn, InputKey.O);
        }
        public InputKey GetResetRotationKey() {
            return GetKey(KeyBindResetRotation, InputKey.I);
        }
        public InputKey GetRotateUpKey() {
            return GetKey(KeyBindRotateUp, InputKey.D1);
        }
        public InputKey GetRotateDownKey() {
            return GetKey(KeyBindRotateDown, InputKey.D2);
        }
        public InputKey GetRotateTiltLeftKey() {
            return GetKey(KeyBindRotateTiltLeft, InputKey.D3);
        }
        public InputKey GetRotateTiltRightKey() {
            return GetKey(KeyBindRotateTiltRight, InputKey.D4);
        }
        public InputKey GetRotateTurnLeftKey() {
            return GetKey(KeyBindRotateTurnLeft, InputKey.D5);
        }
        public InputKey GetRotateTurnRightKey() {
            return GetKey(KeyBindRotateTurnRight, InputKey.D6);
        }

        private InputKey GetKey(string toUse, InputKey defaultKey) {
            InputKey key;
            try {
                toUse = toUse.Length == 1 ? toUse.ToUpper() : toUse;
                key = (InputKey)Enum.Parse(typeof(InputKey), toUse);
            } catch (Exception) { return defaultKey; }
            return key;
        }
    }
}
