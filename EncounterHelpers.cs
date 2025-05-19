using System.Collections.Generic;
using System.Linq;
using ReadTextMod;
using HarmonyLib;
using FFG.Common;
using UnityEngine;
using FFG.JIME;

public static class EncounterHelpers
{


    public static List<string> KeyInfoResolver(MessagePopup MessagePopupObject, LocalizationPacket packet)
    {


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

                filepaths.Add(packet.Key);

                if (ReadText.enemyActivations.Contains(packet.Key))
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

                    var additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
                    if (additionalInfo != null)
                    {
                        filepaths.Add(packet.Key.Replace("ATTACK", "ADDITIONAL"));
                    }
                }

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

                    case "PLACE_SEARCH":
                    case "PLACE_THREAT":
                    case "PLACE_PERSON":

                        filepaths = textPart.OrderBy(text => text == localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case "UI_AWARD_ITEM_FORMATTED":

                        AudioQueueCorrectOrder(localizationText, textPart, filepaths);
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
                        
                    case string s when s.Contains("ENEMIES"): //case "A1_M1_E1_ENEMIES":

                        filepaths = textPart.OrderBy(text => text != localizationText.KeyInfo.Key).ToList();
                        RemoveBracket(filepaths);
                        break;

                    case string s when s.Contains("_SPAWN"):

                        EnemySpawn(packet, localizationText, filepaths, textPart);

                        break;

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
                ReadText.Log.LogError("MessagePopupObject does not exists or is invalid");
                break;
        }

        return filepaths;
    }


    private static void EnemySpawn(LocalizationPacket packet, UILocalizationPacket localizationText, List<string> filepaths, IEnumerable<string> textPart)
    {
        string hero = FindHero(localizationText);
        if (!string.IsNullOrEmpty(hero))
        {
            AudioQueueCorrectOrderEnemy(packet, hero, filepaths);
        }
        else
        {
            filepaths.Add(packet.Key);
        }
    }

    private static string FindHero(UILocalizationPacket localizationText)
    {
        if (localizationText?.KeyInfo?.UniqueArgCount > 0)
        {
            var heroAttacked = localizationText.KeyInfo.Inserts?.Where(insert => insert.IsUsed)
            .Select(insert => insert.CompressedIntData)
            .FirstOrDefault();

            if (heroAttacked.HasValue)
            {
                var gameData = GameObject.Find("PersistentGameObject")?.GetComponent<GameData>();
                if (gameData != null)
                {
                    Hero[] heroes = Traverse.Create(gameData).Field("_heroes").GetValue<Hero[]>();
                    if (heroes != null && heroAttacked.Value < heroes.Length)
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
            ReadText.Log.LogInfo("Compressed value does not exists or is mismatched");
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

    private static void AudioQueueCorrectOrder(UILocalizationPacket localizationText, IEnumerable<string> textPart, List<string> filepaths)
    {

        var prefix = localizationText.KeyInfo.Key;
        var temp = textPart.ToList();
        temp.Remove(localizationText.KeyInfo.Key);
        RemoveBracket(temp);
        filepaths.AddRange(new[] { $"{prefix}_1" }.Concat(temp).Concat(new[] { $"{prefix}_2" }));

    }

    private static void AudioQueueCorrectOrderEnemy(LocalizationPacket packet, string hero, List<string> filepaths)
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

                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp));
                break;

            case "A67_WITCH_KING_DROP_GOOD":
            case "A67_WITCH_KING_DROP_BAD":

                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }).Concat(temp).Concat(new[] { $"{packet.Key}_2" }));
                break;

            case "A67_WITCH_KING_DROP_GOOD_PROMO":
            case "A67_WITCH_KING_DROP_BAD_PROMO":
            case "A40_NALKA_SPAWN":
                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }));
                break;

            default:

                temp.Add(hero);
                filepaths.AddRange(new[] { $"{packet.Key}" }.Concat(temp).Concat(new[] { $"{packet.Key}_1" }).Concat(temp));
                break;
        }
    }
}

