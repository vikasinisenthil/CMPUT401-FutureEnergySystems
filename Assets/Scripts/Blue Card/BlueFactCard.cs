using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "BlueFactCard", menuName = "Cards/BlueFactCard")]
public class BlueFactCard : BlueCard {
    public string fact;

    private TextToSpeech tts = null;

    public override UIDocument GetUiDocument() {
        UIDocument doc = GameObject.Find("BlueFactCardUIDocument").GetComponent<UIDocument>();
        AccessibilitySettingsManager.ApplyLargeTextToDocument(doc);
        VisualElement root = doc.rootVisualElement;
        
        root.Q<Label>("card_title").text = cardName;
        root.Q<Image>("card_image").image = image.texture;
        root.Q<Label>("card_fact").text = fact;
        
        Button closeButton = root.Q<Button>("close_button");
        closeButton.clicked -= OnCloseButtonClicked;
        closeButton.clicked += OnCloseButtonClicked;
        
        // Text to Speech
        List<String> ignoreList = new List<String>();
        Button ttsButton = root.Q<Button>("speaker_button");
        tts = new TextToSpeech(doc, ignoreList, "speaker_button");

        ttsButton.clickable = new Clickable(()=>{ });
        ttsButton.clicked -= PressTTS;
        ttsButton.clicked += PressTTS;

        return doc;
    }

    private void PressTTS()
    {
        tts.Press();
    }

    private void OnCloseButtonClicked() {
        UIDocument doc = GameObject.Find("BlueFactCardUIDocument").GetComponent<UIDocument>();
        VisualElement root = doc.rootVisualElement;

        GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
        if (GameManager.Instance != null && GameManager.Instance.Mode == GameMode.Multiplayer) {
            ScoreManager.Instance.AddScoreToPlayer(gc.player, -1, "Blue fact card bonus");
        } else {
            ScoreManager.Instance.AddScore(-1, "Blue fact card bonus");
        }

        root.style.display = DisplayStyle.None;
        AudioManager.Instance.PlayConfirm();
        GameObject.Find("InGameUIDocument").GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;
        
        Button closeButton = root.Q<Button>("close_button");
        closeButton.clicked -= OnCloseButtonClicked;

        if (gc != null) gc.ResumeGameAfterMinigame(); // re-enabling dice

        tts.ShutDown();
    }
}