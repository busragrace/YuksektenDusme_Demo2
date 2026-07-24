using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyuncunun düşmesini, kamera sarsıntısını (Camera Shake), ekranın kararmasını/beyazlamasını 
/// ve başarı/başarısızlık mesaj ekranlarının yönetimini gerçekleştirir.
/// </summary>
public class PlayerFallController : MonoBehaviour
{
    [Header("Düşme Ayarları")]
    [Tooltip("Düşüş hızı ivmesi (Yerçekimi gücü)")]
    public float gravityMultiplier = 2.0f;
    
    [Tooltip("Zemin Y yüksekliği (Bu yüksekliğe gelince düşüş tamamlanır)")]
    public float groundY = 0.5f;

    [Header("Kamera Sarsıntı Ayarları")]
    [Tooltip("Düşerken kameranın ne kadar şiddetli sarsılacağı")]
    public float shakeMagnitude = 0.15f;

    [Header("UI Elemanları")]
    [Tooltip("Ekran karartma efekti için Canvas")]
    public Canvas fadeCanvas;

    [Tooltip("Senin hazırladığın asıl ölüm ekranı")]
    public GameObject customWarningCanvas; // <-- BU SATIRI EKLE

    [Tooltip("Fade Image bileşeni")]
    public Image fadeImage;

    [Tooltip("Mesajları gösterecek olan Text bileşeni")]
    public Text statusMessageText;

    [Header("Ses Efektleri")]
    [Tooltip("Düşerken çalacak rüzgar/düşme sesi")]
    public AudioClip fallingSound;

    [Tooltip("Yere çakılınca çalacak ses")]
    public AudioClip crashSound;

    [Tooltip("Başarı durumunda çalacak tebrik sesi")]
    public AudioClip successSound;

    private bool isFalling = false;
    private Vector3 velocity = Vector3.zero;
    private AudioSource audioSource;

