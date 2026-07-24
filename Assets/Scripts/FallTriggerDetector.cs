using UnityEngine;

public class FallTriggerDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Alana giren nesne oyuncu mu diye bakar (Hatýrlarsan XR Origin'i Player tag'i yapmýþtýk)
        if (other.CompareTag("Player"))
        {
            // Sahnede PlayerFallController'ý bulur ve düþüþü tetikler
            PlayerFallController fallController = FindFirstObjectByType<PlayerFallController>();
            if (fallController != null)
            {
                fallController.StartFall();
            }
            else
            {
                Debug.LogError("Sahnede PlayerFallController scripti bulunamadý!");
            }
        }
    }
}