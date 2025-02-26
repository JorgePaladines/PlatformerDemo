using System;
using System.Collections;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    // Singleton instance for easy access
    public static Metronome Instance { get; private set; }

    [Header("Metronome Settings")]
    [Tooltip("Beats per minute")]
    public double bpm = 60.0;

    [Tooltip("Tick sound AudioClip (a short, latency-minimized sound)")]
    public AudioClip tickSound;

    [Tooltip("Number of AudioSources in the pool (helps schedule ticks precisely)")]
    public int poolSize = 8;

    // Event callback to notify subscribers when a beat occurs (passing the beat number)
    public event Action<int> OnBeat;

    // Internal state
    private AudioSource[] audioSources; // pool of AudioSources for scheduling ticks
    private int currentSourceIndex = 0; // round-robin index into the pool
    public double nextTickTime = 0.0;  // next DSP time at which to play the tick
    public int beatCount = 0;          // count of beats since start
    private const double scheduleAheadTime = 0.1; // seconds to look ahead for scheduling

    // Flag to control metronome operation
    private bool isRunning = false;

    // Calculate the interval between ticks (in seconds)
    public double TickInterval => 60.0 / bpm;

    void Awake() {
        // Implement singleton pattern
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        }
        else {
            Instance = this;
        }
    }

    void Start() {
        // Create a pool of AudioSources as children to ensure we can schedule overlapping ticks
        audioSources = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++) {
            GameObject go = new GameObject($"TickAudioSource_{i}");
            go.transform.parent = transform;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = tickSound;
            source.playOnAwake = false;
            // Adjust settings (volume, pitch, etc.) here if needed
            audioSources[i] = source;
        }
        // Initialize scheduling based on current DSP time (with a slight delay)
        nextTickTime = AudioSettings.dspTime + 0.1;
    }

    void Update() {
        // Only schedule ticks if the metronome is running
        if (!isRunning)
            return;

        // Schedule all ticks that fall within our scheduling window.
        while (nextTickTime < AudioSettings.dspTime + scheduleAheadTime) {
            ScheduleTick(nextTickTime);
            nextTickTime += TickInterval;
        }
    }

    /// <summary>
    /// Schedules a tick (sound and event) at the given DSP time.
    /// </summary>
    /// <param name="time">The DSP time to schedule the tick.</param>
    private void ScheduleTick(double time) {
        // Get the next available AudioSource from the pool
        AudioSource source = audioSources[currentSourceIndex];
        currentSourceIndex = (currentSourceIndex + 1) % poolSize;

        // Ensure the tick sound is assigned
        if (tickSound == null) {
            Debug.LogWarning("Tick sound is not assigned!");
            return;
        }
        source.clip = tickSound;
        // source.PlayScheduled(time);

        // Fire the OnBeat event as close to the scheduled time as possible.
        // (Note: This callback runs on the main thread and may be subject to frame delays.)
        StartCoroutine(InvokeBeatAtTime(time, beatCount));

        beatCount++;
    }

    /// <summary>
    /// Coroutine that waits until the scheduled DSP time and then invokes the OnBeat event.
    /// </summary>
    private IEnumerator InvokeBeatAtTime(double scheduledTime, int currentBeat) {
        double timeToWait = scheduledTime - AudioSettings.dspTime;
        if (timeToWait > 0) {
            // Convert to seconds (yield waits in seconds)
            yield return new WaitForSeconds((float)timeToWait);
        }
        OnBeat?.Invoke(currentBeat);
    }

    /// <summary>
    /// Starts the metronome.
    /// </summary>
    public void StartMetronome() {
        if (!isRunning) {
            // Reset scheduling and beat count for a fresh start.
            isRunning = true;
            nextTickTime = AudioSettings.dspTime + 0.1;
            beatCount = 0;
        }
    }

    /// <summary>
    /// Stops the metronome. Already scheduled ticks will still play.
    /// </summary>
    public void StopMetronome() {
        isRunning = false;
    }

    /// <summary>
    /// Adjusts the BPM of the metronome.
    /// </summary>
    /// <param name="newBpm">New beats per minute value.</param>
    public void SetBPM(double newBpm) {
        bpm = newBpm;
        // Note: Already scheduled ticks use the previous BPM interval.
        // For immediate effect, you might reset scheduling (e.g., StopMetronome then StartMetronome).
    }
}