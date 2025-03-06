using System;
using System.Collections.Generic;
using UnityEngine;

public class Metronome : MonoBehaviour {
    public static Metronome Instance { get; private set; }

    [Header("Metronome Settings")]
    public double bpm = 60.0;
    public AudioClip tickSound;
    public int poolSize = 8;

    public event Action<int, double> OnBeat; // Now includes scheduled time

    private AudioSource[] audioSources;
    private int currentSourceIndex = 0;
    public double nextTickTime = 0.0;
    public int beatCount = 0;
    private const double scheduleAheadTime = 0.1;

    private Queue<(double scheduledTime, int beatNumber)> scheduledBeats = new Queue<(double scheduledTime, int beatNumber)>();
    public bool isRunning  { get; private set; } = false;

    public double TickInterval => 60.0 / bpm;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
        }
    }

    void Start() {
        audioSources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++) {
            GameObject go = new GameObject($"TickAudioSource_{i}");
            go.transform.parent = transform;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = tickSound;
            source.playOnAwake = false;
            audioSources[i] = source;
        }
        nextTickTime = AudioSettings.dspTime + 0.1;
    }

    void Update() {
        if (!isRunning) return;

        while (nextTickTime < AudioSettings.dspTime + scheduleAheadTime) {
            ScheduleTick(nextTickTime);
            nextTickTime += TickInterval;
        }

        while (scheduledBeats.Count > 0) {
            var beat = scheduledBeats.Peek();
            if (beat.scheduledTime <= AudioSettings.dspTime) {
                scheduledBeats.Dequeue();
                OnBeat?.Invoke(beat.beatNumber, beat.scheduledTime);
            } else {
                break;
            }
        }
    }

    private void ScheduleTick(double time) {
        AudioSource source = audioSources[currentSourceIndex];
        currentSourceIndex = (currentSourceIndex + 1) % poolSize;

        if (tickSound != null) {
            source.clip = tickSound;
            source.PlayScheduled(time);
        }
        scheduledBeats.Enqueue((time, beatCount));
        beatCount++;
    }

    public void StartMetronome() {
        if (!isRunning) {
            isRunning = true;
            nextTickTime = AudioSettings.dspTime + 0.1;
            beatCount = 0;
            scheduledBeats.Clear();
        }
    }

    public void StopMetronome() {
        isRunning = false;
    }

    public void SetBPM(double newBpm) {
        bpm = newBpm;
    }
}