using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RemoteSnapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private XRInteractionManager interactionManager;
    [SerializeField] private XRSocketInteractor socket;
    [SerializeField] private XRGrabInteractable remote;

    [Header("Optional: holder you want to move around")]
    [SerializeField] private Transform holderTransform;

    void Awake()
    {
        if (!socket) socket = GetComponent<XRSocketInteractor>();
        if (!holderTransform && socket) holderTransform = socket.transform;

        if (!interactionManager)
            interactionManager = FindFirstObjectByType<XRInteractionManager>();

        if (socket && interactionManager) socket.interactionManager = interactionManager;
        if (remote && interactionManager) remote.interactionManager = interactionManager;
    }

    public void PlaceRemoteAt(Vector3 worldPos, Quaternion worldRot)
    {
        if (!holderTransform) return;
        holderTransform.SetPositionAndRotation(worldPos, worldRot);
        SnapRemoteIntoSocket();
    }

    public void SnapRemoteIntoSocket()
    {
        if (!interactionManager || !socket || !remote) return;

        // Force release from hand (or any interactor)
        if (remote.isSelected)
        {
            var interactor = remote.firstInteractorSelecting as IXRSelectInteractor;
            var interactable = remote as IXRSelectInteractable;

            if (interactor != null && interactable != null)
                interactionManager.SelectExit(interactor, interactable);
        }

        // Eject current socket selection (optional)
        if (socket.hasSelection)
        {
            var current = socket.firstInteractableSelected as IXRSelectInteractable;
            var socketInteractor = socket as IXRSelectInteractor;

            if (socketInteractor != null && current != null)
                interactionManager.SelectExit(socketInteractor, current);
        }

        // Force socket to select remote
        interactionManager.SelectEnter(socket as IXRSelectInteractor, remote as IXRSelectInteractable);
    }

    public void RemoveRemoteFromSocket()
    {
        if (!interactionManager || !socket) return;
        if (!socket.hasSelection) return;

        var socketInteractor = socket as IXRSelectInteractor;
        var current = socket.firstInteractableSelected as IXRSelectInteractable;

        if (socketInteractor != null && current != null)
            interactionManager.SelectExit(socketInteractor, current);
    }
}
