using UnityEngine;

/// <summary>
/// Asansör çıkışındaki görünmez engele yaklaşıldığında oyuncuya 
/// "Öncelikle emniyet kemerinizi giyin!" uyarısı gösterilmesini sağlar.
/// </summary>
public class ElevatorExitGateTrigger : MonoBehaviour
{
    private HarnessScenarioManager scenarioManager;

    private void Start()
    {
        scenarioManager = FindAnyObjectByType<HarnessScenarioManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Tetikleyiciye giren nesne oyuncu ise ve henüz kemer giyilmemişse uyarı göster
        if (IsPlayer(other.transform))
        {
            if (scenarioManager != null && !scenarioManager.IsHarnessEquipped)
            {
                scenarioManager.ShowHarnessWarning();
            }
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
