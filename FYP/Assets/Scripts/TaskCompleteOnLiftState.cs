using UnityEngine;

public class TaskCompleteOnLiftState : MonoBehaviour
{
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string taskId;
    [SerializeField] private WheelchairLiftSystem liftSystem;
    [SerializeField] private WheelchairLiftSystem.LiftState requiredState;

    private void OnEnable()
    {
        if (liftSystem != null)
            liftSystem.OnLiftReachedState += HandleLiftState;
    }

    private void OnDisable()
    {
        if (liftSystem != null)
            liftSystem.OnLiftReachedState -= HandleLiftState;
    }

    private void HandleLiftState(WheelchairLiftSystem.LiftState state)
    {
        Debug.Log($"[TaskCompleteOnLiftState] Heard {state}, need {requiredState}, taskId={taskId}", this);

        if (taskManager == null)
        {
            Debug.LogWarning("[TaskCompleteOnLiftState] taskManager is null.", this);
            return;
        }

        if (!taskManager.IsCurrentTask(taskId))
        {
            Debug.Log($"[TaskCompleteOnLiftState] '{taskId}' is not the current task.", this);
            return;
        }

        if (state != requiredState)
        {
            Debug.Log($"[TaskCompleteOnLiftState] State mismatch.", this);
            return;
        }

        taskManager.CompleteTask(taskId);
    }
}