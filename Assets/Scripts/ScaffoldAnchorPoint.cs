using UnityEngine;

/// <summary>
/// İskele üzerindeki lanyard kancasının tutturulacağı bağlantı noktasıdır.
/// Demirlerin sağlamlığına göre başarı veya kırılma senaryolarını tetikler.
/// </summary>
public class ScaffoldAnchorPoint : MonoBehaviour
{
    [Header("Ankraj Türü")]
    [Tooltip("Bu bağlantı noktası kırılgan (paslı/güvensiz) mi?")]
    public bool isFragile = false;

    [Header("Görsel Ayrım Materyalleri")]
    [Tooltip("Sağlam demir için gümüş metal materyali")]
    public Material sturdyMaterial;

    [Tooltip("Çürük/kırılgan demir için paslı/kahverengi metal materyali")]
    public Material fragileMaterial;

    [Header("Fiziksel Demir Bağlantısı")]
    [Tooltip("Kırıldığında düşecek olan fiziksel korkuluk demiri GameObject'i")]
    public GameObject physicalBar;

    [Header("Senaryo Bağlantısı")]
    [Tooltip("Senaryoyu yöneten ana kontrolcü")]
    public HarnessScenarioManager scenarioManager;

    [Header("Efektler")]
    [Tooltip("Bağlantı yapıldığında çalacak standart tık sesi")]
    public AudioClip attachSound;

    [Tooltip("Demir kırıldığında çalacak kırılma/metal bükülme sesi")]
    public AudioClip breakSound;

    private Lanyard currentLanyard;
    private AudioSource audioSource;
    private bool isBroken = false;

    private void Start()
    {
        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Otomatik Görsel Ayrım Materyalini Ata
        ApplyMaterials();
    }

    /// <summary>
    /// Sağlam veya çürük durumuna göre materyal atamasını yapar.
    /// </summary>
    public void ApplyMaterials()
    {
        Renderer r = GetComponent<Renderer>();
        if (isFragile)
        {
            if (r != null && fragileMaterial != null) r.material = fragileMaterial;
            if (physicalBar != null && fragileMaterial != null)
            {
                Renderer pBarRenderer = physicalBar.GetComponent<Renderer>();
                if (pBarRenderer != null) pBarRenderer.material = fragileMaterial;
            }
        }
        else
        {
            if (r != null && sturdyMaterial != null) r.material = sturdyMaterial;
            if (physicalBar != null && sturdyMaterial != null)
            {
                Renderer pBarRenderer = physicalBar.GetComponent<Renderer>();
                if (pBarRenderer != null) pBarRenderer.material = sturdyMaterial;
            }
        }
    }

    /// <summary>
    /// Lanyard bu ankraja tutturulduğunda çağrılır.
    /// </summary>
    public void OnLanyardAttached(Lanyard lanyard)
    {
        if (isBroken) return;

        currentLanyard = lanyard;

        if (isFragile)
        {
            // Kırılgan demir tetiklendi! Düşme senaryosu başlasın
            BreakAnchor();
        }
        else
        {
            // Güvenli bağlantı sağlandı! Başarı senaryosu başlasın
            Debug.Log($"ScaffoldAnchorPoint: Güvenli bağlantı sağlandı: {gameObject.name}");
            if (audioSource != null && attachSound != null)
            {
                audioSource.PlayOneShot(attachSound);
            }

            if (scenarioManager != null)
            {
                scenarioManager.OnAnchorConnected(this);
            }
        }
    }

    /// <summary>
    /// Lanyard bu ankrajdan söküldüğünde çağrılır.
    /// </summary>
    public void OnLanyardDetached()
    {
        currentLanyard = null;
        Debug.Log($"ScaffoldAnchorPoint: Bağlantı söküldü: {gameObject.name}");
    }

    /// <summary>
    /// Kırılgan demiri kırar, fiziksel olarak düşürür ve oyuncunun düşüşünü tetikler.
    /// </summary>
    private void BreakAnchor()
    {
        if (isBroken) return;
        isBroken = true;

        Debug.LogWarning($"ScaffoldAnchorPoint: Kritik hata! Çürük ankraj seçildi: {gameObject.name}. Demir kırılıyor!");

        // Kırılma sesini çal
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        // Kırılan demire fizik ekle ve düşür
        if (physicalBar != null)
        {
            Collider barCollider = physicalBar.GetComponent<Collider>();
            if (barCollider != null)
            {
                barCollider.enabled = false; // Oyuncunun içinden geçip düşebilmesi için collider'ı kapat
            }

            Rigidbody rb = physicalBar.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = physicalBar.AddComponent<Rigidbody>();
            }
            rb.isKinematic = false;
            rb.useGravity = true;

            // İskelenin dışına doğru küçük bir itme kuvveti uygula
            Vector3 pushForce = (physicalBar.transform.forward - Vector3.up) * 2.0f;
            rb.AddForce(pushForce, ForceMode.Impulse);
        }

        // Lanyard kancasını serbest bırak/düşür
        if (currentLanyard != null)
        {
            Rigidbody lanyardRb = currentLanyard.GetComponent<Rigidbody>();
            if (lanyardRb == null)
            {
                lanyardRb = currentLanyard.gameObject.AddComponent<Rigidbody>();
            }
            lanyardRb.isKinematic = false;
            lanyardRb.useGravity = true;
        }

        // Senaryo yöneticisine kırılma olayını bildir
        if (scenarioManager != null)
        {
            scenarioManager.OnAnchorFailed(this);
        }
    }
}
