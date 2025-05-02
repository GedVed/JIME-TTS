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
using ReadTextMod;
using UnityEngine.TextCore;







namespace ReadTextMod{


[BepInPlugin("GedVed.lotr.readtext", "LOTR Read Text Mod", "1.0.0")]
    public class ReadText : BaseUnityPlugin
    {
        public static ManualLogSource Log; 
        public static AudioSource AudioSource;
        public static string AudioFolder = Path.Combine(Paths.PluginPath, "ReadTextMod\\TTS"); // Folder with WAVs
        public static bool IsPlayingQueue = false;
        public static bool HasPlayed = false;
        public static bool HasStartedLoading = false;
        public static MessagePopup MessagePopupComponent;
        public static OrderedDictionary AudioQueue = [];
        private static CampaignData CampaignData;

        public static string PreviousKeyInfo;
        public static string PreviousKey;
        


        void Awake(){
            Log = Logger;
            GameObject audioObj = new GameObject("SoundPlayer");
            AudioSource = audioObj.AddComponent<AudioSource>();
            var harmony = new Harmony("GedVed.lotr.readtext");
            harmony.PatchAll(); 
            Log.LogInfo("ReadTextMod Loaded!");
        }

        

        public static void PopupMessageTTS(MessagePopup __instance, GameObject AdditionalInfoAttack = null){
            
            UILocalizationPacket packet = Traverse.Create(__instance).Field("_localizedText").GetValue<UILocalizationPacket>();
            List<string> filepaths = EncounterHelpers.KeyInfoResolver(packet, AdditionalInfoAttack);

            //string[] inserts = Traverse.Create(localizationText.KeyInfo).Field("s_localizedInserts").GetValue<string[]>();
            
            if (filepaths.Count > 0){
                __instance.StartCoroutine(LoadAndPlayWrapper(filepaths));
            }else
            {
                Log.LogInfo("Unable to extract path from KeyInfo");
            }
                
        }

        private static IEnumerator LoadSound(List<string> filepaths)
        {
            if(!HasStartedLoading)
            {
                foreach (string path in filepaths)
                {
                    
                    Log.LogInfo($"{path}");
                        
                    string filePath = Path.Combine(AudioFolder, path + ".wav");

                    if (!AudioQueue.Contains(path))
                    {
                        
                        if (File.Exists(filePath))
                        {

                            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
                            yield return www.SendWebRequest();

                            if (www.result == UnityWebRequest.Result.Success)
                            {
                                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                                AudioQueue.Add(path, clip);
                                Log.LogInfo($"File '{path}' added to AudioQueue.");
                            }
                            else
                            {
                                Log.LogError($"Failed to load audio file: {filePath}");
                                yield break;
                            }
                        }
                        else
                        {
                            Log.LogError($"Audio file for '{path}' not found at '{filePath}'");
                        }
                        
                    }
                }
            }
        }
    

        private static IEnumerator PlayQueue()
        {
            IsPlayingQueue = true;

            while (AudioQueue.Count > 0)
            {
                DictionaryEntry entry = AudioQueue.Cast<DictionaryEntry>().First();
                AudioClip clip = entry.Value as AudioClip;
                AudioQueue.Remove(entry.Key);

                if (AudioSource == null)
                {
                    GameObject audioObj = new GameObject("SoundPlayer");
                    AudioSource = audioObj.AddComponent<AudioSource>();
                    Log.LogInfo("AudioSource was null");
                }

                AudioSource.clip = clip;
                AudioSource.Play();

                yield return new WaitWhile(() => AudioSource.isPlaying);
            }

            IsPlayingQueue = false;
            HasPlayed = true;
        }

        private static IEnumerator LoadAndPlayWrapper(List<string> filepaths)
        {
            yield return LoadSound(filepaths);
        
            if (!IsPlayingQueue && AudioQueue.Count > 0 && HasPlayed == false)
            {
                yield return PlayQueue();
                
                
            }
        }
    
    }
}

[HarmonyPatch(typeof(UIPanel), "LateUpdate")]
public class LateUpdate(){

    static void Postfix(){
        
        GameObject MessagePopupObject = GameObject.Find("MessagePopup"); //normal message
        GameObject MessagePopupEnemy = GameObject.Find("MessagePopup_EnemyActivation"); //enemy attack
        GameObject MessagePopupNew = GameObject.Find("MessagePopup_New"); //hero last stand
        GameObject AdditionalInfoAttack = null;

        try
        {
            if(MessagePopupObject != null)
            {
                ReadText.MessagePopupComponent = (MessagePopup)MessagePopupObject.GetComponentByName("MessagePopup");
            }
            else if(MessagePopupEnemy != null)
            {
                ReadText.MessagePopupComponent = (MessagePopup)MessagePopupEnemy.GetComponentByName("MessagePopup");
                AdditionalInfoAttack = GameObject.Find("Label_Attack_AdditionalEffect");
            }
            else if(MessagePopupEnemy != null && MessagePopupObject != null){
                ReadText.Log.LogInfo("Both messeges active");
            }
            else if(MessagePopupNew != null)
            {
                ReadText.MessagePopupComponent = (MessagePopup)MessagePopupNew.GetComponentByName("MessagePopup");
            }
            else
            {
                ReadText.HasPlayed = false;
                ReadText.HasStartedLoading = false;
            }

            if(ReadText.MessagePopupComponent != null && !ReadText.HasPlayed && ReadText.MessagePopupComponent.IsShowingMessage && ReadText.MessagePopupComponent.isActiveAndEnabled)
            {

                
                if(AdditionalInfoAttack != null)
                {
                    ReadText.Log.LogInfo($"Additional Effect found");
                    ReadText.PopupMessageTTS(ReadText.MessagePopupComponent, AdditionalInfoAttack);
                    ReadText.HasStartedLoading = true;
                    
                    
                }
                else
                {
                    ReadText.PopupMessageTTS(ReadText.MessagePopupComponent);
                    ReadText.HasStartedLoading = true;
                    
                    
                
                }      
            }
        }
        catch (ArgumentNullException)
        {
            
            throw new NullReferenceException("This is expected at some point");
        }

        
        if(ReadText.AudioSource != null && ReadText.AudioSource.isPlaying && MessagePopupObject == null && MessagePopupEnemy == null && MessagePopupNew == null)
        {
                ReadText.AudioSource.Stop();
                ReadText.AudioQueue.Clear();
                ReadText.IsPlayingQueue = false; 
                ReadText.Log.LogInfo("Audio queue playback stopped");
        }

        


    }
}

