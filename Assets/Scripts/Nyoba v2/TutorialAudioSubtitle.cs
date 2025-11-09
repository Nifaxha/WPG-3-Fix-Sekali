using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class TutorialAudioSubtitle : MonoBehaviour
{
    [Header("Audio Tutorial")]
    public AudioSource source;
    public AudioClip tutorialClip;

    [Header("Subtitle Settings")]
    public TMP_Text subtitleTMP;
    [Tooltip("Daftar subtitle sesuai urutan waktu.")]
    public SubtitleLine[] subtitleLines;
    [Tooltip("Jeda kecil sebelum audio mulai (opsional).")]
    public float startDelay = 1f;

    private Coroutine playRoutine;
    private Coroutine subtitleRoutine;

    [System.Serializable]
    public struct SubtitleLine
    {
        [TextArea(1, 3)] public string text;
        [Tooltip("Waktu mulai muncul (detik, dihitung dari awal audio).")]
        public float startTime;
        [Tooltip("Berapa lama tampil (detik).")]
        public float duration;
    }

    void Start()
    {
        // Jalankan otomatis hanya di scene "Cube 1"
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Cube 1")
        {
            StartTutorial();
        }
    }

    public void StartTutorial()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayTutorialSequence());
    }

    private IEnumerator PlayTutorialSequence()
    {
        // Validasi
        if (source == null || tutorialClip == null)
        {
            Debug.LogWarning("[TutorialAudioSubtitle] AudioSource atau AudioClip belum diisi.");
            yield break;
        }

        // Tunggu sampai tidak paused sebelum mulai
        yield return new WaitUntil(() => !AudioListener.pause);

        // Delay awal: hanya menghitung ketika tidak paused
        float t = 0f;
        while (t < startDelay)
        {
            if (!AudioListener.pause) t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Reset subtitle
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        // Putar audio
        source.clip = tutorialClip;
        source.Play();

        // Sinkronisasi dengan timeline audio (blok saat pause)
        foreach (var line in subtitleLines)
        {
            // Tunggu sampai waktu audio mencapai startTime baris ini & game tidak paused
            yield return new WaitUntil(() => !AudioListener.pause && source.isPlaying && source.time >= line.startTime);
            ShowSubtitle(line.text, line.duration);
        }

        // Tunggu audio selesai (lanjutkan cek setelah unpause)
        yield return new WaitUntil(() => !AudioListener.pause && !source.isPlaying);

        // Matikan subtitle terakhir
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);
    }

    private void ShowSubtitle(string text, float duration)
    {
        if (string.IsNullOrEmpty(text) || subtitleTMP == null) return;

        subtitleTMP.text = text;
        subtitleTMP.gameObject.SetActive(true);

        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(HideSubtitleAfter(duration));
    }

    private IEnumerator HideSubtitleAfter(float delay)
    {
        // Timer hide yang membeku saat pause
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!AudioListener.pause)
                elapsed += Time.unscaledDeltaTime; // hitung hanya saat tidak paused
            yield return null;
        }

        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);
        subtitleRoutine = null;
    }
}
