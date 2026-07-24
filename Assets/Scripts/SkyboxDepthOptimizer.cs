using UnityEngine;

/// <summary>
/// Projedeki Skybox ölçek uyumsuzluğunu ve derinlik hissi hatalarını düzeltmek için kullanılır.
/// Kameranın kırpma düzlemlerini optimize eder, zemin ölçeğini dengeler 
/// ve iskele/üniversite modellerine derinlik algısı kazandırmak amacıyla uzaklık sisini (Fog) ayarlar.
/// </summary>
public class SkyboxDepthOptimizer : MonoBehaviour
{
    [Header("Kamera Kırpma Düzlemleri (Clipping Planes)")]
    [Tooltip("Kameranın yakın kırpma sınırı (VR ellerin kırpılmaması için 0.01 önerilir)")]
    public float nearClipPlane = 0.01f;

    [Tooltip("Kameranın uzak kırpma sınırı (Üniversite binasının kaybolmaması için 3000+ önerilir)")]
    public float farClipPlane = 5000f;

    [Header("Derinlik Hissi & Atmosferik Sis (Fog)")]
    [Tooltip("Derinlik hissi oluşturmak için atmosferik sisi aktif et")]
    public bool enableFog = true;

    [Tooltip("Ufuk çizgisine uyumlu sis rengi (Skybox ufuk rengiyle eşleştirilmelidir)")]
    public Color horizonFogColor = new Color(0.74f, 0.82f, 0.89f); // Açık mavi/gri tonu

    [Tooltip("Sis yoğunluğu (Fazla yüksek olmamalıdır, derinlik hissi vermesi yeterlidir)")]
    public float fogDensity = 0.002f;

    private void Start()
    {
        OptimizeSettings();
    }

    /// <summary>
    /// Kamera ve sahne derinlik ayarlarını optimize eder.
    /// </summary>
    public void OptimizeSettings()
    {
        // 1. Kamera Clipping Ayarları
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.nearClipPlane = nearClipPlane;
            mainCam.farClipPlane = farClipPlane;
            Debug.Log($"SkyboxDepthOptimizer: Ana kamera kırpma düzlemleri ayarlandı: Near={nearClipPlane}, Far={farClipPlane}");
        }
        else
        {
            Debug.LogWarning("SkyboxDepthOptimizer: Ana kamera (Main Camera) sahne üzerinde bulunamadı!");
        }

        // 2. XR Origin Ölçek Güvenliği
        // XR Origin'in çarpık ölçeklenmesi derinlik ve algı problemlerine yol açar.
        if (transform.localScale != Vector3.one)
        {
            transform.localScale = Vector3.one;
            Debug.Log("SkyboxDepthOptimizer: XR Origin ölçeği (1, 1, 1) olarak düzeltildi.");
        }

        // 3. Atmosferik Sis Ayarları (Uzak binaların ve gökyüzünün kaynaşmasını sağlayarak derinlik hissi üretir)
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = horizonFogColor;
            RenderSettings.fogDensity = fogDensity;
            Debug.Log($"SkyboxDepthOptimizer: Sahne uzaklık sisi (Fog) aktifleşti. Yoğunluk: {fogDensity}");
        }
        else
        {
            RenderSettings.fog = false;
        }
    }

    private void OnValidate()
    {
        // Editörde değerler değiştiğinde anında güncelle
        if (Application.isPlaying)
        {
            OptimizeSettings();
        }
    }
}
