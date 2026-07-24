using UnityEngine;

/// <summary>
/// Oyuncu bu trigger alanına girdiğinde düşüş senaryosunu (StartFall) tetikler.
/// </summary>
public class FallTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("XR Origin") || other.name.Contains("Camera"))
        {
            Debug.Log("FallTrigger: Oyuncu uç sınırdan boşluğa adım attı, düşüş tetikleniyor.");
            var fallCtrl = other.GetComponent<PlayerFallController>();
            if (fallCtrl == null) fallCtrl = other.GetComponentInParent<PlayerFallController>();
            if (fallCtrl == null) fallCtrl = FindAnyObjectByType<PlayerFallController>();
            
            if (fallCtrl != null)
            {
                fallCtrl.StartFall();
            }
        }
    }
}
