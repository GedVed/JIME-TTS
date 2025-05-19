using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;
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
        private static Queue<AudioClip> AudioQueue = [];
        public static MethodResolver MethodResolver = null;


        public static List<string> enemyActivations = new List<string>{"ENEMY_GOBLIN_ACTIVATION","ENEMY_RUFFIAN_ACTIVATION","ENEMY_ORC_MARAUDER_ACTIVATION","ENEMY_ORC_HUNTER_ACTIVATION",
            "ENEMY_HUNGRY_WARG_ACTIVATION","ENEMY_WIGHT_ACTIVATION","ENEMY_HILL_TROLL_ACTIVATION","ENEMY_ATARIN_ACTIVATION","ENEMY_ULUK_ACTIVATION","ENEMY_GULGOTAR_ACTIVATION",
            "ENEMY_GIANT_SPIDER_ACTIVATION","ENEMY_PIT_GOBLIN_ACTIVATION","ENEMY_ORC_TASKMASTER_ACTIVATION","ENEMY_SHADOWMAN_ACTIVATION","ENEMY_NAMELESS_THING_ACTIVATION",
            "ENEMY_CAVE_TROLL_ACTIVATION","ENEMY_UNGOLIANT_ACTIVATION","ENEMY_BALROG_ACTIVATION","ENEMY_SOLDIER_ACTIVATION","ENEMY_URUK_ACTIVATION","ENEMY_FELL_BEAST_ACTIVATION",
            "ENEMY_WARG_RIDER_ACTIVATION","ENEMY_SIEGE_ENGINE_ACTIVATION","ENEMY_OLIPHAUNT_ACTIVATION"};


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
            MethodResolver.UIMapExecuted += OnUIMapDisplay;
            Log.LogInfo("ReadTextMod Loaded!");
        }

        private void OnDestroy()
        {
            if (MethodResolver != null)
            {
                MethodResolver.MessagePopupMethodExecuted -= OnMessagePopupMethodExecuted;
                MethodResolver.MessagePopupCloseExecuted -= OnMessagePopupClose;
                MethodResolver.UIMapExecuted -= OnUIMapDisplay;
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
            MethodResolver?.TryPatchMessageMethodsNow();
        }

        private void OnUIMapDisplay(object sender, UIMapExecuted e)
        {

            StopPlayback();

            List<string> filepaths = [];
            if (e.GameObject != null)
            {
                if (!string.IsNullOrEmpty(e.LocalizationPacket.key))
                {
                    filepaths.Add(e.LocalizationPacket.key);
                }

                if (filepaths.Count > 0)
                {
                    e.Instance.StartCoroutine(LoadAndPlayWrapper(new Queue<string>(filepaths)));

                }
                else
                {
                    Log.LogInfo("Unable to extract path from key");
                }
            }
        }
        private void OnMessagePopupClose(object sender, MessagePopupCloseExecutedEventArgs e)
        {

            StopPlayback();
        }

        private void OnMessagePopupMethodExecuted(object sender, MessagePopupMethodExecutedEventArgs e)
        {
            if (e.GameObject != null)
            {
                List<string> filepaths = EncounterHelpers.KeyInfoResolver(e.Instance, e.LocalizationPacket);

                if (filepaths.Count > 0)
                {
                    e.Instance.StartCoroutine(LoadAndPlayWrapper(new Queue<string>(filepaths)));

                }
                else
                {
                    Log.LogInfo("Unable to extract path from KeyInfo");
                }
            }
        }

        private static IEnumerator LoadSound(Queue<string> filepaths)
        {
            if (!HasStartedLoading)
            {
                foreach (string path in filepaths)
                {

                    Log.LogInfo($"{path}");

                    string filePath = Path.Combine(AudioFolder, path + ".wav");

                    if (File.Exists(filePath))
                    {

                        UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
                        yield return www.SendWebRequest();

                        if (www.result == UnityWebRequest.Result.Success)
                        {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                            AudioQueue.Enqueue(clip);
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

        private static IEnumerator PlayQueue()
        {
            IsPlayingQueue = true;

            while (AudioQueue.Count > 0)
            {
                AudioClip clip = AudioQueue.Dequeue();
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

        private static IEnumerator LoadAndPlayWrapper(Queue<string> filepaths)
        {
            yield return LoadSound(filepaths);

            if (!IsPlayingQueue && AudioQueue.Count > 0)
            {
                yield return PlayQueue();
            }
        }

        private static void StopPlayback()
        {
             if(AudioSource != null && AudioSource.isPlaying)
            {
                AudioSource.Stop();
                AudioQueue.Clear();
                IsPlayingQueue = false; 
                Log.LogInfo("Audio queue playback stopped");
            }
        }
    
    }
}

