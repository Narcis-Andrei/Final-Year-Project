using System.Collections.Generic;
using UnityEngine;
using Unity.VRTemplate;

public class StageTaskManager : MonoBehaviour
{
    [System.Serializable]
    public class TaskEntry
    {
        public string taskId;
        public GameObject arrow;
    }

    [SerializeField] private StepManager stepManager;
    [SerializeField] private List<TaskEntry> tasks = new List<TaskEntry>();

    [SerializeField] private bool debugLogs = false;

    [SerializeField] private int currentTaskIndex = 0;

    private void OnEnable()
    {
        RefreshArrows();
    }

    private void Start()
    {
        RefreshArrows();
    }

    public string GetCurrentTaskId()
    {
        if (currentTaskIndex < 0 || currentTaskIndex >= tasks.Count)
            return string.Empty;

        return tasks[currentTaskIndex].taskId;
    }

    public bool IsCurrentTask(string taskId)
    {
        return GetCurrentTaskId() == taskId;
    }

    public void CompleteTask(string taskId)
    {
        if (!IsCurrentTask(taskId))
        {
            Log($"Wrong task. Expected '{GetCurrentTaskId()}', got '{taskId}'");
            return;
        }

        Log($"Completing task: {taskId}");

        if (currentTaskIndex < tasks.Count && tasks[currentTaskIndex].arrow != null)
            tasks[currentTaskIndex].arrow.SetActive(false);

        currentTaskIndex++;

        if (stepManager != null)
            stepManager.CompleteTask();

        RefreshArrows();
    }

    public void ResetTasks()
    {
        currentTaskIndex = 0;
        RefreshArrows();
    }

    public void RefreshArrows()
    {
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].arrow != null)
            {
                bool shouldBeActive = (i == currentTaskIndex);
                tasks[i].arrow.SetActive(shouldBeActive);

                Log($"Task '{tasks[i].taskId}' arrow '{tasks[i].arrow.name}' set active = {shouldBeActive}");
            }
            else
            {
                Log($"Task '{tasks[i].taskId}' has no arrow assigned.");
            }
        }

        Log($"Current task index: {currentTaskIndex}, taskId: '{GetCurrentTaskId()}'");
    }

    private void Log(string message)
    {
        if (!debugLogs) return;
        Debug.Log($"[StageTaskManager] {message}", this);
    }
}