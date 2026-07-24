using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Emniyet kemeri ve lanyard senaryosunun ana yöneticisidir.
/// Kemer takılma ve lanyard tutulma durumlarını tutar, İSG uyarı mesajlarını yönetir.
/// </summary>
public class HarnessScenarioManager : MonoBehaviour
{
    [Header("Oyuncu Referansları")]
    [Tooltip("Oyuncu Rig GameObject'i (XR Origin / Player)")]
    public GameObject playerGameObject;

    [Tooltip("Oyuncu gövde ve ayak takip scripti")]
    public VRBodyFollow playerBodyFollow;

    [Tooltip("Oyuncu düşme ve başarı kontrol scripti")]
    public PlayerFallController playerFallController;

    [Header("İSG Uyarı Arayüzü")]
    [Tooltip("İSG Uyarı Canvas'ı (Kemer takılmadığında belirecek panel)")]
    public GameObject warningCanvas;

    [Tooltip("Uyarı mesajının yazılacağı Text bileşeni")]
    public Text warningText;

    [Header("Geliştirici Kısayol Ayarları")]
    [Tooltip("Kask takmadan klavye üzerinden test etmeyi aktif eder")]
    public bool enableKeyboardDebug = true;

    // Durum Değişkenleri (Property'ler üzerinden dışarıya açılır)
    public bool IsHarnessEquipped { get; set; } = false;
    public bool IsLanyardGrabbed { get; set; } = false;

    private Coroutine warningCoroutine;

    private void Start()
    {
        if (playerGameObject == null)
        {
            // Sahnedeki XR Origin'i bulmaya çalış
            var xrOrigin = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator>();
            if (xrOrigin != null) playerGameObject = xrOrigin.gameObject;
            else
            {
                var fallbackOrigin = GameObject.Find("XR Origin (XR Rig)");
                if (fallbackOrigin != null) playerGameObject = fallbackOrigin;
            }
        }

        if (playerFallController == null && playerGameObject != null)
        {
            playerFallController = playerGameObject.GetComponent<PlayerFallController>();
        }

        if (playerBodyFollow == null)
        {
            playerBodyFollow = FindAnyObjectByType<VRBodyFollow>();
        }

        // VR gözlük kafa hareketlerini takip eden dinamik uyarı ekranını oluştur
        CreateDynamicWarningCanvas();
    }

    private void Update()
    {
        // Editör/Gözlüksüz testler için klavye desteği
        if (enableKeyboardDebug)
        {
            var table = GameObject.Find("Safety Equipment Table");
            Transform cam = Camera.main != null ? Camera.main.transform : transform;
            float dist = table != null ? Vector3.Distance(cam.position, table.transform.position) : 100f;

#if ENABLE_INPUT_SYSTEM
            bool hKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.hKey.wasPressedThisFrame;
            bool lKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame;
            bool fKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
#else
            bool hKeyPressed = Input.GetKeyDown(KeyCode.H);
            bool lKeyPressed = Input.GetKeyDown(KeyCode.L);
            bool fKeyPressed = Input.GetKeyDown(KeyCode.F);
#endif

            // Masaya yakınken (3.5 metreden yakın) H ve L tuşlarıyla etkileşim sağlanabilir
            if (dist < 3.5f)
            {
                if (hKeyPressed && !IsHarnessEquipped)
                {
                    var safetyHarness = FindAnyObjectByType<SafetyHarness>();
                    if (safetyHarness != null && safetyHarness.gameObject.activeInHierarchy)
                    {
                        Debug.Log("Debug: [H] tuşu ile masadaki kemer kuşanıldı.");
                        safetyHarness.Equip();
                    }
                }
                if (lKeyPressed && !IsLanyardGrabbed)
                {
                    var lanyard = FindAnyObjectByType<Lanyard>();
                    if (lanyard != null && lanyard.gameObject.activeInHierarchy)
                    {
                        Debug.Log("Debug: [L] tuşu ile masadaki lanyard alındı.");
                        lanyard.OnSelectEntered();
                    }
                }
            }

            if (fKeyPressed)
            {
                Debug.Log("Debug: [F] tuşu ile düşme efekti simüle edildi.");
                if (playerFallController != null)
                {
                    playerFallController.StartFall();
                }
            }
        }
    }

    /// <summary>
    /// Emniyet kemeri masadan tıklandığında/alındığında çağrılır.
    /// </summary>
    public void OnHarnessEquipped()
    {
        if (IsHarnessEquipped) return;
        IsHarnessEquipped = true;
        
        Debug.Log("HarnessScenarioManager: Emniyet kemeri başarıyla giyildi.");

        // Oyuncu gövdesindeki kemeri aktif yap
        if (playerBodyFollow != null)
        {
            playerBodyFollow.SetHarnessActive(true);
        }

        // Aktif senaryo yöneticilerine haber ver
        var s1m = FindAnyObjectByType<Scenario1Manager>();
        if (s1m != null)
        {
            s1m.EquipHarness(false); // Başlangıçta gevşek giyilmiş olarak bildirilir
        }
        var s5m = FindAnyObjectByType<Scenario5Manager>();
        if (s5m != null)
        {
            s5m.PerformAction(Scenario5Action.EquipHarness);
        }
    }

