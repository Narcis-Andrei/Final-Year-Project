using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RemoteButton : MonoBehaviour
{
    public enum ButtonId
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [SerializeField] private ButtonId buttonId;

    [Header("Drag the Remote root that has WheelchairLiftRemoteController")]
    [SerializeField] private WheelchairLift remoteController;

    // block the holding hand from pressing buttons
    [SerializeField] private XRGrabInteractable remoteGrab;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            Debug.LogError($"{name}: Missing XRSimpleInteractable.");
            enabled = false;
            return;
        }

        interactable.selectEntered.AddListener(OnPressed);
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnPressed);
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        // Grab with one hand and press with the other
        if (remoteGrab != null && remoteGrab.isSelected)
        {
            var holder = remoteGrab.firstInteractorSelecting;
            if (holder != null && args.interactorObject == holder)
                return;
        }

        Debug.Log($"REMOTE BUTTON PRESSED: {buttonId}");

        if (remoteController != null)
            remoteController.OnRemoteButton(buttonId);
        else
            Debug.LogWarning($"{name}: remoteController not assigned.");
    }
}
