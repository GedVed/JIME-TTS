using System.Collections.Generic;
using System.Linq;
using ReadTextMod;
using HarmonyLib;
using FFG.Common;
using UnityEngine;
using FFG.JIME;
using System;


public static class EncounterHelpers{

        
    public static List<string>KeyInfoResolver(MessagePopup MessagePopupObject, LocalizationPacket packet){
        
        
        List<string> filepaths = [];
        UILocalizationPacket localizationText = Traverse.Create(MessagePopupObject).Field("_localizedText").GetValue<UILocalizationPacket>();

        switch (MessagePopupObject.name)
        {
            case "MessagePopup_New":
                if (packet.Key == "UI_LAST_STAND_HERO_CONFIRMATION")
                {
                    filepaths = ValueCleaner(localizationText)
                    .Where(text => text.StartsWith("UI_LAST_STAND") || text.StartsWith("HERO_")).ToList();
                }
                else
                {
                    filepaths.Add(packet.Key);
                }
                break;
                
                case "MessagePopup_EnemyActivation":


                    if (ReadText.enemyActivations.Contains(packet.Key))
                    {
                        filepaths.Add(packet.Key);
                        if (localizationText.KeyInfo.UniqueArgCount > 0)
                        {
                            var heroAttacked = localizationText?.KeyInfo?.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList();

                            Component[] components = GameObject.Find("PersistentGameObject").GetComponents(typeof(Component));
                            GameData data = (GameData)components[4]; //Magic number but Persistent game object will always exist in this contex
                            Hero[] heroes = Traverse.Create(data).Field("_heroes").GetValue<Hero[]>();
                            var temp = heroes[heroAttacked[0]].Model.NameKey;

                            filepaths.Insert(1, temp);
                        }
                        filepaths.Add("ENEMY_ACTIVATION_ATTACK");
                    }

                    /*
                    if (string.IsNullOrEmpty(localizationText.key) || !string.IsNullOrEmpty(localizationText.key) && !string.IsNullOrEmpty(localizationText.KeyInfo.Key))
                    {
                        if (localizationText.KeyInfo.UniqueArgCount > 0)
                        {
                            var textPart2 = ValueCleaner(localizationText);
                            filepaths = AudioQueueCorrectOrder(localizationText, textPart2, filepaths);
                            var heroAttacked = localizationText?.KeyInfo?.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList();

                            Component[] components = GameObject.Find("PersistentGameObject").GetComponents(typeof(Component));
                            GameData data = (GameData)components[4];
                            Hero[] heroes = Traverse.Create(data).Field("_heroes").GetValue<Hero[]>();
                            var temp = heroes[heroAttacked[0]].Model.NameKey;

                            filepaths.Insert(1, temp);
                        }
                        else
                        {
                            filepaths.Add(localizationText.KeyInfo.Key);
                        }

                    }
                    else
                    {
                        filepaths.Add(localizationText.key);
                        GameObject additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
                        if (additionalInfo != null)
                        {
                            filepaths.Add(localizationText.key.Replace("ATTACK", "ADDITIONAL"));
                        }
                    }*/
                    
                    break;

                case "MessagePopup":

                    var textPart = ValueCleaner(localizationText);

                        switch (packet.Key)
                        {   
                            case "UI_EXPLORE_TILE_WITH_INTRO_FORMATTED":
                            
                                filepaths = textPart.OrderByDescending(text => text.StartsWith("TILE_")).ToList();
                                filepaths.Remove(localizationText.KeyInfo.Key);
                                RemoveBracket(filepaths);
                                break;

                            case "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED":
                            case "PLACE_TILE_NO_FLAVOR":

                                filepaths = AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                                break;

                            case "PLACE_SEARCH":
                            case "PLACE_THREAT":
                            case "PLACE_PERSON":

                                filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                                RemoveBracket(filepaths);
                                break;

                            case "UI_AWARD_ITEM_FORMATTED":
        
                                filepaths = AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                                break;

                            case "UI_ENEMY_REMOVAL_REMINDER_FORMATTED":
            
                                filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                                filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                                RemoveBracket(filepaths);
                                filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                                break;


                            case "UI_SPAWN_GROUP_FORMAT":

                                filepaths = AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                                filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                                RemoveBracket(filepaths);
                                filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                                break;

                            case "UI_THREAT_INCREASE":
                                filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                                RemoveBracket(filepaths);
                                if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(0) is { IsUsed: true } firstInsert)
                                {
                                    filepaths.Add(firstInsert.RawText);
                                }
                                break;

                            case "A1_M1_E1_ENEMIES":

                                filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                                RemoveBracket(filepaths);
                                break;

                            default:
                                if(textPart.Contains("OBJECTIVE"))
                                {
                                    filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                                    RemoveBracket(filepaths);
                                    if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(1) is { IsUsed: true } secondInsert)
                                    {
                                        filepaths.Add(secondInsert.CompressedIntData.ToString());
                                    }
                                }
                                else
                                {
                                    filepaths.Add(packet.Key);
                                }
                                break;
                        }
                            break;
                    default:
                        ReadText.Log.LogError("MessagePopupObject does not exists or is invalid");
                        break;
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
    private static IEnumerable<string> FindEnemyGroup(List<string> filepaths, UILocalizationPacket localizationText)
    {
        var enemyGroup = localizationText?.KeyInfo?.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert switch
        {
            { RawText: not null } when !string.IsNullOrEmpty(insert.RawText) => insert.RawText,
            { EnemyGroup.Model: not null } when filepaths.Count > 1 && int.TryParse(filepaths[1], out int count) && count > 1 => insert.EnemyGroup.Model.KeyPlural,
            { EnemyGroup.Model: not null } => insert.EnemyGroup.Model.KeySingular,
            _ => null
        }).Where(path => path != null);
        if (enemyGroup.ToList().Count >= 1)
        {
            return enemyGroup;
        }
        else
        {
            return null;
        }                          
    }  
    private static List<string> AudioQueueCorrectOrder(UILocalizationPacket localizationText, IEnumerable<string> textPart, List<string> filepaths){

        var prefix = localizationText.KeyInfo.Key;
        var temp = textPart.ToList();
        temp.Remove(localizationText.KeyInfo.Key);
        RemoveBracket(temp);
        filepaths.AddRange(new[] { $"{prefix}_1" }.Concat(temp).Concat(new[] { $"{prefix}_2" }));
        return filepaths;
    }
}

