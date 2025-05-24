using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;



namespace JIME_TTS_MOD
{

    [BepInPlugin("GedVed.JIME_TTS", "JIME TTS Mod", "1.0.0")]
    public class JIME_TTS : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        private static AudioSource AudioSource;
        private static string AudioFolder = Path.Combine(Paths.PluginPath, "JIME_TTS\\TTS");
        private static bool IsPlayingQueue = false;
        private static bool HasStartedLoading = false;
        private static Queue<AudioClip> AudioQueue = [];
        private static JIME_TTS Instance;
        

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Log.LogWarning("Another instance of JIME_TTS already exists. Destroying this duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            var harmony = new Harmony("GedVed.JIME_TTS");
            Log = Logger;
            //AudioSource
            GameObject audioObj = new GameObject("SoundPlayer");
            AudioSource = audioObj.AddComponent<AudioSource>();
            DontDestroyOnLoad(audioObj);
            //Harmony Patch
            MethodPatcher.Initialize(harmony, this);
            harmony.PatchAll();
            //Event Coordinator
            EventCoordinator.Initialize(this);
            //Events
            EventCoordinator.Instance.MessagePopupMethodExecuted += (sender, e) => EventCoordinator.Instance.WrapMessagePopup(sender, e, OnMessagePopupMethodExecuted);
            EventCoordinator.Instance.TerrainNodesExecuted += EventCoordinator.Instance.WrapTerrainNodes;
            EventCoordinator.Instance.MessagePopupCloseExecuted += OnMessagePopupClose;
            EventCoordinator.Instance.UIMapExecuted += OnUIMapDisplay;
        
            Log.LogInfo("JIME_TTS Loaded!");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Log.LogInfo("Destroying ReadText instance.");
                if (EventCoordinator.Instance != null)
                {
                    EventCoordinator.Instance.MessagePopupMethodExecuted -= OnMessagePopupMethodExecuted;
                    EventCoordinator.Instance.MessagePopupCloseExecuted -= OnMessagePopupClose;
                    EventCoordinator.Instance.UIMapExecuted -= OnUIMapDisplay;
                    EventCoordinator.Instance.TerrainNodesExecuted -= EventCoordinator.Instance.WrapTerrainNodes;
                }
                Instance = null;
            }
        }

        private void OnUIMapDisplay(object sender, UIMapExecutedEventArgs e)
        {

            StopPlayback();
            if (e.GameObject != null && !string.IsNullOrEmpty(e.LocalizationPacket?.key))
            {
                e.Instance?.StartCoroutine(LoadAndPlayWrapper(new Queue<string>(new[] { e.LocalizationPacket.key })));
            }
            else
            {
                Log.LogInfo("Unable to extract path from UIMap key");
            }

        }

        private void OnMessagePopupClose(object sender, MessagePopupCloseExecutedEventArgs e)
        {

            StopPlayback();
        }

        private void OnMessagePopupMethodExecuted(object sender, MessagePopupMethodExecutedEventArgs e)
        {

            if (e.GameObject != null && e.Instance != null)
            {
                List<string> filepaths = EncounterHelpers.KeyInfoResolver(e.Instance, e.LocalizationPacket, e.GameNodes);

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
            if (AudioSource != null && AudioSource.isPlaying)
            {
                AudioSource.Stop();
                AudioQueue.Clear();
                IsPlayingQueue = false;
                Log.LogInfo("Audio queue playback stopped");
            }
        }

    }
}

