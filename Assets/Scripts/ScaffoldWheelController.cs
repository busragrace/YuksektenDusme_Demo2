using System.Collections;
using UnityEngine;

public class ScaffoldWheelController : MonoBehaviour
{
    [Header("Bileşen Referansları")]
    public Transform tireMesh;         // Dönecek olan siyah lastik parçası (Wheel_Tire)
    public Transform brakeLeverMesh;   // Hareket edecek kırmızı fren kolu (Brake_Lock_Lever)
    public Rigidbody scaffoldRigidbody;// İskelenin ana Rigidbody'si (fizik motoru)

    [Header("Ayarlar")]
    public bool isLocked = false;       // Tekerlek kilitli mi?
    public float lockAngle = 25f;       // Fren kolu kilitlendiğinde kaç derece aşağı eğilecek?

    private Vector3 lastPosition;       // Tekerleğin bir önceki karesindeki konumu

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Tekerlek Dönme Fiziği: İskele hareket ediyorsa ve kilitli değilse tekerlekleri döndür
        if (!isLocked && tireMesh != null)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            if (distanceMoved > 0.001f)
            {
                // Tekerleğin yarıçapına göre ne kadar döneceğini hesapla (Aç = Yol / Yarıçap)
                float rotationAngle = (distanceMoved / 0.15f) * Mathf.Rad2Deg;

                // Tekerleği X ekseninde döndür
                tireMesh.Rotate(Vector3.right * rotationAngle, Space.Self);
            }
        }
        lastPosition = transform.position;
    }

    // 2. Kilit Durumunu Değiştiren Fonksiyon (VR Butonuna bağlayacağız)
    public void ToggleWheelLock()
    {
        isLocked = !isLocked;

        // Fren kolunu hareket ettiren coroutine'i (yumuşak animasyonu) başlat
        if (brakeLeverMesh != null)
        {
            StartCoroutine(AnimateBrakeLever());
        }

        // Koordineli fizik ve emniyet durumlarını güncelle
        UpdateCoordinatedState();
    }

    private void UpdateCoordinatedState()
    {
        // Sahnedeki tüm tekerlekleri bul
        ScaffoldWheelController[] wheels = FindObjectsOfType<ScaffoldWheelController>();
        bool allLocked = true;
        foreach (var wheel in wheels)
        {
            if (!wheel.isLocked)
            {
                allLocked = false;
                break;
            }
        }

        // Rigidbody kısıtlamalarını güncelle
        if (scaffoldRigidbody != null)
        {
            if (allLocked)
            {
                scaffoldRigidbody.constraints = RigidbodyConstraints.FreezeAll;
                Debug.Log("Scaffold Physics | Tüm tekerlekler kilitlendi, iskele tamamen sabitlendi.");
            }
            else
            {
                // En az bir tekerlek açıksa kayabilsin, ancak devrilmesin diye rotasyonları kilitli tut
                scaffoldRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("Scaffold Physics | En az bir tekerlek açık, iskele hareket edebilir.");
            }
        }

        // ScaffoldSafetyController'ı bul ve tekerlek kilit durumunu bildir
        ScaffoldSafetyController safetyController = FindAnyObjectByType<ScaffoldSafetyController>();
        if (safetyController != null)
        {
            safetyController.SetWheelsLockedState(allLocked);
        }
    }

    // Kırmızı kolu yumuşakça aşağı-yukarı oynatan animasyon mantığı
    private IEnumerator AnimateBrakeLever()
    {
        float targetAngle = isLocked ? lockAngle : 0f;
        Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
        float elapsed = 0f;
        float duration = 0.3f; // Animasyon 0.3 saniye sürsün

        Quaternion startRotation = brakeLeverMesh.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            brakeLeverMesh.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            yield return null;
        }
        brakeLeverMesh.localRotation = targetRotation;
    }
}
