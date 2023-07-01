using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;

namespace Homesteads.Models {
    public class Homestead : PartyComponent {
        public override Hero PartyOwner => leader;
        public override TextObject Name => new TextObject(name);
        public override Settlement HomeSettlement => Hero.MainHero.HomeSettlement;
        public override Hero Leader => leader;
        public TroopRoster Prisoners => MobileParty.PrisonRoster;
        public TroopRoster Troops => MobileParty.MemberRoster;
        public ItemRoster Stash => MobileParty.ItemRoster;
        public TextObject HomesteadInformation => BuildInformationTextObject();

        [SaveableField(1)]
        private string name = "UNNAMED HOMESTEAD";
        [SaveableField(2)]
        private Hero leader = null;
        [SaveableField(3)]
        public int Tier = 0;
        [SaveableField(4)]
        public int GoldStored = 0;
        [SaveableField(5)]
        public float TierProgress = 0f;
        // DONT USE SAVEABLE FIELD 6 AND 7
        [SaveableField(8)]
        private HomesteadScene? homesteadScene = null;

        public Homestead(Hero initialLeader) {
            leader = initialLeader;
        }

        public override void ChangePartyLeader(Hero newLeader) {
            Hero? oldLeader = leader;

            leader = newLeader;

            if (newLeader != null) {
                AddHeroToPartyAction.Apply(newLeader, MobileParty);
                if (oldLeader != null)
                    AddHeroToPartyAction.Apply(oldLeader, MobileParty.MainParty);
            }

            // Keep this for setting down homesteads to not have the cancel button bug
            if (PartyScreenManager.PartyScreenLogic != null)
                PartyScreenManager.PartyScreenLogic.DoneLogic(true);

            SetGameTextsForMenus();
        }

        protected override void OnFinalize() {
            HomesteadBehavior.Instance.HomesteadMobileParties.Remove(MobileParty);
            if (HomesteadBehavior.Instance.CurrentHomestead == this)
                HomesteadBehavior.Instance.CurrentHomestead = null;
        }

        public HomesteadScene GetHomesteadScene() {
            if (homesteadScene == null) {
                string sceneName = PlayerEncounter.GetBattleSceneForMapPatch(Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position2D));
                homesteadScene = new HomesteadScene(sceneName, this);
            }
            return homesteadScene;
        }

        public void PartyLeaderDied() {
            Utils.ShowMessageBox("A courier arrives...", "They bring you a message that bears bad news. " + leader.Name.ToString() + " has died and " + name + " needs a new leader assigned to it.");
            if (HomesteadBehavior.Instance.CurrentHomestead == this)
                if (PlayerEncounter.Current.IsPlayerWaiting)
                    GameMenu.ActivateGameMenu("homestead_menu_wait_waiting");
                else
                    GameMenu.ActivateGameMenu("homestead_menu_main");
            leader = null;
        }

        public void ChangeName(string newName) {
            name = newName;
            Party.MobileParty.SetCustomName(Name);

            SetGameTextsForMenus();
        }

