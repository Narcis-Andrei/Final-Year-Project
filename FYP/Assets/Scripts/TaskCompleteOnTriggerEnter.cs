using UnityEngine;

public class TaskCompleteOnTriggerEnter : MonoBehaviour
{
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string taskId;
    [SerializeField] private Transform requiredRoot;

    private void OnTriggerEnter(Collider other)
    {
        if (taskManager == null) return;
        if (!taskManager.IsCurrentTask(taskId)) return;

        if (requiredRoot != null)
        {
            if (other.transform != requiredRoot && !other.transform.IsChildOf(requiredRoot))
                return;
        }

        taskManager.CompleteTask(taskId);
    }
}