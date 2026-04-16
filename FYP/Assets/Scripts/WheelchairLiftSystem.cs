using UnityEngine;
using System.Collections;

public class WheelchairLiftSystem : MonoBehaviour
{
    public enum LiftState
    {
        Stowed,
        AtBusLevel,
        AtGround,
        Moving
    }

    [Header("Read Only")]
    [SerializeField] private LiftState state = LiftState.Stowed;

    [Header("Platform to move")]
    [SerializeField] private Transform platform;
    [SerializeField] private Rigidbody platformRb;

    [Header("Key positions")]
    [SerializeField] private Transform closedPoint;
    [SerializeField] private Transform busLevelPoint;
    [SerializeField] private Transform groundPoint;

    [Header("Movement")]
    [SerializeField] private float moveSeconds = 2.0f;

    public LiftState State => state;
    public System.Action<LiftState> OnLiftReachedState;

    private Coroutine moveRoutine;

    private void Awake()
    {
        if (platformRb == null && platform != null)
            platformRb = platform.GetComponent<Rigidbody>();

        if (platformRb != null)
        {
            platformRb.isKinematic = true;
            platformRb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private bool SetupOK()
    {
        return platform != null && closedPoint != null && busLevelPoint != null && groundPoint != null;
    }

    private bool Deny(string msg)
    {
        Debug.Log($"LIFT: Denied - {msg} (state={state})");
        return false;
    }

    public bool RequestDeployToBusLevel()
    {
        if (state != LiftState.Stowed) return Deny("Deploy only from Stowed");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(busLevelPoint.position, busLevelPoint.rotation, LiftState.AtBusLevel, "Deploy to BusLevel");
        return true;
    }

    public bool RequestLowerToGround()
    {
        if (state != LiftState.AtBusLevel) return Deny("Lower only from BusLevel");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(groundPoint.position, groundPoint.rotation, LiftState.AtGround, "Lower to Ground");
        return true;
    }

    public bool RequestRaiseToBusLevel()
    {
        if (state != LiftState.AtGround) return Deny("Raise only from Ground");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(busLevelPoint.position, busLevelPoint.rotation, LiftState.AtBusLevel, "Raise to BusLevel");
        return true;
    }

    public bool RequestStowFromBusLevel()
    {
        if (state != LiftState.AtBusLevel) return Deny("Stored only from BusLevel");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(closedPoint.position, closedPoint.rotation, LiftState.Stowed, "BusLevel To Stored");
        return true;
    }

    private void StartMove(Vector3 targetPos, Quaternion targetRot, LiftState endState, string label)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(targetPos, targetRot, endState, label));
    }

    private IEnumerator MoveRoutine(Vector3 targetPos, Quaternion targetRot, LiftState endState, string label)
    {
        state = LiftState.Moving;
        Debug.Log($"LIFT: {label} (moving)");

        Vector3 startPos = platformRb != null ? platformRb.position : platform.position;
        Quaternion startRot = platformRb != null ? platformRb.rotation : platform.rotation;

        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, moveSeconds);

        while (elapsed < dur)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);

            Vector3 nextPos = Vector3.Lerp(startPos, targetPos, t);
            Quaternion nextRot = Quaternion.Slerp(startRot, targetRot, t);

            if (platformRb != null)
            {
                platformRb.MovePosition(nextPos);
                platformRb.MoveRotation(nextRot);
            }
            else
            {
                platform.position = nextPos;
                platform.rotation = nextRot;
            }

            yield return new WaitForFixedUpdate();
        }

        if (platformRb != null)
        {
            platformRb.MovePosition(targetPos);
            platformRb.MoveRotation(targetRot);
        }
        else
        {
            platform.position = targetPos;
            platform.rotation = targetRot;
        }

        state = endState;
        Debug.Log($"LIFT: Reached {state}");
        OnLiftReachedState?.Invoke(state);
        moveRoutine = null;
    }
}