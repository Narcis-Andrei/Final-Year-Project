using UnityEngine;

public class TaskCompleteOnBothHandsWheelchair : MonoBehaviour
{
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string taskId;
    [SerializeField] private WheelchairDriveWithPlayer wheelchairDrive;

    private void OnEnable()
    {
        if (wheelchairDrive != null)
            wheelchairDrive.OnBothHandsPlaced += HandleBothHands;
    }

    private void OnDisable()
    {
        if (wheelchairDrive != null)
            wheelchairDrive.OnBothHandsPlaced -= HandleBothHands;
    }

    private void HandleBothHands()
    {
        if (taskManager == null) return;
        if (!taskManager.IsCurrentTask(taskId)) return;

        taskManager.CompleteTask(taskId);
    }
}