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




[BepInPlugin("GedVed.lotr.readtext", "LOTR Read Text Mod", "1.0.0")]
public class ReadText : BaseUnityPlugin
{
    public static ManualLogSource log; 
    public static AudioSource audioSource;
    public static string audioFolder = Path.Combine(Paths.PluginPath, "ReadTextMod\\arnorTTS"); // Folder with WAVs
    
    public static bool isPlayingQueue = false;
    public static bool hasPlayed = false;
    public static MessagePopup messagePopupComponent;
    public static OrderedDictionary audioQueue = new OrderedDictionary();


    void Awake(){
        log = Logger;
        GameObject audioObj = new GameObject("SoundPlayer");
        audioSource = audioObj.AddComponent<AudioSource>();
        var harmony = new Harmony("GedVed.lotr.readtext");
        harmony.PatchAll(); 
        log.LogInfo("ReadTextMod Loaded!");
    }

    public static void PopupMessageTTS(MessagePopup __instance, UILocalizationPacket localizationText, GameObject additionalInfo = null){
        
        List<string> filepaths = [];
        
        filepaths.Add(string.IsNullOrEmpty(localizationText.key) ? localizationText.KeyInfo.Key : localizationText.key);
        if (!string.IsNullOrEmpty(localizationText.key) && additionalInfo != null)
            filepaths.Add(localizationText.key.Replace("ATTACK", "ADDITIONAL"));

        if (filepaths.Count > 0){

            foreach(string path in filepaths){

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
    private static IEnumerator PlayQueueCoroutine(){
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
    
        if (!isPlayingQueue && audioQueue.Contains(path)) // Optional: only start if not already playing
        {
            yield return PlayQueueCoroutine();
            hasPlayed = true;
        }
    }

}

[HarmonyPatch(typeof(UIPanel), "LateUpdate")]
public class LateUpdate(){

    static void Postfix(){

        GameObject messagePopupObject = GameObject.Find("MessagePopup");
        GameObject messagePopupEnemy = GameObject.Find("MessagePopup_EnemyActivation");
        GameObject additionalInfo = null;

        if(messagePopupObject != null)
        {
            ReadText.messagePopupComponent = (MessagePopup)messagePopupObject.GetComponentByName("MessagePopup");
        }
        else if(messagePopupEnemy != null)
        {
            ReadText.messagePopupComponent = (MessagePopup)messagePopupEnemy.GetComponentByName("MessagePopup");
            additionalInfo = GameObject.Find("Label_Attack_AdditionalEffect");
        }
        else
        {
            ReadText.hasPlayed = false;
        }

        if(ReadText.messagePopupComponent != null && !ReadText.hasPlayed && ReadText.messagePopupComponent.IsShowingMessage && ReadText.messagePopupComponent.isActiveAndEnabled)
        {
            UILocalizationPacket localizedText = Traverse.Create(ReadText.messagePopupComponent).Field("_localizedText").GetValue<UILocalizationPacket>();
            if(additionalInfo != null)
            {
                ReadText.log.LogInfo($"Additional Effect found");
                ReadText.PopupMessageTTS(ReadText.messagePopupComponent, localizedText, additionalInfo);
            }
            else
            {
                ReadText.PopupMessageTTS(ReadText.messagePopupComponent, localizedText);
            }
                    
        }

        if(ReadText.audioSource != null && ReadText.audioSource.isPlaying && messagePopupObject == null && messagePopupEnemy == null)
        {
                ReadText.audioSource.Stop();
                ReadText.audioQueue.Clear();
                ReadText.isPlayingQueue = false; 
                ReadText.log.LogInfo("Audio queue playback stopped");
        }
    }
}











