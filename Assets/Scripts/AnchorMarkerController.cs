using UnityEngine;

/// <summary>
/// Üst katlardaki sağlam/çürük ankraj işaretçilerinin ve aura parıltılarının 
/// sadece oyuncu lanyard ile demirlere yaklaştığında görünmesini sağlayan kontrolör.
/// </summary>
public class AnchorMarkerController : MonoBehaviour
{
    [Header("Görsel Efekt Elemanları")]
    [Tooltip("Bu ankrajın üzerinde havada duran bilgi Canvas'ı")]
    public GameObject labelCanvas;

    [Tooltip("Demirin etrafındaki yarı saydam yeşil/kırmızı aura parıltı küresi")]
    public GameObject auraVisual;

    [Header("Mesafe Ayarları")]
    [Tooltip("Efektlerin görünmesi için oyuncuya olan maksimum uzaklık (metre)")]
    public float activationDistance = 4.0f;

    private Transform playerCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // Başlangıçta ikisini de gizle
        if (labelCanvas != null) labelCanvas.SetActive(false);
        if (auraVisual != null) auraVisual.SetActive(false);
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main.transform;
            return;
        }

        // Oyuncu ile ankraj arasındaki mesafeyi ölç
        float distance = Vector3.Distance(transform.position, playerCamera.position);
        bool shouldShow = distance <= activationDistance;

        // Görünürlüğü güncelle
        if (labelCanvas != null && labelCanvas.activeSelf != shouldShow)
        {
            labelCanvas.SetActive(shouldShow);
        }

        if (auraVisual != null && auraVisual.activeSelf != shouldShow)
        {
            auraVisual.SetActive(shouldShow);
        }

        // Billboard etkisi: Etiket oyuncuya baksın
        if (shouldShow && labelCanvas != null)
        {
            labelCanvas.transform.LookAt(labelCanvas.transform.position + playerCamera.rotation * Vector3.forward,
                                         playerCamera.rotation * Vector3.up);
        }
    }
}
