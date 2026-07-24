using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class VRInteractionConnector : MonoBehaviour
{
    private Scenario1Manager manager;

    private void Awake()
    {
        // 0. Konsoldaki 999+ adet NullReferenceException affordance hatasını sustur ve performansı düzelt
        FixAffordanceNulls();
    }

    private void Start()
    {
        manager = GetComponent<Scenario1Manager>();
        if (manager == null) return;

        // 1. Kemer (Emniyet Kemeri) VR Etkileşimi
        GameObject kemer = GameObject.Find("kemer");
        if (kemer != null)
        {
            // Yakalanabilmesi için Collider ekle (yoksa)
            if (kemer.GetComponent<Collider>() == null)
            {
                var col = kemer.AddComponent<MeshCollider>();
                col.convex = true;
            }

            // Deforme olmasını engellemek için bağını kopar ama konumunu koru
            kemer.transform.SetParent(null, true);

            var interactable = kemer.GetComponent<XRBaseInteractable>();
            if (interactable == null) interactable = kemer.AddComponent<XRGrabInteractable>();

            // Oyuncu kemeri eline (grip tuşuyla) aldığında giyilmiş sayılır ve masadan kaybolur
            interactable.selectEntered.AddListener((args) => {
                manager.EquipHarness();
                kemer.SetActive(false); // Masadan kaybolmasını sağlar
                Debug.Log("Scenario 1 | Kemer VR eliyle tutuldu, giyildi ve masadan kayboldu.");
            });
        }

        // 2. Güvenli Ankraj VR Etkileşimi (Lazerle seçilebilir)
        GameObject safeAnchor = GameObject.Find("SafeAnchorPoint");
        if (safeAnchor != null)
        {
            if (safeAnchor.GetComponent<Collider>() == null)
            {
                var col = safeAnchor.AddComponent<SphereCollider>();
                col.radius = 0.25f; // Kolay hedef alınabilmesi için alan oluşturuldu
            }

            var interactable = safeAnchor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable == null) interactable = safeAnchor.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

            interactable.selectEntered.AddListener((args) => {
                manager.AttachAnchor(true);
                Debug.Log("Scenario 1 | VR Lazer ile guvenli ankraj secildi.");
            });
        }

        // 3. Güvensiz Ankraj VR Etkileşimi (Lazerle seçilebilir)
        GameObject unsafeAnchor = GameObject.Find("UnsafeAnchorPoint");
        if (unsafeAnchor != null)
        {
            if (unsafeAnchor.GetComponent<Collider>() == null)
            {
                var col = unsafeAnchor.AddComponent<SphereCollider>();
                col.radius = 0.25f;
            }

            var interactable = unsafeAnchor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
            if (interactable == null) interactable = unsafeAnchor.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

            interactable.selectEntered.AddListener((args) => {
                manager.AttachAnchor(false);
                Debug.Log("Scenario 1 | VR Lazer ile guvensiz ankraj secildi.");
            });
        }
    }

    private void FixAffordanceNulls()
    {
        Component[] allComponents = FindObjectsOfType<Component>();
        int count = 0;
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().Name;
            
            // XRI Affordance bileşenleri içi boş kaldığında hata spamlarlar.
            if (typeName.Contains("Helper") || typeName.Contains("Receiver") || typeName.Contains("Affordance"))
            {
                System.Type type = comp.GetType();
                // Üst sınıflara doğru tırmanarak (reflection ile) private Renderer alanlarını bulalım
                while (type != null && type != typeof(MonoBehaviour))
                {
                    var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType.IsSubclassOf(typeof(Renderer)) || field.FieldType == typeof(Renderer))
                        {
                            var val = field.GetValue(comp);
                            if (val == null)
                            {
                                Renderer r = comp.GetComponent<Renderer>();
                                if (r == null) r = comp.GetComponentInChildren<Renderer>();
                                if (r != null)
                                {
                                    field.SetValue(comp, r);
                                    count++;
                                }
                                else
                                {
                                    if (comp is Behaviour b)
                                    {
                                        b.enabled = false;
                                        count++;
                                    }
                                }
                            }
                        }
                    }
                    type = type.BaseType;
                }
            }
        }
        Debug.Log($"VRInteractionConnector | Affordance uyarilari temizlendi. Duzeltilen/Kapatilan bilesen sayisi: {count}");
    }
}
