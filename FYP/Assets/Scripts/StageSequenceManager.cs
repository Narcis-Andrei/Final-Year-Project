using System.Collections.Generic;
using UnityEngine;

public class StageSequenceManager : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        public string stageName;
        public GameObject stageRoot;
        public List<GameObject> taskArrows = new List<GameObject>();
    }

    [Header("Stages")]
    [SerializeField] private List<Stage> stages = new List<Stage>();

    [Header("Startup")]
    [SerializeField] private int startStageIndex = 0;
    [SerializeField] private int startTaskIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private int currentStageIndex = 0;
    private int currentTaskIndex = 0;
    private bool allStagesComplete = false;

    private void Start()
    {
        currentStageIndex = Mathf.Clamp(startStageIndex, 0, Mathf.Max(0, stages.Count - 1));
        currentTaskIndex = Mathf.Max(0, startTaskIndex);

        RefreshAll();
    }

    public void RefreshAll()
    {
        if (stages.Count == 0)
        {
            Log("No stages assigned.");
            return;
        }

        if (allStagesComplete)
        {
            HideAllStages();
            HideAllArrows();
            Log("All stages already complete.");
            return;
        }

        ClampCurrentIndexes();
        ShowOnlyCurrentStage();
        ShowOnlyCurrentTaskArrow();

        Log($"RefreshAll -> Stage {currentStageIndex}, Task {currentTaskIndex}, StageName='{GetCurrentStageName()}'");
    }

    public void CompleteCurrentTask()
    {
        if (!HasValidCurrentStage())
            return;

        Stage stage = stages[currentStageIndex];
        int taskCount = GetValidTaskCount(stage);

        if (taskCount == 0)
        {
            Log($"Stage '{stage.stageName}' has no task arrows. Completing stage immediately.");
            CompleteCurrentStage();
            return;
        }

        currentTaskIndex++;
        Log($"Completed task. New task index = {currentTaskIndex}");

        if (currentTaskIndex >= taskCount)
        {
            CompleteCurrentStage();
            return;
        }

        ShowOnlyCurrentTaskArrow();
    }

    public void GoToStage(int stageIndex, int taskIndex = 0)
    {
        if (stages.Count == 0)
            return;

        if (stageIndex < 0 || stageIndex >= stages.Count)
        {
            Log($"GoToStage failed. Invalid stage index: {stageIndex}");
            return;
        }

        allStagesComplete = false;
        currentStageIndex = stageIndex;
        currentTaskIndex = Mathf.Max(0, taskIndex);

        RefreshAll();
    }

    public void ResetSequence()
    {
        allStagesComplete = false;
        currentStageIndex = Mathf.Clamp(startStageIndex, 0, Mathf.Max(0, stages.Count - 1));
        currentTaskIndex = Mathf.Max(0, startTaskIndex);

        RefreshAll();
        Log("Sequence reset.");
    }

    public int GetCurrentStageIndex() => currentStageIndex;
    public int GetCurrentTaskIndex() => currentTaskIndex;

    public string GetCurrentStageName()
    {
        if (!HasValidCurrentStage())
            return string.Empty;

        return stages[currentStageIndex].stageName;
    }

    private void CompleteCurrentStage()
    {
        Log($"Completed stage '{GetCurrentStageName()}'");

        currentStageIndex++;
        currentTaskIndex = 0;

        if (currentStageIndex >= stages.Count)
        {
            allStagesComplete = true;
            HideAllStages();
            HideAllArrows();
            Log("All stages complete.");
            return;
        }

        RefreshAll();
    }

    private void ShowOnlyCurrentStage()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageRoot != null)
                stages[i].stageRoot.SetActive(i == currentStageIndex);
        }
    }

    private void ShowOnlyCurrentTaskArrow()
    {
        HideAllArrows();

        if (!HasValidCurrentStage())
            return;

        Stage stage = stages[currentStageIndex];
        int taskCount = GetValidTaskCount(stage);

        if (taskCount == 0)
        {
            Log($"Stage '{stage.stageName}' has no task arrows.");
            return;
        }

        currentTaskIndex = Mathf.Clamp(currentTaskIndex, 0, taskCount - 1);

        GameObject arrow = stage.taskArrows[currentTaskIndex];
        if (arrow != null)
        {
            arrow.SetActive(true);
            Log($"Showing arrow '{arrow.name}' for task {currentTaskIndex} in stage '{stage.stageName}'");
        }
        else
        {
            Log($"Task {currentTaskIndex} in stage '{stage.stageName}' has no arrow assigned.");
        }
    }

    private void HideAllStages()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageRoot != null)
                stages[i].stageRoot.SetActive(false);
        }
    }

    private void HideAllArrows()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            Stage stage = stages[i];
            for (int j = 0; j < stage.taskArrows.Count; j++)
            {
                if (stage.taskArrows[j] != null)
                    stage.taskArrows[j].SetActive(false);
            }
        }
    }

    private bool HasValidCurrentStage()
    {
        return currentStageIndex >= 0 && currentStageIndex < stages.Count;
    }

    private int GetValidTaskCount(Stage stage)
    {
        return stage == null ? 0 : stage.taskArrows.Count;
    }

    private void ClampCurrentIndexes()
    {
        if (!HasValidCurrentStage())
            return;

        Stage stage = stages[currentStageIndex];
        int taskCount = GetValidTaskCount(stage);

        if (taskCount <= 0)
        {
            currentTaskIndex = 0;
            return;
        }

        currentTaskIndex = Mathf.Clamp(currentTaskIndex, 0, taskCount - 1);
    }

    private void Log(string message)
    {
        if (!debugLogs)
            return;

        Debug.Log($"[StageSequenceManager] {message}", this);
    }
}