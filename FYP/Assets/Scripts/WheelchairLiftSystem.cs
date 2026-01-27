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

    [Header("Key positions")]
    [SerializeField] private Transform closedPoint;     // where the platform sits when closed
    [SerializeField] private Transform busLevelPoint;   // level with bus floor
    [SerializeField] private Transform groundPoint;     // level with ground

    [Header("Movement")]
    [SerializeField] private float moveSeconds = 2.0f;

    public LiftState State => state;

    private Coroutine moveRoutine;

    private bool SetupOK()
    {
        return platform != null && closedPoint != null && busLevelPoint != null && groundPoint != null;
    }

    private bool Deny(string msg)
    {
        Debug.Log($"LIFT: Denied - {msg} (state={state})");
        return false;
    }

    // Button 1: Open/deploy and end at bus level
    public bool RequestDeployToBusLevel()
    {
        if (state != LiftState.Stowed) return Deny("Deploy only from Stowed");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(busLevelPoint.position, busLevelPoint.rotation, LiftState.AtBusLevel, "Deploy -> BusLevel");
        return true;
    }

    // Button 2: Lower from bus level to ground
    public bool RequestLowerToGround()
    {
        if (state != LiftState.AtBusLevel) return Deny("Lower only from BusLevel");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(groundPoint.position, groundPoint.rotation, LiftState.AtGround, "Lower -> Ground");
        return true;
    }

    // Button 3: Raise from ground to bus level
    public bool RequestRaiseToBusLevel()
    {
        if (state != LiftState.AtGround) return Deny("Raise only from Ground");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(busLevelPoint.position, busLevelPoint.rotation, LiftState.AtBusLevel, "Raise -> BusLevel");
        return true;
    }

    // Button 4: Stow from bus level back into bus
    public bool RequestStowFromBusLevel()
    {
        if (state != LiftState.AtBusLevel) return Deny("Stow only from BusLevel");
        if (!SetupOK()) return Deny("Missing platform/points");

        StartMove(closedPoint.position, closedPoint.rotation, LiftState.Stowed, "BusLevel -> Stow");
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

        Vector3 startPos = platform.position;
        Quaternion startRot = platform.rotation;

        float t = 0f;
        float dur = Mathf.Max(0.01f, moveSeconds);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            platform.position = Vector3.Lerp(startPos, targetPos, t);
            platform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        platform.position = targetPos;
        platform.rotation = targetRot;

        state = endState;
        Debug.Log($"LIFT: Reached {state}");
        moveRoutine = null;
    }
}
