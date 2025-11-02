using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneGate : MonoBehaviour
{
    [Header("Jika kosong, akan load scene berikutnya (build index + 1)")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float delayBeforeFade = 0.5f;
    [SerializeField] private string playerTag = "Player";

    private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag(playerTag)) return;
        used = true;

        StartCoroutine(HandleTransition());
    }

    private IEnumerator HandleTransition()
    {
        yield return new WaitForSeconds(delayBeforeFade);

        if (FadeManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
                yield return FadeManager.Instance.FadeOutAndLoad(nextSceneName);
            else
                yield return FadeManager.Instance.FadeOutAndLoad(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }
}
