using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public enum Scenario1State
{
    INIT,
    BRIEFING,
    PREPARATION,
    SAFETY_CHECK,
    CLIMB,
    WORK,
    DISTURBANCE,
    FALL_OR_RECOVERY,
    RESULT
}

public enum ScenarioMode
{
    TRAINING, // Eğitim Modu (Canlı İpuçları & Rehberlik)
    EXAM      // Sınav Modu (İpuçları Kapalı & 100 Puanlık Değerlendirme)
}

public class Scenario1Manager : MonoBehaviour
{
    [Header("UI Panelleri")]
    public GameObject briefingPanel;
    public GameObject resultPanel;
    public GameObject checklistPanel;
    public GameObject modeSelectionGroup;   // Mod secim butonlarini (Egitim/Sinav) tutan panel/grup
    public GameObject startBriefingButton;  // Ilk basilacak "Baslat" butonu

    [Header("UI Metinleri")]
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI metricsText;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultDetailText;

    [Header("Senaryo Nesneleri")]
    public GameObject harnessVisual;
    public LineRenderer lanyardLine;
    public Transform lanyardStartPoint;
    public Transform safeAnchorPoint;
    public Transform unsafeAnchorPoint;
    public Transform playerTransform; // Kamera veya oyuncu rigi
    public Transform fallTarget;

    [Header("Ses Efektleri")]
    public AudioClip buckleSound;
    public AudioClip windSound;

    private AudioSource audioSource;
    private AudioSource windAudioSource;

    [Header("Ayarlar")]
    public float shakeIntensity = 0.05f;
    public float shakeDuration = 3f;
    public bool writeEventLog = true;

    // Durum Makinesi
    private Scenario1State currentState = Scenario1State.INIT;

    // Senaryo Verileri
    private float scenarioStartTime;
    private float harnessEquippedTime = -1f;
    private float anchorSelectedTime = -1f;
    private float totalTime;

    private bool harnessEquipped;
    public bool IsHarnessEquipped => harnessEquipped;
    private bool isHarnessCorrectlyWorn; // Button pattern completed (Doğru vs Gevşek Kemer)
    public bool IsHarnessCorrectlyWorn => isHarnessCorrectlyWorn;
    public bool IsAnchorSelected => anchorSelected;
    public bool IsSafeAnchorSelected => safeAnchorSelected;
    public bool IsCollectiveProtectionChecked => collectiveProtectionChecked;
    private bool harnessPatternStep1; // Sol Bacak Bandı (Sol Grip)
    private bool harnessPatternStep2; // Sağ Bacak Bandı (Sağ Grip)
    private bool harnessPatternStep3; // Göğüs Tokası (A / Trigger)
    private bool correctLanyardSelected;
    private bool anchorSelected;
    private bool safeAnchorSelected;
    private bool collectiveProtectionChecked; // Kurtay'ın modülünden gelecek
    private bool enteredScaffoldWithoutKkd;
    private bool fallEventOccurred;
    private bool fallCaughtByLanyard;
    private bool safeRetreatSelected; // Kullanıcı riski fark edip güvenli bölgeye kaçtı mı?

    private List<string> warnings = new List<string>();
    private Coroutine currentRoutine;
    private Vector3 playerOriginalPosition;

    private const string LogFileName = "scenario1_log.jsonl";