        public bool PlayerChangeGoldStored(int amountToChange, out string failReason) {
            int amountToChangeAbs = MathF.Abs(amountToChange);
            GameTexts.SetVariable("GOLD_AMOUNT", amountToChangeAbs);
            string iconPath = "";
            string soundPath = "";
            // if withdrawing
            if (amountToChange < 0) {
                if (GoldStored < amountToChangeAbs) {
                    failReason = new TextObject("{=homestead_withdraw_gold_failreason}This homestead does not have that much gold stored!").ToString();
                    return false;
                }
                Hero.MainHero.ChangeHeroGold(amountToChangeAbs);
                GoldStored -= amountToChangeAbs;
                iconPath = "str_you_received_gold_with_icon";
                soundPath = "event:/ui/notification/coins_positive";
            }
            // if depositing
            else {
                if (Hero.MainHero.Gold < amountToChange) {
                    failReason = new TextObject("{=homestead_deposit_gold_failreason}You do not have that much gold!").ToString();
                    return false;
                }
                Hero.MainHero.ChangeHeroGold(-amountToChange);
                GoldStored += amountToChange;
                iconPath = "str_gold_removed_with_icon";
                soundPath = "event:/ui/notification/coins_negative";
            }
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText(iconPath, null).ToString(), soundPath));
            SetGameTextsForMenus();
            failReason = "";
            return true;
        }

        public void HourlyTick() {

        }

        public void DailyTick() {
            DailyTickChangeGoldStored();
            DailyTickNoGoldPenalty();
            DailyTickMoraleChange();
            DailyTickTierProgress();
            DailyTickProduceItems();
            DailyTickAutoRecruitSettlers();
        }

        public void BuildMapIcon(PartyVisual partyVisual) {
            GameEntity gameEntity = GameEntity.CreateEmpty(partyVisual.StrategicEntity.Scene, true);
            gameEntity.AddMultiMesh(MetaMesh.GetCopy("map_icon_siege_camp_tent", true, false), true);
            MatrixFrame identity = MatrixFrame.Identity;
            identity.rotation.ApplyScaleLocal(1.2f);
            gameEntity.SetFrame(ref identity);
            partyVisual.StrategicEntity.AddChild(gameEntity, false);
        }

        public int GetTroopLimit() {
            if (homesteadScene == null)
                return 1;
            else
                return homesteadScene.TotalSpace + 1;
        }

        public int GetPrisonerLimit() {
            if (homesteadScene == null)
                return 0;
            else
                return (int)Math.Round(GetTroopLimit() * 0.30f);
        }

        private void DailyTickAutoRecruitSettlers() {
            if (Troops.Count >= GetTroopLimit())
                return;

            int settlersArriving = 0;
            foreach (Settlement settlement in Campaign.Current.Settlements) {
                if (settlement.Town == null && !settlement.IsVillage)
                    continue;

                if (Troops.Count >= GetTroopLimit())
                    return;

                if (settlement.Position2D.Distance(MobileParty.Position2D) < 35f && MBRandom.RandomFloat < (Tier * 0.05f)) {
                    Troops.AddToCounts(settlement.IsCastle ? settlement.Culture.EliteBasicTroop : settlement.Culture.BasicTroop, 1);
                    settlersArriving++;
                }
            }

            if (settlersArriving > 0)
                Utils.PrintLocalizedMessage("homestead_settlers_have_arrived_to_stay", "{NUMBER_OF_SETTLERS} settlers have arrived to stay at your homestead of {HOMESTEAD_NAME}!", 0, 201, 0,
                    ("NUMBER_OF_SETTLERS", settlersArriving.ToString()), ("HOMESTEAD_NAME", name));
        }

        private void DailyTickProduceItems() {
            if (homesteadScene == null)
                return;

            // try/catch for V2.3
            try {
                foreach (HomesteadScenePlaceableProducedItem produceItem in homesteadScene.ProduceItems) {
                    float randNumForChance = MBRandom.RandomFloat;
                    if (randNumForChance > produceItem.DailyChance)
                        continue;

                    string[] itemIDs = produceItem.ItemProducedID.Split('|');
                    string itemToProduceID = itemIDs.GetRandomElementInefficiently();

                    ItemObject? itemToProduce = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemToProduceID);
                    if (itemToProduce == null)
                        continue;

                    if (produceItem.RequiredItemsToProduce.Count > 0 && !Utils.DoesItemRosterHaveItems(Stash, produceItem.RequiredItemsToProduce, true))
                        continue;

                    Stash.AddToCounts(itemToProduce, produceItem.AmountToProduce);
                }
            }
            catch (NullReferenceException) {
                if (homesteadScene.ProduceItems == null)
                    homesteadScene.ProduceItems = new();
            }
        }

        private void DailyTickChangeGoldStored() {
            float tierMult = Tier > 0 ? Tier : 0.5f;

            // base wage to keep homestead operational
            int amountOfChange = -(int)Math.Round(50f * tierMult);

            if (homesteadScene != null) {
                // productivity bonus
                int productivityRandomNum = MBRandom.RandomInt(7, 15);
                amountOfChange += (int)Math.Round(productivityRandomNum * tierMult * homesteadScene.TotalProductivity);
                // leisure debuff
                int leisureRandomNum = MBRandom.RandomInt(7, 15);
                amountOfChange -= (int)Math.Round(leisureRandomNum * tierMult * homesteadScene.TotalLeisure);
            }

            // Change gold stored
            GoldStored += amountOfChange;
            if (GoldStored < 0)
                GoldStored = 0;
        }

        private void DailyTickMoraleChange() {
            if (homesteadScene == null)
                return;

            int productivityOverLeisure = homesteadScene.TotalProductivity - homesteadScene.TotalLeisure;
            float amountToChange = (productivityOverLeisure / 10f);

            MobileParty.RecentEventsMorale -= amountToChange;
            if (MobileParty.Morale <= 30f) {
                TextObject text = new TextObject("{=homestead_low_morale_warning}Your homestead of {HOMESTEAD_NAME} has low morale!");
                text.SetTextVariable("HOMESTEAD_NAME", name);
                MBInformationManager.AddQuickInformation(text, 0, leader.CharacterObject);
            }
        }

        private void DailyTickNoGoldPenalty() {
            if (GoldStored != 0)
                return;

            MobileParty.RecentEventsMorale -= 5;
            TextObject text = new TextObject("{=homestead_no_gold_stored_warning}Your homestead of {HOMESTEAD_NAME} is lacking gold!");
            text.SetTextVariable("HOMESTEAD_NAME", name);
            MBInformationManager.AddQuickInformation(text, 0, leader.CharacterObject);
        }

        private void DailyTickTierProgress() {
            if (Tier >= 3 || leader == null)
                return;

            float leaderSkillMult = (leader.GetSkillValue(DefaultSkills.Steward)/2 + leader.GetSkillValue(DefaultSkills.Engineering)/2) / 100f;
            float tierProgressChange = 0.001f / (Tier + 1) * Troops.TotalRegulars * leaderSkillMult;
            TierProgress += tierProgressChange;
            leader.AddSkillXp(DefaultSkills.Steward, Tier * 10);

            if (TierProgress >= 1f) {
                Tier += 1;
                TierProgress = 0f;
            }
        }

        private TextObject BuildInformationTextObject() {
            TextObject homesteadTitleText = new TextObject("{=homestead_menu_info_title}Homestead of ");
            TextObject tierText = new TextObject(Tier < 3 ? "{=homestead_menu_info_tier}Tier: {TIER_LEVEL}\n({TIER_PROGRESS_PERCENT}% to next tier!)" : "{=homestead_menu_info_tier_max}Tier: {TIER_LEVEL}");
            tierText.SetTextVariable("TIER_LEVEL", Tier);
            tierText.SetTextVariable("TIER_PROGRESS_PERCENT", (float)Decimal.Round((decimal)TierProgress * 100.0m, 1));

            TextObject goldStoredText = new TextObject("{=homestead_menu_info_gold_stored}Gold Stored: {GOLD_STORED}");
            goldStoredText.SetTextVariable("GOLD_STORED", GoldStored);
            TextObject totalProductivityText = new TextObject("{=homestead_menu_info_productivity}Total Productivity: {TOTAL_PRODUCTIVITY}");
            totalProductivityText.SetTextVariable("TOTAL_PRODUCTIVITY", homesteadScene == null ? 0 : homesteadScene.TotalProductivity);
            TextObject extraSpaceText = new TextObject("{=homestead_menu_info_space}Extra Space: {EXTRA_SPACE}");
            extraSpaceText.SetTextVariable("EXTRA_SPACE", homesteadScene == null ? 0 : homesteadScene.TotalSpace);
            TextObject totalLeisureText = new TextObject("{=homestead_menu_info_leisure}Total Leisure: {TOTAL_LEISURE}");
            totalLeisureText.SetTextVariable("TOTAL_LEISURE", homesteadScene == null ? 0 : homesteadScene.TotalLeisure);
            TextObject moraleText = new TextObject("{=homestead_menu_info_morale}Total Morale: {TOTAL_MORALE}" + "\n" + MobileParty.MoraleExplained.GetExplanations());
            moraleText.SetTextVariable("TOTAL_MORALE", MobileParty.Morale);

            return new TextObject(homesteadTitleText.ToString() + Name.ToString() + " - " + tierText.ToString() + "\n" +
            "--------------------------------------------\n" +
            goldStoredText.ToString() + "\n" +
            totalProductivityText.ToString() + "\n" +
            extraSpaceText.ToString() + "\n" +
            totalLeisureText.ToString() + "\n-------------------------\n" +
            moraleText.ToString());
        }

        public static Homestead? GetFor(MobileParty mobileParty) {
            if (mobileParty == null || HomesteadBehavior.Instance == null || !HomesteadBehavior.Instance.HomesteadMobileParties.ContainsKey(mobileParty))
                return null;
            return HomesteadBehavior.Instance.HomesteadMobileParties[mobileParty];
        }

        public static void SetGameTextsForMenus() {
            if (HomesteadBehavior.Instance == null || HomesteadBehavior.Instance.CurrentHomestead == null)
                return;
            GameTexts.SetVariable("CURRENT_HOMESTEAD_INFORMATION", HomesteadBehavior.Instance.CurrentHomestead.HomesteadInformation);
            GameTexts.SetVariable("CURRENT_HOMESTEAD_NAME", HomesteadBehavior.Instance.CurrentHomestead.Name);
            //Utils.PrintDebugMessage(HomesteadBehavior.Instance.CurrentHomestead.Leader.ToString());
            GameTexts.SetVariable("CURRENT_HOMESTEAD_LEADER_NAME", HomesteadBehavior.Instance.CurrentHomestead.Leader == null ? "" : HomesteadBehavior.Instance.CurrentHomestead.Leader.Name.ToString());
        }
    }
}
