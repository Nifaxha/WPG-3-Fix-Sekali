using UnityEngine;

public class StopLever3D_Interact : MonoBehaviour
{
    [Header("Lever X Rotation (deg)")]
    public float xWhenForward = -85.5f;   // posisi MAJU (DEfault)
    public float xWhenBackward = 0.691f;  // posisi MUNDUR/NETRAL
    public float degreesPerSecond = 180f;

    [Header("Meaning")]
    public bool backwardIsStop = true;    // ✅ kalau rig-mu kebalik, ubah ini jadi false

    [Header("Stop Behaviour")]
    public bool emergencyStop = true;

    [Header("Highlight (opsional)")]
    public Renderer highlightRenderer;
    public Color highlightColor = new Color(1f, .8f, .2f);
    private Color _normalColor;

    [Header("Audio (opsional)")]
    public AudioSource leverAudioSource;
    public AudioClip leverMoveSound;

    private SubmarineCoordinates coordSystem;
    private bool isForward;  //Default maju
    private bool isMoving;
    private bool isHighlighted;
    private float fixedY, fixedZ;
    private float currentX;

    void Start()
    {
        coordSystem = FindObjectOfType<SubmarineCoordinates>();

        var e = transform.localEulerAngles;
        fixedY = e.y; fixedZ = e.z; currentX = e.x;

        // NEW: paksa pose awal ke MAJU secara visual & data
        currentX = xWhenForward;
        transform.localEulerAngles = new Vector3(currentX, fixedY, fixedZ);
        isForward = true;

        if (!leverAudioSource)
        {
            leverAudioSource = GetComponent<AudioSource>();
            if (!leverAudioSource)
            {
                leverAudioSource = gameObject.AddComponent<AudioSource>();
                leverAudioSource.playOnAwake = false;
                leverAudioSource.spatialBlend = 1f;
            }
        }

        if (!highlightRenderer) highlightRenderer = GetComponentInChildren<Renderer>();
        if (highlightRenderer) _normalColor = highlightRenderer.material.color;

        ApplyLockForTarget(isForward);
    }

    void Update()
    {
        // HANYA jika disorot: izinkan input E langsung
        if (isHighlighted && Input.GetKeyDown(KeyCode.E) && !isMoving)
            ToggleLever();

        float targetX = isForward ? xWhenForward : xWhenBackward;
        currentX = Mathf.MoveTowardsAngle(currentX, targetX, degreesPerSecond * Time.deltaTime);
        transform.localEulerAngles = new Vector3(currentX, fixedY, fixedZ);

        if (Mathf.Abs(Mathf.DeltaAngle(currentX, targetX)) < 0.25f) isMoving = false;
    }

    private void ToggleLever()
    {
        isForward = !isForward;   // pindah target
        isMoving = true;

        if (leverAudioSource && leverMoveSound)
            leverAudioSource.PlayOneShot(leverMoveSound);

        ApplyLockForTarget(isForward);
    }

    // 🧠 Atur lock sesuai target: jika target = "posisi STOP", kunci; jika target = "maju", buka
    private void ApplyLockForTarget(bool targetIsForward)
    {
        if (!coordSystem) return;

        bool goingToStop = backwardIsStop ? !targetIsForward : targetIsForward;

        if (goingToStop)
        {
            if (emergencyStop) coordSystem.EmergencyStop();
            else coordSystem.GradualStop();

            coordSystem.LockControlsFromLever(true);   // kunci tombol
        }
        else
        {
            coordSystem.LockControlsFromLever(false);  // buka tombol
            coordSystem.ResumeFromLever();             // lanjut jalan
        }
    }

    // Dipanggil raycaster untuk highlight
    public void SetHighlight(bool h)
    {
        isHighlighted = h;
        if (highlightRenderer)
            highlightRenderer.material.color = h ? highlightColor : _normalColor;
    }

    // Dapat dipanggil raycaster TANPA perlu SetHighlight terlebih dahulu
    public void PressButton()
    {
        if (!isMoving) ToggleLever();
    }
}