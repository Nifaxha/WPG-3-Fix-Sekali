using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class InGameSettingsPanel : MonoBehaviour
{
    [Header("Volume")]
    public Slider masterSlider;            // 0..1
    public AudioMixer audioMixer;          // optional (lebih bagus)
    public string masterParam = "MasterVol";

    const string K_Master = "SET_MASTER";

    void Start()
    {
        float v = PlayerPrefs.GetFloat(K_Master, 0.8f);
        if (masterSlider)
        {
            masterSlider.minValue = 0f; masterSlider.maxValue = 1f;
            masterSlider.value = v;
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        ApplyMaster(v);
    }

    void SetMasterVolume(float v)
    {
        PlayerPrefs.SetFloat(K_Master, v);
        ApplyMaster(v);
    }

    void ApplyMaster(float v)
    {
        if (audioMixer != null && !string.IsNullOrEmpty(masterParam))
        {
            float db = Mathf.Approximately(v, 0f) ? -80f : Mathf.Log10(Mathf.Clamp01(v)) * 20f;
            audioMixer.SetFloat(masterParam, db);
        }
        else
        {
            // fallback kalau tidak pakai AudioMixer
            AudioListener.volume = Mathf.Clamp01(v);
        }
    }
}
