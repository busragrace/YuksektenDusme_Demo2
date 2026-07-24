using UnityEngine;
using UnityEngine.UI;

public class ChecklistSubmitButton : MonoBehaviour
{
    [Header("Checklist Elemanlarý")]
    public Toggle toggle1;
    public Toggle toggle2;
    public Toggle toggle3;
    public Toggle toggle4;

    [Header("Kapanacak & Açýlacak Nesneler")]
    public GameObject checklistCanvas;
    public GameObject elevatorUpButton;

    private Button myButton;

    private void Start()
    {
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnSubmitPressed);
            Debug.Log("ChecklistSubmitButton: Buton baþarýyla dinlenmeye baþlandý!");
        }
    }

    private void OnSubmitPressed()
    {
        // Konsola týklandýðý bilgisini yazdýrýyoruz (Bu çok önemli!)
        Debug.Log("ChecklistSubmitButton: Tamam butonuna týklandý!");

        if (toggle1 == null || toggle2 == null || toggle3 == null || toggle4 == null)
        {
            Debug.LogError("ChecklistSubmitButton: Toggle referanslarýndan biri veya birkaçý eksik!");
            return;
        }

        // Durum kontrolü
        Debug.Log($"Toggle Durumlarý: T1:{toggle1.isOn}, T2:{toggle2.isOn}, T3:{toggle3.isOn}, T4:{toggle4.isOn}");

        if (toggle1.isOn && toggle2.isOn && toggle3.isOn && toggle4.isOn)
        {
            Debug.Log("ChecklistSubmitButton: 4 Toggle da iþaretli! Kapatýlýyor...");
            if (checklistCanvas != null) checklistCanvas.SetActive(false);
            if (elevatorUpButton != null) elevatorUpButton.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ChecklistSubmitButton: Tüm toggle'lar iþaretli deðil!");
        }
    }
}