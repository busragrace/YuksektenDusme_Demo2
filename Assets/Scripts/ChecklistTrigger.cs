using UnityEngine;

public class ChecklistTrigger : MonoBehaviour
{
    [Header("UI Ayarlarý")]
    public GameObject checklistCanvas; // Müfettiþ panelinden Canvas'ý buraya sürükle

    void Start()
    {
        // Oyun baþladýðýnda ekranýn kapalý olduðundan emin oluyoruz
        if (checklistCanvas != null)
        {
            checklistCanvas.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Eðer iskeleye yaklaþan obje "Player" tagine sahipse ekraný aç
        if (other.CompareTag("Player"))
        {
            checklistCanvas.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Kullanýcý iskeleden uzaklaþýrsa ekraný tekrar kapat
        if (other.CompareTag("Player"))
        {
            checklistCanvas.SetActive(false);
        }
    }
}