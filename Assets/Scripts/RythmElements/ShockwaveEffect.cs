using UnityEngine;
using UnityEngine.UI;

public class ShockwaveEffect : MonoBehaviour
{
    private float startScale = 1f;
    private float endScale = 1.5f;
    [SerializeField] private float duration = 0.25f;      // Duration of the effect
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Smooth expansion
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);    // Fade-out

    private Image image;
    private float elapsedTime = 0f;
    private Vector3 initialScale;

    void Start()
    {
        image = GetComponent<Image>();
        initialScale = transform.localScale * startScale;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        // Scale the shockwave
        float scaleProgress = scaleCurve.Evaluate(progress);
        float currentScale = Mathf.Lerp(startScale, endScale, scaleProgress);
        transform.localScale = initialScale * currentScale;

        // Fade the shockwave
        float alphaProgress = alphaCurve.Evaluate(progress);
        Color color = image.color;
        color.a = alphaProgress;
        image.color = color;

        // Destroy when finished
        if (progress >= 1f){
            Destroy(gameObject);
        }
    }
}