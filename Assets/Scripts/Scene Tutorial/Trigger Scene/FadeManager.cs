using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Pastikan mulai fade in dari hitam saat scene dimulai
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeIn()
    {
        canvasGroup.alpha = 1;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1 - t / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 0;
    }

    public IEnumerator FadeOutAndLoad(string nextScene)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        SceneManager.LoadScene(nextScene);
    }

    public IEnumerator FadeOutAndLoad(int nextIndex)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        SceneManager.LoadScene(nextIndex);
    }
}
