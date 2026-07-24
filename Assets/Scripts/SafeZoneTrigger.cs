using UnityEngine;

public class SafeZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Eðer tetikleyiciye giren oyuncuysa manager'daki SafeRetreat'i çaðýrýr
        if (other.CompareTag("Player") || other.name.ToLower().Contains("player") || other.name.ToLower().Contains("camera"))
        {
            var manager = FindAnyObjectByType<Scenario1Manager>();
            if (manager != null)
            {
                manager.SafeRetreat();
            }
        }
    }
}