    /// <summary>
    /// Lanyard masadan tıklandığında/alındığında çağrılır.
    /// </summary>
    public void OnLanyardGrabbed(bool isSafe = true)
    {
        if (IsLanyardGrabbed) return;
        IsLanyardGrabbed = true;

        Debug.Log("HarnessScenarioManager: Lanyard ele alındı. Güvenli mi: " + isSafe);

        // Aktif senaryo yöneticilerine haber ver
        var s1m = FindAnyObjectByType<Scenario1Manager>();
        if (s1m != null)
        {
            s1m.SelectLanyard(isSafe);
        }
    }

    /// <summary>
    /// Kemer giyilmeden kısıtlı bir işlem yapılmaya çalışıldığında uyarı tetikler.
    /// </summary>
    public void ShowHarnessWarning()
    {
        if (warningCanvas == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(ShowWarningRoutine("Öncelikle emniyet kemerinizi giyin!", 3.0f));
    }

    /// <summary>
    /// Ekranda istenilen özel bir uyarı mesajını gösterir.
    /// </summary>
    public void ShowCustomWarning(string message)
    {
        if (warningCanvas == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(ShowWarningRoutine(message, 3.0f));
    }

    private IEnumerator ShowWarningRoutine(string message, float duration)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }

        warningCanvas.SetActive(true);
        yield return new WaitForSeconds(duration);
        warningCanvas.SetActive(false);
        warningCoroutine = null;
    }

    /// <summary>
    /// Lanyard sağlam/güvenli bir ankraja başarıyla bağlandığında çağrılır.
    /// </summary>
    public void OnAnchorConnected(ScaffoldAnchorPoint anchor)
    {
        Debug.Log($"HarnessScenarioManager: Güvenli ankraj sağlandı: {anchor.name}");
        if (playerFallController != null)
        {
            playerFallController.TriggerSuccess();
        }
    }

    /// <summary>
    /// Lanyard kırılgan/çürük bir ankraja bağlandığında çağrılır.
    /// </summary>
    [Header("Plastik Bot Uyarı Arayüzü")]
    [Tooltip("Güvensiz ankraja bağlandığında belirecek plastik bot görseli içeren Canvas")]
    public GameObject plasticBootWarningCanvas;

    /// <summary>
    /// Lanyard kırılgan/çürük bir ankraja bağlandığında çağrılır.
    /// </summary>
    public void OnAnchorFailed(ScaffoldAnchorPoint anchor)
    {
        Debug.LogWarning($"HarnessScenarioManager: Çürük ankraj tıklandı/bağlandı: {anchor.name}. Plastik bot uyarısı gösteriliyor.");
        StartCoroutine(PlasticBootWarningRoutine());
    }

    private IEnumerator PlasticBootWarningRoutine()
    {
        // 1. Uyarı Canvas'ını aç
        if (plasticBootWarningCanvas != null)
        {
            plasticBootWarningCanvas.SetActive(true);
        }
        
        // 2. Oyuncunun uyarıyı okuması için 3 saniye bekle
        yield return new WaitForSeconds(3.0f);
        
        // 3. Uyarıyı kapat
        if (plasticBootWarningCanvas != null)
        {
            plasticBootWarningCanvas.SetActive(false);
        }
        
        // 4. Düşüşü tetikle (Ekran sallanması ve düşüş)
        if (playerFallController != null)
        {
            playerFallController.StartFall();
        }
    }

    /// <summary>
    /// Dinamik olarak kameranın önünde bir Uyarı Canvas'ı oluşturur.
    /// </summary>
    private void CreateDynamicWarningCanvas()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : transform;

        GameObject canvasGo = new GameObject("DynamicWarningCanvas");
        canvasGo.transform.SetParent(cam);
        canvasGo.transform.localPosition = new Vector3(0, 0.15f, 1.8f); // 1.8 metre önünde, göz hizasının hafif üstünde
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f); // Ölçeği küçültüyoruz (clipping'i ve devasa boyutu engeller)

        warningCanvas = canvasGo;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(800f, 250f); // Gerçekçi arayüz çözünürlüğü

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        // Arka plan paneli
        GameObject panelGo = new GameObject("BackgroundPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f); // Yarı şeffaf siyah arka plan
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Alt turuncu çizgi
        GameObject borderGo = new GameObject("BorderLine");
        borderGo.transform.SetParent(canvasGo.transform, false);
        Image borderImage = borderGo.AddComponent<Image>();
        borderImage.color = new Color(1.0f, 0.6f, 0.0f); // Turuncu
        RectTransform borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0, 0);
        borderRect.anchorMax = new Vector2(1, 0.05f);
        borderRect.sizeDelta = Vector2.zero;

        // Uyarı Metni
        GameObject textGo = new GameObject("WarningText");
        textGo.transform.SetParent(canvasGo.transform, false);
        warningText = textGo.AddComponent<Text>();
        warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (warningText.font == null)
        {
            warningText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        warningText.fontSize = 28; // Rahat okunabilir piksel boyutu
        warningText.alignment = TextAnchor.MiddleCenter;
        warningText.horizontalOverflow = HorizontalWrapMode.Wrap;
        warningText.verticalOverflow = VerticalWrapMode.Overflow;
        warningText.color = Color.yellow;
        warningText.text = "";
        
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-40, -40); // Metnin kenarlara taşmasını önleyen pay
        
        warningCanvas.SetActive(false);
    }
}
