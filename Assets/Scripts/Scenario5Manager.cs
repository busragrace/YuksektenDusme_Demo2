using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum Scenario5Action
{
    CheckCollectiveProtection,
    EquipHarness,
    SelectSafeAnchor,
    SelectUnsafeAnchor,
    ResetScenario
}

public class Scenario5Manager : MonoBehaviour
{
    [Header("UI")]
    public Text titleText;
    public Text stepText;
    public Text metricsText;
    public Button collectiveProtectionButton;
    public Button harnessButton;
    public Button safeAnchorButton;
    public Button unsafeAnchorButton;
    public Button resetButton;

    [Header("Scenario Objects")]
    public GameObject harnessVisual;
    public LineRenderer lanyardLine;
    public Transform lanyardStartPoint;
    public Transform safeAnchorPoint;
    public Transform unsafeAnchorPoint;
    public Transform fallDummy;
    public Transform fallTarget;
    public GameObject safeAnchorMarker;
    public GameObject unsafeAnchorBreakMarker;

    [Header("Anchor Feedback")]
    public Renderer[] safeAnchorRenderers;
    public Renderer[] unsafeAnchorRenderers;
    public Material neutralAnchorMaterial;
    public Material safeAnchorMaterial;
    public Material unsafeAnchorMaterial;
    public Material selectedAnchorMaterial;
    public Material brokenAnchorMaterial;

    [Header("Assessment")]
    public int requiredAnchorCapacityKg = 1000;
    public bool writeEventLog = true;

    private readonly List<string> warnings = new List<string>();
    private float scenarioStartTime;
    private float collectiveProtectionTime = -1f;
    private float harnessEquippedTime = -1f;
    private float anchorSelectedTime = -1f;
    private bool collectiveProtectionChecked;
    private bool harnessEquipped;
    private bool collectiveProtectionBeforeHarness;
    private bool anchorSelected;
    private bool safeAnchorSelected;

    // Public properties to access status from other scripts
    public bool IsHarnessEquipped => harnessEquipped;
    public bool IsAnchorSelected => anchorSelected;
    public bool IsSafeAnchorSelected => safeAnchorSelected;
    private Vector3 fallDummyStartPosition;
    private Quaternion fallDummyStartRotation;
    private Coroutine fallRoutine;

    private const string LogFileName = "scenario5_log.jsonl";

    private void Awake()
    {
        if (fallDummy != null)
        {
            fallDummyStartPosition = fallDummy.position;
            fallDummyStartRotation = fallDummy.rotation;
        }
    }

    private void Start()
    {
        ResetScenario();
    }

    private void Update()
    {
        RefreshLanyardLine();
    }

    public void PerformAction(Scenario5Action action)
    {
        switch (action)
        {
            case Scenario5Action.CheckCollectiveProtection:
                CheckCollectiveProtection();
                break;
            case Scenario5Action.EquipHarness:
                EquipHarness();
                break;
            case Scenario5Action.SelectSafeAnchor:
                SelectSafeAnchor();
                break;
            case Scenario5Action.SelectUnsafeAnchor:
                SelectUnsafeAnchor();
                break;
            case Scenario5Action.ResetScenario:
                ResetScenario();
                break;
        }
    }

    public void CheckCollectiveProtection()
    {
        if (collectiveProtectionChecked)
        {
            return;
        }

        collectiveProtectionChecked = true;
        collectiveProtectionTime = Time.time;
        RecordEvent("collective_protection_checked", "Guardrail 100 cm and toe board checklist completed.");
        Debug.Log("Scenario 5 | Toplu koruma kontrolu tamamlandi: korkuluk 100 cm ve supurgelik kontrol edildi.");
        RefreshUi();
    }

    public void EquipHarness()
    {
        if (harnessEquipped)
        {
            return;
        }

        harnessEquipped = true;
        harnessEquippedTime = Time.time;
        collectiveProtectionBeforeHarness = collectiveProtectionChecked;

        if (harnessVisual != null)
        {
            harnessVisual.SetActive(true);
        }

        if (!collectiveProtectionBeforeHarness)
        {
            AddWarning("Toplu koruma kontrolu KKD'den once yapilmadi.");
        }

        RecordEvent("harness_equipped", "Full body harness and lanyard equipped.");
        Debug.Log("Scenario 5 | Emniyet kemeri ve lanyard kusanildi.");
        RefreshUi();
    }

    public void SelectSafeAnchor()
    {
        SelectAnchor(true);
    }

    public void SelectUnsafeAnchor()
    {
        SelectAnchor(false);
    }

