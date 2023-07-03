using Homesteads.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Homesteads {
    public static class Utils {
		public static ItemObject? GetItemFromID(string itemID) {
			ItemObject? item = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemID);
			return item;
        }

		public static bool DoesItemRosterHaveItems(ItemRoster itemRoster, Dictionary<string, int> itemsRequired, bool takeItems = false) {
			if (itemsRequired.Count == 0)
				return true;

			Dictionary<ItemObject, int> itemsToTake = new();
			foreach (KeyValuePair<string, int> pair in itemsRequired) {
				string[] itemIDs = pair.Key.Split('|');

				ItemObject? item = null;
				ItemRosterElement itemInItemRoster = ItemRosterElement.Invalid;
				foreach (string itemID in itemIDs) {
					// if the item exists
					ItemObject? thisItem = Campaign.Current.ObjectManager.GetObject<ItemObject>(itemID);
					if (thisItem == null) {
						Utils.PrintDebugMessage(pair.Key + " IS NOT A VALID ITEM ID", 255, 0, 0);
						continue;
					}
					// if the item is in the homestead stash
					try {
						itemInItemRoster = itemRoster.First(x => x.EquipmentElement.Item == thisItem);
						// check for amount
						if (itemInItemRoster.Amount < pair.Value) {
							itemInItemRoster = ItemRosterElement.Invalid;
							continue;
                        }
						// set the item to this item and break loop
						item = thisItem;
						break;
					} catch (InvalidOperationException) {
						continue;
					}
				}

				if (item == null)
					return false;

				itemsToTake[item] = pair.Value;
			}

			if (takeItems)
				foreach (KeyValuePair<ItemObject, int> pair in itemsToTake)
					itemRoster.AddToCounts(pair.Key, -pair.Value);

			return true;
		}

		public static GameEntity CreateGameEntityWithPrefab(string prefabName, Vec3 position, Mat3 rotation) {
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation = rotation;

			GameEntity entity = GameEntity.Instantiate(Mission.Current.Scene, prefabName, frame);
			entity.SetLocalPosition(position);

			return entity;
        }

		public static void ShowNameHomesteadScreen(Homestead homestead, Action? doneAction = null) {
			string title = GetLocalizedString("{=homestead_rename_title}Name Homestead");
			string text = GetLocalizedString("{=homestead_rename_text}What would you like to name this homestead?");
			ShowTextInputMessage(title, text, (name) => {
				homestead.ChangeName(name);

				if (doneAction != null)
					doneAction();
			});
		}

		public static void ShowSelectNewHomesteadLeaderScreen(Homestead homestead, bool fromHomesteadMenu = false) {
			TextObject titleText = new TextObject("{=homestead_choose_new_leader}CHOOSE NEW HOMESTEAD LEADER");
			Utils.ShowHeroSelectionScreen(titleText.ToString(), "", Campaign.Current.AliveHeroes.Where(x => x.PartyBelongedTo != null && x.PartyBelongedTo == MobileParty.MainParty && !x.IsHumanPlayerCharacter).ToList(), (elements) => {
				homestead.ChangePartyLeader(elements[0].Identifier as Hero);
				if (fromHomesteadMenu)
					GameMenu.SwitchToMenu("homestead_menu_main");
			});
		}
		public static void ShowHeroSelectionScreen(string title, string text, List<Hero> heroes, Action<List<InquiryElement>> onPressedOk) {
			List<InquiryElement> elements = new();
			foreach (Hero hero in heroes) {
				InquiryElement element = new InquiryElement(hero, hero.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(hero.CharacterObject)), true, GetHeroPropertiesHint(hero));
				elements.Add(element);
			}
			MultiSelectionInquiryData inquiry = new MultiSelectionInquiryData(title, text, elements, true, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), onPressedOk, null);
			MBInformationManager.ShowMultiSelectionInquiry(inquiry, true, true);
		}

		public static void ShowMessageBox(string title, string text, bool pauseGameActiveState = true, bool priority = true) {
			InquiryData inquiry = new InquiryData(title, text, true, false, GameTexts.FindText("str_done").ToString(), null, null, null);
			InformationManager.ShowInquiry(inquiry, pauseGameActiveState, priority);
        }

		public static void ShowTextInputMessage(string title, string text, Action<string> onPressedOk) {
			TextInquiryData inquiry = new TextInquiryData(title, text, true, false, GameTexts.FindText("str_done").ToString(), null, onPressedOk, null);
			InformationManager.ShowTextInquiry(inquiry, true, true);
		}

		public static string GetLocalizedString(string str, params (string, string)[] textVars) {
			TextObject textObject = new TextObject(str);
			foreach ((string, string) value in textVars)
				textObject.SetTextVariable(value.Item1, value.Item2);
			return textObject.ToString();
		}
		public static void PrintLocalizedMessage(string localizationString, string str, float r = 255, float g = 255, float b = 255, params (string, string)[] textVars) {
			float[] newValues = { r / 255.0f, g / 255.0f, b / 255.0f };
			Color col = new(newValues[0], newValues[1], newValues[2]);
			InformationManager.DisplayMessage(new InformationMessage(GetLocalizedString("{=" + localizationString + "}" + str, textVars), col));
		}

		public static void PrintDebugMessage(string str, float r = 255, float g = 255, float b = 255) {
			float normR = r / 255;
			float normG = g / 255;
			float normB = b / 255;
			InformationManager.DisplayMessage(new InformationMessage(str, new Color(normR, normG, normB)));
		}

		private static string GetHeroPropertiesHint(Hero hero) {
			GameTexts.SetVariable("newline", "\n");
			string content = hero.Name.ToString();
			TextObject textObject = GameTexts.FindText("str_STR1_space_STR2", null);
			textObject.SetTextVariable("STR1", GameTexts.FindText("str_enc_sf_age", null).ToString());
			textObject.SetTextVariable("STR2", ((int)hero.Age).ToString());
			string content2 = GameTexts.FindText("str_attributes", null).ToString();
			foreach (CharacterAttribute characterAttribute in Attributes.All) {
				GameTexts.SetVariable("LEFT", characterAttribute.Name.ToString());
				GameTexts.SetVariable("RIGHT", hero.GetAttributeValue(characterAttribute));
				string content3 = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
				GameTexts.SetVariable("STR1", content2);
				GameTexts.SetVariable("STR2", content3);
				content2 = GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			int num = 0;
			string content4 = GameTexts.FindText("str_skills", null).ToString();
			foreach (SkillObject skillObject in Skills.All) {
				int skillValue = hero.GetSkillValue(skillObject);
				if (skillValue > 50) {
					GameTexts.SetVariable("LEFT", skillObject.Name.ToString());
					GameTexts.SetVariable("RIGHT", skillValue);
					string content5 = GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon", null).ToString();
					GameTexts.SetVariable("STR1", content4);
					GameTexts.SetVariable("STR2", content5);
					content4 = GameTexts.FindText("str_string_newline_string", null).ToString();
					num++;
				}
			}
			GameTexts.SetVariable("STR1", content);
			GameTexts.SetVariable("STR2", textObject.ToString());
			string text = GameTexts.FindText("str_string_newline_string", null).ToString();
			GameTexts.SetVariable("newline", "\n \n");
			GameTexts.SetVariable("STR1", text);
			GameTexts.SetVariable("STR2", content2);
			text = GameTexts.FindText("str_string_newline_string", null).ToString();
			if (num > 0) {
				GameTexts.SetVariable("STR1", text);
				GameTexts.SetVariable("STR2", content4);
				text = GameTexts.FindText("str_string_newline_string", null).ToString();
			}
			GameTexts.SetVariable("newline", "\n");
			return text;
		}
	}
}
