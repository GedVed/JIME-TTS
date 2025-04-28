using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using FFG.Common;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.Specialized;
using System;
using System.Text.RegularExpressions;





[BepInPlugin("GedVed.lotr.readtext", "LOTR Read Text Mod", "1.0.0")]
public class ReadText : BaseUnityPlugin
{
    public static ManualLogSource log; 
    public static AudioSource audioSource;
    public static string audioFolder = Path.Combine(Paths.PluginPath, "ReadTextMod\\TTS"); // Folder with WAVs
    public static bool isPlayingQueue = false;
    public static bool hasPlayed = false;
    public static MessagePopup messagePopupComponent;
    public static OrderedDictionary audioQueue = [];


    void Awake(){
        log = Logger;
        GameObject audioObj = new GameObject("SoundPlayer");
        audioSource = audioObj.AddComponent<AudioSource>();
        var harmony = new Harmony("GedVed.lotr.readtext");
        harmony.PatchAll(); 
        log.LogInfo("ReadTextMod Loaded!");
    }

    private static void RemoveBracket(List<String> strings){
        if(strings.Contains("0]")){
            strings.Remove("0]");
        }
    }

    public static void PopupMessageTTS(MessagePopup __instance, UILocalizationPacket localizationText, GameObject additionalInfoAttack = null){
        
        List<string> filepaths = [];
        

        if(!string.IsNullOrEmpty(localizationText.key))
        {
            filepaths.Add(localizationText.key);
            if (additionalInfoAttack != null)
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
        

  
        if (filepaths.Count > 0){

            foreach(string path in filepaths){
                log.LogInfo($"{path}");
                string filePath = Path.Combine(audioFolder, path + ".wav");

                if(!audioQueue.Contains(path))
                {
                    if (File.Exists(filePath))
                    {
                        __instance.StartCoroutine(LoadAndPlayWrapper(path, filePath));
                        hasPlayed = true;
                    }
                    else
                    {
                    log.LogError($"Audio file for '{path}' not found at '{filePath}'");
                    } 
                }else{
                    __instance.StartCoroutine(PlayQueue());
                }   
            }
        }
    }
    private static IEnumerator LoadSound(string fileName, string filePath){
        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success){
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            if(!audioQueue.Contains(fileName))
            {
                audioQueue.Add(fileName, clip);
                log.LogInfo($"File '{fileName}' added to audioQueue.");
            }
        }
        else
        {
            log.LogError($"Failed to load audio file: {filePath}");
            yield break;
        }
    }
    private static IEnumerator PlayQueue(){
        isPlayingQueue = true;

        while(audioQueue.Count > 0)
        {

            DictionaryEntry entry = audioQueue.Cast<DictionaryEntry>().First();
            AudioClip clip = entry.Value as AudioClip;
            audioQueue.Remove(entry.Key);

            if (audioSource == null)
            {
                GameObject audioObj = new GameObject("SoundPlayer");
                audioSource = audioObj.AddComponent<AudioSource>();
                log.LogInfo("AudioSource was null");
            }

            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitWhile(() => audioSource.isPlaying);

        }
        isPlayingQueue = false;
    }

    private static IEnumerator LoadAndPlayWrapper(string path, string filePath)
    {
        yield return LoadSound(path, filePath);
    
        if (!isPlayingQueue && audioQueue.Contains(path))
        {
            yield return PlayQueue();
            hasPlayed = true;
        }
    }

}

[HarmonyPatch(typeof(UIPanel), "LateUpdate")]
public class LateUpdate(){

    static void Postfix(){

        GameObject messagePopupObject = GameObject.Find("MessagePopup"); //normal message
        GameObject messagePopupEnemy = GameObject.Find("MessagePopup_EnemyActivation"); //enemy attack
        GameObject messagePopupNew = GameObject.Find("MessagePopup_New"); //hero last stand
        GameObject additionalInfoAttack = null;

        try
        {
            if(messagePopupObject != null)
            {
                ReadText.messagePopupComponent = (MessagePopup)messagePopupObject.GetComponentByName("MessagePopup");
            }
            else if(messagePopupEnemy != null)
            {
                ReadText.messagePopupComponent = (MessagePopup)messagePopupEnemy.GetComponentByName("MessagePopup");
                additionalInfoAttack = GameObject.Find("Label_Attack_AdditionalEffect");
            }
            else if(messagePopupEnemy != null && messagePopupObject != null){
                ReadText.log.LogInfo("Both messeges active");
            }
            else if(messagePopupNew != null)
            {
                ReadText.messagePopupComponent = (MessagePopup)messagePopupNew.GetComponentByName("MessagePopup");
            }
            else
            {
                ReadText.hasPlayed = false;
            }

            if(ReadText.messagePopupComponent != null && !ReadText.hasPlayed && ReadText.messagePopupComponent.IsShowingMessage && ReadText.messagePopupComponent.isActiveAndEnabled)
            {

                UILocalizationPacket localizedText = Traverse.Create(ReadText.messagePopupComponent).Field("_localizedText").GetValue<UILocalizationPacket>();

                if(additionalInfoAttack != null)
                {
                    ReadText.log.LogInfo($"Additional Effect found");
                    ReadText.PopupMessageTTS(ReadText.messagePopupComponent, localizedText, additionalInfoAttack);
                    
                }
                else
                {
                    ReadText.PopupMessageTTS(ReadText.messagePopupComponent, localizedText);
                    
                }      
            }
        }
        catch (ArgumentNullException)
        {
            
            throw new NullReferenceException("This is expected at some point");
        }


        if(ReadText.audioSource != null && ReadText.audioSource.isPlaying && messagePopupObject == null && messagePopupEnemy == null && messagePopupNew == null)
        {
                ReadText.audioSource.Stop();
                ReadText.audioQueue.Clear();
                ReadText.isPlayingQueue = false; 
                ReadText.log.LogInfo("Audio queue playback stopped");
        }
    }
}











