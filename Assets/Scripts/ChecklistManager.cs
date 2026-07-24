using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ChecklistManager : MonoBehaviour
{
    [Header("Kontrol Elemanlarý")]
    public Toggle[] toggles; // Müfettiþ panelinden 4 Toggle'ý buraya sürükle
    public Button okayButton; // Tamam butonunu buraya sürükle

    void Start()
    {
        // Baþlangýçta okey tuþu kapalý olmalý
        if (okayButton != null)
        {
            okayButton.interactable = false;
        }
    }

    void Update()
    {
        // Canvas her zaman kullanýcýya baksýn (Billboard etkisi)
        // VR'da daldýrma hissi için önemlidir
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                         Camera.main.transform.rotation * Vector3.up);
    }

    // Bu fonksiyon her Toggle'ýn "On Value Changed" kýsmýna baðlanmalý
    public void CheckAllToggles()
    {
        // Tüm toggle'lar iþaretli mi diye bakýyoruz
        bool allChecked = toggles.All(t => t != null && t.isOn);

        if (okayButton != null)
        {
            okayButton.interactable = allChecked;
        }
    }

    public void OnClickOkay()
    {
        Debug.Log("ÝSG Kontrolü Baþarýlý: Ýskele eriþimi açýldý.");
        gameObject.SetActive(false); // Ekraný kapat
    }

    public void OnClickBack()
    {
        gameObject.SetActive(false); // Geri tuþuyla ekraný kapat
    }
}