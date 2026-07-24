using UnityEngine;

/// <summary>
/// VR gözlük projelerinde oyuncunun kendi gövdesini (torso) ve ayaklarını 
/// görebilmesi için kamerayı takip eden basit bir avatar sistemi.
/// </summary>
public class VRBodyFollow : MonoBehaviour
{
    [Header("Hedef Takip Elemanları")]
    [Tooltip("Takip edilecek VR Kamerası (Main Camera)")]
    public Transform targetCamera;
    
    [Tooltip("VR Origin (XR Origin veya Rig) - Zemin hizasını almak için")]
    public Transform xrOrigin;

    [Header("Beden Ayarları")]
    [Tooltip("Gövdenin kameradan ne kadar aşağıda duracağı (metre)")]
    public float torsoVerticalOffset = 0.35f;
    
    [Tooltip("Gövde dönüş hızı yumuşatma değeri")]
    public float rotationSmoothing = 5.0f;

    [Header("Görsel Elemanlar")]
    [Tooltip("Beden üzerindeki emniyet kemeri görseli (Kuşanılınca aktifleşecek)")]
    public GameObject bodyHarnessVisual;

    [Tooltip("Sol ayak görseli")]
    public Transform leftFoot;
    
    [Tooltip("Sağ ayak görseli")]
    public Transform rightFoot;

    [Tooltip("Ayakların gövde merkezinden X eksenindeki genişliği (metre)")]
    public float feetSpacing = 0.15f;

    private void Start()
    {
        // Kamera atanmamışsa ana kamerayı bulmaya çalış
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }

        // Beden kemer görseli başlangıçta gizli olmalı
        if (bodyHarnessVisual != null)
        {
            bodyHarnessVisual.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Gövde Pozisyonu: Kameranın X-Z koordinatı, Y koordinatının biraz aşağısı
        float groundY = xrOrigin != null ? xrOrigin.position.y : (targetCamera.position.y - 1.6f);
        Vector3 targetTorsoPos = new Vector3(
            targetCamera.position.x,
            Mathf.Max(groundY + 0.5f, targetCamera.position.y - torsoVerticalOffset),
            targetCamera.position.z
        );
        transform.position = targetTorsoPos;

        // 2. Gövde Dönüşü: Sadece Y ekseninde kameraya baksın (Yumuşatılarak)
        Vector3 cameraForward = targetCamera.forward;
        cameraForward.y = 0; // Sadece yatay düzlemde dönüş
        if (cameraForward.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
        }

        // 3. Ayakların Pozisyonu (Zeminde):
        if (leftFoot != null && rightFoot != null)
        {
            Vector3 rightDir = transform.right;
            
            // Sol ve sağ ayağı gövdenin altına ve yanlarına yerleştir
            Vector3 leftFootTarget = new Vector3(
                transform.position.x - (rightDir.x * feetSpacing),
                groundY,
                transform.position.z - (rightDir.z * feetSpacing)
            );
            
            Vector3 rightFootTarget = new Vector3(
                transform.position.x + (rightDir.x * feetSpacing),
                groundY,
                transform.position.z + (rightDir.z * feetSpacing)
            );

            // Ayakları zemin hizasına taşı ve gövde yönüne döndür
            leftFoot.position = leftFootTarget;
            leftFoot.rotation = transform.rotation;

            rightFoot.position = rightFootTarget;
            rightFoot.rotation = transform.rotation;
        }
    }

    /// <summary>
    /// Emniyet kemerini kuşanıldığında çağrılır. Beden üzerindeki kemer görselini açar.
    /// </summary>
    public void SetHarnessActive(bool active)
    {
        if (bodyHarnessVisual != null)
        {
            bodyHarnessVisual.SetActive(active);
        }
    }
}
