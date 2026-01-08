using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using FFG.Common;
using UnityEngine;
using FFG.JIME;
using System.Text.RegularExpressions;
using System;
using JIME_TTS_MOD;
using System.IO;


public static class EncounterHelpers
{

    private static readonly List<string> EnemyActivations = new List<string>{"ENEMY_GOBLIN_ACTIVATION","ENEMY_RUFFIAN_ACTIVATION","ENEMY_ORC_MARAUDER_ACTIVATION","ENEMY_ORC_HUNTER_ACTIVATION",
            "ENEMY_HUNGRY_WARG_ACTIVATION","ENEMY_WIGHT_ACTIVATION","ENEMY_HILL_TROLL_ACTIVATION","ENEMY_ATARIN_ACTIVATION","ENEMY_ULUK_ACTIVATION","ENEMY_GULGOTAR_ACTIVATION",
            "ENEMY_GIANT_SPIDER_ACTIVATION","ENEMY_PIT_GOBLIN_ACTIVATION","ENEMY_ORC_TASKMASTER_ACTIVATION","ENEMY_SHADOWMAN_ACTIVATION","ENEMY_NAMELESS_THING_ACTIVATION",
            "ENEMY_CAVE_TROLL_ACTIVATION","ENEMY_UNGOLIANT_ACTIVATION","ENEMY_BALROG_ACTIVATION","ENEMY_SOLDIER_ACTIVATION","ENEMY_URUK_ACTIVATION","ENEMY_FELL_BEAST_ACTIVATION",
            "ENEMY_WARG_RIDER_ACTIVATION","ENEMY_SIEGE_ENGINE_ACTIVATION","ENEMY_OLIPHAUNT_ACTIVATION", "A59_GIRANDAR_ACTIVATION", "ENEMY_URSA_ACTIVATION"};

    private static readonly Dictionary<string, string> AdditionalEffectUnique = new Dictionary<string, string>
    {
        {"UI_ENEMY_ATTACK_ADDITIONAL_EFFECT","Po tym ataku każdy bohater, który nie przyjął karty obrażeń ani strachu, otrzymuje 1 żeton natchnienia"},
        {"UI_ENEMY_ATTACK_ADDITIONAL_EFFECT_1","Przed tym atakiem każdy bohater odrzuca 2 karty z wierzchu swojej talii."},
        {"UI_ENEMY_ATTACK_ADDITIONAL_EFFECT_2","Po tym ataku każdy bohater odrzuca 1 żeton natchnienia."},

    };

    public static List<string> KeyInfoResolverDialog( UILocalizationPacket packet)
    {

        List<string> filepaths = [];

        if(packet.key != null)
        {
            filepaths.Add(packet.key);
            if (packet?.KeyInfo?.UniqueArgCount > 0)
            {
                var amount = packet.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).FirstOrDefault();
                JIME_TTS.Log.LogInfo($"Damage value from EnemyInfoDialog was {amount}");
            }
            return filepaths;
        }

