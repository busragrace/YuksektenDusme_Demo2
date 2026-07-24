using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class LanyardTrigger : MonoBehaviour
{
    public Scenario1Manager manager;
    public bool isSafe;

    private void Start()
    {
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelect);
        }
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        if (manager != null)
        {
            manager.SelectLanyard(isSafe);
            Debug.Log("Scenario 1 | Lanyard secildi. Guvenli mi: " + isSafe);
        }
    }
}
