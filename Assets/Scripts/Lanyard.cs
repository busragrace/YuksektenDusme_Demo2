using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Masanın üzerindeki lanyard objesine ve daha sonra oyuncunun elindeki kancaya eklenir.
/// Kemer kuşanılmadan önce alınmasını engeller ve uyarı tetikler.
/// </summary>
public class Lanyard : MonoBehaviour
{
    [Header("El / Kamera Tanımlaması")]
    [Tooltip("Lanyardın tıklanınca bağlanacağı nesne (Boş bırakılırsa otomatik Kameraya bağlanır)")]
    public Transform rightController;

    [Header("Senaryo Bağlantısı")]
    [Tooltip("Senaryoyu yöneten ana kontrolcü")]
    public HarnessScenarioManager scenarioManager;

    [Header("İp Görseli Ayarları")]
    [Tooltip("Lanyard ipini çizecek olan LineRenderer bileşeni")]
    public LineRenderer lineRenderer;

    [Tooltip("İpin oyuncunun bedenine bağlanacağı nokta")]
    public Transform bodyConnectionPoint;

    [Tooltip("Lanyard kancasının ucu")]
    public Transform hookPoint;

    [Header("Etkileşim Ayarları")]
    [Tooltip("Kancanın ankraja kilitlenmesi için gereken maksimum mesafe")]
    public float snapDistance = 0.35f;

    [Tooltip("Lanyardın bağlı olduğu mevcut ankraj noktası")]
    public ScaffoldAnchorPoint attachedAnchor;

    [Header("Güvenlik Türü")]
    [Tooltip("Lanyardın güvenli (yeşil) olup olmadığı")]
    public bool isSafe = true;

