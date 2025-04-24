using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using FFG.Common;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;



[BepInPlugin("GedVed.lotr.readtext", "LOTR Read Text Mod", "1.0.0")]
public class ReadText : BaseUnityPlugin
{
    public static ManualLogSource log; 
    public static AudioSource audioSource;
    public static string audioFolder = Path.Combine(Paths.PluginPath, "ReadTextMod\\arnorTTS"); // Folder with WAVs
    public static Dictionary<string, AudioClip> audioClips = [];
    public static bool hasPlayed = false;
    public static MessagePopup messagePopupComponent;

    //public static List<AudioSource> audioSources;

    void Awake(){
        log = Logger;
        GameObject audioObj = new GameObject("SoundPlayer");
        audioSource = audioObj.AddComponent<AudioSource>();
        var harmony = new Harmony("GedVed.lotr.readtext");
        harmony.PatchAll(); 
        log.LogInfo("ReadTextMod Loaded!");
    }

    public static void PopupMessageTTS(MessagePopup __instance, UILocalizationPacket localizationText){
        string message;
        if(string.IsNullOrEmpty(localizationText.key)){
            message = localizationText.KeyInfo.Key;
        }else{
            message = localizationText.key;
        }
        
        if (!string.IsNullOrEmpty(message)){
            log.LogInfo($"[LOTR Message Popup - UILabel]: {message}");

            // Load the sound file dynamically when needed
            string filePath = Path.Combine(audioFolder, message + ".wav");
            if (File.Exists(filePath))
            {
                // Start the coroutine to load the audio file dynamically
                __instance.StartCoroutine(LoadAndPlaySound(message, filePath));
            }
            else
            {
                log.LogError($"Audio file for '{message}' not found at '{filePath}'");
            }
        }
        else
        {
            log.LogError("Failed to find UILocalizationPacket in MessagePopup!");
        }
    }
        
    public static IEnumerator LoadAndPlaySound(string fileName, string filePath){
        // If the clip has already been loaded, no need to load again
        if (audioClips.ContainsKey(fileName)){
            log.LogInfo($"Audio clip for '{fileName}' already loaded.");
        }
        else{
            // Start loading the audio file asynchronously
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);

            // Wait for the web request to complete
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success){
                // Get the AudioClip from the request
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                // Add the successfully loaded clip to the dictionary
                audioClips[fileName] = clip;
                log.LogInfo($"File '{fileName}' added to dictionary.");
            }
            else{
                log.LogError($"Failed to load audio file: {fileName}");
                yield break;
            }
        }

        // Play the sound (assuming the sound is loaded)
        if (audioClips.ContainsKey(fileName)){
            AudioClip clip = audioClips[fileName];

            if (audioSource == null){
                GameObject audioObj = new GameObject("SoundPlayer");
                audioSource = audioObj.AddComponent<AudioSource>();
                log.LogInfo("AudioSource was null");
            }
            audioSource.clip = clip;
            audioSource.Play();
            hasPlayed = true;
            log.LogInfo($"Playing sound for '{fileName}'.");
        }
        else{
            log.LogError($"Audio clip for '{fileName}' not found after loading.");
        }
    }
}

[HarmonyPatch(typeof(UIPanel), "LateUpdate")]
public class LateUpdate(){

    static void Postfix(){

        GameObject messagePopupObject = GameObject.Find("MessagePopup");
        GameObject messagePopupEnemy = GameObject.Find("MessagePopup_EnemyActivation");
        
        if(messagePopupObject != null){
            ReadText.messagePopupComponent = (MessagePopup)messagePopupObject.GetComponentByName("MessagePopup");
        }else if(messagePopupEnemy != null){
            ReadText.messagePopupComponent = (MessagePopup)messagePopupEnemy.GetComponentByName("MessagePopup");
            GameObject additionalInfo = GameObject.Find("Table_AdditionalInfo");
        }else{
            ReadText.hasPlayed = false;
        }

        if(ReadText.messagePopupComponent != null && !ReadText.hasPlayed && ReadText.messagePopupComponent.IsShowingMessage && ReadText.messagePopupComponent.isActiveAndEnabled){
                    UILocalizationPacket localizedText = Traverse.Create(ReadText.messagePopupComponent).Field("_localizedText").GetValue<UILocalizationPacket>();  
                    ReadText.PopupMessageTTS(ReadText.messagePopupComponent, localizedText);
        }

        if(ReadText.audioSource != null && ReadText.audioSource.isPlaying && messagePopupObject == null && messagePopupEnemy == null){
                ReadText.audioSource.Stop();
                ReadText.log.LogInfo("Audio clip stopped");
            }
    }
}











