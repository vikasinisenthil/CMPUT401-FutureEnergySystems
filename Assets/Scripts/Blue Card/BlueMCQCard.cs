using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

[System.Serializable]
public class MCQAnswer
{
    public string answer;
    public string messageWhenChosen;
    public bool correctAnswer;
}

[CreateAssetMenu(fileName = "BlueMCQCard", menuName = "Cards/BlueMCQCard")]
public class BlueMCQCard : BlueCard
{
    public string statement;
    public string question;
    public List<MCQAnswer> answers;
    private bool WasCorrect = true;

    // GBM.09: Keep selection state so the player can change their mind before submitting
    private int selectedIndex = -1;
    private readonly List<Button> answerButtons = new List<Button>();
    private Button sensorButton;
    private System.Action sensorClickHandler;
    private VisualElement questionSlide;
    private VisualElement feedbackSlide;
    private Label feedbackResultLabel;
    private Label feedbackReasonLabel;
    private Button continueButton;
    private Button feedbackSlideTTSButton;
    private bool sensorUsedThisCard = false;
    private TextToSpeech tts = null;
    private TextToSpeech feedbackTTS = null;

    private static EventCallback<ClickEvent> continueCallback;
    private static BlueMCQCard currentCard;

    public override UIDocument GetUiDocument()
    {
        currentCard = this;

        UIDocument doc = GameObject.Find("BlueMCQCardUIDocument").GetComponent<UIDocument>();
        AccessibilitySettingsManager.ApplyLargeTextToDocument(doc);
        VisualElement root = doc.rootVisualElement;

        root.Q<Label>("card_title").text = cardName;
        root.Q<Image>("card_image").image = image != null ? image.texture : null;
        root.Q<Label>("card_statement").text = statement;
        root.Q<Label>("card_question").text = question;

        questionSlide = root.Q<VisualElement>("question_slide");
        feedbackSlide = root.Q<VisualElement>("feedback_slide");
        feedbackResultLabel = root.Q<Label>("feedback_result");
        feedbackReasonLabel = root.Q<Label>("feedback_reason");
        continueButton = root.Q<Button>("feedback_continue");
        feedbackSlideTTSButton = root.Q<Button>("feedback_speaker_button");

        if (continueButton != null)
        {
            // Unregister old callback if it exists
            if (continueCallback != null)
            {
                continueButton.UnregisterCallback(continueCallback);
            }

            continueCallback = evt =>
            {
                if (feedbackTTS != null) {
                    feedbackTTS.ShutDown();
                    feedbackTTS = null;
                }
                if (currentCard == null) return;

                root.style.display = DisplayStyle.None;
                GameObject.Find("InGameUIDocument").GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.Flex;

                GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
                if (!currentCard.WasCorrect)
                {
                    gc.MovePlayerOneStepBackwards();
                }
                gc.ResumeGameAfterMinigame(); // Re-enable dice (e.g. after Cyclist bonus landed on blue)
            };

            continueButton.RegisterCallback(continueCallback);
        }

        VisualElement answersRoot = root.Q<VisualElement>("answers_element");
        BuildAnswersUI(root, answersRoot, doc);
        sensorUsedThisCard = false; 
        SetupSensorButton(root);

        // Text to Speech
        List<String> ignoreList = new List<String>();
        ignoreList.Add("Awesome!");
        ignoreList.Add("Good luck next time!");
        ignoreList.Add("Sensor");
        Button ttsButton = root.Q<Button>("speaker_button");

        tts = new TextToSpeech(doc, ignoreList, "speaker_button");

        ttsButton.clickable = new Clickable(()=>{ });
        ttsButton.clicked += () =>
        {
            tts.Press();
        };

        return doc;
    }

private void BuildAnswersUI(VisualElement root, VisualElement answersRoot, UIDocument doc)
{
    selectedIndex = -1;
    answerButtons.Clear();
    answersRoot.Clear();

    if (questionSlide != null) questionSlide.style.display = DisplayStyle.Flex;
    if (feedbackSlide != null) feedbackSlide.style.display = DisplayStyle.None;
    if (feedbackResultLabel != null) feedbackResultLabel.text = "";
    if (feedbackReasonLabel != null) feedbackReasonLabel.text = "";

    // Layout for answer buttons
    answersRoot.style.flexDirection = FlexDirection.Row;
    answersRoot.style.justifyContent = Justify.Center;
    answersRoot.style.alignItems = Align.Center;
    answersRoot.style.flexWrap = Wrap.Wrap;

    answersRoot.RemoveFromClassList("answers-two");
    answersRoot.RemoveFromClassList("answers-many");

    if (answers.Count <= 2)
        answersRoot.AddToClassList("answers-two");
    else
        answersRoot.AddToClassList("answers-many");

    for (int i = 0; i < answers.Count; i++)
    {
        int index = i;
        Button button = new Button();
        button.text = answers[index].answer;
        button.AddToClassList("answer-option");
        button.style.fontSize = new StyleLength(StyleKeyword.Null);

        button.RegisterCallback<ClickEvent>(_ => AnswerClicked(index, doc));
        answerButtons.Add(button);
        answersRoot.Add(button);
    }
}