    private void Start()
    {
        if (playerTransform != null)
        {
            playerOriginalPosition = playerTransform.position;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        windAudioSource = gameObject.AddComponent<AudioSource>();
        windAudioSource.loop = true;
        windAudioSource.playOnAwake = false;
        windAudioSource.volume = 0f;

        ResetScenario();
    }

    public void ResetScenario()
    {
        currentState = Scenario1State.INIT;
        scenarioStartTime = Time.time;
        harnessEquippedTime = -1f;
        anchorSelectedTime = -1f;
        totalTime = 0f;

        harnessEquipped = false;
        isHarnessCorrectlyWorn = false;
        harnessPatternStep1 = false;
        harnessPatternStep2 = false;
        harnessPatternStep3 = false;
        correctLanyardSelected = false;
        anchorSelected = false;
        safeAnchorSelected = false;
        collectiveProtectionChecked = false;
        enteredScaffoldWithoutKkd = false;
        fallEventOccurred = false;
        fallCaughtByLanyard = false;
        safeRetreatSelected = false;

        // HarnessScenarioManager senkronizasyonu sıfırla
        var hsm = FindAnyObjectByType<HarnessScenarioManager>();
        if (hsm != null)
        {
            hsm.IsHarnessEquipped = false;
            hsm.IsLanyardGrabbed = false;
            if (hsm.playerBodyFollow != null)
            {
                hsm.playerBodyFollow.SetHarnessActive(false);
            }
        }

        warnings.Clear();

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (playerTransform != null)
        {
            playerTransform.position = playerOriginalPosition;
        }

        if (harnessVisual != null) harnessVisual.SetActive(true);
        if (lanyardLine != null) lanyardLine.gameObject.SetActive(false);

        // UI Durumları
        if (briefingPanel != null) briefingPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (checklistPanel != null) checklistPanel.SetActive(false);
        if (startBriefingButton != null) startBriefingButton.SetActive(true);
        if (modeSelectionGroup != null) modeSelectionGroup.SetActive(false);

        for (int i = 0; i < 4; i++)
        {
            UpdateChecklistToggle(i, false, true); // Sifirla ve tiklanabilir yap
        }

        RecordEvent("scenario_init", "Senaryo 1 baslatildi.");
        SetState(Scenario1State.BRIEFING);
    }

    private void Update()
    {
        // Lanyard çizgisi güncellemesi
        if (lanyardLine != null && lanyardLine.gameObject.activeSelf)
        {
            Transform targetAnchor = safeAnchorSelected ? safeAnchorPoint : unsafeAnchorPoint;
            if (targetAnchor != null)
            {
                // Eğer oyuncu kemeri giydiyse ip oyuncunun sırtından (kamerasından) ankraja uzanmalı!
                Vector3 startPos = (harnessEquipped && playerTransform != null) 
                    ? playerTransform.position + new Vector3(0f, 1.3f, -0.1f) // Oyuncunun sırt seviyesi (Rig yüksekliğine göre yaklaşık 1.3m yükseklik ve arkası)
                    : (lanyardStartPoint != null ? lanyardStartPoint.position : Vector3.zero);

                lanyardLine.SetPosition(0, startPos);
                lanyardLine.SetPosition(1, targetAnchor.position);
            }
        }

        // Meta Quest 3 VR Kumanda Tuş Örüntüsü Dinleyicisi (Sol Grip -> Sağ Grip -> Trigger)
        CheckVRControllerPatternInputs();

        // --- TEST KLAVYE KISAYOLLARI (Yeni Input System API ile) ---
        if (Application.isEditor && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) CompleteBriefing();
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                EquipHarness(true); // Hile ile kemer doğru giyildi sayılır
                CheckPreparationCompletion();
            }
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SelectLanyard(true);
                CheckPreparationCompletion();
            }
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                // Klavye Hilesi: Ankraj kilidini bypass et
                anchorSelected = true;
                safeAnchorSelected = true;
                anchorSelectedTime = Time.time;
                if (lanyardLine != null) lanyardLine.gameObject.SetActive(true);
                RecordEvent("anchor_attached", "Guvenli ankraja baglanildi (Klavye Hilesi).");
                CheckSafetyCheckCompletion();
                RefreshUi();
                Debug.Log("Scenario 1 | HİLE: Güvenli ankraj bağlantısı klavyeyle yapıldı (Kilit Bypass).");
            }
            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                // Klavye Hilesi: Ankraj kilidini bypass et
                anchorSelected = true;
                safeAnchorSelected = false;
                anchorSelectedTime = Time.time;
                if (lanyardLine != null) lanyardLine.gameObject.SetActive(true);
                RecordEvent("anchor_attached", "Guvensiz ankraja baglanildi (Klavye Hilesi).");
                CheckSafetyCheckCompletion();
                RefreshUi();
                Debug.Log("Scenario 1 | HİLE: Güvensiz ankraj bağlantısı klavyeyle yapıldı (Kilit Bypass).");
            }
            if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                // Klavye Hilesi: Toplu koruma kilidini bypass et
                collectiveProtectionChecked = true;
                RecordEvent("collective_protection_checked", "Toplu koruma kontrolleri tamamlandı (Klavye Hilesi).");
                CheckSafetyCheckCompletion();
                RefreshUi();
                Debug.Log("Scenario 1 | HİLE: Toplu koruma kontrolleri klavyeyle yapıldı (Kilit Bypass).");
            }
            if (Keyboard.current.digit7Key.wasPressedThisFrame) AttemptClimb();
            if (Keyboard.current.digit8Key.wasPressedThisFrame) CompleteWork();
            if (Keyboard.current.digit9Key.wasPressedThisFrame) SafeRetreat(); // 9 tuşu ile geri çekilme testi
            if (Keyboard.current.rKey.wasPressedThisFrame) ResetScenario();
        }
    }

    private bool lastLeftGripState = false;
    private bool lastRightGripState = false;
    private bool lastRightTriggerState = false;

    private void CheckVRControllerPatternInputs()
    {
        // Sadece Senaryo Başlat butonuna tıklandıktan (PREPARATION) sonra kilitler aktifleşir!
        if (currentState != Scenario1State.PREPARATION && currentState != Scenario1State.SAFETY_CHECK) return;

        var leftHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
        var rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);

        if (leftHand.isValid && leftHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool leftGripPressed))
        {
            if (leftGripPressed && !lastLeftGripState)
            {
                ProcessHarnessPatternStep(1); // Sol Bacak Bandı Sıkıldı
            }
            lastLeftGripState = leftGripPressed;
        }

        if (rightHand.isValid && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool rightGripPressed))
        {
            if (rightGripPressed && !lastRightGripState)
            {
                ProcessHarnessPatternStep(2); // Sağ Bacak Bandı Sıkıldı
            }
            lastRightGripState = rightGripPressed;
        }

        if (rightHand.isValid && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool rightTriggerPressed))
        {
            if (rightTriggerPressed && !lastRightTriggerState)
            {
                ProcessHarnessPatternStep(3); // Göğüs Tokası Kilitlendi
            }
            lastRightTriggerState = rightTriggerPressed;
        }
    }

    public void SetState(Scenario1State newState)
    {
        currentState = newState;
        Debug.Log("Scenario 1 | State changed to: " + newState);
        RefreshUi();
    }

    // --- DIŞARIDAN TETİKLENECEK API FONKSİYONLARI (KOPYALA/YAPIŞTIR YAPILABİLİR) ---

    // 1. Brifing Tamamlandı
    public void CompleteBriefing()
    {
        if (currentState == Scenario1State.BRIEFING)
        {
            if (briefingPanel != null) briefingPanel.SetActive(false);
            if (checklistPanel != null) checklistPanel.SetActive(true);
            SetState(Scenario1State.PREPARATION);
            RecordEvent("briefing_completed", "Kullanici brifingi tamamladi.");
        }
    }

    // 2. Kemer Giyildi
    public void EquipHarness(bool isCorrect = false)
    {
        if (harnessEquipped) return;

        harnessEquipped = true;
        isHarnessCorrectlyWorn = isCorrect;
        harnessEquippedTime = Time.time;
        if (harnessVisual != null) harnessVisual.SetActive(true);

        // HarnessScenarioManager senkronizasyonu
        var hsm = FindAnyObjectByType<HarnessScenarioManager>();
        if (hsm != null)
        {
            hsm.IsHarnessEquipped = true;
            if (hsm.playerBodyFollow != null)
            {
                hsm.playerBodyFollow.SetHarnessActive(true);
            }
        }

        if (isCorrect)
        {
            RecordEvent("harness_equipped_correct", "Emniyet kemeri tokaları kilitlenerek DOĞRU giyildi.");
        }
        else
        {
            AddWarning("Emniyet kemeri bacak/göğüs tokaları sıkılmadan GEVŞEK giyildi!");
            RecordEvent("harness_equipped_loose", "Emniyet kemeri GEVŞEK/HATALI giyildi!");
        }
        RefreshUi();
    }

    // Kemer Toka Örüntüsü (Sol Grip -> Sağ Grip -> Trigger)
    public void ProcessHarnessPatternStep(int step)
    {
        if (!harnessEquipped) EquipHarness(false); // Önce giyilir

        bool stepSuccess = false;

        if (step == 1 && !harnessPatternStep1)
        {
            harnessPatternStep1 = true;
            stepSuccess = true;
            Debug.Log("Scenario 1 | Kemer: Sol Bacak Bandı Sıkıldı (Sol Grip).");
        }
        else if (step == 2 && harnessPatternStep1 && !harnessPatternStep2)
        {
            harnessPatternStep2 = true;
            stepSuccess = true;
            Debug.Log("Scenario 1 | Kemer: Sağ Bacak Bandı Sıkıldı (Sağ Grip).");
        }
        else if (step == 3 && harnessPatternStep1 && harnessPatternStep2 && !harnessPatternStep3)
        {
            harnessPatternStep3 = true;
            isHarnessCorrectlyWorn = true;
            stepSuccess = true;
            UpdateChecklistToggle(0, true, false); // Kemer dogru giyildi toggle'ini isaretle
            Debug.Log("Scenario 1 | Kemer: Göğüs Tokası Kilitlendi (Trigger) -> KEMER EKSİKSİZ DOĞRU GIYILDI!");
            RecordEvent("harness_pattern_completed", "Kemer tokaları ve bacak bantları buton örüntüsüyle (Sol Grip + Sağ Grip + Trigger) eksiksiz sıkıldı.");
            CheckPreparationCompletion();
        }

        if (stepSuccess && audioSource != null && buckleSound != null)
        {
            audioSource.PlayOneShot(buckleSound);
        }

        RefreshUi();
    }

    // 3. Lanyard Seçildi
    public void SelectLanyard(bool isCorrect)
    {
        if (currentMode == ScenarioMode.TRAINING && !isHarnessCorrectlyWorn)
        {
            Debug.LogWarning("Scenario 1 | ⚠️ ÖNCE EMNİYET KEMERİNİ KİLİTLİ GİYMELİSİNİZ!");
            return;
        }

        correctLanyardSelected = isCorrect;

        // HarnessScenarioManager senkronizasyonu
        var hsm = FindAnyObjectByType<HarnessScenarioManager>();
        if (hsm != null)
        {
            hsm.IsLanyardGrabbed = true;
        }

        if (!isCorrect)
        {
            AddWarning("Yanlis lanyard/karabina secildi!");
            // Eğitim modundaysa puan detayını boş bırakır, sınav modundaysa ekler
            string pointsDetail = currentMode == ScenarioMode.EXAM ? " — 0 Puan / İhlal" : "";
            Debug.Log($"Scenario 1 | ⚠️ GÜVENSİZ LANYARD SEÇİLDİ (Kırmızı Lanyard{pointsDetail}).");
        }
        else
        {
            UpdateChecklistToggle(1, true, false); // Lanyard secildi toggle'ini isaretle
            // Eğitim modundaysa puan detayını boş bırakır, sınav modundaysa ekler
            string pointsDetail = currentMode == ScenarioMode.EXAM ? " — +20 Puan" : "";
            Debug.Log($"Scenario 1 | ✅ GÜVENLİ LANYARD SEÇİLDİ (Yeşil Şok Emicili Lanyard{pointsDetail}).");
            CheckPreparationCompletion();
        }
    }

    // 4. Ankraj Noktasına Bağlanıldı
    public void AttachAnchor(bool isSafe)
    {
        if (currentMode == ScenarioMode.TRAINING && !collectiveProtectionChecked)
        {
            Debug.LogWarning("Scenario 1 | ⚠️ ÖNCE TOPLU KORUMA KONTROLLERİNİ TAMAMLAMALISINIZ!");
            return;
        }

        anchorSelected = true;
        safeAnchorSelected = isSafe;
        anchorSelectedTime = Time.time;

        if (!harnessEquipped)
        {
            AddWarning("Emniyet kemerini kuşanmadan ankraj yapılmaya çalışıldı.");
        }

        if (lanyardLine != null) lanyardLine.gameObject.SetActive(true);

        if (isSafe)
        {
            UpdateChecklistToggle(3, true, false); // Ankraj baglandi toggle'ini isaretle
            CheckSafetyCheckCompletion();
        }

        RecordEvent("anchor_attached", isSafe ? "Guvenli ankraja baglanildi." : "Guvensiz ankraja baglanildi.");
        RefreshUi();
    }

    // 5. Ankraj Çıkarıldı
    public void DetachAnchor()
    {
        anchorSelected = false;
        safeAnchorSelected = false;
        if (lanyardLine != null) lanyardLine.gameObject.SetActive(false);

        RecordEvent("anchor_detached", "Ankraj baglantisi kesildi.");
        RefreshUi();
    }

    // 6. Kurtay'ın modülünden çağrılacak: Toplu Koruma Kontrolleri
    public void SetCollectiveProtectionChecked(bool isOk)
    {
        if (currentMode == ScenarioMode.TRAINING && !correctLanyardSelected)
        {
            Debug.LogWarning("Scenario 1 | ⚠️ ÖNCE GÜVENLİ LANYARDI SEÇMELİSİNİZ!");
            return;
        }

        collectiveProtectionChecked = isOk;
        UpdateChecklistToggle(2, isOk, false); // Toplu koruma toggle'ini guncelle
        RecordEvent("collective_protection_checked", isOk ? "Toplu koruma kontrolleri eksiksiz." : "Toplu koruma eksikligi tespit edildi.");
        if (isOk)
        {
            CheckSafetyCheckCompletion();
        }
        RefreshUi();
    }

    // 7. İskeleye Tırmanma Girişimi
    public void AttemptClimb()
    {
        if (currentMode == ScenarioMode.TRAINING && currentState != Scenario1State.CLIMB && currentState != Scenario1State.WORK)
        {
            string missing = "";
            if (!harnessEquipped) missing += "Kemer Kuşanılmadı, ";
            if (!isHarnessCorrectlyWorn) missing += "Kemer Tokaları Sıkılmadı (Pattern Eksik), ";
            if (!correctLanyardSelected) missing += "Güvenli Lanyard Seçilmedi, ";
            if (!collectiveProtectionChecked) missing += "Toplu Koruma Kilitlenmedi, ";
            if (!anchorSelected || !safeAnchorSelected) missing += "Güvenli Ankraj Yapılmadı, ";
            
            if (missing.EndsWith(", ")) missing = missing.Substring(0, missing.Length - 2);

            Debug.LogWarning($"Scenario 1 | ⚠️ ÖNCE GÜVENLİK GEREKSİNİMLERİNİ TAMAMLAMALISINIZ! Eksikler: [{missing}] CurrentState: {currentState}");
            return;
        }

        SetState(Scenario1State.CLIMB);

        // Kemer veya ankraj yoksa ihlal
        if (!harnessEquipped || !anchorSelected)
        {
            enteredScaffoldWithoutKkd = true;
            AddWarning("KKD kusanim veya ankraj tamamlanmadan iskeleye cikildi!");
            RecordEvent("climb_violation", "Emniyetsiz tirmanis denendi.");
        }

        // Çalışma durumuna geç
        SetState(Scenario1State.WORK);
    }

    // 8. Çalışma Başarılı (Bakım tamamlandı)
    public void CompleteWork()
    {
        if (currentState == Scenario1State.WORK)
        {
            RecordEvent("work_completed", "Bakim gorevi tamamlandi.");
            TriggerDisturbance();
        }
    }

    // 9. Rüzgar / Sarsıntı Başlat
    public void TriggerDisturbance()
    {
        SetState(Scenario1State.DISTURBANCE);
        // Oyuncuya tahliye için 10 saniye süre veriyoruz
        float duration = 10f;
        float intensity = collectiveProtectionChecked ? shakeIntensity : shakeIntensity * 3f;

        currentRoutine = StartCoroutine(DisturbanceRoutine(duration, intensity));
    }

    private IEnumerator DisturbanceRoutine(float duration, float intensity)
    {
        float elapsed = 0f;
        Vector3 origPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        RecordEvent("disturbance_started", "Ruzgar/sarsinti olayi tetiklendi. Baslangic siddeti: " + intensity);

        // Oyuncunun görüş alanında büyük tahliye uyarısı göster
        ShowCustomWarning("⚠️ UYARI: Rüzgar şiddetleniyor! İskeleyi terk edin ve zemin seviyesindeki güvenli bölgeye geri çekilin!", 8f);
        // Konsolda da uyarı göster (Editör testlerinde görünmesi için)
        Debug.LogWarning("⚠️ UYARI: Rüzgar şiddetleniyor! İskeleyi terk edin ve zemin seviyesindeki güvenli bölgeye geri çekilin!");

        // Rüzgar sesini başlat
        if (windAudioSource != null && windSound != null)
        {
            windAudioSource.clip = windSound;
            windAudioSource.volume = 0.1f;
            windAudioSource.Play();
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Eğer sarsıntı devam ederken oyuncu güvenli bölgeye indi ve SafeRetreat tetiklendiyse, sarsıntıyı kes
            if (safeRetreatSelected)
            {
                if (playerTransform != null) playerTransform.position = origPos;
                if (windAudioSource != null) windAudioSource.Stop(); // Sesi kes
                yield break;
            }

            if (playerTransform != null)
            {
                // Rüzgarın şiddetlendiğini hissettirmek için sarsıntı gücünü saniye saniye artırıyoruz
                float t = elapsed / duration;
                float progressiveIntensity = intensity * (1f + t * 2f);
                float x = UnityEngine.Random.Range(-1f, 1f) * progressiveIntensity;
                float z = UnityEngine.Random.Range(-1f, 1f) * progressiveIntensity;
                playerTransform.position = new Vector3(origPos.x + x, playerTransform.position.y, origPos.z + z);

                // Rüzgar sesinin volume değerini de kademeli artırıyoruz (0.1'den 1.0'a)
                if (windAudioSource != null && windSound != null)
                {
                    windAudioSource.volume = Mathf.Lerp(0.1f, 1.0f, t);
                }
            }
            yield return null;
        }

        if (playerTransform != null) playerTransform.position = origPos;
        if (windAudioSource != null) windAudioSource.Stop(); // Süre bitince sesi durdur

        // 10 saniye dolmasına rağmen oyuncu iskelede kaldıysa (SafeRetreat çağrılmadıysa):
        if (currentMode == ScenarioMode.TRAINING)
        {
            // Eğitim modunda tahliye kuralını öğretmek amacıyla her durumda düşüş tetiklenir
            Debug.LogWarning("Scenario 1 | Oyuncu eğitim modunda tahliye uyarısına uymadı!");
            StartCoroutine(FallRoutine());
        }
        else
        {
            // Sınav modunda: Güvenlik önlemleri eksikse (veya kural ihlali varsa) düşer, tamsa başarıyla bitirir
            if (enteredScaffoldWithoutKkd || !collectiveProtectionChecked || !anchorSelected || !safeAnchorSelected)
            {
                Debug.LogWarning("Scenario 1 | Sınav modunda güvenlik önlemleri eksik olduğu için düşüş tetiklenir!");
                StartCoroutine(FallRoutine());
            }
            else
            {
                EndScenario("Görev Başarıyla Tamamlandı (Tam Başarı)");
            }
        }
    }

    // 10. Düşme Animasyonu ve Kontrolü
    private IEnumerator FallRoutine()
    {
        SetState(Scenario1State.FALL_OR_RECOVERY);
        fallEventOccurred = true;
        
        // Kemer ve ankraj varsa lanyard tutar
        fallCaughtByLanyard = harnessEquipped && anchorSelected && safeAnchorSelected;

        float elapsed = 0f;
        float duration = 1.5f;

        Vector3 startPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        // Lanyard tuttuysa havada asılı kalır, tutmadıysa yere (fallTarget) düşer
        Vector3 targetPos = fallCaughtByLanyard 
            ? Vector3.Lerp(startPos, fallTarget != null ? fallTarget.position : startPos, 0.4f) 
            : (fallTarget != null ? fallTarget.position : startPos);

        RecordEvent("fall_triggered", fallCaughtByLanyard ? "Kullanici dustu fakat lanyard yakaladı." : "Tehlikeli dusus gerceklesti!");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (playerTransform != null)
            {
                playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
            }
            yield return null;
        }

        EndScenario(fallCaughtByLanyard ? "Dusus Yasandi - Emniyet Kemeri Hayat Kurtardi" : "Tehlikeli Dusus - Olumcul Hata!");
    }

    // 11. Senaryoyu Sonlandır
    private void EndScenario(string reason)
    {
        SetState(Scenario1State.RESULT);
        totalTime = Time.time - scenarioStartTime;

        if (checklistPanel != null) checklistPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        int score = CalculateScore();

        if (resultScoreText != null)
        {
            // Eğitim modunda puan yerine sadece "EĞİTİM TAMAMLANDI" yazar
            if (currentMode == ScenarioMode.TRAINING)
            {
                resultScoreText.text = "EĞİTİM TAMAMLANDI";
            }
            else // Sınav modunda sayısal skoru yazar
            {
                resultScoreText.text = "SKOR: " + score + "/100";
            }
        }
        if (resultDetailText != null)
        {
            string harnessStr = harnessEquipped ? (isHarnessCorrectlyWorn ? "Evet (Kilitli)" : "Evet (Gevşek)") : "Hayır";
            string anchorStr = anchorSelected ? (safeAnchorSelected ? "Güvenli" : "Güvensiz") : "Hayır";
            string retreatStr = safeRetreatSelected ? "Evet (Güvenli Karar)" : "Hayır";

            resultDetailText.text = "Sonuç: " + reason + "\n" +
                                    "Süre: " + totalTime.ToString("0.0") + " sn\n\n" +
                                    "Kemer Giyildi: " + harnessStr + "\n" +
                                    "Ankraj Yapıldı: " + anchorStr + "\n" +
                                    "Toplu Koruma Kontrolü: " + (collectiveProtectionChecked ? "Evet" : "Hayır") + "\n" +
                                    "Kontrollü Geri Çekilme: " + retreatStr;
        }

        RecordEvent("scenario_ended", "Senaryo bitti. Sonuc: " + reason + " | Skor: " + score);
    }

    private int CalculateScore()
    {
        int score = 0;
        if (harnessEquipped && isHarnessCorrectlyWorn) score += 20; // Doğru giyilen kemer 20 puan
        else if (harnessEquipped) score += 5; // Gevşek kemer sadece 5 puan
        if (collectiveProtectionChecked) score += 25;
        if (anchorSelected && safeAnchorSelected) score += 25;
        if (correctLanyardSelected) score += 20;

        // Güvenli Karar / Geri Çekilme veya Sorunsuz Tamamlama: 10 Puan
        if (safeRetreatSelected || (!enteredScaffoldWithoutKkd && !fallEventOccurred))
        {
            score += 10;
        }
        return score;
    }

    // 12. Hazırlık ve Güvenlik Aşaması Kontrol Yardımcıları
    private void CheckPreparationCompletion()
    {
        if (currentState == Scenario1State.PREPARATION && harnessEquipped && isHarnessCorrectlyWorn && correctLanyardSelected)
        {
            SetState(Scenario1State.SAFETY_CHECK);
            RecordEvent("preparation_completed", "KKD hazırlığı tamamlandı. Güvenlik kontrolü (SAFETY_CHECK) aşamasına geçildi.");
        }
    }

    private void CheckSafetyCheckCompletion()
    {
        if (currentState == Scenario1State.SAFETY_CHECK && collectiveProtectionChecked && anchorSelected && safeAnchorSelected)
        {
            SetState(Scenario1State.CLIMB);
            RecordEvent("safety_check_completed", "Güvenlik kontrolleri ve ankraj bağlantısı tamamlandı. Tırmanış aşamasına geçildi.");
        }
    }

    // 13. Kontrollü Geri Çekilme (Safe Retreat)
    public void SafeRetreat()
    {
        // Geri çekilme sadece tırmanma, çalışma veya sarsıntı durumlarında geçerlidir. 
        // Yerdeki hazırlık aşamalarında tetiklenmesi engellenir.
        if (currentState == Scenario1State.CLIMB ||
            currentState == Scenario1State.WORK ||
            currentState == Scenario1State.DISTURBANCE)
        {
            safeRetreatSelected = true;
            RecordEvent("safe_retreat", "Kullanıcı riski fark ederek güvenli bölgeye geri çekildi.");
            
            if (currentMode == ScenarioMode.TRAINING)
            {
                EndScenario("Eğitim Tamamlandı: Riski fark ederek güvenli alana geri çekildiniz.");
            }
            else
            {
                EndScenario("Kontrollü Geri Çekilme Yapıldı (Güvenli Karar)");
            }
        }
    }

    private void AddWarning(string warning)
    {
        if (!warnings.Contains(warning))
        {
            warnings.Add(warning);
            RecordEvent("warning", warning);
        }
    }

    private void UpdateChecklistToggle(int index, bool state, bool forceInteractable = false)
    {
        var clm = FindAnyObjectByType<ChecklistManager>();
        if (clm != null && clm.toggles != null && index >= 0 && index < clm.toggles.Length)
        {
            var toggle = clm.toggles[index];
            if (toggle != null)
            {
                toggle.isOn = state;
                toggle.interactable = forceInteractable; // Egitim modunda false olur, sifirladigimizda true olur
                clm.CheckAllToggles(); // Buton durumunu guncelle
            }
        }
    }

    private void UpdateStateLogic()
    {
        if (currentState == Scenario1State.INIT || currentState == Scenario1State.RESULT) return;

        // 1. Aşamadan 2. Aşamaya Geçiş (PREPARATION'dan SAFETY_CHECK'e)
        if (currentState == Scenario1State.PREPARATION)
        {
            if (harnessEquipped && isHarnessCorrectlyWorn && correctLanyardSelected)
            {
                SetState(Scenario1State.SAFETY_CHECK);
                RecordEvent("preparation_completed", "KKD hazırlığı tamamlandı. Güvenlik kontrolü (SAFETY_CHECK) aşamasına geçildi.");
            }
        }

        // 2. Aşamadan 3. Aşamaya Geçiş (SAFETY_CHECK'ten CLIMB'a)
        if (currentState == Scenario1State.SAFETY_CHECK)
        {
            if (collectiveProtectionChecked && anchorSelected && safeAnchorSelected)
            {
                SetState(Scenario1State.CLIMB);
                RecordEvent("safety_check_completed", "Güvenlik kontrolleri ve ankraj bağlantısı tamamlandı. Tırmanış aşamasına geçildi.");
            }
        }
    }

    private void RefreshUi()
    {
        UpdateStateLogic(); // Her UI güncellemesinde durumları otomatik ve güvenli şekilde kontrol et/geçir

        if (stepText != null)
        {
            stepText.text = GetStateInstructions();
        }

        if (metricsText != null)
        {
            string harnessStr = harnessEquipped ? (isHarnessCorrectlyWorn ? "Doğru (Tokalı)" : "Gevşek") : "Takılmadı";
            string lanyardStr = correctLanyardSelected ? "Güvenli (Yeşil)" : "Güvensiz (Kırmızı)";
            string anchorStr = anchorSelected ? (safeAnchorSelected ? "Güvenli" : "Güvensiz") : "Seçilmedi";

            metricsText.text = "Kemer: " + harnessStr + " | Lanyard: " + lanyardStr + " | Ankraj: " + anchorStr;
        }
    }

    [Header("Mod Seçimi")]
    public ScenarioMode currentMode = ScenarioMode.TRAINING;

    public void OnStartPressed()
    {
        if (startBriefingButton != null) startBriefingButton.SetActive(false);
        if (modeSelectionGroup != null) modeSelectionGroup.SetActive(true);
    }

    public void SetTrainingMode()
    {
        currentMode = ScenarioMode.TRAINING;
        RecordEvent("mode_selected", "EĞİTİM MODU seçildi.");

        // Eğitim modunda yeşil görseli aktif et
        var safeZone = GameObject.Find("SafeZone");
        if (safeZone != null)
        {
            var visual = safeZone.transform.Find("SafeZoneVisual");
            if (visual != null) visual.gameObject.SetActive(true);
        }

        CompleteBriefing();
    }

    public void SetExamMode()
    {
        currentMode = ScenarioMode.EXAM;
        RecordEvent("mode_selected", "SINAV MODU seçildi.");

        // Sınav modunda yeşil görseli gizle (oyuncu kendi bulmalı)
        var safeZone = GameObject.Find("SafeZone");
        if (safeZone != null)
        {
            var visual = safeZone.transform.Find("SafeZoneVisual");
            if (visual != null) visual.gameObject.SetActive(false);
        }

        CompleteBriefing();
    }

    private string GetStateInstructions()
    {
        if (currentMode == ScenarioMode.EXAM)
        {
            switch (currentState)
            {
                case Scenario1State.BRIEFING: return "Lütfen EĞİTİM MODU veya SINAV MODU butonuna basın.";
                case Scenario1State.PREPARATION: return "SINAV MODU: KKD hazırlığınızı yapın ve iskeleye ilerleyin.";
                case Scenario1State.WORK: return "SINAV MODU: Bakım görevini tamamlayın.";
                case Scenario1State.RESULT: return "SINAV MODU: Değerlendirme tamamlandı. Karnenizi inceleyin.";
                default: return "Sınav Devam Ediyor...";
            }
        }

        // EĞİTİM MODU (Rehberlik & Canlı İpuçları)
        if (currentState == Scenario1State.BRIEFING)
        {
            return "EĞİTİM MODU: Lütfen başlamak için mod seçin.";
        }

        if (!harnessEquipped || !isHarnessCorrectlyWorn)
        {
            return "EĞİTİM REHBERİ (ADIM 1/6): KEMER KUŞANMA\n" +
                   "(Baret, eldiven, gözlük ve çelik burunlu ayakkabılar otomatik kuşanılmıştır.)\n" +
                   "-> Masadaki sarı emniyet kemerini tutun.\n" +
                   "-> Kumanda Tuş Örüntüsü: Sol Grip -> Sağ Grip -> Tetik (Trigger) tuşuna basıp kemeri kilitleyin.";
        }
        if (!correctLanyardSelected)
        {
            return "EĞİTİM REHBERİ (ADIM 2/6): LANYARD SEÇİMİ\n" +
                   "-> [OK] Emniyet Kemeri Doğru Giyildi.\n" +
                   "-> Şimdi masadaki YEŞİL şok emicili lanyardı elinizle tutup seçin.";
        }
        if (!collectiveProtectionChecked)
        {
            return "EĞİTİM REHBERİ (ADIM 3/6): TOPLU KORUMA KONTROLÜ\n" +
                   "-> [OK] Kemer ve Lanyard Hazır.\n" +
                   "-> İskele alanına gidin. Tekerlek kilitleri ve korkuluk pimlerini kontrol edip kilitleyin (toggles).";
        }
        if (!anchorSelected || !safeAnchorSelected)
        {
            return "EĞİTİM REHBERİ (ADIM 4/6): ANKRAJ BAĞLANTISI\n" +
                   "-> [OK] İskele Güvenlik Kontrolleri Tamamlandı.\n" +
                   "-> İskeleye tırmanmadan önce, yeşil (güvenli) ankraj noktasına karabinalarınızı bağlayın.";
        }
        if (currentState == Scenario1State.PREPARATION || currentState == Scenario1State.SAFETY_CHECK || currentState == Scenario1State.CLIMB)
        {
            return "EĞİTİM REHBERİ (ADIM 5/6): İSKELEYE TIRMANIŞ\n" +
                   "-> [OK] Güvenli Ankraj Bağlantısı Yapıldı.\n" +
                   "-> Şimdi asansörle veya tırmanarak iskelenin üst platformuna çıkın.";
        }
        if (currentState == Scenario1State.WORK)
        {
            return "EĞİTİM REHBERİ (ADIM 6/6): BAKIM GÖREVİ\n" +
                   "-> [OK] Üst Platforma Ulaşıldı.\n" +
                   "-> Üst platformdaki görev nesnesine (bakım panosuna) ulaşın ve bakım görevini tamamlayın.";
        }
        if (currentState == Scenario1State.DISTURBANCE)
        {
            return "EĞİTİM: Rüzgar/Sarsıntı başladı, dengenizi koruyun!";
        }
        if (currentState == Scenario1State.FALL_OR_RECOVERY)
        {
            return "EĞİTİM: Denge kaybı yaşandı. Alınan güvenlik önlemlerinin sonucu izleniyor...";
        }
        if (currentState == Scenario1State.RESULT)
        {
            int score = CalculateScore();
            if (safeRetreatSelected)
            {
                return "Tebrikler! EĞİTİM BAŞARIYLA TAMAMLANDI!\nGüvenlik eksikliğini/sarsıntıyı fark edip güvenli bölgeye geri çekilerek doğru bir İSG kararı verdiniz.";
            }
            if (score >= 100)
            {
                return "Tebrikler! EĞİTİM BAŞARIYLA TAMAMLANDI!\nTüm İSG adımlarını kusursuz uyguladınız.";
            }
            return "EĞİTİM TAMAMLANDI.\nHatalarınızı görmek için rapor panelini inceleyin.";
        }
        return "Eğitim Devam Ediyor...";
    }

    private void RecordEvent(string eventName, string detail)
    {
        if (!writeEventLog) return;

        string json = "{" +
                      "\"utc\":\"" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + "\"," +
                      "\"event\":\"" + eventName + "\"," +
                      "\"elapsed\":" + (Time.time - scenarioStartTime).ToString("0.000", CultureInfo.InvariantCulture) + "," +
                      "\"harness\":" + (harnessEquipped ? "true" : "false") + "," +
                      "\"anchor\":\"" + (anchorSelected ? (safeAnchorSelected ? "safe" : "unsafe") : "none") + "\"," +
                      "\"collectiveChecked\":" + (collectiveProtectionChecked ? "true" : "false") + "," +
                      "\"detail\":\"" + detail + "\"" +
                      "}";

        try
        {
            string path = Path.Combine(Application.persistentDataPath, LogFileName);
            File.AppendAllText(path, json + Environment.NewLine);

            string projPath = Path.Combine(Application.dataPath, "..", LogFileName);
            File.AppendAllText(projPath, json + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Scenario 1 log yazilamadi: " + ex.Message);
        }
    }

    // --- DİNAMİK VR UYARI EKRANI YÖNETİMİ ---
    private GameObject warningCanvas;
    private TextMeshProUGUI warningText;
    private Coroutine warningCoroutine;

    private void CreateDynamicWarningCanvas()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : transform;

        GameObject canvasGo = new GameObject("DynamicWarningCanvas");
        canvasGo.transform.SetParent(cam);
        canvasGo.transform.localPosition = new Vector3(0, 0.15f, 1.8f); // 1.8 metre önünde, göz hizasının hafif üstünde
        canvasGo.transform.localRotation = Quaternion.identity;
        canvasGo.transform.localScale = new Vector3(0.0018f, 0.0018f, 0.0018f); // Ölçeği küçültüyoruz (clipping'i ve devasa boyutu engeller)

        warningCanvas = canvasGo;
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900f, 200f); // İnce, şık panoramik pano boyutu

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
        warningText = textGo.AddComponent<TextMeshProUGUI>();
        warningText.fontSize = 34; // TMPro ile cam gibi keskin ve ideal boyutta yazdırılır
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.color = Color.yellow;
        warningText.text = "";
        
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-60, -30); // Levha kenarlarından dengeli paylar
        
        warningCanvas.SetActive(false);
    }

    public void ShowCustomWarning(string message, float duration = 5f)
    {
        if (warningCanvas == null)
        {
            CreateDynamicWarningCanvas();
        }

        if (warningCanvas == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(ShowWarningRoutine(message, duration));
    }

    private IEnumerator ShowWarningRoutine(string message, float duration)
    {
        if (warningText != null)
        {
            warningText.text = message;
        }

        warningCanvas.SetActive(true);
        yield return new WaitForSeconds(duration);

        if (warningCanvas != null)
        {
            warningCanvas.SetActive(false);
        }
        warningCoroutine = null;
    }
}
