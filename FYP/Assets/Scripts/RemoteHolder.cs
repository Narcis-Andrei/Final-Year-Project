using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RemoteHolder : MonoBehaviour
{
    [Header("References")]
    public XRInteractionManager interactionManager;
    public XRSocketInteractor socket;
    public XRGrabInteractable remote;

    [Header("Startup")]
    public bool startSocketed = true;

    [Header("Socketed Stability")]
    public bool makeKinematicWhileSocketed = true;
    public bool disableGravityWhileSocketed = true;

    [Header("DEBUG")]
    public bool debugMode = false;
    public bool drawAttachGizmo = true;
    public bool logScaleDebug = false;

    private Rigidbody remoteRb;
    private bool prevKinematic;
    private bool prevGravity;

    // Scale lock
    private Vector3 originalWorldScale;
    private bool scaleInitialized;

    // Parent restore only
    private Transform originalParent;

    private void Awake()
    {
        if (!socket)
            socket = GetComponent<XRSocketInteractor>();

        if (!interactionManager)
            interactionManager = FindObjectOfType<XRInteractionManager>();

        if (socket && interactionManager && !socket.interactionManager)
            socket.interactionManager = interactionManager;

        if (remote)
        {
            remoteRb = remote.GetComponent<Rigidbody>();

            originalWorldScale = remote.transform.lossyScale;
            scaleInitialized = true;

            originalParent = remote.transform.parent;

            Log($"Stored original remote world scale: {originalWorldScale}");
            Log($"Stored initial parent: {(originalParent ? originalParent.name : "none")}");
        }

        Log("Awake complete.");
    }

    private void Start()
    {
        if (startSocketed && remote && socket && interactionManager)
        {
            Log("Forcing initial socket selection...");

            interactionManager.SelectEnter(
                (IXRSelectInteractor)socket,
                (IXRSelectInteractable)remote
            );
        }
    }

    private void OnEnable()
    {
        if (!socket) return;

        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        if (!socket) return;

        socket.selectEntered.RemoveListener(OnSelectEntered);
        socket.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (!IsCorrectRemote(args))
            return;

        Transform remoteTransform = args.interactableObject.transform;

        if (logScaleDebug)
            Log($"Scale BEFORE socket: lossy={remoteTransform.lossyScale} local={remoteTransform.localScale} parentLossy={(remoteTransform.parent ? remoteTransform.parent.lossyScale.ToString() : "none")}");

        Log($"SELECT ENTER: {remoteTransform.name}");

        // Remember parent before socketing
        originalParent = remoteTransform.parent;

        // Parent to socket root
        remoteTransform.SetParent(socket.transform, false);

        // Snap to socket attach pose
        if (socket.attachTransform != null)
        {
            remoteTransform.SetPositionAndRotation(
                socket.attachTransform.position,
                socket.attachTransform.rotation
            );
        }

        ForceRemoteWorldScale(remoteTransform);

        if (logScaleDebug)
            Log($"Scale AFTER socket:  lossy={remoteTransform.lossyScale} local={remoteTransform.localScale} parentLossy={remoteTransform.parent.lossyScale}");

        StabilizeRigidbody(true);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!IsCorrectRemote(args))
            return;

        Transform remoteTransform = args.interactableObject.transform;

        if (logScaleDebug)
            Log($"Scale BEFORE detach: lossy={remoteTransform.lossyScale} local={remoteTransform.localScale} parentLossy={(remoteTransform.parent ? remoteTransform.parent.lossyScale.ToString() : "none")}");

        Log($"SELECT EXIT: {remoteTransform.name}");

        // Restore parent only.
        remoteTransform.SetParent(originalParent, true);

        ForceRemoteWorldScale(remoteTransform);

        if (logScaleDebug)
            Log($"Scale AFTER detach:  lossy={remoteTransform.lossyScale} local={remoteTransform.localScale} parentLossy={(remoteTransform.parent ? remoteTransform.parent.lossyScale.ToString() : "none")}");

        StabilizeRigidbody(false);
    }

    private bool IsCorrectRemote(BaseInteractionEventArgs args)
    {
        if (!remote)
            return true;

        return args.interactableObject.transform == remote.transform;
    }

    private void ForceRemoteWorldScale(Transform target)
    {
        if (!scaleInitialized)
            return;

        if (!target.parent)
        {
            target.localScale = originalWorldScale;
            return;
        }

        Vector3 parentScale = target.parent.lossyScale;

        target.localScale = new Vector3(
            SafeDiv(originalWorldScale.x, parentScale.x),
            SafeDiv(originalWorldScale.y, parentScale.y),
            SafeDiv(originalWorldScale.z, parentScale.z)
        );
    }

    private float SafeDiv(float a, float b)
    {
        return Mathf.Approximately(b, 0f) ? a : a / b;
    }

    private void StabilizeRigidbody(bool socketed)
    {
        if (!remoteRb || !makeKinematicWhileSocketed)
            return;

        if (socketed)
        {
            prevKinematic = remoteRb.isKinematic;
            prevGravity = remoteRb.useGravity;

            remoteRb.isKinematic = true;

            if (disableGravityWhileSocketed)
                remoteRb.useGravity = false;

#if UNITY_6000_0_OR_NEWER
            remoteRb.linearVelocity = Vector3.zero;
#else
            remoteRb.velocity = Vector3.zero;
#endif
            remoteRb.angularVelocity = Vector3.zero;

            Log("Rigidbody stabilized.");
        }
        else
        {
            remoteRb.isKinematic = prevKinematic;
            remoteRb.useGravity = prevGravity;

            Log("Rigidbody restored.");
        }
    }

    private void Log(string message)
    {
        if (!debugMode) return;
        Debug.Log($"[RemoteHolder] {message}");
    }

    private void OnDrawGizmos()
    {
        if (!drawAttachGizmo || !socket || !socket.attachTransform)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(socket.attachTransform.position, 0.05f);
    }
}