    public void ResetScenario()
    {
        scenarioStartTime = Time.time;
        collectiveProtectionTime = -1f;
        harnessEquippedTime = -1f;
        anchorSelectedTime = -1f;
        collectiveProtectionChecked = false;
        harnessEquipped = false;
        collectiveProtectionBeforeHarness = false;
        anchorSelected = false;
        safeAnchorSelected = false;
        warnings.Clear();

        if (fallRoutine != null)
        {
            StopCoroutine(fallRoutine);
            fallRoutine = null;
        }

        if (fallDummy != null)
        {
            fallDummy.position = fallDummyStartPosition;
            fallDummy.rotation = fallDummyStartRotation;
            fallDummy.gameObject.SetActive(true);
        }

        if (harnessVisual != null)
        {
            harnessVisual.SetActive(false);
        }

        if (lanyardLine != null)
        {
            lanyardLine.gameObject.SetActive(false);
            lanyardLine.positionCount = 2;
        }

        if (safeAnchorMarker != null)
        {
            safeAnchorMarker.SetActive(false);
        }

        if (unsafeAnchorBreakMarker != null)
        {
            unsafeAnchorBreakMarker.SetActive(false);
        }

        SetRenderersMaterial(safeAnchorRenderers, safeAnchorMaterial != null ? safeAnchorMaterial : neutralAnchorMaterial);
        SetRenderersMaterial(unsafeAnchorRenderers, neutralAnchorMaterial);

        RecordEvent("scenario_reset", "Scenario 5 reset.");
        RefreshUi();
    }

    private void SelectAnchor(bool safe)
    {
        if (anchorSelected)
        {
            return;
        }

        anchorSelected = true;
        safeAnchorSelected = safe;
        anchorSelectedTime = Time.time;

        if (!harnessEquipped)
        {
            AddWarning("Ankraj secimi emniyet kemeri kusanmadan yapildi.");
        }

        if (!collectiveProtectionChecked)
        {
            AddWarning("Toplu koruma kontrolu atlandi.");
        }

        if (lanyardLine != null)
        {
            lanyardLine.gameObject.SetActive(true);
        }

        if (safe)
        {
            SetRenderersMaterial(safeAnchorRenderers, selectedAnchorMaterial != null ? selectedAnchorMaterial : safeAnchorMaterial);
            SetRenderersMaterial(unsafeAnchorRenderers, unsafeAnchorMaterial);

            if (safeAnchorMarker != null)
            {
                safeAnchorMarker.SetActive(true);
            }

            RecordEvent("anchor_selected", "Safe steel profile or lifeline anchor selected.");
            Debug.Log("Scenario 5 | Ankraj secimi dogru: 1000 kg kapasiteli celik profil/yasam hatti secildi.");
        }
        else
        {
            SetRenderersMaterial(safeAnchorRenderers, safeAnchorMaterial);
            SetRenderersMaterial(unsafeAnchorRenderers, brokenAnchorMaterial != null ? brokenAnchorMaterial : unsafeAnchorMaterial);
            AddWarning("Guvensiz ankraj secildi: plastik boru kopma riski tasir.");
            RecordEvent("anchor_selected", "Unsafe plastic pipe anchor selected. Weak anchor failure simulated.");
            Debug.LogWarning("Scenario 5 | Kritik hata: Plastik boru ankraj olarak secildi, kopma simulasyonu basladi.");
            BeginWeakAnchorSimulation();
        }

        RefreshLanyardLine();
        RefreshUi();
    }

    private void BeginWeakAnchorSimulation()
    {
        if (unsafeAnchorBreakMarker != null)
        {
            unsafeAnchorBreakMarker.SetActive(true);
        }

        if (fallDummy != null && fallTarget != null)
        {
            fallRoutine = StartCoroutine(AnimateFall());
        }
    }

    private IEnumerator AnimateFall()
    {
        var startPosition = fallDummy.position;
        var startRotation = fallDummy.rotation;
        var endPosition = fallTarget.position;
        var endRotation = Quaternion.Euler(78f, fallDummy.eulerAngles.y, 14f);
        const float duration = 1.15f;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            fallDummy.position = Vector3.Lerp(startPosition, endPosition, eased);
            fallDummy.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
            yield return null;
        }

