using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WheelchairDriveWithPlayer : MonoBehaviour
{
    [Header("Handles (Simple Interactable, one per bar)")]
    [SerializeField] private XRSimpleInteractable leftHandle;
    [SerializeField] private XRSimpleInteractable rightHandle;

    [Header("Player rig root (drag your XR Origin object here - NOT the chair)")]
    [SerializeField] private Transform xrOrigin;

    [Header("Wheelchair root Rigidbody (optional)")]
    [SerializeField] private Rigidbody chairRb;

    [Header("Movement")]
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float deadzone = 0.15f;

    public enum ForwardAxis { ForwardZ, RightX }
    [Header("Which axis is 'forward' for your wheelchair model?")]
    [SerializeField] private ForwardAxis forwardAxis = ForwardAxis.ForwardZ;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private InputDevice leftDevice;
    private Transform originalParent;
    private bool attached;

    private bool TwoHandsOn =>
        leftHandle != null && rightHandle != null &&
        leftHandle.isSelected && rightHandle.isSelected;

    private void OnEnable()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    }

    private void Update()
    {
        if (!leftDevice.isValid)
            leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (xrOrigin == transform)
        {
            Debug.LogError("WheelchairDriveWithPlayer: xrOrigin is set to the wheelchair root. " +
                           "Assign your XR Origin (XR Rig) object instead.");
            return;
        }

        // Attach/detach player to chair based on both-hands
        if (!attached && TwoHandsOn && xrOrigin != null)
        {
            originalParent = xrOrigin.parent;
            xrOrigin.SetParent(transform, true); // player moves with chair
            attached = true;
        }
        else if (attached && !TwoHandsOn && xrOrigin != null)
        {
            xrOrigin.SetParent(originalParent, true);
            attached = false;
        }

        if (logDebug && leftDevice.isValid)
        {
            leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 s);
        }
    }

    private void FixedUpdate()
    {
        if (!TwoHandsOn) return;
        if (!leftDevice.isValid) return;

        if (!leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick))
            return;

        float drive = stick.y; // UP = forward, DOWN = backward
        if (Mathf.Abs(drive) < deadzone) return;

        Vector3 dir = (forwardAxis == ForwardAxis.RightX) ? transform.right : transform.forward;
        dir.y = 0f;
        dir.Normalize();

        Vector3 delta = dir * (drive * speed * Time.fixedDeltaTime);

        if (chairRb != null && !chairRb.isKinematic)
            chairRb.MovePosition(chairRb.position + delta);
        else
            transform.position += delta;
    }
}
