using System.Collections.Generic;
using System.Linq;
using ReadTextMod;
using HarmonyLib;
using FFG.Common;
using UnityEngine;
public static class EncounterHelpers{

        
        public static List<string>KeyInfoResolver(MessagePopup MessagePopupObject){
            
            List<string> filepaths = [];
            UILocalizationPacket localizationText = Traverse.Create(MessagePopupObject).Field("_localizedText").GetValue<UILocalizationPacket>();
            
            switch (MessagePopupObject.name)
            {
                case "MessagePopup_New":
                    if (localizationText.KeyInfo.Key == "UI_LAST_STAND_HERO_CONFIRMATION")
                    {
                        if (localizationText.key == "UI_CHOOSE_LAST_STAND")
                        {
                            filepaths.Add(localizationText.key);
                        }
                        else
                        {
                            filepaths = ValueCleaner(localizationText)
                                .Where(text => text.StartsWith("UI_LAST_STAND") || text.StartsWith("HERO_")).ToList();
                        }
                    }
                    else if (localizationText.key == "UI_CHOOSE_LAST_STAND")
                    {
                        filepaths.Add(localizationText.KeyInfo.Key);
                    }
                    else if (new[] { "UI_LAST_STAND_PASSED_PHYSICAL", "UI_LAST_STAND_PASSED_FEAR", "UI_LAST_STAND_FAILED" }.Contains(localizationText.key))
                    {
                        filepaths.Add(localizationText.key);
                    }
                    break;
                case "MessagePopup_EnemyActivation":
                    filepaths.Add(localizationText.key);
                    GameObject additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
                    if (additionalInfo != null)
                    {
                        filepaths.Add(localizationText.key.Replace("ATTACK", "ADDITIONAL"));
                    }
                    break;

                case "MessagePopup":

                        if(!string.IsNullOrEmpty(localizationText.key)){
                            filepaths.Add(localizationText.key);
                        }else
                        {
                            var textPart = ValueCleaner(localizationText);

                            switch (localizationText.KeyInfo.Key)
                            {   
                                case "UI_EXPLORE_TILE_WITH_INTRO_FORMATTED":
                                    filepaths = textPart.OrderByDescending(text => text.StartsWith("TILE_")).ToList();
                                    filepaths.Remove(localizationText.KeyInfo.Key);
                                    RemoveBracket(filepaths);
                                    break;

                                case "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED":
                                case "PLACE_TILE_NO_FLAVOR":
                                    var prefix = localizationText.KeyInfo.Key == "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED" ? "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED" : "PLACE_TILE_NO_FLAVOR";
                                    var temp = textPart.ToList();
                                    temp.Remove(localizationText.KeyInfo.Key);
                                    RemoveBracket(temp);
                                    filepaths.AddRange(new[] { $"{prefix}_1" }.Concat(temp).Concat(new[] { $"{prefix}_2" }));
                                    break;

                                case "PLACE_SEARCH":
                                case "PLACE_THREAT":
                                case "PLACE_PERSON":
                                    filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                                    RemoveBracket(filepaths);
                                    break;

                                case "UI_AWARD_ITEM_FORMATTED":
                                    filepaths.Add("UI_AWARD_ITEM_FORMATTED_1");
                                    filepaths.AddRange(textPart.Where(text => text.StartsWith("ITEM_")));
                                    filepaths.Add("UI_AWARD_ITEM_FORMATTED_2");
                                    break;
                                default:
                                    filepaths.Add(localizationText.KeyInfo.Key);
                                    break;
                            }
                            
                        }
                            break;
                    default:
                        break;

            }
            if(MessagePopupObject.name == "MessagePopup"){
                localizationText.key = "";
            }
            
            return filepaths;
            
        }

        private static IEnumerable<string> ValueCleaner(UILocalizationPacket localizationPacket){
            if(localizationPacket != null)
            {
                var textPart = localizationPacket.KeyInfo.CompressedValue.Trim('[', ']').Split('|').Where(p => !int.TryParse(p, out _)).Select(p => p.Trim());
                return textPart;
            }else
            {
                ReadText.Log.LogInfo("Compressed value does not exists or is mismatched");
                return null;
            }
            
        }
        private static void RemoveBracket(List<string> strings){
            if(strings.Contains("0]"))
            {
                strings.Remove("0]");
            }
        }

    }