    private void AnswerClicked(int index, UIDocument doc)
    {
        selectedIndex = index;

        // visually indicate selected option.
        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (i == selectedIndex)
                answerButtons[i].AddToClassList("answer-selected");
            else
                answerButtons[i].RemoveFromClassList("answer-selected");
        }

        SubmitSelectedAnswer(doc);
    }

    private void SubmitSelectedAnswer(UIDocument doc)
    {
        GameController gc = GameObject.Find("GameController").GetComponent<GameController>();
        if (selectedIndex < 0 || selectedIndex >= answers.Count) return;

        foreach (var b in answerButtons) b.SetEnabled(false);

        MCQAnswer chosen = answers[selectedIndex];
        bool isCorrect = chosen.correctAnswer;
        WasCorrect = isCorrect;

        // Show immediate visual correctness feedback on answer buttons.
        for (int i = 0; i < answerButtons.Count; i++)
        {
            answerButtons[i].RemoveFromClassList("answer-correct");
            answerButtons[i].RemoveFromClassList("answer-wrong");
        }

        if (isCorrect)
        {
            answerButtons[selectedIndex].AddToClassList("answer-correct");
        }
        else
        {
            answerButtons[selectedIndex].AddToClassList("answer-wrong");
            for (int i = 0; i < answers.Count; i++)
            {
                if (answers[i].correctAnswer)
                {
                    answerButtons[i].AddToClassList("answer-correct");
                    break;
                }
            }
        }

        // score update (adjust reasons if your ScoreManager supports it)
        // In SubmitSelectedAnswer method:
        if (isCorrect)  {
            if (GameManager.Instance.Mode == GameMode.Multiplayer) {
                ScoreManager.Instance.AddScoreToPlayer(gc.player, -1, "Correct answer");
            } else {
                ScoreManager.Instance.AddScore(-1, "Correct answer");
            }
        } else {
            if (GameManager.Instance.Mode == GameMode.Multiplayer) {
                ScoreManager.Instance.AddScoreToPlayer(gc.player, 1, "Incorrect answer");
            } else {
                ScoreManager.Instance.AddScore(1, "Incorrect answer");
            }
        }

        if (feedbackResultLabel != null)
            feedbackResultLabel.text = isCorrect ? "Correct!" : "That is incorrect.";

        if (feedbackReasonLabel != null)
            feedbackReasonLabel.text = chosen.messageWhenChosen;

        if (continueButton != null)
            continueButton.text = isCorrect ? "Awesome!" : "Good luck next time!";

        

        // stats
        if (isCorrect) gc.MCQQuestionAnsweredCorrectly();
        else gc.MCQQuestionAnsweredIncorrectly();

        // Briefly keep question slide visible so correct/wrong colors can be seen.
        if (questionSlide != null)
        {
            questionSlide.schedule.Execute(() =>
            {
                if (questionSlide != null) questionSlide.style.display = DisplayStyle.None;
                if (feedbackSlide != null) feedbackSlide.style.display = DisplayStyle.Flex;
            }).ExecuteLater(350);
        }
        else
        {
            if (questionSlide != null) questionSlide.style.display = DisplayStyle.None;
            if (feedbackSlide != null) feedbackSlide.style.display = DisplayStyle.Flex;
        }

        tts.ShutDown();

        List<String> ttsIgnoreList = new List<String>();
        ttsIgnoreList.Add("Sensor");
        foreach (var a in answers)
        {
            ttsIgnoreList.Add(a.answer);
        }
        ttsIgnoreList.Add(statement);
        ttsIgnoreList.Add(question);
        ttsIgnoreList.Add(cardName);
        feedbackTTS = new TextToSpeech(doc, ttsIgnoreList, "feedback_speaker_button");

        feedbackSlideTTSButton.clickable = new Clickable(()=>{ });
        feedbackSlideTTSButton.clicked += () =>
        {
            feedbackTTS.Press();
        };
    }

    private void SetupSensorButton(VisualElement root)
    {
        GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
        if (gc == null) return;

        sensorButton = root.Q<Button>("sensor_button");

        // Fallback: if the UXML button wasn't found for some reason
        if (sensorButton == null)
        {
            sensorButton = new Button();
            sensorButton.name = "sensor_button";
            sensorButton.AddToClassList("sensor-button");

            VisualElement answersRoot = root.Q<VisualElement>("answers_element");
            if (answersRoot != null && answersRoot.parent != null)
            {
                answersRoot.parent.Insert(answersRoot.parent.IndexOf(answersRoot), sensorButton);
            }
        }

        if (sensorClickHandler != null)
        {
            sensorButton.clicked -= sensorClickHandler;
            sensorClickHandler = null;
        }

        if (gc.player.heroType != HeroType.Scientist)
        {
            sensorButton.style.display = DisplayStyle.None;
            return;
        }

        // Scientist: ALWAYS show the button
        // but enable/disable based on uses remaining
        sensorButton.style.display = DisplayStyle.Flex;
        sensorButton.text = "";

        if (gc.player.forecastingUses <= 0 || sensorUsedThisCard)
        {
            sensorButton.style.display = DisplayStyle.None;
            return;
        }

        sensorButton.SetEnabled(true);
        sensorButton.tooltip = $"Use Sensor ({gc.player.forecastingUses} left)";

        sensorClickHandler = () => UseSensor();
        sensorButton.clicked += sensorClickHandler;
    }

    private void UseSensor()
    {
        GameController gc = GameObject.Find("GameController")?.GetComponent<GameController>();
        if (gc == null || sensorButton == null) return;

        if (sensorUsedThisCard) return;
        sensorUsedThisCard = true; 

        if (gc.player.forecastingUses <= 0)
        {
            sensorButton.SetEnabled(false);
            sensorButton.tooltip = "Sensor (0 left)";
            return;
        }

        int correctIndex = -1;
        for (int i = 0; i < answers.Count; i++)
        {
            if (answers[i].correctAnswer)
            {
                correctIndex = i;
                break;
            }
        }

        if (correctIndex >= 0 && correctIndex < answerButtons.Count)
        {
            Button correctButton = answerButtons[correctIndex];
            correctButton.AddToClassList("sensor-highlight");

            gc.player.forecastingUses = Mathf.Max(0, gc.player.forecastingUses - 1);
            sensorUsedThisCard = true;
            sensorButton.style.display = DisplayStyle.None;

            if (gc.player.forecastingUses > 0)
                sensorButton.tooltip = $"Sensor used ({gc.player.forecastingUses} left)";
            else
                sensorButton.tooltip = "Sensor used (0 left)";

            Debug.Log($"Sensor used! Correct answer highlighted. {gc.player.forecastingUses} uses remaining.");
        }
    }
}