using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Homesteads.Models {
    public class HomesteadScenePlaceable {
        [SaveableField(1)]
        public string DisplayName;
        [SaveableField(2)]
        public string Description;
        [SaveableField(3)]
        public string PrefabName;
        [SaveableField(4)]
        public int BuildPointsRequired;
        [SaveableField(5)]
        public int ProductivityIncrease;
        [SaveableField(6)]
        public int SpaceIncrease;
        [SaveableField(7)]
        public int LeisureIncrease;
        [SaveableField(8)]
        public List<string> ProduceItems;
        [SaveableField(9)]
        public Dictionary<string, int> ItemRequirements;

        public HomesteadScenePlaceable(string displayName, string desc, string prefabName, int buildPointsRequired, int productivity, int space, int leisure, List<string> produceItems, Dictionary<string, int> itemRequirements) {
            DisplayName = displayName;
            Description = "--------------------\n" + " - " + displayName + " - \n" + desc + "\n";
            PrefabName = prefabName;
            BuildPointsRequired = buildPointsRequired;
            ProductivityIncrease = productivity;
            SpaceIncrease = space;
            LeisureIncrease = leisure;
            ProduceItems = produceItems;
            ItemRequirements = itemRequirements;

            TextObject buildPointsRequiredText = new TextObject("{=homestead_current_placeable_buildpoints_required}{BUILD_POINTS_NEEDED} BUILD POINTS NEEDED" + "\n");
            buildPointsRequiredText.SetTextVariable("BUILD_POINTS_NEEDED", buildPointsRequired);
            TextObject increasesText = new TextObject("{=homestead_current_placeable_increases}+{PRODUCTIVITY} Productivity | +{SPACE} Space | +{LEISURE} Leisure");
            increasesText.SetTextVariable("PRODUCTIVITY", productivity);
            increasesText.SetTextVariable("SPACE", space);
            increasesText.SetTextVariable("LEISURE", leisure);

            string producesTextRaw = "";
            if (produceItems.Count > 0) {
                Dictionary<string, int> itemNamesAndCount = new();
                foreach (string produceItem in produceItems) {
                    int currentCount = itemNamesAndCount.ContainsKey(produceItem) ? itemNamesAndCount[produceItem] : 0;
                    itemNamesAndCount[produceItem] = currentCount + 1;
                }
                List<string> modifiedProduceItemNames = new();
                foreach (KeyValuePair<string, int> pair in itemNamesAndCount)
                    modifiedProduceItemNames.Add(pair.Key + " x" + pair.Value);
                TextObject producesText = new TextObject("\n" + "{=homestead_current_placeable_produces}PRODUCES:\n" + String.Join(", ", modifiedProduceItemNames));
                producesTextRaw = producesText.ToString();
            }

            string itemRequirementsTextRaw = "";
            if (itemRequirements.Count > 0) {
                List<string> modifiedItemRequirementStrings = new();
                foreach (KeyValuePair<string, int> pair in itemRequirements)
                    modifiedItemRequirementStrings.Add(pair.Key + " x" + pair.Value);
                TextObject itemRequirementsText = new TextObject("\n" + "{=homestead_current_placeable_item_requirements}ITEMS REQUIRED:\n" + String.Join(", ", modifiedItemRequirementStrings));
                itemRequirementsTextRaw = itemRequirementsText.ToString();
            }

            Description += "--------------------\n" +
                buildPointsRequiredText.ToString() +
                increasesText.ToString() +
                producesTextRaw +
                itemRequirementsTextRaw +
                "\n--------------------";
        }

        public static List<HomesteadScenePlaceable> GetTierGroup(int tier) {
            return GetFromXMLs(tier);
        }

        public static List<HomesteadScenePlaceable> GetFromXMLs(int tier) {
            List<HomesteadScenePlaceable> placeables = new();

            DirectoryInfo modulesDirectory = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName).FullName).FullName);

            foreach (DirectoryInfo modulesChildDirectory in modulesDirectory.GetDirectories())
                foreach (FileInfo fileInfo in modulesChildDirectory.GetFiles())
                    if (fileInfo.Name == "HomesteadsPlaceables.xml")
                        placeables.AddRange(GetFromXMLStringPath(fileInfo.FullName, tier));

            return placeables;
        }

        private static List<HomesteadScenePlaceable> GetFromXMLStringPath(string fullXMLPath, int tier) {
            List<HomesteadScenePlaceable> placeables = new();
            XElement placeablesXML = XElement.Load(fullXMLPath);

            foreach (XElement element in placeablesXML.Descendants("Placeable")) {
                // Tier check
                int tierRequired = (int)element.Element("tierRequired");
                if (tierRequired > tier)
                    continue;

                string displayName = element.Element("displayName").Value;
                string description = element.Element("description").Value;
                string prefabName = element.Element("prefabName").Value;
                int buildPointsRequired = (int)element.Element("buildPointsRequired");
                int productivityIncrease = (int)element.Element("productivityIncrease");
                int spaceIncrease = (int)element.Element("spaceIncrease");
                int leisureIncrease = (int)element.Element("leisureIncrease");

                List<string> produceItems = new();
                XElement? produceItemsElement = element.Element("ProduceItems");
                if (produceItemsElement != null)
                    foreach (XElement produceItem in produceItemsElement.Descendants("produceItem"))
                        produceItems.Add(produceItem.Value);

                Dictionary<string, int> itemsRequired = new();
                XElement? itemsRequiredElement = element.Element("ItemRequirements");
                if (itemsRequiredElement != null)
                    foreach (XElement itemRequired in itemsRequiredElement.Descendants("Item"))
                        itemsRequired.Add(itemRequired.Element("name").Value, (int)itemRequired.Element("amount"));

                HomesteadScenePlaceable placeable = new HomesteadScenePlaceable(displayName, description, prefabName, buildPointsRequired, productivityIncrease, spaceIncrease, leisureIncrease, produceItems, itemsRequired);
                placeables.Add(placeable);
            }

            return placeables;
        }
    }
}