    private bool isGrabbed = false;
    private Transform activeController;

    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start()
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        // XRBaseInteractable olay dinleyicisini otomatik bağla (Hem Simple hem Grab Interactable'ları destekler)
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnXRISelectEntered);
            interactable.selectExited.AddListener(OnXRISelectExited);
        }
    }

    private void OnXRISelectEntered(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (args != null && args.interactorObject != null)
        {
            bool success = GrabLanyard(args.interactorObject.transform);
            if (!success)
            {
                // Tutma engellendi, elden zorla düşür
                var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
                if (interactable != null)
                {
                    StartCoroutine(ForceDropInteractable(interactable));
                }
            }
        }
    }

    private void OnXRISelectExited(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        if (attachedAnchor == null)
        {
            DropLanyard();
        }
    }

    public void DropLanyard()
    {
        if (!isGrabbed) return;
        isGrabbed = false;
        activeController = null;

        // Ankraja bağlı değilse masaya geri koy
        if (attachedAnchor == null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }
        Debug.Log("Lanyard: Serbest bırakıldı (masaya geri döndü).");
    }

    private System.Collections.IEnumerator ForceDropInteractable(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        interactable.enabled = false;
        yield return new WaitForEndOfFrame();
        interactable.enabled = true;
    }

    private void Update()
    {
        // İp çizgisini güncelle
        if (lineRenderer != null && lineRenderer.enabled && bodyConnectionPoint != null && hookPoint != null)
        {
            lineRenderer.SetPosition(0, bodyConnectionPoint.position);
            lineRenderer.SetPosition(1, hookPoint.position);
        }

        // --- KLAVYE KONTROLÜ: 'L' TUŞU ---
#if ENABLE_INPUT_SYSTEM
        bool lKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame;
#else
        bool lKeyPressed = Input.GetKeyDown(KeyCode.L);
#endif
        if (lKeyPressed)
        {
            // 1. Durum: Lanyard elimizde ve demire bağlamak istiyoruz
            if (isGrabbed && attachedAnchor == null)
            {
                TryKeyboardAttach();
            }
            // 2. Durum: Lanyard zaten demire bağlı ve geri elimize almak istiyoruz
            else if (attachedAnchor != null)
            {
                DetachFromAnchorAndGrab();
            }
        }
    }

    /// <summary>
    /// Lanyard masa üzerinden alınmak/tutulmak istendiğinde çağrılır.
    /// </summary>
    public bool GrabLanyard(Transform handTransform)
    {
        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        }
        var s1m = FindAnyObjectByType<Scenario1Manager>();

        bool isHarnessWorn = (scenarioManager != null && scenarioManager.IsHarnessEquipped) || (s1m != null && s1m.IsHarnessEquipped);
        bool isHarnessCorrect = (s1m != null && s1m.IsHarnessCorrectlyWorn);

        // 1. Emniyet kemeri giyilmemişse kesinlikle lanyard alınamaz!
        if (!isHarnessWorn)
        {
            Debug.LogWarning("Lanyard: Emniyet kemeri takılmadan lanyard alınamaz!");
            if (scenarioManager != null)
            {
                scenarioManager.ShowHarnessWarning();
            }
            else if (s1m != null)
            {
                s1m.ShowCustomWarning("Öncelikle emniyet kemerinizi giyin!");
            }
            return false;
        }

        // 2. Eğitim modunda kemer doğru/kilitli giyilmemişse de lanyard alınamaz!
        if (s1m != null && s1m.currentMode == ScenarioMode.TRAINING && !isHarnessCorrect)
        {
            Debug.LogWarning("Lanyard: Eğitim modunda kemer doğru giyilmeden (kilitli) lanyard alınamaz!");
            if (scenarioManager != null)
            {
                scenarioManager.ShowCustomWarning("Önce emniyet kemerini kilitli giyin!");
            }
            else if (s1m != null)
            {
                s1m.ShowCustomWarning("Önce emniyet kemerini kilitli giyin!");
            }
            return false;
        }

        // 3. Eğitim modunda güvensiz lanyard engelleme
        if (s1m != null && s1m.currentMode == ScenarioMode.TRAINING && !isSafe)
        {
            Debug.LogWarning("Lanyard: Eğitim modunda güvensiz lanyard seçemezsiniz!");
            if (scenarioManager != null)
            {
                scenarioManager.ShowCustomWarning("Şok emicili (yeşil) lanyardı seçin!");
            }
            else if (s1m != null)
            {
                s1m.ShowCustomWarning("Şok emicili (yeşil) lanyardı seçin!");
            }
            return false;
        }

        if (isGrabbed) return true;

        isGrabbed = true;
        activeController = handTransform;
        Debug.Log("Lanyard: Lanyard el/kamera ile tutuldu.");

        // Kameraya veya ele bağlama işlemi
        transform.SetParent(handTransform);

        // --- KESİN ÖNÜNDE GÖSTERME AYARI ---
        if (handTransform.CompareTag("MainCamera") || handTransform.name.ToLower().Contains("camera"))
        {
            // Kameranın baktığı yönü baz alarak 1.5 metre önüne, hafif sağa ve aşağıya yerleştirir
            transform.position = handTransform.position + (handTransform.forward * 1.5f) + (handTransform.right * 0.3f) + (handTransform.up * -0.2f);
            transform.rotation = handTransform.rotation * Quaternion.Euler(0, -90, 0);
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        transform.localScale = Vector3.one;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        }

        if (scenarioManager != null)
        {
            scenarioManager.OnLanyardGrabbed(isSafe);
        }
        else
        {
            Debug.LogWarning("Lanyard: HarnessScenarioManager bulunamadı! Doğrudan Scenario1Manager tetikleniyor.");
            if (s1m != null)
            {
                s1m.SelectLanyard(isSafe);
            }
        }

        Debug.Log($"[KONTROL] Lanyard başarıyla ele alındı (tutan ele yerleşti). Pozisyon: {transform.localPosition}");
        return true;
    }

    /// <summary>
    /// Lanyardı bağlı olduğu ankrajdan ayırır ve otomatik olarak tekrar elimize geri verir.
    /// </summary>
    private void DetachFromAnchorAndGrab()
    {
        DetachFromAnchor();

        // Geri sökünce otomatik olarak kameraya/ele geri yapışsın
        Transform hand = rightController != null ? rightController : (Camera.main != null ? Camera.main.transform : transform);
        GrabLanyard(hand);
    }

    /// <summary>
    /// Oyuncu L tuşuna bastığında yakındaki demire bağlama dener.
    /// </summary>
    private void TryKeyboardAttach()
    {
        ScaffoldAnchorPoint[] anchors = FindObjectsByType<ScaffoldAnchorPoint>(FindObjectsSortMode.None);
        ScaffoldAnchorPoint closestAnchor = null;

        float maxClickDistance = 20.0f;
        float minDistance = maxClickDistance;

        // Mesafeyi senin durduğun yerin koordinatına göre hesaplar
        Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : transform.position;

        foreach (var anchor in anchors)
        {
            if (anchor == null || !anchor.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(playerPos, anchor.transform.position);

            if (dist < minDistance)
            {
                minDistance = dist;
                closestAnchor = anchor;
            }
        }

        if (closestAnchor != null)
        {
            AttachToAnchor(closestAnchor);
        }
        else
        {
            float testMinDist = 999f;
            foreach (var anchor in anchors)
            {
                if (anchor != null)
                {
                    float d = Vector3.Distance(playerPos, anchor.transform.position);
                    if (d < testMinDist) testMinDist = d;
                }
            }
            Debug.LogWarning($"Lanyard: Yakınınızda iskele demiri algılanamadı. Ölçülen en yakın mesafe: {testMinDist:F2} metre. Limit: 20 metre.");
        }
    }

    /// <summary>
    /// Lanyard kancasını belirtilen ankraj noktasına bağlar.
    /// Kancanın ucu (hookPoint) tam hedefe oturacak şekilde otomatik hizalama yapar.
    /// </summary>
    public void AttachToAnchor(ScaffoldAnchorPoint anchor)
    {
        if (anchor == null) return;

        if (attachedAnchor != null)
        {
            DetachFromAnchor();
        }

        attachedAnchor = anchor;
        isGrabbed = false; // Artık elde değil, demire bağlı

        // Kontrolcüden/Kameradan ayır (Unparent)
        transform.SetParent(null);

        // 1. Önce rotasyonu demire eşitle
        transform.rotation = anchor.transform.rotation;

        // 2. Kancanın ucu (AttachPoint) ile ana nesne (lanyard) arasındaki offseti hesapla
        Transform referencePoint = hookPoint != null ? hookPoint : transform;
        Vector3 offset = referencePoint.position - transform.position;

        // 3. Ana nesneyi kaydırarak kancanın ucunu tam demire oturt
        transform.position = anchor.transform.position - offset;

        Debug.Log($"Lanyard: Ankraja bağlandı (L Tuşu ile): {anchor.name}");

        anchor.OnLanyardAttached(this);
    }

    /// <summary>
    /// Lanyardı bağlı olduğu ankrajdan ayırır.
    /// </summary>
    public void DetachFromAnchor()
    {
        if (attachedAnchor == null) return;

        Debug.Log($"Lanyard: Ankrajdan ayrıldı: {attachedAnchor.name}");

        attachedAnchor.OnLanyardDetached();
        attachedAnchor = null;
    }

    // Bilgisayardan sadece İLK ALMA işlemi için mouse tıklama desteği
    private void OnMouseDown()
    {
        // YALNIZCA masada duruyorsa tıklayıp elimize alalım. 
        if (!isGrabbed && attachedAnchor == null)
        {
            Transform hand = rightController != null ? rightController : (Camera.main != null ? Camera.main.transform : transform);
            GrabLanyard(hand);
        }
    }

    // XR Interaction Toolkit etkileşim dinleyicileri
    public void OnSelectEntered()
    {
        Transform hand = rightController != null ? rightController : (Camera.main != null ? Camera.main.transform : transform);
        GrabLanyard(hand);
    }

    private void ForceDrop()
    {
        isGrabbed = false;
        if (lineRenderer != null) lineRenderer.enabled = false;
        transform.SetParent(null);
    }
}