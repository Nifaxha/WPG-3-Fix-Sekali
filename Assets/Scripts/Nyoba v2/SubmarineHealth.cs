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

    [Header("UI References")]
    public Image[] healthIcons; // Array untuk 3 icon health (misal: gambar hati)
    public TextMeshProUGUI healthText; // Text untuk menampilkan "Health: 3/3"
    public GameObject damagePanel; // Panel merah yang flash saat kena damage
    public float damagePanelDuration = 0.5f;

    [Header("Game Over Settings")]
    public string mainMenuSceneName = "MainMenu"; // Nama scene main menu Anda
    public GameObject gameOverPanel; // Panel Game Over (opsional)
    public float delayBeforeMainMenu = 2f; // Delay sebelum kembali ke main menu

    [Header("Damage Cooldown")]
    public float damageCooldown = 1f; // Cooldown agar tidak langsung kena damage berkali-kali
    private float lastDamageTime = -999f;

    [Header("Audio (Optional)")]
    public AudioSource damageAudioSource;
    public AudioClip damageSound;
    public AudioClip gameOverSound;

    [Header("Camera Shake on Damage")]
    public CameraShaker cameraShaker; // Referensi ke CameraShaker jika ada
    public float damageShakeIntensity = 0.3f;
    public float damageShakeDuration = 0.5f;

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
        // Update health icons (misal: 3 hati, yang habis jadi abu-abu atau hilang)
        if (healthIcons != null && healthIcons.Length > 0)
        {
            for (int i = 0; i < healthIcons.Length; i++)
            {
                if (healthIcons[i] != null)
                {
                    // Aktifkan icon jika masih ada health
                    healthIcons[i].enabled = (i < currentHealth);

                    // Atau ubah warna jadi abu-abu
                    // healthIcons[i].color = (i < currentHealth) ? Color.white : Color.gray;
                }
            }
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth}/{maxHealth}";
        }
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