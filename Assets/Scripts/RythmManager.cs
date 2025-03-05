using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RhythmManager : MonoBehaviour {
    public static RhythmManager Instance;

    [Header("Beat Settings")]
    [Tooltip("Seconds per beat")]
    public double beatInterval;
    [Tooltip("Acceptable window for an action to be considered on-beat")]
    public float beatWindow = 0.3f;
    [Tooltip("Number of consecutive valid actions needed for a power-up attack")]
    public int requiredStreak = 4;
    [Tooltip("Tracks consecutive on-beat actions")]
    public int streak;
    private bool lastActionWasAttack;
    [Tooltip("0 - Miss, 1 - First Window, 2 - Second Window")]
    [SerializeField] int lastBeatPlace;
    [Tooltip("Time when the player hit the last time")]
    [SerializeField] double lastHitTime;
    [Tooltip("When the nexct beat will drop")]
    private double nextBeatTime;
    [Tooltip("When the last beat droppped")]
    private double lastBeatTime;
    [SerializeField] private double nextBeatTimeCheck;
    [SerializeField] private double lastBeatTimeCheck;
    [Tooltip("Check the time when the player hit this time")]
    public double timeHit;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip beatSound;
    [SerializeField] [Range(0, 1)] private float soundVolume = 0.25f;
    private AudioSource audioSource;

    public bool usePowerAttack { get; private set; } = false;
    public event EventHandler OnPowerAttack;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        if (Metronome.Instance == null) {
            Debug.LogError("Metronome instance is missing from the scene!");
            return;
        }
        
        Metronome.Instance.StartMetronome();
        Metronome.Instance.OnBeat += OnBeatTriggered;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        lastBeatTime = Metronome.Instance.nextTickTime - Metronome.Instance.TickInterval;
        nextBeatTime = Metronome.Instance.nextTickTime;
        beatInterval = Metronome.Instance.TickInterval;
        streak = 0;
    }

    void OnBeatTriggered(int beatNumber, double scheduledTime) {
        lastBeatTime = scheduledTime;
        nextBeatTime = scheduledTime + beatInterval;
    }

    void OnDestroy() {
        // Unsubscribe from event to avoid memory leaks
        if (Metronome.Instance != null) {
            Metronome.Instance.OnBeat -= OnBeatTriggered;
        }
    }

    // Determines if the current time is within the valid window for the most recent beat
    public bool IsOnBeat() {
        timeHit = AudioSettings.dspTime;
        lastBeatTimeCheck = lastBeatTime;
        nextBeatTimeCheck = nextBeatTime;

        // Sometimes it registers as a hit just after the next beat, meaning the hit registered before the beat
        if(timeHit > nextBeatTime){
            timeHit = nextBeatTime;
        }

        double nextBeatStartWindow = nextBeatTime - beatWindow;
        double lastBeatEndWindow = lastBeatTime + beatWindow;

        bool withinStartWindow = timeHit >= lastBeatTime && timeHit <= lastBeatEndWindow;
        bool withinEndWindow = timeHit >= nextBeatStartWindow && timeHit <= nextBeatTime;

        // A miss
        if(!withinStartWindow && !withinEndWindow){
            lastBeatPlace = 0;
            lastHitTime = AudioSettings.dspTime;
            return false;
        }
        
        if(withinStartWindow){
            double previousBeatTime = lastBeatTime - beatInterval;
            // If last hit was during the first window
            if(lastBeatPlace == 1){
                double previousBeatEndWindow = previousBeatTime + beatWindow;
                // It must have come from the previous beat only
                bool lastHitWithinPreviousFirstWindow = lastHitTime >= previousBeatTime && lastHitTime <= previousBeatEndWindow;
                // So it is is outside of the previous last window, the streak resets
                if(!lastHitWithinPreviousFirstWindow){
                    ResetStreak();
                }
            }
            // If last hit was during the second window
            else if(lastBeatPlace == 2){
                double previousBeatStartWindow = previousBeatTime - beatWindow;
                bool lastHitWithinPreviousSecondWindow = lastHitTime >= previousBeatStartWindow && lastHitTime <= previousBeatTime;
                if(!lastHitWithinPreviousSecondWindow){
                    ResetStreak();
                }
            }
            lastBeatPlace = 1;
            lastHitTime = AudioSettings.dspTime;
            return true;
        }

        if(withinEndWindow){
            // If the player attacked too fast during the same beatWindow
            if(lastHitTime >= (nextBeatTime - beatWindow)){
                ResetStreak();
            }
            // If the player missed the last beat, the streak must reset
            else{
                double previousBeatStartTime = lastBeatTime - beatWindow;
                double previousEndTime = lastBeatTime + beatWindow;
                bool withinLastBeatRange = lastHitTime >= previousBeatStartTime && lastHitTime <= previousEndTime;
                if(!withinLastBeatRange){
                    ResetStreak();
                }
            }
            
            lastBeatPlace = 2;
            lastHitTime = AudioSettings.dspTime;
            return true;
        }

        Debug.Log("Not supposed to be here");
        return false;
    }

    // Call this when a player action occurs.
    // isAttack should be true for attacks, false for other actions (jumps, dashes, etc.).
    public bool RegisterAction(bool isAttack) {
        if (IsOnBeat()) {
            SuccessfulHit(isAttack);
            return true;
        }
        else {
            ResetStreak();
            return false;
        }
    }

    private void SuccessfulHit(bool isAttack) {
        streak++;
        lastActionWasAttack = isAttack;
        if (streak >= requiredStreak && lastActionWasAttack) {
            PerformPowerAttack();
        }
    }

    private void PerformPowerAttack() {
        Debug.Log("POWER ATTACK");
        OnPowerAttack?.Invoke(this, EventArgs.Empty);
        usePowerAttack = true;
        ResetStreak();
    }

    private void ResetStreak() {
        streak = 0;
        lastActionWasAttack = false;
    }

    public void ResetPowerAttack(){
        usePowerAttack = false;
    }
}