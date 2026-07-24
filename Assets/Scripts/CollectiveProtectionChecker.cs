using UnityEngine;

public class CollectiveProtectionChecker : MonoBehaviour
{
    [Header("Manager Reference")]
    public Scenario5Manager manager;

    [Header("Check Status")]
    public bool wheelLocked = false;
    public bool guardrailChecked = false;
    public bool platformLocked = false;

    public void LockWheels()
    {
        if (wheelLocked) return;
        wheelLocked = true;
        Debug.Log("Collective Protection | Tekerlekler kilitlendi.");
        CheckCompletion();
    }

    public void CheckGuardrails()
    {
        if (guardrailChecked) return;
        guardrailChecked = true;
        Debug.Log("Collective Protection | Korkuluklar kontrol edildi.");
        CheckCompletion();
    }

    public void LockPlatform()
    {
        if (platformLocked) return;
        platformLocked = true;
        Debug.Log("Collective Protection | Platform kilidi kontrol edildi.");
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (wheelLocked && guardrailChecked && platformLocked)
        {
            if (manager != null)
            {
                // Trigger the main manager's collective protection action
                manager.PerformAction(Scenario5Action.CheckCollectiveProtection);
                Debug.Log("Collective Protection | Tum toplu koruma kontrolleri tamamlandi, ana yoneticiye bildirildi.");
            }
            else
            {
                Debug.LogWarning("Collective Protection | Manager referansi eksik!");
            }
        }
    }
}
