using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Script ini OPSIONAL - attach ke setiap health icon untuk animasi hilang
public class HealthIconAnimator : MonoBehaviour
{
    private Image iconImage;
    private Vector3 originalScale;

    void Awake()
    {
        iconImage = GetComponent<Image>();
        originalScale = transform.localScale;
    }

    // Fungsi untuk animasi hilang saat health berkurang
    public void AnimateDisappear()
    {
        StartCoroutine(DisappearAnimation());
    }

    IEnumerator DisappearAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Scale down
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);

            // Fade out
            if (iconImage != null)
            {
                Color color = iconImage.color;
                color.a = Mathf.Lerp(1f, 0f, progress);
                iconImage.color = color;
            }

            yield return null;
        }

        // Disable setelah animasi selesai
        if (iconImage != null)
        {
            iconImage.enabled = false;
        }
    }

    // Fungsi untuk animasi muncul kembali (jika ada healing)
    public void AnimateAppear()
    {
        StartCoroutine(AppearAnimation());
    }

    IEnumerator AppearAnimation()
    {
        if (iconImage != null)
        {
            iconImage.enabled = true;
        }

        float duration = 0.3f;
        float elapsed = 0f;

        transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Scale up
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, progress);

            // Fade in
            if (iconImage != null)
            {
                Color color = iconImage.color;
                color.a = Mathf.Lerp(0f, 1f, progress);
                iconImage.color = color;
            }

            yield return null;
        }

        transform.localScale = originalScale;
    }
}