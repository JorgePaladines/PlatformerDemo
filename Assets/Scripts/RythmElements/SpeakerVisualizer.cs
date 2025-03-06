using UnityEngine;

public class SpeakerVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] float baseScale = 1f;
    [SerializeField] float maxScale = 1.3f;
    [SerializeField] [Range(0.01f, 1f)] float timeBeforeBeatToReachMax = 0.03f;
    [SerializeField] [Range(0.01f, 1f)] float holdAtMaxDuration = 0.2f;
    [SerializeField] [Range(0.01f, 1f)] float decayDuration = 0.12f;
    [SerializeField] AnimationCurve anticipationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shockwave Settings")]
    [SerializeField] GameObject shockwavePrefab; // Assign the Shockwave prefab here
    private bool hasSpawnedShockwave = false;    // Flag to spawn shockwave once per beat

    public double lastBeatTime = 0f;
    public double nextBeatTime = 0f;
    public double beatInterval = 0f;

    public double currentTime;
    public double timeSinceLastBeat;
    public double timeUntilNextBeat;
    public Vector3 initialScale;

    void Start()
    {
        if (Metronome.Instance == null) {
            Debug.LogError("Metronome instance is missing!");
            enabled = false;
            return;
        }

        Metronome.Instance.OnBeat += OnBeatTriggered;
        beatInterval = Metronome.Instance.TickInterval;
        nextBeatTime = Metronome.Instance.nextTickTime;
        lastBeatTime = nextBeatTime - beatInterval;
        initialScale = transform.localScale;
    }

    void OnDestroy()
    {
        if (Metronome.Instance != null)
        {
            Metronome.Instance.OnBeat -= OnBeatTriggered;
        }
    }

    void Update()
    {
        if (Metronome.Instance == null || !Metronome.Instance.isRunning) return;

        beatInterval = Metronome.Instance.TickInterval;
        currentTime = AudioSettings.dspTime;
        timeSinceLastBeat = currentTime - lastBeatTime;
        timeUntilNextBeat = nextBeatTime - currentTime;

        float targetScale;

        // Phase 1: Hold at max scale just before beat
        if ((timeUntilNextBeat <= holdAtMaxDuration && timeUntilNextBeat > 0) || timeUntilNextBeat < 0) {
            targetScale = maxScale;

            // Spawn shockwave at the exact beat drop (when timeSinceLastBeat is near 0)
            if (!hasSpawnedShockwave && shockwavePrefab != null) {
                GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
                shockwave.transform.SetParent(transform, false);
                // Reset the local position to (0, 0, 0) to align it with the speaker
                shockwave.transform.localPosition = Vector2.zero;
                
                // Move the shockwave behind the speaker by adjusting the z position
                Vector3 shockwavePosition = shockwave.transform.localPosition;
                shockwavePosition.z = -1;
                shockwave.transform.localPosition = shockwavePosition;

                hasSpawnedShockwave = true;
            }
        }
        // Phase 2: Anticipation (accelerated scaling before beat)
        else if (timeUntilNextBeat <= (timeBeforeBeatToReachMax + holdAtMaxDuration) && timeUntilNextBeat > holdAtMaxDuration)
        {
            double anticipationStartTime = nextBeatTime - (timeBeforeBeatToReachMax + holdAtMaxDuration);
            double timeInAnticipation = currentTime - anticipationStartTime;
            float progress = Mathf.Clamp01((float)(timeInAnticipation / timeBeforeBeatToReachMax));
            targetScale = Mathf.Lerp(baseScale, maxScale, anticipationCurve.Evaluate(progress));
        }
        // Phase 3: Immediate decay after beat
        else if (timeSinceLastBeat <= decayDuration)
        {
            float progress = Mathf.Clamp01((float)(timeSinceLastBeat / decayDuration));
            targetScale = Mathf.Lerp(maxScale, baseScale, progress);
        }
        // Phase 4: Maintain base scale between phases
        else
        {
            targetScale = baseScale;
        }

        transform.localScale = initialScale * targetScale;
    }

    private void OnBeatTriggered(int beatNumber, double scheduledTime)
    {
        lastBeatTime = scheduledTime;
        nextBeatTime = scheduledTime + beatInterval;
        hasSpawnedShockwave = false; // Reset flag for the next beat
    }
}