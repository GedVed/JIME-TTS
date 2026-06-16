using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using FFG.Common;
using UnityEngine;
using FFG.JIME;
using System.Text.RegularExpressions;
using System;
using JIME_TTS_MOD;




public class EncounterHelpers
{

    private readonly List<string> EnemyActivations = new List<string>{"ENEMY_GOBLIN_ACTIVATION","ENEMY_RUFFIAN_ACTIVATION","ENEMY_ORC_MARAUDER_ACTIVATION","ENEMY_ORC_HUNTER_ACTIVATION",
            "ENEMY_HUNGRY_WARG_ACTIVATION","ENEMY_WIGHT_ACTIVATION","ENEMY_HILL_TROLL_ACTIVATION","ENEMY_ATARIN_ACTIVATION","ENEMY_ULUK_ACTIVATION","ENEMY_GULGOTAR_ACTIVATION",
            "ENEMY_GIANT_SPIDER_ACTIVATION","ENEMY_PIT_GOBLIN_ACTIVATION","ENEMY_ORC_TASKMASTER_ACTIVATION","ENEMY_SHADOWMAN_ACTIVATION","ENEMY_NAMELESS_THING_ACTIVATION",
            "ENEMY_CAVE_TROLL_ACTIVATION","ENEMY_UNGOLIANT_ACTIVATION","ENEMY_BALROG_ACTIVATION","ENEMY_SOLDIER_ACTIVATION","ENEMY_URUK_ACTIVATION","ENEMY_FELL_BEAST_ACTIVATION",
            "ENEMY_WARG_RIDER_ACTIVATION","ENEMY_SIEGE_ENGINE_ACTIVATION","ENEMY_OLIPHAUNT_ACTIVATION", "A59_GIRANDAR_ACTIVATION", "ENEMY_URSA_ACTIVATION"};

    public List<string> KeyInfoResolverCombatDialog(UILocalizationPacket packet)
    {

        List<string> filepaths = [];

        if(packet.key != null)
        {
            
            if (packet?.KeyInfo?.UniqueArgCount > 0)
            {
                string amount = packet.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).FirstOrDefault();
                filepaths.Add(packet.key + $"_{amount}");
            }
            return filepaths;
        }

