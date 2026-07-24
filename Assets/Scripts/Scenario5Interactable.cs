using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Scenario5Interactable : MonoBehaviour
{
    public Scenario5Manager manager;
    public Scenario5Action action;
    public bool triggerOnPlayerEnter;
    public bool allowMouseClick = true;
    public bool oneShot;

    private bool used;

    private void Reset()
    {
        var targetCollider = GetComponent<Collider>();
        if (targetCollider != null)
        {
            targetCollider.isTrigger = triggerOnPlayerEnter;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnPlayerEnter && other.CompareTag("Player"))
        {
            InvokeAction();
        }
    }

    private void OnMouseDown()
    {
        if (allowMouseClick)
        {
            InvokeAction();
        }
    }

    public void InvokeAction()
    {
        if (used || manager == null)
        {
            return;
        }

        manager.PerformAction(action);

        if (oneShot)
        {
            used = true;
        }
    }
}