    private Coroutine shakeCoroutine;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Canvases oluştur veya bul
        //CreateDynamicCanvases();
    }

    private void Update()
    {
        // F tuşuna basıldığında manuel düşüşü tetikle
#if ENABLE_INPUT_SYSTEM
        bool fKeyPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
#else
        bool fKeyPressed = Input.GetKeyDown(KeyCode.F);
#endif
        if (fKeyPressed && !isFalling)
        {
            StartFall();
        }

        if (isFalling)
        {
            velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            if (transform.position.y <= groundY)
            {
                Vector3 pos = transform.position;
                pos.y = groundY;
                transform.position = pos;
                
                OnHitGround();
            }
        }
    }

    /// <summary>
    /// Kırılgan demir koptuğunda veya boşluğa adım atıldığında düşme ve başarısızlık senaryosunu tetikler.
    /// </summary>
    public void StartFall()
    {
        if (isFalling) return;

        isFalling = true;
        velocity = Vector3.zero;

        Debug.Log("PlayerFallController: Düşüş ve başarısızlık senaryosu tetiklendi!");

        // Canvas ve ölüm ekranı yapılandırmasını zorla
        //CreateDynamicCanvases();

        // Kamera sarsıntısını başlat (Düşme süresince)
        // Eski hali: StartCoroutine(CameraShakeRoutine(2.0f, shakeMagnitude));
        // Yeni hali:
        shakeCoroutine = StartCoroutine(CameraShakeRoutine(2.0f, shakeMagnitude));

        // Düşüş sesi çal
        if (audioSource != null && fallingSound != null)
        {
            audioSource.clip = fallingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Ekranı SİYAH yap
        StartCoroutine(FadeToColorRoutine(Color.black, 1.5f));
    }

    /// <summary>
    /// Başarılı bağlantı yapıldığında çağrılır (Beyaz ekran ve başarı mesajı).
    /// </summary>
    public void TriggerSuccess()
    {
        Debug.Log("PlayerFallController: Başarı senaryosu tetiklendi!");

        // Başarı sesi çal
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }

        // Ekranı BEYAZ yap
        StartCoroutine(FadeToColorRoutine(Color.white, 1.5f));

        // Başarı mesajını göster
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(true);
            statusMessageText.color = Color.black; // Beyaz ekran üzerinde siyah yazı daha okunaklı olur
            statusMessageText.text = "TEBRİKLER\n\nSimülasyon başarıyla tamamlandı!\nEmniyet kemerini doğru ve sağlam ankraja bağladınız.";
        }
    }

    /// <summary>
    /// Oyuncu yere çakıldığında çağrılır.
    /// </summary>
    private void OnHitGround()
    {
        isFalling = false;
        Debug.Log("PlayerFallController: Oyuncu yere ulaştı.");

        // === SARSINTIYI BURADA DURDURUYORUZ ===
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            // Kameranın açısını düzeltmek için:
            Transform shakeTarget = Camera.main != null ? Camera.main.transform.parent : null;
            if (shakeTarget == null && Camera.main != null) shakeTarget = Camera.main.transform;
            if (shakeTarget != null) shakeTarget.localPosition = Vector3.zero;
        }

        // Düşme sesini durdur, çakılma sesini çal
        if (audioSource != null)
        {
            audioSource.Stop();
            if (crashSound != null)
            {
                audioSource.PlayOneShot(crashSound);
            }
        }

        // Ekranı tamamen siyah yap
        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
        }

        // Başarısızlık mesajını göster
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(true);
            statusMessageText.color = Color.red;
            statusMessageText.text = "Düştünüz. Simülasyon Bitmiştir.\n\nKırılgan/Çürük bir demire bağlandığınız için iskeleden düştünüz.";
        }

        // Senin hazırladığın Canvas'ı açar
        if (customWarningCanvas != null)
        {
            customWarningCanvas.SetActive(true);
        }

        // === BURAYI EKLE ===
        Time.timeScale = 0f; // Oyun zamanını tamamen dondurur, oyuncu artık hareket edemez!
    }

    /// <summary>
    /// Kamera sarsıntı Coroutine'i (Kamera parent'ı varsa onu, yoksa kamerayı sarsar)
    /// </summary>
    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Transform shakeTarget = Camera.main != null ? Camera.main.transform.parent : null;
        if (shakeTarget == null && Camera.main != null) shakeTarget = Camera.main.transform;
        if (shakeTarget == null) yield break;

        Vector3 originalLocalPos = shakeTarget.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            shakeTarget.localPosition = originalLocalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalLocalPos;
    }

    /// <summary>
    /// Ekranın belirlenen renge yavaşça bürünmesini sağlar.
    /// </summary>
    private IEnumerator FadeToColorRoutine(Color targetColor, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color initialColor = targetColor;
        initialColor.a = 0f;
        fadeImage.color = initialColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color temp = targetColor;
            temp.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = temp;
            yield return null;
        }

        Color final = targetColor;
        final.a = 1f;
        fadeImage.color = final;
    }

    private void CreateDynamicCanvases()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : transform;

        // 1. DynamicFadeCanvas (Siyah Perde)
        GameObject fadeGo = GameObject.Find("DynamicFadeCanvas");
        if (fadeGo == null)
        {
            fadeGo = new GameObject("DynamicFadeCanvas");
            fadeGo.transform.SetParent(cam);
            fadeGo.transform.localPosition = new Vector3(0, 0, 0.3f);
            fadeGo.transform.localRotation = Quaternion.identity;
        }
        fadeCanvas = fadeGo.GetComponent<Canvas>() ?? fadeGo.AddComponent<Canvas>();
        ConfigureGameOverCanvas(fadeCanvas, 998); // Perde katmanı 998

        fadeImage = fadeGo.GetComponentInChildren<Image>();
        if (fadeImage == null)
        {
            GameObject imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(fadeGo.transform, false);
            fadeImage = imageGo.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            RectTransform imgRect = imageGo.GetComponent<RectTransform>();
            imgRect.anchorMin = Vector2.zero;
            imgRect.anchorMax = Vector2.one;
            imgRect.sizeDelta = Vector2.zero;
        }

        // 2. DynamicWarningCanvas (Ölüm/Başarı Yazı Arayüzü)
        GameObject warningGo = GameObject.Find("DynamicWarningCanvas");
        if (warningGo == null)
        {
            warningGo = new GameObject("DynamicWarningCanvas");
            warningGo.transform.SetParent(cam);
            warningGo.transform.localPosition = new Vector3(0, 0, 0.28f); // Kameraya daha yakın
            warningGo.transform.localRotation = Quaternion.identity;
        }
        Canvas warningCanvas = warningGo.GetComponent<Canvas>() ?? warningGo.AddComponent<Canvas>();
        ConfigureGameOverCanvas(warningCanvas, 999); // Metin katmanı en üstte 999!

        statusMessageText = warningGo.GetComponentInChildren<Text>();
        if (statusMessageText == null)
        {
            GameObject textGo = new GameObject("StatusMessageText");
            textGo.transform.SetParent(warningGo.transform, false);
            statusMessageText = textGo.AddComponent<Text>();
            statusMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusMessageText.fontSize = 64; // Screen space için 64 harika!
            statusMessageText.alignment = TextAnchor.MiddleCenter;
            statusMessageText.color = Color.white;
            statusMessageText.text = "";
            
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
    }

    private void ConfigureGameOverCanvas(Canvas canvas, int sortingOrder)
    {
        if (canvas == null) return;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        canvas.sortingOrder = sortingOrder;

        // Boyutlarını 1920x1080 olarak güncelle
        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(1920f, 1080f);
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
