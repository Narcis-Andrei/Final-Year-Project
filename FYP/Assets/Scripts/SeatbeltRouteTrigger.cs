using UnityEngine;

public class SeatbeltRouteTrigger : MonoBehaviour
{
    [SerializeField] private SeatbeltOrderedRoute route;

    [Header("Forward / Reverse Stages")]
    [SerializeField, Range(0, 3)] private int forwardTargetStage = 1;
    [SerializeField, Range(0, 3)] private int reverseTargetStage = 0;

    [Header("Optional Task Progress")]
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string forwardTaskId;
    [SerializeField] private string reverseTaskId;

    private void OnTriggerEnter(Collider other)
    {
        if (route == null)
            return;

        if (!route.IsCorrectBuckle(other))
            return;

        int current = route.CurrentStage;

        // Forward case
        if (forwardTargetStage == current + 1)
        {
            route.TryAdvanceToStage(forwardTargetStage);

            if (taskManager != null &&
                !string.IsNullOrWhiteSpace(forwardTaskId) &&
                taskManager.IsCurrentTask(forwardTaskId))
            {
                taskManager.CompleteTask(forwardTaskId);
            }

            return;
        }

        // Reverse case
        if (reverseTargetStage == current - 1)
        {
            route.TryReverseToStage(reverseTargetStage);

            if (taskManager != null &&
                !string.IsNullOrWhiteSpace(reverseTaskId) &&
                taskManager.IsCurrentTask(reverseTaskId))
            {
                taskManager.CompleteTask(reverseTaskId);
            }

            return;
        }
    }
}