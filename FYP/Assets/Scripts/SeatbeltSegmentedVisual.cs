using System.Collections.Generic;
using UnityEngine;

public class SeatbeltSegmentedVisual : MonoBehaviour
{
    public enum LengthAxis
    {
        X,
        Y,
        Z
    }

    [Header("Segment Visual")]
    [SerializeField] private GameObject segmentPrefab;
    [SerializeField] private float beltWidth = 0.05f;
    [SerializeField] private float beltThickness = 0.005f;

    [Header("Orientation")]
    [SerializeField] private Vector3 segmentUpAxis = Vector3.up;
    [SerializeField] private LengthAxis lengthAxis = LengthAxis.Z;

    private readonly List<Transform> activePath = new List<Transform>();
    private readonly List<Transform> spawnedSegments = new List<Transform>();

    public void SetFixedPath(List<Transform> points)
    {
        activePath.Clear();

        if (points != null)
            activePath.AddRange(points);

        EnsureCorrectSegmentCount();
        UpdateSegments();
    }

    private void OnValidate()
    {
        beltWidth = Mathf.Max(0.001f, beltWidth);
        beltThickness = Mathf.Max(0.001f, beltThickness);
    }

    private void LateUpdate()
    {
        UpdateSegments();
    }

    private void EnsureCorrectSegmentCount()
    {
        int required = Mathf.Max(0, activePath.Count - 1);

        if (spawnedSegments.Count == required)
            return;

        ClearSegments();

        for (int i = 0; i < required; i++)
        {
            GameObject seg;

            if (segmentPrefab != null)
            {
                seg = Instantiate(segmentPrefab, transform);
            }
            else
            {
                seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.transform.SetParent(transform, false);

                Collider c = seg.GetComponent<Collider>();
                if (c != null)
                    Destroy(c);
            }

            seg.name = $"BeltSection_{i}";
            spawnedSegments.Add(seg.transform);
        }
    }

    private void ClearSegments()
    {
        for (int i = spawnedSegments.Count - 1; i >= 0; i--)
        {
            if (spawnedSegments[i] != null)
                Destroy(spawnedSegments[i].gameObject);
        }

        spawnedSegments.Clear();
    }

    private void UpdateSegments()
    {
        if (activePath.Count < 2)
        {
            HideAllSegments();
            return;
        }

        for (int i = 0; i < spawnedSegments.Count; i++)
        {
            Transform segment = spawnedSegments[i];

            if (segment == null)
                continue;

            if (i >= activePath.Count - 1)
            {
                segment.gameObject.SetActive(false);
                continue;
            }

            Transform pointA = activePath[i];
            Transform pointB = activePath[i + 1];

            if (pointA == null || pointB == null)
            {
                segment.gameObject.SetActive(false);
                continue;
            }

            PlaceSegment(segment, pointA.position, pointB.position);
        }
    }

    private void HideAllSegments()
    {
        for (int i = 0; i < spawnedSegments.Count; i++)
        {
            if (spawnedSegments[i] != null)
                spawnedSegments[i].gameObject.SetActive(false);
        }
    }

    private void PlaceSegment(Transform segment, Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float length = dir.magnitude;

        if (length <= 0.0001f)
        {
            segment.gameObject.SetActive(false);
            return;
        }

        segment.gameObject.SetActive(true);
        segment.position = (start + end) * 0.5f;

        Vector3 forward = dir.normalized;
        Vector3 up = segmentUpAxis.sqrMagnitude > 0.0001f ? segmentUpAxis.normalized : Vector3.up;

        if (Vector3.Cross(forward, up).sqrMagnitude > 0.0001f)
            segment.rotation = Quaternion.LookRotation(forward, up);

        Vector3 scale = Vector3.one;

        switch (lengthAxis)
        {
            case LengthAxis.X:
                scale.x = length;
                scale.y = beltThickness;
                scale.z = beltWidth;
                break;

            case LengthAxis.Y:
                scale.x = beltWidth;
                scale.y = length;
                scale.z = beltThickness;
                break;

            case LengthAxis.Z:
                scale.x = beltWidth;
                scale.y = beltThickness;
                scale.z = length;
                break;
        }

        segment.localScale = scale;
    }
}