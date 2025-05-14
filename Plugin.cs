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
using UnityEngine.SceneManagement;



namespace ReadTextMod{


[BepInPlugin("GedVed.lotr.readtext", "LOTR Read Text Mod", "1.0.0")]
    public class ReadText : BaseUnityPlugin
    {
        public static ManualLogSource Log; 
        private static AudioSource AudioSource;
        private bool SceneLoadPatchTriggered = false;
        private static string AudioFolder = Path.Combine(Paths.PluginPath, "ReadTextMod\\TTS");
        private static bool IsPlayingQueue = false;
        private static bool HasStartedLoading = false;
        private static OrderedDictionary AudioQueue = [];
        public static MethodResolver MethodResolver = null;
        
        void Awake()
        {
            var harmony = new Harmony("GedVed.lotr.readtext");
            Log = Logger;
            MethodResolver = new MethodResolver(harmony, this);
            GameObject audioObj = new GameObject("SoundPlayer");
            AudioSource = audioObj.AddComponent<AudioSource>();
            MethodResolver.Initialize();
            harmony.PatchAll(); 
            SceneManager.sceneLoaded += OnSceneLoaded;
            MethodResolver.MessagePopupMethodExecuted += OnMessagePopupMethodExecuted;
            MethodResolver.MessagePopupCloseExecuted += OnMessagePopupClose;
            Log.LogInfo("ReadTextMod Loaded!");
        }

        private void OnDestroy()
        {
            if (MethodResolver != null)
            {
                MethodResolver.MessagePopupMethodExecuted -= OnMessagePopupMethodExecuted;
                MethodResolver.MessagePopupCloseExecuted -= OnMessagePopupClose;
                
            }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
       
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SceneLoadPatchTriggered)
            {
                Log.LogInfo($"Scene loaded: {scene.name}. Skipping patch attempt as it was already triggered.");
                return;
            }

            Log.LogInfo($"Scene loaded: {scene.name}. Checking for MessagePopup GameObjects.");
            SceneLoadPatchTriggered = true;
            MethodResolver?.TryPatchMethodsNow();
        }


        private void OnMessagePopupClose(object sender, MessagePopupCloseExecutedEventArgs e)
        {
             
            if(AudioSource != null && AudioSource.isPlaying)
            {
                AudioSource.Stop();
                AudioQueue.Clear();
                IsPlayingQueue = false; 
                Log.LogInfo("Audio queue playback stopped");
            }
        }

        private void OnMessagePopupMethodExecuted(object sender, MessagePopupMethodExecutedEventArgs e)
        {
    
           
            if(e.GameObject != null){
                
                List<string> filepaths = EncounterHelpers.KeyInfoResolver(e.Instance);

                if (filepaths.Count > 0)
                {
                    e.Instance.StartCoroutine(LoadAndPlayWrapper(filepaths));
                    
                }else
                {
                    Log.LogInfo("Unable to extract path from KeyInfo");
                }
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
        }

        private static IEnumerator LoadAndPlayWrapper(List<string> filepaths)
        {
            yield return LoadSound(filepaths);
        
            if (!IsPlayingQueue && AudioQueue.Count > 0)
            {
                yield return PlayQueue();
            }
        }
    
    }
}



