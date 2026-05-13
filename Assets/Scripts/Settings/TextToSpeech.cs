using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

class ElementToRead
{
    public ElementToRead(string m, float yPos, float xPos)
    {
        message = m;
        y = yPos;
        x = xPos;
    }

    public string message;
    public float y;
    public float x;
}

public class TextToSpeech
{
    public TTS tts;
    public TextToSpeech(UIDocument document, List<String> ignoreList, string speakerButtonStrName)
    {
        uiDoc = document;
        ignore = ignoreList;
        speakerButtonStringName = speakerButtonStrName;

        ignore.Add("Read Aloud");
    }

    public void Press()
    {
        if (reading)
        {
            Stop();

        } else
        {
            Start();
        }
    }

    public void ShutDown()
    {
        Stop();
    }

    private void Start()
    {
        EnableRead();

        var textElements = uiDoc.rootVisualElement.Query<TextElement>().ToList();
        var toRead = new List<ElementToRead>();

        foreach (var element in textElements)
        {
            if (element.style.display == DisplayStyle.None) continue;

            bool parentVisible = true;
            var parent = element.parent;
            while (true)
            {
                if (parent == uiDoc.rootVisualElement) break;

                if (parent.style.display == DisplayStyle.None) {
                    parentVisible = false; 
                    break;
                }

                parent = parent.parent;
            }

            if (!string.IsNullOrEmpty(element.text))
            {
                if (ignore.Contains(element.text)) continue;

                toRead.Add(new ElementToRead(element.text, element.worldBound.y, element.worldBound.x));
            }
        }

        if (uiDoc.rootVisualElement.style.display == DisplayStyle.None) toRead.Clear();

        toRead = toRead
            .OrderBy(e => e.y)
            // .ThenBy(e => e.x)
            .ToList();

        foreach (var e in toRead)
        {
            Debug.Log("Reading: " + e.message);
            ReadAloud(e.message);
        }

        if (tts == null) tts = GameObject.Find("TTS").GetComponent<TTS>();
        ttsKey = tts.AddCallback(tts =>
        {
            if (!tts.speaking && !tts.pending)
            {
                DisableRead();
            }
        });
    }

    private void Stop()
    {
        DisableRead();

        if (tts == null) tts = GameObject.Find("TTS").GetComponent<TTS>();
        tts.Stop();
    }

    private void ReadAloud(String text)
    {
        if (tts == null) tts = GameObject.Find("TTS").GetComponent<TTS>();
        tts.Speak(text);
    }

    private void EnableRead()
    {
        reading = true;
        uiDoc.rootVisualElement.Q<Button>(speakerButtonStringName).AddToClassList("speaker-button-reading");
    }

    private void DisableRead()
    {
        reading = false;
        uiDoc.rootVisualElement.Q<Button>(speakerButtonStringName).RemoveFromClassList("speaker-button-reading");
    }

    private string speakerButtonStringName;
    private int ttsKey = -1;
    private UIDocument uiDoc;
    private List<String> ignore;
    private bool reading = false;
}
