using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class SubmarineHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("UI Health")]
    public Image[] healthIcons;                 // ⬅️ hanya sekali
    [Tooltip("Sprite untuk hati penuh (normal)")]
    public Sprite heartFull;
    [Tooltip("Sprite untuk hati kosong (transparan)")]
    public Sprite heartEmpty;
    public TextMeshProUGUI healthText;          // ⬅️ hanya sekali

    [Header("UI References")]
    public GameObject damagePanel;
    public float damagePanelDuration = 0.5f;

    [Header("Game Over Settings")]
    public string mainMenuSceneName = "MainMenu";
    public GameObject gameOverPanel;
    public float delayBeforeMainMenu = 2f;

    [Header("Damage Cooldown")]
    public float damageCooldown = 1f;
    private float lastDamageTime = -999f;

    [Header("Audio (Optional)")]
    public AudioSource damageAudioSource;
    public AudioClip damageSound;
    public AudioClip gameOverSound;

    //[Header("Camera Shake on Damage")]
    //public CameraShaker cameraShaker;
    //public float damageShakeIntensity = 0.3f;
    //public float damageShakeDuration = 0.5f;

    [Header("Camera Shake (via SubmarineCoordinates)")]
    public SubmarineCoordinates movementShake; // drag komponen SubmarineCoordinates di Inspector
    public float hitShakeIntensity = 0.08f;
    public float hitShakeDuration = 0.25f;

    private bool isDead = false;


    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        // Pastikan damage panel dan game over panel tersembunyi di awal
        if (damagePanel != null)
            damagePanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Setup audio source jika belum ada
        if (damageAudioSource == null)
        {
            damageAudioSource = gameObject.AddComponent<AudioSource>();
            damageAudioSource.playOnAwake = false;
            damageAudioSource.spatialBlend = 0f; // 2D sound
        }
    }

    public void TakeDamage(int damage = 1)
    {
        // Cek cooldown agar tidak spam damage
        if (Time.time - lastDamageTime < damageCooldown)
        {
            Debug.Log("<color=yellow>Damage cooldown aktif, diabaikan.</color>");
            return;
        }

        if (isDead) return;

        lastDamageTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"<color=orange>Kapal kena damage! Health tersisa: {currentHealth}/{maxHealth}</color>");

        // Update UI
        UpdateHealthUI();

        // Efek visual damage
        ShowDamageEffect();

        if (movementShake != null)
        {
            movementShake.TriggerShake(hitShakeIntensity, hitShakeDuration);
        }

        // Play sound
        if (damageAudioSource != null && damageSound != null)
        {
            damageAudioSource.PlayOneShot(damageSound);
        }


        // Cek apakah mati
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        // Selalu tampilkan 3 ikon; yang hilang dibuat transparan
        if (healthIcons != null && healthIcons.Length > 0)
        {
            for (int i = 0; i < healthIcons.Length; i++)
            {
                if (healthIcons[i] == null) continue;

                // Pastikan ikon aktif (tidak di-disable)
                healthIcons[i].enabled = true;

                // Ubah sprite sesuai kondisi
                bool full = i < currentHealth;
                if (heartFull != null && heartEmpty != null)
                    healthIcons[i].sprite = full ? heartFull : heartEmpty;

                // Opaque untuk health yang masih ada, transparan untuk yang sudah hilang
                // contoh transparan 35%
                float alpha = full ? 1f : 0.35f;

                Color c = healthIcons[i].color;
                c.a = alpha;
                healthIcons[i].color = c;
            }
        }

        // (Opsional) tampilkan teks Health kalau ada
        if (healthText != null)
            healthText.text = $"Health: {currentHealth}/{maxHealth}";
    }


    void ShowDamageEffect()
    {
        if (damagePanel != null)
        {
            StartCoroutine(FlashDamagePanel());
        }
    }

    IEnumerator FlashDamagePanel()
    {
        damagePanel.SetActive(true);

        // Fade in cepat
        CanvasGroup canvasGroup = damagePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = damagePanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0.5f;

        // Fade out
        float elapsed = 0f;
        while (elapsed < damagePanelDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0.5f, 0f, elapsed / damagePanelDuration);
            yield return null;
        }

        damagePanel.SetActive(false);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red><b>KAPAL HANCUR! GAME OVER!</b></color>");

        // Play game over sound
        if (damageAudioSource != null && gameOverSound != null)
        {
            damageAudioSource.PlayOneShot(gameOverSound);
        }

        // Tampilkan game over panel jika ada
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Freeze game (opsional)
        Time.timeScale = 0f;

        // Kembali ke main menu setelah delay
        StartCoroutine(ReturnToMainMenu());
    }

    IEnumerator ReturnToMainMenu()
    {
        // Wait dengan unscaled time karena timeScale = 0
        yield return new WaitForSecondsRealtime(delayBeforeMainMenu);

        // Reset time scale
        Time.timeScale = 1f;

        // Load main menu
        Debug.Log($"<color=cyan>Kembali ke Main Menu: {mainMenuSceneName}</color>");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Fungsi untuk healing (opsional, jika ada power-up)
    public void Heal(int amount = 1)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthUI();

        Debug.Log($"<color=green>Kapal di-heal! Health sekarang: {currentHealth}/{maxHealth}</color>");
    }

    // Fungsi untuk reset health (misal saat respawn)
    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}