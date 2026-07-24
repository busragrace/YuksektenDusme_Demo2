using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ScaffoldElevator : MonoBehaviour
{
    public float[] localStops = { 0.28f, 2.45f, 4.65f };
    public float speed = 1.2f;
    public bool allowKeyboardControls = true;
    public KeyCode upKey = KeyCode.PageUp;
    public KeyCode downKey = KeyCode.PageDown;
    public GameObject exitGateCollider;



    private readonly List<Transform> riders = new List<Transform>();
    private int targetStopIndex;
    private Vector3 previousPosition;

    private void Awake()
    {
        SortStops();
        targetStopIndex = FindClosestStopIndex();
        previousPosition = transform.position;
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (allowKeyboardControls)
        {
            if (Input.GetKeyDown(upKey))
            {
                MoveUp();
            }

            if (Input.GetKeyDown(downKey))
            {
                MoveDown();
            }
        }
#endif

        MoveCab();
        CarryRiders(transform.position - previousPosition);
        previousPosition = transform.position;
    }

    public void MoveUp()
    {
        SortStops();
        SetTargetStop(Mathf.Min(FindClosestStopIndex() + 1, localStops.Length - 1));
    }

    public void MoveDown()
    {
        SortStops();
        SetTargetStop(Mathf.Max(FindClosestStopIndex() - 1, 0));
    }

    public void MoveToStop(int stopIndex)
    {
        SortStops();
        SetTargetStop(stopIndex);
    }

    public void AddRider(Transform rider)
    {
        var playerRoot = FindPlayerRoot(rider);
        if (playerRoot != null && !riders.Contains(playerRoot))
        {
            riders.Add(playerRoot);
        }
    }

    public void RemoveRider(Transform rider)
    {
        var playerRoot = FindPlayerRoot(rider);
        if (playerRoot != null)
        {
            riders.Remove(playerRoot);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AddRider(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveRider(other.transform);
    }

    private void MoveCab()
    {
        if (localStops == null || localStops.Length == 0)
        {
            return;
        }

        var localPosition = transform.localPosition;
        var targetY = localStops[Mathf.Clamp(targetStopIndex, 0, localStops.Length - 1)];
        localPosition.y = Mathf.MoveTowards(localPosition.y, targetY, speed * Time.deltaTime);
        transform.localPosition = localPosition;
    }

    private void CarryRiders(Vector3 delta)
    {
        if (delta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        for (var i = riders.Count - 1; i >= 0; i--)
        {
            var rider = riders[i];
            if (rider == null)
            {
                riders.RemoveAt(i);
                continue;
            }

            var controller = rider.GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
            {
                controller.Move(delta);
            }
            else
            {
                rider.position += delta;
            }
        }
    }

    private void SetTargetStop(int stopIndex)
    {
        if (localStops == null || localStops.Length == 0)
        {
            return;
        }

        targetStopIndex = Mathf.Clamp(stopIndex, 0, localStops.Length - 1);
    }

    private int FindClosestStopIndex()
    {
        if (localStops == null || localStops.Length == 0)
        {
            return 0;
        }

        var closestIndex = 0;
        var closestDistance = Mathf.Abs(transform.localPosition.y - localStops[0]);
        for (var i = 1; i < localStops.Length; i++)
        {
            var distance = Mathf.Abs(transform.localPosition.y - localStops[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void SortStops()
    {
        if (localStops != null && localStops.Length > 1)
        {
            System.Array.Sort(localStops);
        }
    }

    private static Transform FindPlayerRoot(Transform candidate)
    {
        while (candidate != null)
        {
            if (candidate.CompareTag("Player"))
            {
                return candidate;
            }

            candidate = candidate.parent;
        }

        return null;
    }
}
