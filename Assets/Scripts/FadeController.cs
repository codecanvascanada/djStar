using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public static FadeController instance;

    [Tooltip("Image used for the fade effect.")]
    public Image fadeImage;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Optional: DontDestroyOnLoad(gameObject); 
            // If you want the fader to persist across scenes. For now, we'll keep it scene-specific.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage == null)
        {
            Debug.LogError("Fade Image is not assigned in the FadeController.", this);
            enabled = false;
        }
    }

    public Coroutine FadeIn(float duration)
    {
        return StartCoroutine(Fade(1f, 0f, duration));
    }

    public Coroutine FadeOut(float duration)
    {
        return StartCoroutine(Fade(0f, 1f, duration));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        if (endAlpha == 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
}
