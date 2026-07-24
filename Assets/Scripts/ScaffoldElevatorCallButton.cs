using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScaffoldElevatorCallButton : MonoBehaviour
{
    public ScaffoldElevator elevator;
    public bool moveUp = true;
    public bool triggerOnPlayerEnter = true;
    public bool allowMouseClick = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnPlayerEnter || !IsPlayer(other.transform))
        {
            return;
        }

        if (elevator != null)
        {
            elevator.AddRider(other.transform);
        }

        Press();
    }

    private void OnMouseDown()
    {
        if (allowMouseClick)
        {
            Press();
        }
    }

    public void Press()
    {
        if (elevator == null)
        {
            return;
        }

        if (moveUp)
        {
            elevator.MoveUp();
        }
        else
        {
            elevator.MoveDown();
        }
    }

    private static bool IsPlayer(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.CompareTag("Player"))
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }
}
