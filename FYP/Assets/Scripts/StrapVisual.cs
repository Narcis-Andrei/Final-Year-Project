using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class StrapVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform strapRoot;   // empty
    [SerializeField] private Transform strapMesh;   // visible mesh child
    [SerializeField] private XRGrabInteractable hookGrab;
    [SerializeField] private XRSocketInteractor endSocket;

    [Header("Look")]
    [SerializeField] private float strapThickness = 0.015f; // in WORLD meters
    [SerializeField] private float endOffset = 0.0f;        // in WORLD meters

    public bool IsConnected { get; private set; }

    private void Awake()
    {
        if (endSocket != null)
        {
            endSocket.selectEntered.AddListener(OnSocketed);
            endSocket.selectExited.AddListener(OnUnsocketed);
        }
    }

    private void OnDestroy()
    {
        if (endSocket != null)
        {
            endSocket.selectEntered.RemoveListener(OnSocketed);
            endSocket.selectExited.RemoveListener(OnUnsocketed);
        }
    }

    private void LateUpdate()
    {
        if (!startAnchor || !strapRoot || !strapMesh || !hookGrab) return;

        Vector3 a = startAnchor.position;

        Vector3 b =
            (!IsConnected || endSocket == null)
                ? hookGrab.transform.position
                : (endSocket.attachTransform ? endSocket.attachTransform.position : endSocket.transform.position);

        Vector3 dir = b - a;
        float dist = dir.magnitude;
        if (dist < 0.0001f)
        {
            strapRoot.gameObject.SetActive(false);
            return;
        }

        strapRoot.gameObject.SetActive(true);

        Vector3 fwd = dir / dist;

        // Apply world-space end offsets
        Vector3 a2 = a + fwd * endOffset;
        Vector3 b2 = b - fwd * endOffset;

        Vector3 finalDir = b2 - a2;
        float finalDist = finalDir.magnitude;
        if (finalDist < 0.0001f)
        {
            strapRoot.gameObject.SetActive(false);
            return;
        }

        Vector3 finalFwd = finalDir / finalDist;

        // Root pinned at the start
        strapRoot.position = a2;
        strapRoot.rotation = Quaternion.LookRotation(finalFwd, startAnchor.up);

        // --- SCALE-SAFE PART ---
        // Get end point in strapRoot local space.
        // After the LookRotation, local +Z points toward the end.
        Vector3 endLocal = strapRoot.InverseTransformPoint(b2);
        float lenLocal = Mathf.Max(0.0001f, endLocal.z);

        // Convert desired WORLD thickness to LOCAL thickness
        // (avoid division by ~0)
        Vector3 lossy = strapRoot.lossyScale;
        float thickLocalX = strapThickness / Mathf.Max(0.0001f, lossy.x);
        float thickLocalY = strapThickness / Mathf.Max(0.0001f, lossy.y);

        // Move mesh so its "start" is at Z=0 and it grows forward only
        strapMesh.localPosition = new Vector3(0f, 0f, lenLocal * 0.5f);
        strapMesh.localRotation = Quaternion.identity;
        strapMesh.localScale = new Vector3(thickLocalX, thickLocalY, lenLocal);
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        if (args.interactableObject == hookGrab) IsConnected = true;
    }

    private void OnUnsocketed(SelectExitEventArgs args)
    {
        if (args.interactableObject == hookGrab) IsConnected = false;
    }
}
