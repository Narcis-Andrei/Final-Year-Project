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
    [SerializeField] private float strapThickness = 0.03f; // meters
    [SerializeField] private float endOffset = 0.0f;

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
        if (!startAnchor || !hookGrab || !strapRoot || !strapMesh)
            return;

        Vector3 a = startAnchor.position;

        Vector3 b =
            (!IsConnected || endSocket == null)
                ? hookGrab.transform.position
                : (endSocket.attachTransform ? endSocket.attachTransform.position : endSocket.transform.position);

        // Apply end offsets (trim)
        Vector3 dir = (b - a);
        float dist = dir.magnitude;
        if (dist < 0.0001f)
        {
            strapRoot.gameObject.SetActive(false);
            return;
        }

        Vector3 fwd = dir / dist;
        Vector3 a2 = a + fwd * endOffset;
        Vector3 b2 = b - fwd * endOffset;

        if ((b2 - a2).sqrMagnitude < 0.000001f)
        {
            strapRoot.gameObject.SetActive(false);
            return;
        }

        strapRoot.gameObject.SetActive(true);
        RenderStraightMesh(a2, b2);
    }

    private void RenderStraightMesh(Vector3 a2, Vector3 b2)
    {
        Vector3 finalDir = b2 - a2;
        float finalDist = finalDir.magnitude;
        Vector3 finalFwd = finalDir / Mathf.Max(0.0001f, finalDist);

        strapRoot.position = a2;
        strapRoot.rotation = Quaternion.LookRotation(finalFwd, startAnchor.up);

        Vector3 endLocal = strapRoot.InverseTransformPoint(b2);
        float lenLocal = Mathf.Max(0.0001f, endLocal.z);

        Vector3 lossy = strapRoot.lossyScale;
        float thickLocalX = strapThickness / Mathf.Max(0.0001f, lossy.x);
        float thickLocalY = strapThickness / Mathf.Max(0.0001f, lossy.y);

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