        return null;
    }
        

    public static List<string> KeyInfoResolver(MessagePopup MessagePopupObject, LocalizationPacket packet, GameNode[] gameNodes = null)
    {


        List<string> filepaths = [];


        UILocalizationPacket localizationText = Traverse.Create(MessagePopupObject).Field("_localizedText").GetValue<UILocalizationPacket>();
        if (packet.Key == "PLACE_TILE")
        {
            localizationText.KeyInfo.CompressedValue = Regex.Replace(localizationText.KeyInfo.CompressedValue, @"\bPLACE_TILE\b", "PLACE_TILE_NO_FLAVOR");
            JIME_TTS.Log.LogInfo($"{localizationText.KeyInfo.CompressedValue}");
        }

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

                filepaths.Add(packet.Key);

                AddAdditionalAttackInfo(packet, localizationText, filepaths);

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

                        AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                        break;

                    case "UI_TERRAIN_NODES_REVEAL_FORMATTED":

                        if (gameNodes != null && gameNodes.Length >= 1)
                        {
                            AudioQueueSpawnTerrain(localizationText, gameNodes, filepaths);
                        }
                        break;

                    case "PLACE_TILE":

                        localizationText.KeyInfo.Key = "PLACE_TILE_NO_FLAVOR";
                        AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                        filepaths = AudioQueuePlaceTile(filepaths, 2); //numberStartingIndex: 2 → [A35_INTO, PLACE_TILE_NO_FLAVOR_1, 220A, PLACE_TILE_NO_FLAVOR_2]
                        break;

                    case "PLACE_SEARCH":
                    case "PLACE_THREAT":
                    case "PLACE_PERSON":

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "UI_AWARD_ITEM_FORMATTED":

                        if (localizationText.KeyInfo.Inserts[0].IsUsed)
                        {
                            filepaths.Add(packet.Key + $"_{localizationText.KeyInfo.Inserts[0].CompressedStringData}");
                        }
                        
                        break;

                    case "UI_AWARD_MOUNT_FORMATTED":

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "UI_ENEMY_REMOVAL_REMINDER_FORMATTED":

                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                        RemoveBracket(filepaths);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        break;
                    case "UI_ENEMY_REMOVAL_UNIQUE_REMINDER_FORMATTED":
                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                        RemoveBracket(filepaths);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        break;
                    case "UI_SPAWN_GROUP_FORMAT":

                        AudioQueueCorrectOrder(localizationText, textPart, filepaths);
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

                    case "TRAVEL_MAP_INTRO":
                    case "TRAVEL_MAP_STREAM":
                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    //Spreading War
                    
                    case "A59_GOOD_PROGRESS_1":
                    case "A59_GOOD_PROGRESS_2":
                    case "A59_OBJECTIVE_2A":
                    case "A59_GOOD_PROGRESS_4":
                    case "A59_GOOD_PROGRESS_5":
                    case "A62_INTRO_4":

                        List<int> insertsA59_GOOD1 = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList();
                        if(filepaths.Count == 2)
                        {
                            filepaths.Add(packet.Key + $"_{insertsA59_GOOD1[0]}_{insertsA59_GOOD1[1]}");
                        }
                        else
                        {
                            JIME_TTS.Log.LogError("Error in inserts of Spreading War.");
                        }
                        
                        break;

                    case "A59_GOOD_PROGRESS_3":
                    case "A59_BAD_PROGRESS_1":
                    case "A59_BAD_PROGRESS_2":
                    case "A58_GATE_OBECTIVE_UPDATE":
                    
                        List<int> insertA59_GOOD = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList();
                        if(insertA59_GOOD.Count == 1)
                        {
                            filepaths.Add(packet.Key + $"_{insertA59_GOOD[0]}");
                        }
                        break;

                    case "A60_ENEMY_QUESTION_PASS_TRAITOR":

                        List<string> insertsA60 = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        if(insertsA60.Count == 2)
                        {
                            filepaths.Add(packet.Key + $"_{insertsA60[0]}_{insertsA60[1]}");
                        }
                
                        break;

                    case "A60_CROSSROADS_CONFIRM":
                        List<string> inserts3 = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        filepaths.Add(packet.Key + $"_{inserts3[0]}");

                        break;
                        
                    case "A59_FELL_BEAST_CLOSER":
                    case "A59_FELL_BEAST_SPAWN":
                    case "A59_THREAT_2_DIV_COMPLETE":
                    case "A59_THREAT_4_DIV_COMPLETE":
                    case "A58_THREAT_5":
                    case "A58_THREAT_5_PASS":
                    case "A58_THREAT_5_FAIL":

                        filepaths.Add(packet.Key + $"_{FindHero(localizationText)}");

                        break;

                    case "TRAVEL_MAP_TOKEN_1_PASS":

                        if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(0) is { IsUsed: true } firstInsertTokenPass)
                        {
                            filepaths.Add(packet.Key + $"_{firstInsertTokenPass.RawText}");
                        }
                        
                        break;



                    case "A59_OBJECTIVE_3":

                        filepaths.Add(localizationText.KeyInfo.Key);
                        FindIntInsert(localizationText, filepaths);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        break;

                    case "A64_THREAT_2":

                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        break;
                    case "A64_THREAT_4A":
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        break;
                    case "A64_THREAT_4B":
                    
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key + "_1");
                        break;
                    case "TRAVEL_MAP_THREAT_2":
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key + "_1");
                        break;
                    
                    
                    case "A65_THREAT_2":
                    case "A59_THREAT_4_DIV_COMPLETE_1":
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key + "_1");
                        break;
                    case "A57_EMPTY_TRACKER_2B":

                        filepaths.Add(packet.Key);
                        if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(0) is { IsUsed: true } secondInsertTokenPass)
                        {
                            filepaths.Add(secondInsertTokenPass.RawText);
                        }
                        break;

                    

                    case "CAM_5_TRAVEL_CHOICE_2":

                        List<string> inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        filepaths.Add(localizationText.KeyInfo.Key);
                        filepaths.Add(inserts[0]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        filepaths.Add(inserts[1]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_2");
                        filepaths.Add(inserts[0]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        filepaths.Add(inserts[1]);
                        break;
                        //A60_DUNHARROW_CONFIRM
                    
                    case  "A57_SWAP_MAP":
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key + "_1");
                        break;
                    
                    case "A67_FELL_BEAST_TIMER_NO":
                    case "A67_FELL_BEAST_TIMER_YES":
                    case "A67_FELL_BEAST_TIMER":
                        filepaths.Add(packet.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key + "_1");
                        break;

                    case "A57_THREAT_1_TEST_2":
                        filepaths.Add(localizationText.KeyInfo.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        break;
                    case "A61_THREAT_1":

                        string hero2 = FindHero(localizationText);
                        filepaths.Add(localizationText.KeyInfo.Key);
                        filepaths.Add(hero2);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        filepaths.Add(hero2);
                        filepaths.Add(localizationText.KeyInfo.Key + "_2");
                        break;

                    case "A67_WITCH_KING_DEFEAT_1":
                        List<string> inserts33 = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.RawText).ToList();
                        List<int> inserts34 = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList();
                        filepaths.Add(packet.Key);
                        filepaths.Add(inserts33[0]);
                        filepaths.Add(packet.Key + "_1");
                        filepaths.Add(inserts34[0].ToString());
                        filepaths.Add(inserts34[0].ToString());
                        filepaths.Add(packet.Key + "_2");
                        
            
                        break;
                    case "A62_SPIRITS_TIMER1":
                    
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(localizationText.KeyInfo.Key);
                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(localizationText.KeyInfo.Key+ "_1");
                        break;
                    
                    case "A57_PLAYER_MOUNT":
                    case "A57_SWAP_MAP_2":
                    case "A57_THREAT_SKIPPED":
                        filepaths.Add(packet.Key + $"_{FindHero(localizationText)}");
                        break;

                    case "A57_SWAP_SETUP":

                        filepaths.Add(localizationText.KeyInfo.Key);
                        FindIntInsert(localizationText, filepaths);
                        break;

                    case "A35_TIMER_THREAT":
                    case "A35_SPAWN_WIGHTS":

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "A2_M1_INTRO":

                        filepaths.Add(FindHero(localizationText));
                        filepaths.Add(packet.Key);
                        break;

                    case string s when s.Contains("ENEMIES"): //case "A1_M1_E1_ENEMIES":

                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;
                    case string s when s.Contains("_SPAWN"):

                        EnemySpawn(packet, localizationText, filepaths, textPart);

                        break;
                    //A2_OBJECTIVE_2
                    case string s when s.Contains("OBJECTIVE"):

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(1) is { IsUsed: true } secondInsert)
                        {
                            filepaths.Add(secondInsert.CompressedIntData.ToString());
                        }
                        break;


                    default:

                        filepaths.Add(packet.Key);
                        break;
                }
                break;

            default:
                JIME_TTS.Log.LogError("MessagePopupObject does not exists or is invalid");
                break;
        }

        return filepaths;
    }




    private static void EnemySpawn(LocalizationPacket packet, UILocalizationPacket localizationText, List<string> filepaths, IEnumerable<string> textPart)
    {
        string hero = FindHero(localizationText);
        if (!string.IsNullOrEmpty(hero))
        {
            AudioQueueCorrectOrderEnemySpawn(packet, hero, filepaths);
        }
        else
        {
            filepaths.Add(packet.Key);
        }
    }

    private static void AddAdditionalAttackInfo(LocalizationPacket packet, UILocalizationPacket localizationText, List<string> filepaths)
    {

        if (EnemyActivations.Contains(packet.Key))
        {
            string hero = FindHero(localizationText);
            if (!string.IsNullOrEmpty(hero))
            {
                filepaths.Insert(1, hero);
            }

            string attackKey = packet.Key == "ENEMY_BALROG_ACTIVATION"
                ? "ENEMY_ACTIVATION_ATTACK_BALROG"
                : "ENEMY_ACTIVATION_ATTACK";
            filepaths.Add(attackKey);
        }
        else
        {
            switch (packet.Key)
            {

                case string s when s.Contains("ASSASSIN"):

                    if (FindAdditionalAttackInfo())
                    {
                        filepaths.Add("A57_ASSASSIN_EXTRA");
                    }
                    break;

                default:

                    if (FindAdditionalAttackInfo())
                    {
                        filepaths.Add(packet.Key.Replace("ATTACK", "ADDITIONAL"));
                    }
                    break;
            }
        }
    }


    private static bool FindAdditionalAttackInfo()
    {
        var additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
        if (additionalInfo != null)
        {
            return true;
        }
        return false;
    }

    private static string FindHero(UILocalizationPacket localizationText)
    {
        if (localizationText?.KeyInfo?.UniqueArgCount > 0)
        {
            var heroAttacked = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData)
            .FirstOrDefault();

            if (heroAttacked.HasValue)
            {
                var gameData = GameObject.Find("PersistentGameObject")?.GetComponent<GameData>();
                if (gameData != null)
                {
                    Hero[] heroes = Traverse.Create(gameData).Field("_heroes").GetValue<Hero[]>();
                    if (heroes != null && heroAttacked.Value <= heroes.Length)
                    {
                        return heroes[heroAttacked.Value].Model.NameKey;
                    }
                }
            }
        }
        return null;
    }

    private static IEnumerable<string> ValueCleaner(UILocalizationPacket localizationPacket)
    {

        if (localizationPacket != null)
        {
            var textPart = localizationPacket.KeyInfo.CompressedValue.Trim('[', ']').Split('|').Where(p => !int.TryParse(p, out _)).Select(p => p.Trim());
            return textPart;
        }
        else
        {
            JIME_TTS.Log.LogInfo("Compressed value does not exists or is mismatched");
            return null;
        }

    }

    private static void RemoveBracket(List<string> strings)
    {

        if (strings.Contains("0]"))
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

    private static void AudioQueueSpawnTerrain(UILocalizationPacket localizationText, GameNode[] gameNodes, List<string> filepaths)
    {
        filepaths.Add(localizationText.KeyInfo.Key);

        var countWithOrder = new List<(string Key, int Count)>();
        var seenKeys = new Dictionary<string, int>();

        foreach (var gameNode in gameNodes)
        {
            string key = gameNode.TerrainModel.NameKey;

            if (!seenKeys.TryGetValue(key, out int index)) 
            {
                index = countWithOrder.Count;
                seenKeys[key] = index;
                countWithOrder.Add((key, 1));
            }
            else
            {
                var existingKey = countWithOrder[index];
                countWithOrder[index] = (key, existingKey.Count + 1);
            }
        }

        foreach (var (key, count) in countWithOrder)
        {
            filepaths.Add(count.ToString());
            filepaths.Add(count > 1 ? key + "_PLURAL" : key);
        }
    }

    private static void AudioQueueCorrectOrder(UILocalizationPacket localizationText, IEnumerable<string> textPart, List<string> filepaths)
    {
        var prefix = localizationText.KeyInfo.Key;
        var temp = textPart.ToList();
        temp.Remove(localizationText.KeyInfo.Key);
        RemoveBracket(temp);
        filepaths.AddRange(new[] { $"{prefix}_1" }.Concat(temp).Concat(new[] { $"{prefix}_2" }));

    }

    private static List<string> AudioQueuePlaceTile(List<string> filepaths, int numberStartingIndex)
    {


        const string placeTile1 = "PLACE_TILE_NO_FLAVOR_1";
        const string placeTile2 = "PLACE_TILE_NO_FLAVOR_2";


        string numberStarting = filepaths.FirstOrDefault(s => s.Length > 0 && char.IsDigit(s[0]))
            ?? throw new InvalidOperationException("No string starting with a number found.");


        string randomText = filepaths.FirstOrDefault(s => s != placeTile1 && s != placeTile2 && s != numberStarting)
            ?? throw new InvalidOperationException("No random text found.");


        var result = new List<string>();


        int currentIndex = 0;
        result.Add(randomText); // First: random text
        currentIndex++;
        if (currentIndex == numberStartingIndex)
        {
            result.Add(numberStarting);
            currentIndex++;
        }
        result.Add(placeTile1); // PLACE_TILE_NO_FLAVOR_1
        currentIndex++;
        if (currentIndex == numberStartingIndex)
        {
            result.Add(numberStarting);
            currentIndex++;
        }
        result.Add(placeTile2); // PLACE_TILE_NO_FLAVOR_2
        if (currentIndex == numberStartingIndex)
        {
            result.Add(numberStarting);
        }

        return result.Where(s => filepaths.Contains(s)).ToList();
    }

    private static void AudioQueueCorrectOrderEnemySpawn(LocalizationPacket packet, string hero, List<string> filepaths)
    {

        List<string> temp = [];
        temp.Add(hero);

        switch (packet.Key)
        {
            //Shadowed Paths
            case "A29_FIRST_SPIDER_SPAWN":
            case "A29_FIRST_SPIDER_SPAWN_LARGE":

                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }).Concat(temp).Concat(new[] { $"{packet.Key}_2" }).Concat(temp));
                break;

            //Spreading War
            case "A59_FELL_BEAST_SPAWN":

                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(new[] { $"{packet.Key}_1" }).Concat(temp));
                break;

            case "A67_WITCH_KING_DROP_GOOD":
            case "A67_WITCH_KING_DROP_BAD":

                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }).Concat(temp).Concat(new[] { $"{packet.Key}_2" }));
                break;

            case "A67_WITCH_KING_DROP_GOOD_PROMO":
            case "A67_WITCH_KING_DROP_BAD_PROMO":
            case "A40_NALKA_SPAWN": //Shadowed Paths
                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }));
                break;

            default:

                temp.Add(hero);
                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }).Concat(temp));
                break;
        }
    }

    private static void FindIntInsert(UILocalizationPacket localizationText, List<string> filepaths)
    {
        string value = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).FirstOrDefault().ToString();
        if (!string.IsNullOrEmpty(value))
        {
            filepaths.Add(value);
        }
    }
}

