using UnityEngine;

/// <summary>
/// Oyuncu belirli bir alana (masa, asansör önü vb.) girdiğinde 
/// ilgili Canvas/tabela rehber yazılarının görünmesini sağlayan tetikleyici.
/// </summary>
public class AreaCanvasTrigger : MonoBehaviour
{
    [Header("Hedef Arayüz")]
    [Tooltip("Tetiklendiğinde açılıp kapanacak Canvas veya Tabela GameObject'i")]
    public GameObject targetCanvas;

    [Header("Koşul Ayarları")]
    [Tooltip("Eğer aktifse, oyuncu emniyet kemerini giydiğinde bu rehber kalıcı olarak kapanır")]
    public bool hidePermanentlyOnHarnessEquipped = false;

    private HarnessScenarioManager scenarioManager;
    private Scenario1Manager scenario1Manager;

    private void Start()
    {
        scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
        scenario1Manager = FindAnyObjectByType<Scenario1Manager>();
        
        // Başlangıçta hedef Canvas'ı gizle
        if (targetCanvas != null)
        {
            targetCanvas.SetActive(false);
        }
    }

    private bool IsHarnessEquipped()
    {
        if (scenario1Manager != null) return scenario1Manager.IsHarnessEquipped;
        if (scenarioManager != null) return scenarioManager.IsHarnessEquipped;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetCanvas == null) return;

        // Kemer takıldıktan sonra bu uyarıyı bir daha gösterme koşulu
        if (hidePermanentlyOnHarnessEquipped && IsHarnessEquipped())
        {
            targetCanvas.SetActive(false);
            return;
        }

        if (IsPlayer(other.transform))
        {
            targetCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetCanvas == null) return;

        if (IsPlayer(other.transform))
        {
            targetCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        // Kemer giyildiyse ve kalıcı gizleme aktifse Canvas'ı kapat ve tetikleyiciyi devredışı bırak
        if (hidePermanentlyOnHarnessEquipped && IsHarnessEquipped())
        {
            if (targetCanvas != null && targetCanvas.activeSelf)
            {
                targetCanvas.SetActive(false);
            }
            gameObject.SetActive(false); // Tetikleyici alanını kapat
        }
    }

    private static bool IsPlayer(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.CompareTag("Player"))
            {
                return true;
            }
            candidate = candidate.parent;
        }
        return false;
    }
}
