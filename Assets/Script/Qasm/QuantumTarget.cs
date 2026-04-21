using UnityEngine;

public class QuantumTarget : MonoBehaviour
{
    [Header("音效設定")]
    public AudioClip popSound;
    private AudioSource audioSource;
    private bool isHit = false;

    [HideInInspector] public float targetX, targetY, targetZ;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 啟用 3D 音效

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isHit && other.CompareTag("PlayerHand"))
        {
            isHit = true;
            if (popSound != null) audioSource.PlayOneShot(popSound);

            Renderer rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;

            FindObjectOfType<CircuitGridController>()?.AdvanceToNextStep();
            Destroy(gameObject, 1.0f);
        }
    }
}