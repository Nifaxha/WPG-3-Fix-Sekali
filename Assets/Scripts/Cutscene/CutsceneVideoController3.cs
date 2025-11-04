using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class CutsceneVideoController3 : MonoBehaviour
{
    public VideoPlayer vp;
    public CanvasGroup fade;          // overlay hitam
    public string nextScene = "Level_01";
    public float fadeDur = 0.6f;
    public bool allowSkip = true;     // Space/Esc untuk skip

    void Start()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        // Fade in dari hitam
        if (fade) yield return FadeTo(0f, fadeDur);

        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Cutscene/GoodEndBaru.mp4");
        vp.url = videoPath;

        // Prepare & play
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();

        // Tunggu selesai / skip
        bool done = false;
        vp.loopPointReached += _ => done = true;
        while (!done)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
            {
                break;
            }
            yield return null;
        }

        // Fade out & load next
        if (fade) yield return FadeTo(1f, fadeDur);
        SceneManager.LoadScene(nextScene);
    }

    IEnumerator FadeTo(float target, float dur)
    {
        float start = fade.alpha, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            fade.alpha = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        fade.alpha = target;
    }
}
