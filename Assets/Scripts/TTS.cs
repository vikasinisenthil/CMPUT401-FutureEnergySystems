using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class TTS : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void JS_Speak(string str, float volume);

    [DllImport("__Internal")]
    private static extern void JS_Pause();

    [DllImport("__Internal")]
    private static extern void JS_Resume();

    [DllImport("__Internal")]
    private static extern void JS_Stop();

    [DllImport("__Internal")]
    private static extern bool JS_Paused();

    [DllImport("__Internal")]
    private static extern bool JS_Pending();

    [DllImport("__Internal")]
    private static extern bool JS_Speaking();

    private const string URL = "http://localhost:3000/";

    IEnumerator LocalSpeak(string str, float volume)
    {
        using (UnityWebRequest req = new UnityWebRequest(URL + "speak", "POST"))
        {
            string data = $"{{\"text\":\"{str}\", \"volume\":\"{volume}\"}}";
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success) {
                Debug.Log("Response: " + req.downloadHandler.text);
            }
            else {
                Debug.LogError("Error: " + req.error);
            }
        }
    }
    IEnumerator PostRequest(string path)
    {
        Debug.Log("Going to path: " + path);
        using (UnityWebRequest req = new UnityWebRequest(URL + path, "POST"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success) {
                Debug.Log("Response: " + req.downloadHandler.text);
            }
            else {
                Debug.LogError("Error: " + req.error);
            }
        }
    }

    public void Speak(string str)
    {
        float volume = VolumeManager.Instance.GetAdjustedTTSVolume();

#if !UNITY_EDITOR
        JS_Speak(str, volume);
#else
        // StartCoroutine(LocalSpeak(str, volume));
        speaking = true; // Approximate real state
#endif
    }

    public void Pause()
    {
#if !UNITY_EDITOR
        JS_Pause();
#else
        // StartCoroutine(PostRequest("pause"));
        paused = true; // Approximate real state
#endif
    }

    public void Resume()
    {
#if !UNITY_EDITOR
        JS_Resume();
#else
        // StartCoroutine(PostRequest("resume"));
        paused = false; // Approximate real state
#endif
    }

    public void Stop()
    {
#if !UNITY_EDITOR
        JS_Stop();
#else
        // StartCoroutine(PostRequest("stop"));
        speaking = false; // Approximate real state
#endif
    }

    public bool Paused()
    {
#if !UNITY_EDITOR
        return JS_Paused();
#else
        return paused;
#endif
    }

    public bool Pending()
    {
#if !UNITY_EDITOR
        return JS_Pending();
#else
        return pending;
#endif
    }

    public bool Speaking()
    {
#if !UNITY_EDITOR
        return JS_Speaking();
#else
        return speaking;
#endif
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public bool speaking = false;
    public bool pending = false;
    public bool paused = false;

    public void Update()
    {
        bool preSpeaking = speaking;
        bool prePending = pending;
        bool prePaused = paused;

        speaking = Speaking();
        pending = Pending();
        paused = Paused();

        if (preSpeaking != speaking || prePaused != paused || prePending != pending)
        {
            foreach (var callback in callbacks)
            {
                callback.Value(this);
            }
        }
    }

    public int AddCallback(Action<TTS> action)
    {
        int key = i;
        callbacks[i] = action;

        ++i;

        return key;
    }

    public void RemoveCallback(int key)
    {
        callbacks.Remove(key);
    }

    private int i = 0;
    private Dictionary<int, Action<TTS>> callbacks = new Dictionary<int, Action<TTS>>();
}
