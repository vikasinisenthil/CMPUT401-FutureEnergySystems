using System;
using System.Collections.Generic;
using UnityEngine;

public class FinalScoreLogger : MonoBehaviour
{
    public static FinalScoreLogger Instance { get; private set; }

    [Serializable]
    public class RollEntry
    {
        public int moveNumber;
        public int roll;
        public int startSquare;
        public int endSquare;
        public string timestamp;
    }

    [Serializable]
    public class ScoreEntry
    {
        public int from;
        public int to;
        public int delta;
        public string reason;
        public string timestamp;
    }

    private readonly List<RollEntry> rolls = new();
    private readonly List<ScoreEntry> scoreChanges = new();

    private int moveCounter = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void ResetLog()
    {
        rolls.Clear();
        scoreChanges.Clear();
        moveCounter = 0;
    }

    public int GetMoveCount() => moveCounter;
    public IReadOnlyList<RollEntry> GetRolls() => rolls;
    public IReadOnlyList<ScoreEntry> GetScoreChanges() => scoreChanges;

    public void LogRoll(int roll, int startSquare, int endSquare)
    {
        moveCounter++;
        rolls.Add(new RollEntry
        {
            moveNumber = moveCounter,
            roll = roll,
            startSquare = startSquare,
            endSquare = endSquare
        });
    }

    public void LogScoreChange(int from, int to, int delta, string reason)
    {
        scoreChanges.Add(new ScoreEntry
        {
            from = from,
            to = to,
            delta = delta,
            reason = string.IsNullOrWhiteSpace(reason) ? "Score updated" : reason,
        });
    }
}
