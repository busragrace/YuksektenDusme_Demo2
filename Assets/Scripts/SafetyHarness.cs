using UnityEngine;

/// <summary>
/// Masanın üzerindeki emniyet kemeri objesine eklenir.
/// Oyuncu bu objeyi tuttuğunda (grab) veya ışınla tıkladığında (select/activate) giyilmiş sayılır.
/// </summary>
public class SafetyHarness : MonoBehaviour
{
    [Header("Senaryo Bağlantısı")]
    [Tooltip("Senaryoyu yöneten ana kontrolcü")]
    public HarnessScenarioManager scenarioManager;

    [Header("Görsel/Ses Efektleri")]
    [Tooltip("Kemer giyildiğinde çalacak ses efekti")]
    public AudioClip equipSound;

    [Tooltip("Ses kaynağı")]
    public AudioSource audioSource;

    private bool isEquipped = false;

    private void Start()
    {
        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && equipSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // XRSimpleInteractable olay dinleyicisini otomatik bağla
        var simple = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (simple != null)
        {
            simple.selectEntered.AddListener((args) => Equip());
        }
    }

    /// <summary>
    /// Kemer kuşanıldığında çağrılır.
    /// XR Interactable olaylarına (Select Entered, Activated vb.) veya mouse tıklamasına bağlanabilir.
    /// </summary>
    public void Equip()
    {
        if (isEquipped) return;

        isEquipped = true;
        Debug.Log("SafetyHarness: Emniyet kemeri alındı ve kuşanıldı.");

        // Ses çal
        if (audioSource != null && equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }

        // Senaryo yöneticisine bildir
        if (scenarioManager != null)
        {
            scenarioManager.OnHarnessEquipped();
        }
        else
        {
            Debug.LogError("SafetyHarness: HarnessScenarioManager bulunamadı!");
        }

        // Sahnedeki (masadaki) kemer objesini gizle
        gameObject.SetActive(false);
    }

    // Bilgisayardan denemek için mouse tıklama desteği
    private void OnMouseDown()
    {
        Equip();
    }

    // XR Interaction Toolkit etkileşim dinleyicisi (SendMessage / Trigger)
    public void OnSelectEntered()
    {
        Equip();
    }

    public void OnActivated()
    {
        Equip();
    }
}
