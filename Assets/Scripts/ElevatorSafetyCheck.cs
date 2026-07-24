using System.Collections;
using UnityEngine;

public class ElevatorSafetyCheck : MonoBehaviour
{
    [Header("UI Elemanlari")]
    [Tooltip("Ekrana gelecek kirmizi uyari canvas'i")]
    public GameObject safetyWarningCanvas;

    [Header("Kapanma Ayari")]
    [Tooltip("Uyarinin kac saniye sonra kapanacagi")]
    public float warningDuration = 3f;

    private bool isSafetyEquipped = false;
    private Coroutine hideCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        // Alana giren nesne oyuncu mu?
        if (IsPlayer(other))
        {
            isSafetyEquipped = CheckPlayerEquipment();

            if (!isSafetyEquipped)
            {
                // Eger zaten calisan bir geri sayim varsa once onu durdur
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                }

                // Uyariyi goster
                if (safetyWarningCanvas != null)
                {
                    safetyWarningCanvas.SetActive(true);
                }

                // Belirlenen saniye sonra kapatacak Coroutine'i baslat
                hideCoroutine = StartCoroutine(HideWarningAfterDelay(warningDuration));

                Debug.LogWarning("Guvenlik ihlali! Ekipmanlar eksik.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Eger oyuncu asansorden erkenden geri cikarsa uyariyi aninda kapat
        if (IsPlayer(other))
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
            }

            if (safetyWarningCanvas != null)
            {
                safetyWarningCanvas.SetActive(false);
            }
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        return other.CompareTag("Player") || 
               other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null || 
               other.name.Contains("XR Origin") || 
               other.name.Contains("Camera");
    }

    // Belirlenen sure sonunda uyariyi kapatan fonksiyon
    private IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (safetyWarningCanvas != null)
        {
            safetyWarningCanvas.SetActive(false);
        }
    }

    private bool CheckPlayerEquipment()
    {
        var scenario1 = FindAnyObjectByType<Scenario1Manager>();
        if (scenario1 != null)
        {
            if (scenario1.currentMode == ScenarioMode.TRAINING)
            {
                // Eğitim modunda: Kemer tam sıkılmadıysa (doğru giyilmediyse) uyarı gösterilir
                return scenario1.IsHarnessCorrectlyWorn;
            }
            else
            {
                // Sınav modunda: Kemer giyilmişse (gevşek olsa bile) uyarı verilmez (skordan düşülecektir)
                return scenario1.IsHarnessEquipped;
            }
        }
        var scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        if (scenarioManager != null)
        {
            return scenarioManager.IsHarnessEquipped;
        }
        return false;
    }
}