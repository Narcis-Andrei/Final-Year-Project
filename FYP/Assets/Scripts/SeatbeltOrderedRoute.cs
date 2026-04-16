using System.Collections.Generic;
using UnityEngine;

public class SeatbeltOrderedRoute : MonoBehaviour
{
    [Header("Route Points")]
    [SerializeField] private Transform startAnchor;
    [SerializeField] private Transform guide01;
    [SerializeField] private Transform guide02;
    [SerializeField] private Transform socketPoint;

    [Header("Tracked Buckle")]
    [SerializeField] private Transform buckleRoot;

    [Header("Visual")]
    [SerializeField] private SeatbeltSegmentedVisual segmentedVisual;

    [Header("Debug")]
    [SerializeField, Range(0, 3)] private int currentStage = 0;
    // 0 = nothing reached
    // 1 = Guide_01 reached
    // 2 = Guide_02 reached
    // 3 = Socket reached

    public int CurrentStage => currentStage;

    private void Start()
    {
        RefreshVisual();
    }

    public bool IsCorrectBuckle(Collider other)
    {
        if (buckleRoot == null || other == null)
            return false;

        return other.transform == buckleRoot || other.transform.IsChildOf(buckleRoot);
    }

    public void TryAdvanceToStage(int nextStage)
    {
        if (nextStage == currentStage + 1)
        {
            currentStage = nextStage;
            RefreshVisual();
        }
    }

    public void TryReverseToStage(int previousStage)
    {
        if (previousStage == currentStage - 1)
        {
            currentStage = previousStage;
            RefreshVisual();
        }
    }

    public void SetStage(int stage)
    {
        currentStage = Mathf.Clamp(stage, 0, 3);
        RefreshVisual();
    }

    public void ResetRoute()
    {
        currentStage = 0;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (segmentedVisual == null)
            return;

        List<Transform> activePoints = new List<Transform>();

        if (currentStage >= 1 && startAnchor != null)
            activePoints.Add(startAnchor);

        if (currentStage >= 1 && guide01 != null)
            activePoints.Add(guide01);

        if (currentStage >= 2 && guide02 != null)
            activePoints.Add(guide02);

        if (currentStage >= 3 && socketPoint != null)
            activePoints.Add(socketPoint);

        segmentedVisual.SetFixedPath(activePoints);
    }
}