        return null;
    }
        

    public List<string> KeyInfoResolver(MessagePopup MessagePopupObject, LocalizationPacket packet, GameNode[] gameNodes = null)
    {


        List<string> filepaths = [];
        List<string> inserts  = [];

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
                    filepaths.Add(packet.Key + $"_{FindHeroByString(localizationText)}");
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


                //Multiple objectives 
                //Unique argument count?

                try
                {
                    int UniqueArgCount = (int)localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData).ToList().Count();
                }
                catch (Exception e)
                {
                    JIME_TTS.Log.LogError(e);

                    throw;
                }
                
                

                switch (packet.Key)
                {
                    case "UI_EXPLORE_TILE_WITH_INTRO_FORMATTED":

                        filepaths = textPart.OrderByDescending(text => text.StartsWith("TILE_")).ToList();
                        filepaths.Remove(localizationText.KeyInfo.Key);
                        RemoveBracket(filepaths);
                        break;

                    case "UI_SECTION_REVEAL_PLACE_TILE_FORMATTED":
                    case "PLACE_TILE_NO_FLAVOR":

                        /*
                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.RawText).ToList();
                        filepaths.Add(packet.Key + "_{inserts[0]}");
                        */

                        
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
                    case "UI_AWARD_TITLE_CHOOSE_HERO":
                    case "UI_AWARD_TITLE_FORMATTED":
                    //case "UI_THREAT_INCREASE":
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

                        filepaths.Add(packet.Key);
                        filepaths.Add(localizationText.KeyInfo.Inserts[0].CompressedStringData);
                        filepaths.InsertRange(2, FindEnemyGroup(filepaths, localizationText));
                        if (localizationText.KeyInfo.Inserts[2].IsUsed)
                        {
                            filepaths.Add(localizationText.KeyInfo.Inserts[2].CompressedStringData);
                        }
                        /*
                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                        RemoveBracket(filepaths);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        */
                        break;
                        
                    case "UI_ENEMY_REMOVAL_UNIQUE_REMINDER_FORMATTED":
                        
                        filepaths.Add(packet.Key);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        if (localizationText.KeyInfo.Inserts[2].IsUsed)
                        {
                            filepaths.Add(localizationText.KeyInfo.Inserts[2].CompressedStringData);
                        }
                        /*
                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                        RemoveBracket(filepaths);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        */
                        break;
                    case "UI_SPAWN_GROUP_FORMAT":
                        
                        filepaths.Add(packet.Key + "_1");
                        filepaths.Add(localizationText.KeyInfo.Inserts[0].CompressedStringData);
                        filepaths.InsertRange(2, FindEnemyGroup(filepaths, localizationText));
                        filepaths.Add(packet.Key + "_2");
                        /*
                        AudioQueueCorrectOrder(localizationText, textPart, filepaths);
                        filepaths.RemoveAll(s => s == null || s.Any(c => char.IsLetter(c) && !char.IsUpper(c)));
                        RemoveBracket(filepaths);
                        filepaths.InsertRange(1, FindEnemyGroup(filepaths, localizationText));
                        */
                        
                        break;

                    case "UI_THREAT_INCREASE":

                        
                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(0) is { IsUsed: true } firstInsert)
                        {
                            filepaths.Add(firstInsert.RawText);
                        }
                        break;

                    

                    case "A2_M1_INTRO":

                        filepaths.Add(FindHeroByInt(localizationText));
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







                    //Spreading War


                    case "TRAVEL_MAP_INTRO":
                    case "TRAVEL_MAP_STREAM":
                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;
                    
                    case "A59_GOOD_PROGRESS_1":
                    case "A59_GOOD_PROGRESS_2":
                    case "A59_OBJECTIVE_2A":
                    case "A59_GOOD_PROGRESS_4":
                    case "A59_GOOD_PROGRESS_5":
                    case "A62_INTRO_4":
                    case "A62_GARRISON_ACT1_INSPECT":

                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData.ToString()).ToList();
                        if(inserts?.Count == 2)
                        {
                            filepaths.Add(packet.Key + $"_{inserts[0]}_{inserts[1]}");
                        }
                        else
                        {
                            JIME_TTS.Log.LogError("A62_GARRISON_ACT1_INSPECT insert error");
                        }
                        
                        break;

                    case "A59_GOOD_PROGRESS_3":
                    case "A59_BAD_PROGRESS_1":
                    case "A59_BAD_PROGRESS_2":
                    case "A58_GATE_OBECTIVE_UPDATE":
                    case "A62_ASSAULT_ENEMY_STRONG":
                    case "A62_ASSAULT_ENEMY_WEAK":
                    case "A62_ASSAULT_ALLY_STRONG":
                    case "A62_ASSAULT_ALLY_WEAK":
                    case "A63_MAYOR_MORE PROOF_GIVE":
                    case "A63_RUDE_EMISSARY_HEADMAN":
                    case "A66_1_LOST CALEMBEL":
                    case "A67_EARLY_BOSS_FIGHT":
                    case "A59_OBJECTIVE_3":
                    

                    
                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData.ToString()).ToList();
                        if (inserts != null && inserts.Count == 1)
                        {
                            filepaths.Add(packet.Key + $"_{inserts[0]}");
                        }
                        break;

                    case "TRAVEL_MAP_TOKEN_1_PASS":

                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.RawText).ToList();

                        if (inserts != null && inserts.Count == 1)
                        {
                            filepaths.Add(packet.Key + $"_{inserts[0]}");
                        }
                        
                        break;


                    case "A60_ENEMY_QUESTION_PASS_TRAITOR":

                        
                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        if(inserts.Count == 2)
                        {
                            filepaths.Add(packet.Key + $"_{inserts[0]}_{inserts[1]}");
                        }
                
                        break;

                    case "A60_CROSSROADS_CONFIRM":
                    case "A60_DUNHARROW_CONFIRM":

                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        filepaths.Add(packet.Key + $"_{inserts[0]}");

                        break;
                    

                    case "A59_FELL_BEAST_CLOSER":
                    case "A59_FELL_BEAST_SPAWN":
                    case "A59_THREAT_2_DIV_COMPLETE":
                    case "A59_THREAT_4_DIV_COMPLETE":
                    case "A58_THREAT_5":
                    case "A58_THREAT_5_PASS":
                    case "A58_THREAT_5_FAIL":
                    case "A63_THREAT_1":
                    case "A63_THREAT_3":
                    case "A64_THREAT_1":
                    case "A64_THREAT_2":
                    case "A64_THREAT_4A":
                    case "A64_THREAT_4B":
                    case "A65_THREAT_2":
                    case "A67_WITCH_KING_DROP_BAD":
                    case "A67_WITCH_KING_DROP_BAD_PROMO":
                    case "A67_FELL_BEAST_TIMER":
                    case "A67_FELL_BEAST_TIMER_YES":
                    case "A67_FELL_BEAST_TIMER_NO":
                    case "A57_THREAT_1_TEST_2":
                    case "A61_THREAT_1":
                    case "A57_SWAP_MAP":
                    case "A62_SPIRITS_TIMER1":
                    case "A57_THREAT_SKIPPED":
                    case "A57_PLAYER_MOUNT":
                    case "A57_SWAP_MAP_2":
                    case "TRAVEL_MAP_THREAT_2":
                    
                        filepaths.Add(packet.Key + $"_{FindHeroByInt(localizationText)}" + packet.Key + "_1");

                        break;

    
                    case "A57_EMPTY_TRACKER_2B":

                        filepaths.Add(packet.Key);
                        if (localizationText?.KeyInfo?.Inserts?.ElementAtOrDefault(0) is { IsUsed: true } secondInsertTokenPass)
                        {
                            filepaths.Add(secondInsertTokenPass.RawText);
                        }
                        break;

                    

                    case "CAM_5_TRAVEL_CHOICE_2":

                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();

                        filepaths.Add(localizationText.KeyInfo.Key);
                        filepaths.Add(inserts[0]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        filepaths.Add(inserts[1]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_2");
                        filepaths.Add(inserts[0]);
                        filepaths.Add(localizationText.KeyInfo.Key + "_1");
                        filepaths.Add(inserts[1]);
                        break;
                        
                    
                    
    
                    case "A67_WITCH_KING_DEFEAT_1":

                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedIntData.ToString()).ToList();
                        inserts.AddRange(localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.RawText).ToList());
                        
                        if(inserts != null && inserts.Count == 2)
                        {
                            
                            filepaths.Add(packet.Key);
                            filepaths.Add(inserts[0]);
                            filepaths.Add(packet.Key + "_1");
                            filepaths.Add(inserts[1]);
                            filepaths.Add(inserts[1]);
                            filepaths.Add(packet.Key + "_2");

                        }

                        break;
                

                    case "A35_TIMER_THREAT":
                    case "A35_SPAWN_WIGHTS":

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;


                    //Broken Promise


                    case "A68_THREAT_4_FOE":
                    case "NEW_RULE":
                        inserts = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData).ToList();
                        filepaths.Add(packet.Key);
                        filepaths.Add(inserts[0]);
                    break;

                    case "A69_OBJECTIVE_2_SET":
                        filepaths.Add(packet.Key);
                        break;
                    case "A69_OBJECTIVE_2":
                    case "A71_OBJECTIVE_REPEAT":
                    case "A71_PLOT_SEARCH_2_PASS":
                        filepaths.Add(packet.Key + $"_{localizationText.KeyInfo.Inserts[0].CompressedIntData}");
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




    private void EnemySpawn(LocalizationPacket packet, UILocalizationPacket localizationText, List<string> filepaths, IEnumerable<string> textPart)
    {
        string hero = FindHeroByInt(localizationText);
        if (!string.IsNullOrEmpty(hero))
        {
            AudioQueueCorrectOrderEnemySpawn(packet, hero, filepaths);
        }
        else
        {
            filepaths.Add(packet.Key);
        }
    }

    private void AddAdditionalAttackInfo(LocalizationPacket packet, UILocalizationPacket localizationText, List<string> filepaths)
    {

        if (EnemyActivations.Contains(packet.Key))
        {
            string hero = FindHeroByInt(localizationText);
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


    private bool FindAdditionalAttackInfo()
    {
        var additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
        if (additionalInfo != null)
        {
            return true;
        }
        return false;
    }

    private string FindHeroByString(UILocalizationPacket localizationText)
    {
        if (localizationText?.KeyInfo?.UniqueArgCount > 0)
        {
            string hero = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert.CompressedStringData)
            .FirstOrDefault();

            if(hero != null)
            {
                return hero;
            }

        }
        return null;
    }



    private string FindHeroByInt(UILocalizationPacket localizationText)
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

    private IEnumerable<string> ValueCleaner(UILocalizationPacket localizationPacket)
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

    private void RemoveBracket(List<string> strings)
    {

        if (strings.Contains("0]"))
        {
            strings.Remove("0]");
        }

    }

    private IEnumerable<string> FindEnemyGroup(List<string> filepaths, UILocalizationPacket localizationText)
    {

        var enemyGroup = localizationText?.KeyInfo?.Inserts?.Where(insert => insert.IsUsed).Select(insert => insert switch
        {
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

    private void AudioQueueSpawnTerrain(UILocalizationPacket localizationText, GameNode[] gameNodes, List<string> filepaths)
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

    private void AudioQueueCorrectOrder(UILocalizationPacket localizationText, IEnumerable<string> textPart, List<string> filepaths)
    {
        var prefix = localizationText.KeyInfo.Key;
        var temp = textPart.ToList();
        temp.Remove(localizationText.KeyInfo.Key);
        RemoveBracket(temp);
        filepaths.AddRange(new[] { $"{prefix}_1" }.Concat(temp).Concat(new[] { $"{prefix}_2" }));

    }

    private List<string> AudioQueuePlaceTile(List<string> filepaths, int numberStartingIndex)
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

    private void AudioQueueCorrectOrderEnemySpawn(LocalizationPacket packet, string hero, List<string> filepaths)
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
}

