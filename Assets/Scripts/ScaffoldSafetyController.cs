using UnityEngine;
using System;

public class ScaffoldSafetyController : MonoBehaviour
{
    [Header("Güvenlik Durumları")]
    [Tooltip("Tekerlek kilitlerinin durumu")]
    public bool wheelLocked = false;
    [Tooltip("Korkuluk kontrol durumu")]
    public bool guardrailChecked = false;
    [Tooltip("Platform kilidinin durumu")]
    public bool platformLocked = false;

    [Header("Fiziksel Sarsıntı Ayarları")]
    [Tooltip("Tekerlekler açıkken iskele sallantı gücü")]
    public float wobbleForceAmount = 40f;
    [Tooltip("Sallantı frekansı")]
    public float wobbleFrequency = 6f;

    [Header("Debug Ayarları")]
    [Tooltip("Editörde test için oyuncuyu iskeledeymiş gibi simüle et")]
    public bool debugForcePlayerOnScaffold = false;

    [Header("VR Görsel Geri Bildirim (Opsiyonel)")]
    public Renderer[] wheelLockRenderers;
    public Renderer[] guardrailCheckRenderers;
    public Renderer[] platformLockRenderers;
    public Material lockedMaterial;
    public Material unlockedMaterial;

    // Olaylar (Events)
    public static event Action OnWheelLocked;
    public static event Action OnGuardrailChecked;
    public static event Action OnPlatformLocked;
    public static event Action OnAllChecksCompleted;

    private CollectiveProtectionChecker sceneChecker;
    private Rigidbody scaffoldRigidbody;

    void Start()
    {
        // Sahnedeki Rigidbody'yi alıyoruz
        scaffoldRigidbody = GetComponent<Rigidbody>();
        if (scaffoldRigidbody == null)
        {
            scaffoldRigidbody = GetComponentInParent<Rigidbody>();
        }

        // Sahnedeki CollectiveProtectionChecker bileşenini buluyoruz
        sceneChecker = FindAnyObjectByType<CollectiveProtectionChecker>();
        if (sceneChecker == null)
        {
            Debug.LogWarning("ScaffoldSafetyController | Sahne içerisinde CollectiveProtectionChecker bulunamadı.");
        }
        UpdateVisuals();
    }

    void FixedUpdate()
    {
        // Eğer tekerlekler kilitli değilse ve oyuncu iskele üzerindeyse iskeleyi fiziksel olarak sars / salla
        if (scaffoldRigidbody != null && !wheelLocked && IsPlayerOnScaffold())
        {
            // İskele Rigidbody'sine döngüsel yatay sallantı kuvveti ekle
            Vector3 force = new Vector3(
                Mathf.Sin(Time.fixedTime * wobbleFrequency), 
                0f, 
                Mathf.Cos(Time.fixedTime * wobbleFrequency * 1.3f)
            ) * wobbleForceAmount;
            
            scaffoldRigidbody.AddForce(force, ForceMode.Force);
        }
    }

    private bool IsPlayerOnScaffold()
    {
        if (debugForcePlayerOnScaffold) return true;
        Transform player = null;
        Scenario1Manager manager = FindAnyObjectByType<Scenario1Manager>();
        if (manager != null && manager.playerTransform != null)
        {
            player = manager.playerTransform;
        }
        else
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
            else if (Camera.main != null) player = Camera.main.transform;
        }

        if (player == null) return false;

        // Oyuncunun iskeleye göre relatif koordinatlarını bul
        Vector3 relativePos = transform.InverseTransformPoint(player.position);
        
        // Eğer oyuncu iskele tabanından yukarıda (y > 1.5) ve yatay olarak iskele sınırları içerisindeyse (x, z < 2.5m)
        if (relativePos.y > 1.5f && Mathf.Abs(relativePos.x) < 2.5f && Mathf.Abs(relativePos.z) < 2.5f)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// VR etkileşimi veya tekerlek kilit durumu değiştiğinde çağrılır.
    /// </summary>
    public void SetWheelsLockedState(bool isLocked)
    {
        if (wheelLocked == isLocked) return;
        wheelLocked = isLocked;
        Debug.Log("ScaffoldSafetyController | Tekerlek kilit durumu güncellendi: " + isLocked);

        if (isLocked)
        {
            OnWheelLocked?.Invoke();
            if (sceneChecker != null)
            {
                sceneChecker.LockWheels();
            }
        }
        else
        {
            // Eğer tekerlek kilidi açıldıysa collective protection durumunu boz
            if (sceneChecker != null)
            {
                sceneChecker.wheelLocked = false;
            }
        }

        UpdateVisuals();
        CheckAllCompleted();
    }

    /// <summary>
    /// VR etkileşimi ile tekerlekler kilitlendiğinde çağrılır (Eski uyumluluk için).
    /// </summary>
    public void LockWheels()
    {
        SetWheelsLockedState(true);
    }

    /// <summary>
    /// VR etkileşimi ile korkuluklar kontrol edildiğinde çağrılır.
    /// </summary>
    public void CheckGuardrails()
    {
        if (guardrailChecked) return;
        guardrailChecked = true;
        Debug.Log("ScaffoldSafetyController | Korkuluk kontrolü tamamlandı.");

        // Sahnedeki ana denetleyiciye bildiriyoruz
        if (sceneChecker != null)
        {
            sceneChecker.CheckGuardrails();
        }

        OnGuardrailChecked?.Invoke();
        UpdateVisuals();
        CheckAllCompleted();
    }

    /// <summary>
    /// VR etkileşimi ile platform kilitlendiğinde çağrılır.
    /// </summary>
    public void LockPlatform()
    {
        if (platformLocked) return;
        platformLocked = true;
        Debug.Log("ScaffoldSafetyController | Platform kilidi kapatıldı.");

        // Sahnedeki ana denetleyiciye bildiriyoruz
        if (sceneChecker != null)
        {
            sceneChecker.LockPlatform();
        }

        OnPlatformLocked?.Invoke();
        UpdateVisuals();
        CheckAllCompleted();
    }

    private void CheckAllCompleted()
    {
        bool allChecked = wheelLocked && guardrailChecked && platformLocked;
        
        if (allChecked)
        {
            Debug.Log("ScaffoldSafetyController | İskele üzerindeki tüm toplu güvenlik kontrolleri başarıyla tamamlandı!");
            OnAllChecksCompleted?.Invoke();
        }

        // Scenario1Manager'ı doğrudan bilgilendir
        Scenario1Manager scenario1 = FindAnyObjectByType<Scenario1Manager>();
        if (scenario1 != null)
        {
            scenario1.SetCollectiveProtectionChecked(allChecked);
        }
    }

    private void UpdateVisuals()
    {
        // Tekerlek kilitleri için görsel güncelleme
        SetRenderersMaterial(wheelLockRenderers, wheelLocked ? lockedMaterial : unlockedMaterial);
        
        // Korkuluklar için görsel güncelleme
        SetRenderersMaterial(guardrailCheckRenderers, guardrailChecked ? lockedMaterial : unlockedMaterial);
        
        // Platform kilidi için görsel güncelleme
        SetRenderersMaterial(platformLockRenderers, platformLocked ? lockedMaterial : unlockedMaterial);
    }

    private void SetRenderersMaterial(Renderer[] renderers, Material mat)
    {
        if (renderers == null || mat == null) return;
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.material = mat;
            }
        }
    }
}
