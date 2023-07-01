using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homesteads {
    public static class HomesteadTutorial {
        public static int TutorialStage {
            get {
                return HomesteadBehavior.Instance != null ? HomesteadBehavior.Instance.TutorialStage : 0;
            }
            set {
                HomesteadBehavior.Instance.TutorialStage = value;
            }
        }

        // stage 0
        public static void LaunchedMenu() {
            if (TutorialStage > 0)
                return;
            TutorialStage++;

            string localizedTag = "homestead_tutorial_launched_menu";
            string text = @"Hello, you beautiful person. Welcome to Homesteads! This tutorial will hopefully help you a little along this journey.
To the left of this message box, you will see your homestead's game menu. The two most important options are 'Walk around' and 'Manage homestead'.
Please click 'Manage homestead' to continue the tutorial, I won't pop up in 'Walk around' until you do. :)";
            Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text));
        }

        // stage 1
        public static void ManagingHomestead() {
            if (TutorialStage > 1)
                return;
            TutorialStage++;

            string localizedTag = "homestead_tutorial_managing_homestead";
            string text = @"Here you will find information about your homestead and some options to manage it. You might want to consider putting some starting food in the stash and depositing some gold.
In the information box, you will find that homesteads operate based on 3 main components; space, productivity, and leisure.
Space will increase the amount of troops you can have in your homestead.
Productivity will increase the amount of gold your homestead generates, but it will decrease morale.
Leisure does the opposite of productivity. It will decrease your gold made, but will increase the morale.
You will also find that your homestead has a tier level at the top! Your homestead, at maximum, can be tier 3. Upgrading your tier is important to many different systems in homesteads.
Upgrading your tier can be done by placing more troops in your homestead and assigning a leader with good Steward and Engineering skill.
Let's go 'Walk around' a bit, yea? I'm feeling antsy :) also don't forget to store some gold and food before you leave!";
            Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text));
        }

        // stage 2
        public static void WalkAround() {
            if (TutorialStage > 2)
                return;
            TutorialStage++;

            string localizedTag = "homestead_tutorial_walk_around";
            string text = @"Welcome to the site of your new homestead! Hopefully, this is a good spot for you, but if not feel free to walk around a bit and see if you can't find a better one.
You can also feel free to pack up the homestead and move to a different spot on the campaign map by talking to the leader you assigned!
Here's where the fun begins! Press P, by default, to cycle through your edit modes. Feel free to press O to set your spawn position while in any edit mode.
While in build mode, press ' (yes, the key) to cycle through categories and [ ] to cycle through the current category.
While cycling, pay attention to the bottom left as it will give you information about the current building you have highlighted! Press 1-2 3-4 5-6 to rotate the building. You can reset rotation by pressing I.
When you are happy with the position, press Q to place! Pressing P again will cycle through additional edit modes! Feel free to try those out as well.";
            Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text), false);
        }

        private static string GetTextLocalizedString(string localizedTag, string text) {
            return Utils.GetLocalizedString("{=" + localizedTag + "}" + text);
        }

        private static string GetTitleLocalizedString() {
            return Utils.GetLocalizedString("{=homestead_tutorial_title}Homesteads Tutorial");
        }
    }
}
