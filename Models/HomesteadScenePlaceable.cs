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
using TaleWorlds.Core;

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
        // DO NOT USE FIELD 8
        [SaveableField(9)]
        public Dictionary<string, int> ItemRequirements;
        [SaveableField(10)]
        public List<HomesteadScenePlaceableProducedItem> ProduceItems;
        [SaveableField(11)]
        public string BuilderMenuCategoryString;

        public HomesteadScenePlaceable(string builderMenuCategoryString, string displayName, string desc, string prefabName, int buildPointsRequired, int productivity, int space, int leisure, List<HomesteadScenePlaceableProducedItem> produceItems, Dictionary<string, int> itemRequirements) {
            BuilderMenuCategoryString = builderMenuCategoryString;
            DisplayName = Utils.GetLocalizedString(displayName);
            Description = "--------------------\n" + Utils.GetLocalizedString(desc) + "\n";
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
                List<string> modifiedProduceItemNames = new();
                foreach (HomesteadScenePlaceableProducedItem item in ProduceItems) {
                    string[] producedItemIDs = item.ItemProducedID.Split('|');
                    List<string> validItemProducedNames = new();
                    foreach (string itemID in producedItemIDs) {
                        ItemObject? itemProduced = Utils.GetItemFromID(itemID);
                        if (itemProduced == null)
                            continue;
                        validItemProducedNames.Add(itemProduced.Name.ToString());
                    }
                    string itemString = String.Join(Utils.GetLocalizedString("{=homestead_current_placeable_or} OR "), validItemProducedNames) + " x" + item.AmountToProduce;
                    if (item.RequiredItemsToProduce.Count > 0) {
                        itemString += Utils.GetLocalizedString("{=homestead_current_placeable_needs} NEEDS ");
                        List<string> modifiedRequiredItemNames = new();
                        foreach (KeyValuePair<string, int> pair in item.RequiredItemsToProduce) {
                            string[] requiredItemIDs = pair.Key.Split('|');
                            List<string> validItemRequiredNames = new();
                            foreach (string itemID in requiredItemIDs) {
                                ItemObject? itemRequired = Utils.GetItemFromID(itemID);
                                if (itemRequired == null)
                                    continue;
                                validItemRequiredNames.Add(itemRequired.Name.ToString());
                            }
                            modifiedRequiredItemNames.Add(String.Join(Utils.GetLocalizedString("{=homestead_current_placeable_or} OR "), validItemRequiredNames) + " x" + pair.Value);
                        }
                        itemString += String.Join(", ", modifiedRequiredItemNames);
                    }
                    modifiedProduceItemNames.Add(itemString);
                }
                TextObject producesText = new TextObject("\n" + "{=homestead_current_placeable_produces}PRODUCES:" + "\n" + String.Join(", \n", modifiedProduceItemNames));
                producesTextRaw = producesText.ToString() + "\n--------------------";
            }

            string itemRequirementsTextRaw = "";
            if (itemRequirements.Count > 0) {
                List<string> modifiedItemRequirementStrings = new();
                foreach (KeyValuePair<string, int> pair in itemRequirements) {
                    ItemObject? item = Utils.GetItemFromID(pair.Key);
                    if (item == null)
                        continue;
                    modifiedItemRequirementStrings.Add(item.Name.ToString() + " x" + pair.Value);
                }
                TextObject itemRequirementsText = new TextObject("\n" + "{=homestead_current_placeable_item_requirements}ITEMS REQUIRED:" + "\n" + String.Join(", \n", modifiedItemRequirementStrings));
                itemRequirementsTextRaw = itemRequirementsText.ToString();
            }

            if (producesTextRaw != "" || itemRequirementsTextRaw != "")
                increasesText = new TextObject(increasesText.ToString() + "\n--------------------");

            Description += "--------------------\n" +
                buildPointsRequiredText.ToString() +
                increasesText.ToString() +
                producesTextRaw +
                itemRequirementsTextRaw;
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

                string builderMenuCategory = element.Element("menuCategory").Value;
                string displayName = element.Element("displayName").Value;
                string description = element.Element("description").Value;
                string prefabName = element.Element("prefabName").Value;
                int buildPointsRequired = (int)element.Element("buildPointsRequired");
                int productivityIncrease = (int)element.Element("productivityIncrease");
                int spaceIncrease = (int)element.Element("spaceIncrease");
                int leisureIncrease = (int)element.Element("leisureIncrease");

                List<HomesteadScenePlaceableProducedItem> produceItems = new();
                XElement? produceItemsElement = element.Element("ProduceItems");
                if (produceItemsElement != null) {
                    foreach (XElement produceItem in produceItemsElement.Descendants("ProduceItem")) {
                        string produceItemName = produceItem.Element("name").Value;
                        int produceItemAmount = (int)produceItem.Element("amount");
                        float dailyChance = (float)produceItem.Element("dailyChance");
                        Dictionary<string, int> requiredItems = new();
                        XElement? produceItemsRequiredItemsElement = produceItem.Element("RequiredItems");
                        if (produceItemsRequiredItemsElement != null) {
                            foreach (XElement requiredItem in produceItemsRequiredItemsElement.Descendants("RequiredItem")) {
                                string requiredItemName = requiredItem.Element("name").Value;
                                int requiredItemAmount = (int)requiredItem.Element("amount");
                                requiredItems[requiredItemName] = requiredItemAmount;
                            }
                        }
                        produceItems.Add(new HomesteadScenePlaceableProducedItem(produceItemName, produceItemAmount, dailyChance, requiredItems));
                    }
                }

                Dictionary<string, int> itemsRequired = new();
                XElement? itemsRequiredElement = element.Element("ItemRequirements");
                if (itemsRequiredElement != null)
                    foreach (XElement itemRequired in itemsRequiredElement.Descendants("Item"))
                        itemsRequired.Add(itemRequired.Element("name").Value, (int)itemRequired.Element("amount"));

                HomesteadScenePlaceable placeable = new HomesteadScenePlaceable(builderMenuCategory, displayName, description, prefabName, buildPointsRequired, productivityIncrease, spaceIncrease, leisureIncrease, produceItems, itemsRequired);
                placeables.Add(placeable);
            }

            return placeables;
        }
    }
}
