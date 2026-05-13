using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the boss encounter triggered at the end of the board.
/// </summary>
public class BossManager : MonoBehaviour
{
    public const string PREF_SKIP_BOSS_ANSWER_DELAY_FOR_TESTS = "SkipBossAnswerDelayForTests";
    private const int BossAnswerFeedbackDelayMs = 350;

    [Tooltip("Pool of available bosses. One is picked at random each run.")]
    public List<Boss> bossPool;
    public UIDocument bossUiDocument;

    private Boss _activeBoss;
    private int _currentQuestionIndex;
    private int _wrongCount;
    private Action _onBossWinComplete;
    private Action _onBossLoseComplete;

    private Button _continueButton;
    private EventCallback<ClickEvent> _continueCallback;
    private EventCallback<ClickEvent> _sensorCallback;

    private int _selectedAnswerIndex;
    private readonly List<Button> _answerButtons = new List<Button>();
    private readonly List<BlueMCQCard> _activeQuestions = new List<BlueMCQCard>();

    private void Start()
    {
        // Check if tests want to skip boss
        if (PlayerPrefs.GetInt("SkipBossForTests", 0) == 1)
        {
            _skipBoss = true;
        }
        
        if (bossUiDocument != null)
            bossUiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private bool _skipBoss = false;

    /// <summary>
    /// Call this in tests to bypass the boss fight entirely and go straight to the final score.
    /// </summary>
    public void SkipBoss() => _skipBoss = true;

    /// <summary>
    /// Single-callback overload: win and lose both invoke <paramref name="onComplete"/>.
    /// </summary>
    public bool StartBossFight(Action onComplete)
    {
        return StartBossFight(onComplete, onComplete);
    }

    /// <summary>
    /// Picks a random boss from the pool, shows its questions one by one.
    /// On win (all correct): calls <paramref name="onWinComplete"/>.
    /// On lose (first wrong): ends battle immediately, shows lose state, then calls <paramref name="onLoseComplete"/>.
    /// Returns false if no bosses or questions are available.
    /// </summary>
    public bool StartBossFight(Action onWinComplete, Action onLoseComplete)
    {
        if (_skipBoss) return false;

        if (bossPool == null || bossPool.Count == 0)
        {
            Debug.LogWarning("BossManager: bossPool is empty.");
            return false;
        }

        _activeBoss = bossPool[UnityEngine.Random.Range(0, bossPool.Count)];

        if (_activeBoss == null || _activeBoss.questions == null || _activeBoss.questions.Count == 0)
        {
            Debug.LogWarning($"BossManager: Boss '{_activeBoss?.bossName}' has no questions.");
            return false;
        }

        // Select a random subset of 3 questions from the boss's pool.
        _activeQuestions.Clear();
        List<BlueMCQCard> pool = new List<BlueMCQCard>(_activeBoss.questions);
        int questionCount = Mathf.Min(3, pool.Count);
        for (int i = 0; i < questionCount; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
            _activeQuestions.Add(pool[i]);
        }

        GameController gc = GetComponent<GameController>();
        if (gc != null && gc.player != null)
        {
            _activeBoss.SetBossPlayer(gc.player, gc.currentPlayerIndex);
            Debug.Log($"Boss battle started for Player {gc.currentPlayerIndex + 1}");
        }

        _currentQuestionIndex = 0;
        _wrongCount = 0;
        _onBossWinComplete = onWinComplete;
        _onBossLoseComplete = onLoseComplete ?? onWinComplete;

        ShowIntro();
        return true;
    }

    private int GetBossAnswerFeedbackDelayMs()
    {
        return PlayerPrefs.GetInt(PREF_SKIP_BOSS_ANSWER_DELAY_FOR_TESTS, 0) == 1
            ? 0
            : BossAnswerFeedbackDelayMs;
    }

    private void ApplyBossLargeText()
    {
        AccessibilitySettingsManager.ApplyLargeTextToDocument(bossUiDocument);
    }

    private void ShowIntro()
    {
        ApplyBossLargeText();
        VisualElement root = bossUiDocument.rootVisualElement;

        root.Q<Label>("boss_name").text = _activeBoss.bossName;
        root.Q<Label>("boss_challenge").text = $"{_activeBoss.bossName} challenges you!";

        var bossImage = root.Q<UnityEngine.UIElements.Image>("boss_image");
        if (bossImage != null) bossImage.image = _activeBoss.bossImage != null ? _activeBoss.bossImage.texture : null;

        root.Q<VisualElement>("intro_slide").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("question_slide").style.display = DisplayStyle.None;
        root.Q<VisualElement>("feedback_slide").style.display = DisplayStyle.None;

        Button beginBtn = root.Q<Button>("intro_begin");
        SwapContinueCallback(beginBtn, _ => ShowQuestion(0));

        root.style.display = DisplayStyle.Flex;
    }

    private void ShowQuestion(int index)
    {
        ApplyBossLargeText();
        BlueMCQCard card = _activeQuestions[index];
        VisualElement root = bossUiDocument.rootVisualElement;

        // Populate header and body
        root.Q<Label>("card_title").text = $"{_activeBoss.bossName} ({index + 1}/{_activeQuestions.Count})";
        var cardImage = root.Q<UnityEngine.UIElements.Image>("card_image");
        if (cardImage != null) cardImage.image = card.image != null ? card.image.texture : null;
        root.Q<Label>("card_statement").text = card.statement;
        root.Q<Label>("card_question").text = card.question;

        // Set up the scientist sensor button for this question
        SetupSensorButton(root, card);

        // Show question slide, hide others
        root.Q<VisualElement>("intro_slide").style.display = DisplayStyle.None;
        root.Q<VisualElement>("question_slide").style.display = DisplayStyle.Flex;
        root.Q<VisualElement>("feedback_slide").style.display = DisplayStyle.None;

        // Build answer buttons
        VisualElement answersRoot = root.Q<VisualElement>("answers_element");
        answersRoot.Clear();
        _answerButtons.Clear();
        _selectedAnswerIndex = -1;

        answersRoot.style.flexDirection = FlexDirection.Row;
        answersRoot.style.justifyContent = Justify.Center;
        answersRoot.style.alignItems = Align.Stretch;
        answersRoot.style.flexWrap = Wrap.Wrap;

        answersRoot.RemoveFromClassList("answers-two");
        answersRoot.RemoveFromClassList("answers-many");

        if (card.answers.Count <= 2)
            answersRoot.AddToClassList("answers-two");
        else
            answersRoot.AddToClassList("answers-many");

        for (int i = 0; i < card.answers.Count; i++)
        {
            int capturedIndex = i;
            Button btn = new Button { text = card.answers[i].answer };
            btn.AddToClassList("answer-option");
            btn.RegisterCallback<ClickEvent>(_ => SelectAnswer(capturedIndex, card));
            _answerButtons.Add(btn);
            answersRoot.Add(btn);
        }

        root.style.display = DisplayStyle.Flex;
    }

    private void SetupSensorButton(VisualElement root, BlueMCQCard card)
    {
        Button sensorButton = root.Q<Button>("sensor_button");
        if (sensorButton == null) return;

        if (_sensorCallback != null)
            sensorButton.UnregisterCallback(_sensorCallback);

        GameController gc = GetComponent<GameController>();
        if (gc != null && gc.player.heroType == HeroType.Scientist && gc.player.forecastingUses > 0)
        {
            sensorButton.style.display = DisplayStyle.Flex;
            sensorButton.tooltip = $"Use Sensor ({gc.player.forecastingUses} left)";

            _sensorCallback = _ =>
            {
                int correctIndex = card.answers.FindIndex(a => a.correctAnswer);
                if (correctIndex >= 0 && correctIndex < _answerButtons.Count)
                    _answerButtons[correctIndex].AddToClassList("sensor-highlight");

                gc.player.forecastingUses--;
                sensorButton.style.display = DisplayStyle.None;
            };
            sensorButton.RegisterCallback(_sensorCallback);
        }
        else
        {
            sensorButton.style.display = DisplayStyle.None;
        }
    }

    private void SelectAnswer(int index, BlueMCQCard card)
    {
        _selectedAnswerIndex = index;

        for (int i = 0; i < _answerButtons.Count; i++)
        {
            if (i == _selectedAnswerIndex)
                _answerButtons[i].AddToClassList("answer-selected");
            else
                _answerButtons[i].RemoveFromClassList("answer-selected");
        }

        SubmitAnswer(card);
    }

    private void SubmitAnswer(BlueMCQCard card)
    {
        if (_selectedAnswerIndex < 0 || _selectedAnswerIndex >= card.answers.Count) return;

        foreach (var btn in _answerButtons) btn.SetEnabled(false);

        MCQAnswer chosen = card.answers[_selectedAnswerIndex];
        bool isCorrect = chosen.correctAnswer;
        string correctAnswerText = GetCorrectAnswerText(card);

        for (int i = 0; i < _answerButtons.Count; i++)
        {
            _answerButtons[i].RemoveFromClassList("answer-correct");
            _answerButtons[i].RemoveFromClassList("answer-wrong");
        }

        if (isCorrect)
        {
            _answerButtons[_selectedAnswerIndex].AddToClassList("answer-correct");
        }
        else
        {
            _answerButtons[_selectedAnswerIndex].AddToClassList("answer-wrong");

            for (int i = 0; i < card.answers.Count; i++)
            {
                if (card.answers[i].correctAnswer)
                {
                    _answerButtons[i].AddToClassList("answer-correct");
                    break;
                }
            }
        }

        if (!isCorrect)
        {
            _wrongCount++;
            int delayMs = GetBossAnswerFeedbackDelayMs();
            if (delayMs <= 0)
            {
                ShowImmediateLossFeedback(chosen.messageWhenChosen, correctAnswerText);
                return;
            }

            bossUiDocument.rootVisualElement.schedule.Execute(() =>
            {
                ShowImmediateLossFeedback(chosen.messageWhenChosen, correctAnswerText);
            }).ExecuteLater(delayMs);
            return;
        }

        int feedbackDelayMs = GetBossAnswerFeedbackDelayMs();
        if (feedbackDelayMs <= 0)
        {
            ShowFeedback(chosen.messageWhenChosen);
            return;
        }

        bossUiDocument.rootVisualElement.schedule.Execute(() =>
        {
            ShowFeedback(chosen.messageWhenChosen);
        }).ExecuteLater(feedbackDelayMs);
    }

    private string GetCorrectAnswerText(BlueMCQCard card)
    {
        if (card == null || card.answers == null) return "";

        int correctIndex = card.answers.FindIndex(a => a.correctAnswer);
        if (correctIndex < 0 || correctIndex >= card.answers.Count) return "";

        return card.answers[correctIndex].answer ?? "";
    }

    private void ShowFeedback(string reason)
    {
        VisualElement root = bossUiDocument.rootVisualElement;
        root.Q<VisualElement>("question_slide").style.display = DisplayStyle.None;
        root.Q<VisualElement>("feedback_slide").style.display = DisplayStyle.Flex;

        root.Q<Label>("feedback_result").text = "Correct!";
        root.Q<Label>("feedback_reason").text = reason;

        bool isLastQuestion = _currentQuestionIndex >= _activeQuestions.Count - 1;
        Button continueBtn = root.Q<Button>("feedback_continue");
        continueBtn.text = isLastQuestion ? "See Results" : "Next Question";

        SwapContinueCallback(continueBtn, isLastQuestion
            ? (EventCallback<ClickEvent>)(_ => ShowBossResult())
            : (_ => { _currentQuestionIndex++; ShowQuestion(_currentQuestionIndex); }));
    }

    private void ShowImmediateLossFeedback(string reason, string correctAnswerText)
    {
        // Apply boss loss penalty immediately when the first wrong answer is submitted.
        _activeBoss.OnBossFailure(_wrongCount);

        VisualElement root = bossUiDocument.rootVisualElement;
        root.Q<VisualElement>("question_slide").style.display = DisplayStyle.None;
        root.Q<VisualElement>("feedback_slide").style.display = DisplayStyle.Flex;

        root.Q<Label>("feedback_result").text = "That is incorrect.";
        if (string.IsNullOrWhiteSpace(correctAnswerText))
        {
            root.Q<Label>("feedback_reason").text = reason;
        }
        else if (string.IsNullOrWhiteSpace(reason))
        {
            root.Q<Label>("feedback_reason").text = $"Correct answer: {correctAnswerText}";
        }
        else
        {
            root.Q<Label>("feedback_reason").text = $"{reason}\nCorrect answer: {correctAnswerText}";
        }

        Button continueBtn = root.Q<Button>("feedback_continue");
        continueBtn.text = "Return to Board";
        SwapContinueCallback(continueBtn, _ => CompleteBossFight(false));
    }

    private void ShowBossResult()
    {
        bool success = _wrongCount == 0;

        if (success)
            _activeBoss.OnBossSuccess();
        else
            _activeBoss.OnBossFailure(_wrongCount);

        VisualElement root = bossUiDocument.rootVisualElement;
        root.Q<VisualElement>("question_slide").style.display = DisplayStyle.None;
        root.Q<VisualElement>("feedback_slide").style.display = DisplayStyle.Flex;

        if (success)
        {
            root.Q<Label>("feedback_result").text = $"{_activeBoss.bossName} Defeated!";
            root.Q<Label>("feedback_reason").text = "You answered every question correctly! -3 Pollution.";
        }
        else
        {
            root.Q<Label>("feedback_result").text = $"{_activeBoss.bossName} Wins!";
            root.Q<Label>("feedback_reason").text =
                $"You got {_wrongCount} question(s) wrong. +{_wrongCount} Pollution.";
        }

        Button continueBtn = root.Q<Button>("feedback_continue");
        continueBtn.text = "See Final Score";

        SwapContinueCallback(continueBtn, _ => CompleteBossFight(success));
    }

    private void CompleteBossFight(bool won)
    {
        Debug.Log($"CompleteBossFight called with won={won}");
        bossUiDocument.rootVisualElement.style.display = DisplayStyle.None;
        if (won)
        {
            Debug.Log("Invoking OnBossWinComplete callback");
            _onBossWinComplete?.Invoke();
        }
        else
        {
            Debug.Log("Invoking OnBossLoseComplete callback");
            _onBossLoseComplete?.Invoke();
        }
    }

    /// <summary>
    /// Replaces the continue button's ClickEvent callback safely,
    /// unregistering the previous one first to avoid stacking handlers.
    /// </summary>
    private void SwapContinueCallback(Button btn, EventCallback<ClickEvent> newCallback)
    {
        if (_continueCallback != null && _continueButton != null)
            _continueButton.UnregisterCallback(_continueCallback);

        _continueButton = btn;
        _continueCallback = newCallback;
        btn.RegisterCallback(_continueCallback);
    }
}