        fallDummy.position = endPosition;
        fallDummy.rotation = endRotation;
        fallRoutine = null;
        RecordEvent("weak_anchor_failure", "Unsafe anchor broke during fall simulation.");
    }

    private void RefreshLanyardLine()
    {
        if (lanyardLine == null || !lanyardLine.gameObject.activeSelf || lanyardStartPoint == null)
        {
            return;
        }

        var target = safeAnchorSelected ? safeAnchorPoint : unsafeAnchorPoint;
        if (target == null)
        {
            return;
        }

        lanyardLine.SetPosition(0, lanyardStartPoint.position);
        lanyardLine.SetPosition(1, target.position);
    }

    private void RefreshUi()
    {
        if (titleText != null)
        {
            titleText.text = "Senaryo 5: Mobil Iskele ve KKD Disiplini";
        }

        if (stepText != null)
        {
            stepText.text = BuildStepText();
        }

        if (metricsText != null)
        {
            metricsText.text = BuildMetricsText();
        }

        if (collectiveProtectionButton != null)
        {
            collectiveProtectionButton.interactable = !collectiveProtectionChecked && !anchorSelected;
        }

        if (harnessButton != null)
        {
            harnessButton.interactable = !harnessEquipped && !anchorSelected;
        }

        if (safeAnchorButton != null)
        {
            safeAnchorButton.interactable = !anchorSelected;
        }

        if (unsafeAnchorButton != null)
        {
            unsafeAnchorButton.interactable = !anchorSelected;
        }

        if (resetButton != null)
        {
            resetButton.interactable = true;
        }
    }

    private string BuildStepText()
    {
        if (!collectiveProtectionChecked)
        {
            return "1. Iskele korkuluklarini (100 cm) ve supurgelikleri kontrol et.";
        }

        if (!harnessEquipped)
        {
            return "2. Tam vucut emniyet kemeri ve lanyardi kusan.";
        }

        if (!anchorSelected)
        {
            return "3. 1000 kg kapasiteli celik profil veya yasam hatti ankrajini sec.";
        }

        return safeAnchorSelected
            ? "Tamamlandi: guvenli ankraj secildi ve log kaydi olustu."
            : "Kritik hata: zayif ankraj koptu, dusus simulasyonu calisti.";
    }

    private string BuildMetricsText()
    {
        var anchorStatus = !anchorSelected ? "Bekleniyor" : safeAnchorSelected ? "Guvenli" : "Guvensiz";
        var harnessTime = harnessEquippedTime >= 0f ? FormatDuration(harnessEquippedTime - scenarioStartTime) : "Bekleniyor";
        var connectionTime = anchorSelectedTime >= 0f
            ? FormatDuration(anchorSelectedTime - (harnessEquippedTime >= 0f ? harnessEquippedTime : scenarioStartTime))
            : "Bekleniyor";
        var collectiveOrder = harnessEquipped
            ? collectiveProtectionBeforeHarness ? "Evet" : "Hayir"
            : "Bekleniyor";

        var text =
            "Takip / Loglama\n" +
            "- Ankraj teknik dogrulugu: " + anchorStatus + "\n" +
            "- Kemer kusanma suresi: " + harnessTime + "\n" +
            "- Baglanti yapma suresi: " + connectionTime + "\n" +
            "- Korkuluk/supurgelik KKD'den once kontrol edildi mi: " + collectiveOrder + "\n" +
            "- Gereken ankraj kapasitesi: " + requiredAnchorCapacityKg + " kg";

        if (warnings.Count > 0)
        {
            text += "\n\nUyarilar\n- " + string.Join("\n- ", warnings);
        }

        return text;
    }

    private void AddWarning(string warning)
    {
        if (!warnings.Contains(warning))
        {
            warnings.Add(warning);
            RecordEvent("warning", warning);
        }
    }

    private void SetRenderersMaterial(Renderer[] renderers, Material material)
    {
        if (renderers == null || material == null)
        {
            return;
        }

        foreach (var targetRenderer in renderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.material = material;
            }
        }
    }

    private string FormatDuration(float seconds)
    {
        return Mathf.Max(0f, seconds).ToString("0.0", CultureInfo.InvariantCulture) + " sn";
    }

    private void RecordEvent(string eventName, string detail)
    {
        if (!writeEventLog)
        {
            return;
        }

        var json =
            "{\"utc\":\"" + Escape(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)) + "\"," +
            "\"event\":\"" + Escape(eventName) + "\"," +
            "\"elapsed\":" + Mathf.Max(0f, Time.time - scenarioStartTime).ToString("0.000", CultureInfo.InvariantCulture) + "," +
            "\"anchor\":\"" + Escape(anchorSelected ? safeAnchorSelected ? "safe" : "unsafe" : "pending") + "\"," +
            "\"collectiveChecked\":" + Bool(collectiveProtectionChecked) + "," +
            "\"harnessEquipped\":" + Bool(harnessEquipped) + "," +
            "\"detail\":\"" + Escape(detail) + "\"}";

        try
        {
            var path = Path.Combine(Application.persistentDataPath, LogFileName);
            File.AppendAllText(path, json + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Scenario 5 log dosyasina yazilamadi: " + exception.Message);
        }
    }

    private static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string Escape(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
