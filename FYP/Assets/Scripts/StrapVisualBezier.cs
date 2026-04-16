using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class StrapVisualBezier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform startAnchor;
    [SerializeField] private XRSocketInteractor endSocket;

    [Tooltip("Usually your Hook transform. If set, can be used as the end position.")]
    [SerializeField] private Transform fallbackEnd;

    [Tooltip("Transform used as the strap root (often an empty GameObject).")]
    [SerializeField] private Transform strapMesh;

    [Header("Strap Size (meters, world space)")]
    [SerializeField] private float forcedWidth = 0.05f;      // 5 cm
    [SerializeField] private float forcedThickness = 0.008f; // 8 mm

    [Tooltip("Trim both ends by this amount (meters).")]
    [SerializeField] private float endOffset = 0.0f;

    [Header("Length Axis")]
    [SerializeField] private Axis lengthAxis = Axis.Z;

    [Header("Behavior")]
    [SerializeField] private bool pivotAtStart = true;
    [SerializeField] private bool useStartAnchorUp = true;

    [Tooltip("If true, use fallbackEnd (Hook) as the end when assigned. If false, prefer socket when selected.")]
    [SerializeField] private bool preferFallbackEnd = true;

    [Tooltip("If true, the mesh near end is bounds.min on the length axis. If flipped, turn this off.")]
    [SerializeField] private bool nearEndIsMin = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Transform meshChild;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private Bounds geomBounds;
    private Vector3 geomSizeLocal;
    private bool hasGeom;

    private float nextLogTime;

    // Keeps a safe direction so LookRotation never receives Vector3.zero
    private Vector3 lastValidForward = Vector3.forward;

    public enum Axis { X = 0, Y = 1, Z = 2 }

    private void Awake()
    {
        SetupMeshChild();

        if (startAnchor != null && startAnchor.forward.sqrMagnitude > 0.000001f)
            lastValidForward = startAnchor.forward.normalized;
    }

    private void OnValidate()
    {
        forcedWidth = Mathf.Max(0.001f, forcedWidth);
        forcedThickness = Mathf.Max(0.001f, forcedThickness);
        endOffset = Mathf.Max(0f, endOffset);
    }

    private void SetupMeshChild()
    {
        if (!strapMesh) return;

        meshChild = strapMesh.Find("__StrapGeom");
        if (!meshChild)
        {
            var originalMF = strapMesh.GetComponent<MeshFilter>();
            var originalMR = strapMesh.GetComponent<MeshRenderer>();

            var go = new GameObject("__StrapGeom");
            go.transform.SetParent(strapMesh, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            if (originalMF)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = originalMF.sharedMesh;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(originalMF);
                else Destroy(originalMF);
#else
                Destroy(originalMF);
#endif
            }

            if (originalMR)
            {
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = originalMR.sharedMaterials;
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(originalMR);
                else Destroy(originalMR);
#else
                Destroy(originalMR);
#endif
            }

            meshChild = go.transform;
        }

        meshRenderer = meshChild.GetComponentInChildren<MeshRenderer>(true);
        meshFilter = meshChild.GetComponentInChildren<MeshFilter>(true);

        hasGeom = false;
        if (meshFilter && meshFilter.sharedMesh)
        {
            geomBounds = meshFilter.sharedMesh.bounds;
            geomSizeLocal = geomBounds.size;

            geomSizeLocal.x = Mathf.Max(0.0001f, geomSizeLocal.x);
            geomSizeLocal.y = Mathf.Max(0.0001f, geomSizeLocal.y);
            geomSizeLocal.z = Mathf.Max(0.0001f, geomSizeLocal.z);

            hasGeom = true;
        }
        else
        {
            geomBounds = new Bounds(Vector3.zero, Vector3.one);
            geomSizeLocal = Vector3.one;
        }
    }

    private void LateUpdate()
    {
        if (!startAnchor || !strapMesh) return;
        if (!meshChild || !hasGeom) SetupMeshChild();
        if (!meshChild) return;

        if (meshRenderer && !meshRenderer.enabled) meshRenderer.enabled = true;

        Vector3 startPos = startAnchor.position;
        if (!TryGetEndPosition(out Vector3 endPos))
            endPos = startPos + startAnchor.forward * 0.1f;

        Vector3 dir = endPos - startPos;
        float dist = dir.magnitude;

        // Trim ends in world space
        if (endOffset > 0f && dist > endOffset * 2f)
        {
            Vector3 trimFwd = dir.normalized;
            startPos += trimFwd * endOffset;
            endPos -= trimFwd * endOffset;

            dir = endPos - startPos;
            dist = dir.magnitude;
        }

        // Safe forward
        Vector3 fwd;
        if (dir.sqrMagnitude > 0.000001f)
        {
            fwd = dir.normalized;
            lastValidForward = fwd;
        }
        else
        {
            fwd = (lastValidForward.sqrMagnitude > 0.000001f)
                ? lastValidForward
                : (startAnchor.forward.sqrMagnitude > 0.000001f ? startAnchor.forward.normalized : Vector3.forward);
        }

        // Prevent zero / nearly-zero length from breaking scale math
        float safeDist = Mathf.Max(dist, 0.001f);

        // Safe up
        Vector3 up = useStartAnchorUp ? startAnchor.up : Vector3.up;
        if (up.sqrMagnitude < 0.000001f || Mathf.Abs(Vector3.Dot(up.normalized, fwd)) > 0.999f)
        {
            up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(up, fwd)) > 0.999f)
                up = Vector3.right;
        }

        strapMesh.rotation = Quaternion.LookRotation(fwd, up);
        strapMesh.position = pivotAtStart ? startPos : (startPos + endPos) * 0.5f;

        int a = (int)lengthAxis;
        int b = (a + 1) % 3;
        int c = (a + 2) % 3;

        float targetLengthW = safeDist;
        float targetWidthW = forcedWidth;
        float targetThickW = forcedThickness;

        float parentScaleA = Mathf.Abs(GetAxis(strapMesh.lossyScale, a));
        float parentScaleB = Mathf.Abs(GetAxis(strapMesh.lossyScale, b));
        float parentScaleC = Mathf.Abs(GetAxis(strapMesh.lossyScale, c));

        parentScaleA = Mathf.Max(0.0001f, parentScaleA);
        parentScaleB = Mathf.Max(0.0001f, parentScaleB);
        parentScaleC = Mathf.Max(0.0001f, parentScaleC);

        float baseLenLocal = GetAxis(geomSizeLocal, a);
        float baseWLocal = GetAxis(geomSizeLocal, b);
        float baseTLocal = GetAxis(geomSizeLocal, c);

        float childScaleA = targetLengthW / (baseLenLocal * parentScaleA);
        float childScaleB = targetWidthW / (baseWLocal * parentScaleB);
        float childScaleC = targetThickW / (baseTLocal * parentScaleC);

        Vector3 s = Vector3.one;
        SetAxis(ref s, a, childScaleA);
        SetAxis(ref s, b, childScaleB);
        SetAxis(ref s, c, childScaleC);

        meshChild.localScale = s;
        meshChild.localRotation = Quaternion.identity;

        float axisScaleLocal = GetAxis(meshChild.localScale, a);
        float minA = GetAxis(geomBounds.min, a) * axisScaleLocal;
        float maxA = GetAxis(geomBounds.max, a) * axisScaleLocal;

        float near = nearEndIsMin ? minA : maxA;

        float offsetA = pivotAtStart
            ? -near
            : -(minA + maxA) * 0.5f;

        Vector3 p = Vector3.zero;
        SetAxis(ref p, a, offsetA);
        meshChild.localPosition = p;

        if (debugLogs && Time.time >= nextLogTime)
        {
            Debug.Log(
                $"[Strap] distW={dist:F3} safeDist={safeDist:F3} parentLossy={strapMesh.lossyScale} childScale={meshChild.localScale} end={(preferFallbackEnd ? "HookFirst" : "SocketFirst")}",
                this
            );
            nextLogTime = Time.time + 0.5f;
        }
    }

    private bool TryGetEndPosition(out Vector3 endPos)
    {
        if (preferFallbackEnd && fallbackEnd != null)
        {
            endPos = fallbackEnd.position;
            return true;
        }

        if (endSocket != null && endSocket.hasSelection)
        {
            var at = endSocket.attachTransform;
            endPos = (at != null) ? at.position : endSocket.transform.position;
            return true;
        }

        if (!preferFallbackEnd && fallbackEnd != null)
        {
            endPos = fallbackEnd.position;
            return true;
        }

        if (endSocket != null)
        {
            endPos = endSocket.transform.position;
            return true;
        }

        endPos = default;
        return false;
    }

    private static float GetAxis(Vector3 v, int axis)
    {
        if (axis == 0) return v.x;
        if (axis == 1) return v.y;
        return v.z;
    }

    private static void SetAxis(ref Vector3 v, int axis, float value)
    {
        if (axis == 0) v.x = value;
        else if (axis == 1) v.y = value;
        else v.z = value;
    }

    private void OnDrawGizmos()
    {
        if (!startAnchor) return;
        if (!TryGetEndPosition(out var endPos)) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startAnchor.position, endPos);
        Gizmos.DrawSphere(startAnchor.position, 0.01f);
        Gizmos.DrawSphere(endPos, 0.01f);
    }
}