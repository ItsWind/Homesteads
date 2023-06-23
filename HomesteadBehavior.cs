﻿using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Actions;
using Homesteads.Models;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Inventory;
using System.Linq;

namespace Homesteads {
    public class HomesteadBehavior : CampaignBehaviorBase {
        public static HomesteadBehavior Instance;

        private Homestead? currentHomestead = null;
        public Homestead? CurrentHomestead {
            get => currentHomestead;
            set {
                currentHomestead = value;
                Homestead.SetGameTextsForMenus();
            }
        }
        public Dictionary<MobileParty, Homestead> HomesteadMobileParties = new();

        public HomesteadBehavior() {
            Instance = this;
        }

        public override void RegisterEvents() {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddGameMenusAndDialogs);
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, CheckHomesteadHourlyTick);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, CheckHomesteadDailyTick);

            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, (args) => {
                string gameMenuStringId = args.MenuContext.GameMenu.StringId;

                // Check if homestead menu needs to be closed after packing it up in dialog
                if (gameMenuStringId == "homestead_menu_main" && CurrentHomestead == null) {
                    PlayerEncounter.Finish();
                    return;
                }
            });
        }

        public override void SyncData(IDataStore dataStore) {
            dataStore.SyncData("CurrentHomestead", ref currentHomestead);
            dataStore.SyncData("HomesteadMobileParties", ref HomesteadMobileParties);
        }

        private void AddGameMenusAndDialogs(CampaignGameStarter starter) {
            AddDialogs(starter);
            AddGameMenus(starter);
        }

        private void AddDialogs(CampaignGameStarter starter) {
            // Setup new homestead
            TextObject homestead_setup_new_dialog = new TextObject("{=homestead_setup_new_dialog}We should set up a homestead here.");
            starter.AddPlayerLine("homestead_setup_new", "hero_main_options", "homestead_setup_options", homestead_setup_new_dialog.ToString(), () => {
                return Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty && Hero.MainHero.CurrentSettlement == null;
            }, null, 500);
            starter.AddDialogLine("homestead_setup_accept", "homestead_setup_options", "close_window", "okie dokie", () => {
                return true;
            }, () => {
                CreateNewHomesteadAtPlayerLocation(Hero.OneToOneConversationHero);
            }, 500);

            // Pack up and remove homestead
            TextObject homestead_teardown_homestead_dialog = new TextObject("{=homestead_teardown_homestead_dialog}Let's pack up the homestead and leave.");
            starter.AddPlayerLine("homestead_teardown_homestead", "hero_main_options", "homestead_teardown_approval", homestead_teardown_homestead_dialog.ToString(), () => {
                return CurrentHomestead != null && CurrentHomestead.Leader == Hero.OneToOneConversationHero;
            }, null, 500);
            starter.AddDialogLine("homestead_teardown_accept", "homestead_teardown_approval", "close_window", "okie dokie", () => {
                return true;
            }, () => {
                AddHeroToPartyAction.Apply(CurrentHomestead.Leader, MobileParty.MainParty, true);
                MergePartiesAction.Apply(PartyBase.MainParty, CurrentHomestead.Party);
                CurrentHomestead = null;
            }, 500);

            // Change homestead leader
            TextObject homestead_change_leader_homestead_dialog = new TextObject("{=homestead_change_leader_homestead_dialog}I'd like someone else to lead this homestead.");
            starter.AddPlayerLine("homestead_change_leader_homestead", "hero_main_options", "homestead_change_leader_approval", homestead_change_leader_homestead_dialog.ToString(), () => {
                return CurrentHomestead != null && CurrentHomestead.Leader == Hero.OneToOneConversationHero && MobileParty.MainParty.MemberRoster.TotalHeroes > 1;
            }, null, 499);
            starter.AddDialogLine("homestead_change_leader_accept", "homestead_change_leader_approval", "close_window", "okie dokie", () => {
                return true;
            }, () => {
                TextObject titleText = new TextObject("{=homestead_choose_new_leader}CHOOSE NEW HOMESTEAD LEADER");
                Utils.ShowHeroSelectionScreen(titleText.ToString(), "", Campaign.Current.AliveHeroes.Where(x => x.PartyBelongedTo != null && x.PartyBelongedTo == MobileParty.MainParty && !x.IsHumanPlayerCharacter).ToList(), (elements) => {
                    CurrentHomestead.ChangePartyLeader(elements[0].Identifier as Hero);
                });
            }, 500);
        }

        private void AddGameMenus(CampaignGameStarter starter) {
            Homestead.SetGameTextsForMenus();

            starter.AddGameMenu("homestead_menu_main", "You arrive at your homestead of {CURRENT_HOMESTEAD_NAME}. What would you like to do?", null);

            // Talk to leader
            starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_talk_leader", "Talk to {CURRENT_HOMESTEAD_LEADER_NAME}", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                return true;
            }, (args) => {
                ConversationCharacterData playerCharData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
                ConversationCharacterData leaderCharData = new ConversationCharacterData(CurrentHomestead.Leader.CharacterObject, CurrentHomestead.Party);
                CampaignMission.OpenConversationMission(playerCharData, leaderCharData);
            });

            // Walk around
            starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_walk_around", "Walk around", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                return true;
            }, (args) => {
                CustomMissions.StartHomesteadMission(CurrentHomestead);
            });

            // - Manage homestead
            starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_goto_manage", "Manage homestead", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }, (args) => {
                Homestead.SetGameTextsForMenus();
                GameMenu.SwitchToMenu("homestead_menu_manage_main");
            });
            starter.AddGameMenu("homestead_menu_manage_main", "{CURRENT_HOMESTEAD_INFORMATION}", null);

            // -> Rename homestead
            starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_rename", "Rename homestead", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leaderboard;
                return true;
            }, (args) => {
                Utils.ShowTextInputMessage("Name Homestead", "What would you like to name this homestead?", (name) => {
                    CurrentHomestead.ChangeName(name);
                    GameMenu.SwitchToMenu("homestead_menu_manage_main");
                });
            });

            // -> Manage garrison
            starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_garrison", "Manage garrison", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
                return true;
            }, (args) => {
                PartyScreenManager.OpenScreenAsManageTroopsAndPrisoners(CurrentHomestead.MobileParty);
            });

            // -> Manage items
            starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_stash", "Manage food/stash", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.OpenStash;
                return true;
            }, (args) => {
                InventoryManager.OpenScreenAsStash(CurrentHomestead.MobileParty.ItemRoster);
            });

            // -> Deposit/withdraw gold
            starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_gold", "Deposit/withdraw gold", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
                return true;
            }, (args) => {
                Utils.ShowTextInputMessage("Deposit/Withdraw Gold", "Enter an amount to deposit or withdraw. A negative number means you will withdraw.", (text) => {
                    int amount = 0;
                    Int32.TryParse(text, out amount);
                    if (amount == 0) {
                        Utils.PrintDebugMessage("Amount entered must be a valid number.");
                        return;
                    }
                    string failReason;
                    if (!CurrentHomestead.PlayerChangeGoldStored(amount, out failReason))
                        Utils.PrintDebugMessage(failReason);
                    else
                        GameMenu.SwitchToMenu("homestead_menu_manage_main");
                });
            });

            // <- Manage back to main
            starter.AddGameMenuOption("homestead_menu_manage_main", "homestead_menu_manage_back", "Back", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (args) => {
                GameMenu.SwitchToMenu("homestead_menu_main");
            }, true);

            // Wait
            starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_wait", "Wait here", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                return true;
            }, (args) => {
                GameMenu.SwitchToMenu("homestead_menu_wait_waiting");
            });
            starter.AddWaitGameMenu("homestead_menu_wait_waiting", "Waiting...", (args) => {
                PlayerEncounter.Current.IsPlayerWaiting = true;
            }, (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                return true;
            }, null, null, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);
            starter.AddGameMenuOption("homestead_menu_wait_waiting", "homestead_menu_wait_leave", "Stop waiting", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (args) => {
                PlayerEncounter.Current.IsPlayerWaiting = false;
                GameMenu.SwitchToMenu("homestead_menu_main");
            }, true);

            // Leave
            starter.AddGameMenuOption("homestead_menu_main", "homestead_menu_leave", "Leave", (args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (args) => {
                PlayerEncounter.Finish();
                CurrentHomestead = null;
            }, true);
        }

        private void CheckHomesteadHourlyTick(MobileParty party, PartyThinkParams thinkParams) {
            Homestead? homestead = Homestead.GetFor(party);
            if (homestead != null)
                homestead.HourlyTick();
        }

        private void CheckHomesteadDailyTick(MobileParty party) {
            Homestead? homestead = Homestead.GetFor(party);
            if (homestead != null)
                homestead.DailyTick();
        }

        private void CreateNewHomesteadAtPlayerLocation(Hero leaderHero) {
            Homestead component = new Homestead(leaderHero);
            MobileParty homesteadParty = MobileParty.CreateParty("homestead_" + leaderHero.StringId, component);
            homesteadParty.InitializeMobilePartyAroundPosition(new TroopRoster(homesteadParty.Party), new TroopRoster(homesteadParty.Party), MobileParty.MainParty.Position2D, 1f);

            Utils.ShowTextInputMessage("Name Homestead", "What would you like to name this homestead?", (name) => {
                component.ChangeName(name);
            });

            AddHeroToPartyAction.Apply(leaderHero, homesteadParty);
            if (PartyScreenManager.PartyScreenLogic != null)
                PartyScreenManager.PartyScreenLogic.DoneLogic(true);

            homesteadParty.ActualClan = Hero.MainHero.Clan;
            homesteadParty.ShouldJoinPlayerBattles = true;
            homesteadParty.Party.Visuals.SetMapIconAsDirty();

            HomesteadMobileParties[homesteadParty] = component;
        }
    }

    public class CustomSaveDefiner : SaveableTypeDefiner {
        public CustomSaveDefiner() : base(321601531) { }

        protected override void DefineClassTypes() {
            AddClassDefinition(typeof(Homestead), 1);
            AddClassDefinition(typeof(HomesteadScene), 2);
            AddClassDefinition(typeof(HomesteadSceneSavedEntity), 3);
            AddClassDefinition(typeof(HomesteadScenePlaceable), 4);
        }

        protected override void DefineContainerDefinitions() {
            ConstructContainerDefinition(typeof(Dictionary<MobileParty, Homestead>));
            ConstructContainerDefinition(typeof(List<HomesteadSceneSavedEntity>));
        }
    }
}