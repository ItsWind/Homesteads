using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models {
    public class HomesteadScenePlaceableProducedItem {
        [SaveableField(1)]
        public string ItemProducedID;
        [SaveableField(2)]
        public int AmountToProduce;
        [SaveableField(3)]
        public float DailyChance;
        [SaveableField(4)]
        public Dictionary<string, int> RequiredItemsToProduce;

        public HomesteadScenePlaceableProducedItem(string itemProducedID, int amountToProduce, float dailyChance, Dictionary<string, int> requiredItemsToProduce) {
            ItemProducedID = itemProducedID;
            AmountToProduce = amountToProduce;
            DailyChance = dailyChance;
            RequiredItemsToProduce = requiredItemsToProduce;
        }
    }
}
