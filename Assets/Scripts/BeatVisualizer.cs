using UnityEngine;

public class BeatVisualizer : MonoBehaviour {
    [SerializeField] private float minScale = 1f;        // Minimum size of the speaker
    [SerializeField] private float maxScale = 1.5f;      // Maximum size when beat hits
    [SerializeField] private float pulseDuration = 0.1f; // How long the pulse lasts
    [SerializeField] private Color baseColor = Color.gray;
    [SerializeField] private Color beatColor = Color.white;

    private float bpm;                  // Beats per minute
    private float beatInterval;         // Time between beats in seconds
    private float timer;               // Time tracking
    private SpriteRenderer spriteRenderer;
    private bool isPulsing;

    void Start()
    {
        // Create the circular speaker visualization
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite();
        spriteRenderer.color = baseColor;
        transform.localScale = Vector3.one * minScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Calculate scale based on beat timing
        float scale = CalculateScale();
        transform.localScale = Vector3.one * scale;

        // Reset timer when we reach beat interval
        if (timer >= beatInterval)
        {
            timer = 0f;
            TriggerBeat();
        }
    }

    // Call this to set or update BPM
    public void SetBPM(float newBPM)
    {
        bpm = newBPM;
        beatInterval = 60f / bpm; // Convert BPM to seconds per beat
    }

    private void TriggerBeat()
    {
        isPulsing = true;
        spriteRenderer.color = beatColor;
        Invoke(nameof(ResetPulse), pulseDuration);
    }

    private void ResetPulse()
    {
        isPulsing = false;
        spriteRenderer.color = baseColor;
    }

    private float CalculateScale()
    {
        if (isPulsing)
        {
            return maxScale;
        }

        // Creates a smooth anticipation effect before the beat
        float progress = timer / beatInterval;
        float anticipation = Mathf.Sin(progress * Mathf.PI);
        return Mathf.Lerp(minScale, minScale + (maxScale - minScale) * 0.3f, anticipation);
    }

    // Creates a simple circular sprite
    private Sprite CreateCircleSprite()
    {
        int size = 64; // Texture size
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                colors[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}