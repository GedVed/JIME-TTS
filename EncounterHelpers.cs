using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Text.Json;



namespace ReadTextMod
{

    public static class EncounterHelpers{

        [System.Serializable]
        private class LocalizationTextData
        {
                public string LocalizationKey;
                public string LocalizationKeyInfoKey;
                public string CompressedValue;
        }

        private static void RemoveBracket(List<string> strings){
            if(strings.Contains("0]")){
                strings.Remove("0]");
            }
        }

        public static List<string>KeyInfoResolver(UILocalizationPacket localizationText, GameObject MessagePopupObject, GameObject AdditionalInfoAttack = null){
            ReadText.Log.LogInfo($"{ConvertLocalizationDataToBytes(localizationText)}");

            List<string> filepaths = [];

            /*
            switch (MessagePopupObject.name)
            {
                case "MessagePopup_New":

                    if(localizationText.KeyInfo.Key == "UI_LAST_STAND_HERO_CONFIRMATION" && string.IsNullOrEmpty(localizationText.key))
                    {
                        filepaths = ValueCleaner(localizationText).Where(text => text.StartsWith("UI_LAST_STAND") || text.StartsWith("HERO_")).ToList();
                    }else if(localizationText.KeyInfo.Key == "UI_LAST_STAND_HERO_CONFIRMATION" && !string.IsNullOrEmpty(localizationText.key))
                    {
                        filepaths.Add(localizationText.key);

                    }else if(localizationText.key == "UI_CHOOSE_LAST_STAND" && !string.IsNullOrEmpty(localizationText.KeyInfo.Key))
                    {   
                        
                    }
                    break;

                default:
                    break;

            }*/

            
            
            if(localizationText.key != "" && localizationText.KeyInfo.Key != "")
            {

                    
                switch (localizationText.KeyInfo.Key)
                {
                case "UI_EXPLORE_TILE_WITH_INTRO_FORMATTED":

                    localizationText.KeyInfo.Key = "";
                    break;
                case "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED":

                    localizationText.KeyInfo.Key = "";
                    break;
                default:

                    break;
                }
                    
            }
            

            
            if(!string.IsNullOrEmpty(localizationText.key))
            {
                filepaths.Add(localizationText.key);
                if (AdditionalInfoAttack != null)
                            filepaths.Add(localizationText.key.Replace("ATTACK", "ADDITIONAL"));
            }
            else
            {

                var textPart = localizationText.KeyInfo.CompressedValue.Trim('[', ']').Split('|').Where(p => !int.TryParse(p, out _)).Select(p => p.Trim());
                

                switch (localizationText.KeyInfo.Key)
                {
                    case "UI_EXPLORE_TILE_WITH_INTRO_FORMATTED":
                        //[-1|-1|UI_EXPLORE_TILE_WITH_INTRO_FORMATTED|2|8|0|TILE_201A_3|0|8|0|UI_DISCARD_EXPLORE_TOKEN_WITH_INSPIRATION|0]
                        filepaths = textPart.OrderByDescending(text => text.StartsWith("TILE_")).ToList();
                        filepaths.Remove(localizationText.KeyInfo.Key);
                        RemoveBracket(filepaths);
                        
                        break;

                    case "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED":
                            //[-1|-1|UI_SECTION_REVEAL_PLACE_TILE_FORMATTED|1|10|0|209A|0]
                            var temp_UI_SECTION = textPart.ToList();
                            temp_UI_SECTION.Remove(localizationText.KeyInfo.Key);
                            RemoveBracket(temp_UI_SECTION);
                            filepaths.AddRange(new[]
                            {
                                "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED_1"
                            }.Concat(temp_UI_SECTION).Concat(new[] {
                                "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED_2"
                            }));
                        break;

                    case "PLACE_TILE_NO_FLAVOR":
                        //[-1|-1|PLACE_TILE_NO_FLAVOR|1|8|0|402A|0]
                        var temp_TILE_NO = textPart.ToList();
                        temp_TILE_NO.Remove(localizationText.KeyInfo.Key);
                        RemoveBracket(temp_TILE_NO);
                        filepaths.AddRange(new[]
                            {
                                "PLACE_TILE_NO_FLAVOR_1"
                            }.Concat(temp_TILE_NO).Concat(new[] {
                                "PLACE_TILE_NO_FLAVOR_2"
                            }));
                        break;

                    case "PLACE_SEARCH":
                        //[-1|-1|PLACE_SEARCH|1|8|0|C_STORM_SEARCH|0]

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "PLACE_THREAT":
                        
                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "PLACE_PERSON":
                        //[-1|-1|PLACE_PERSON|1|8|0|A29_ELF_PLACE|0]
                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "UI_AWARD_ITEM_FORMATTED":

                        filepaths.Add("UI_AWARD_ITEM_FORMATTED_1");
                        filepaths.AddRange(textPart.Where(text => text.StartsWith("ITEM_"))); //Should only add 1 one item
                        
                        filepaths.Add("UI_AWARD_ITEM_FORMATTED_2");
                        break;
                
                    case "UI_LAST_STAND_HERO_CONFIRMATION":

                        filepaths = textPart.Where(text => text.StartsWith("UI_LAST_STAND") || text.StartsWith("HERO_")).ToList();
                        break;
                        
                    default:
                        
                        filepaths.Add(localizationText.KeyInfo.Key);

                        break;
                }
            }
                
                return filepaths;
        }

        private static IEnumerable<string> ValueCleaner(UILocalizationPacket localizationPacket){
            if(localizationPacket != null){
                var textPart = localizationPacket.KeyInfo.CompressedValue.Trim('[', ']').Split('|').Where(p => !int.TryParse(p, out _)).Select(p => p.Trim());
                return textPart;
            }else{
                ReadText.Log.LogInfo("Compressed value does not exists or is mismatched");
                return null;
            }
            
        }

        public static string ConvertLocalizationDataToBytes(UILocalizationPacket localizationPacket)
        {
            if (localizationPacket == null)
            {
                ReadText.Log.LogError("LocalizationPacket is null!");
                return null;
            }

            LocalizationTextData localizationData = new LocalizationTextData
            {
                LocalizationKey = localizationPacket.key,
                LocalizationKeyInfoKey = localizationPacket.KeyInfo.Key,
                CompressedValue = localizationPacket.KeyInfo.CompressedValue
            };
            string serialized = JsonSerializer.Serialize(localizationData);
           // return Encoding.UTF8.GetBytes(serialized);
            return serialized;
            
        }
    }
}
