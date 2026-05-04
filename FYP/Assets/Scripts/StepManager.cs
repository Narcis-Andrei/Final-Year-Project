using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.VRTemplate
{
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        private class Step
        {
            [Header("Stage Info")]
            public string stageName = "Stage";
            public int stageNumber = 0;
            [Min(1)] public int totalTasksRequired = 1;

            [Header("Progression")]
            public bool requireTasksToAdvance = true;

            [Header("UI")]
            public GameObject stepObject;

            [TextArea(2, 6)]
            public string description;

            [Header("Optional Teleport")]
            public bool teleportOnEnter = false;
            public Transform teleportTarget;

            [HideInInspector] public int completedTasks = 0;
        }

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI stepDescriptionTextField;
        [SerializeField] private TextMeshProUGUI stageTitleTextField;
        [SerializeField] private TextMeshProUGUI taskProgressTextField;
        [SerializeField] private TextMeshProUGUI stageStatusTextField;

        [SerializeField] private List<Step> stepList = new List<Step>();

        [Header("XR Rig Root")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private bool disableCharacterControllerDuringTeleport = true;

        [Header("Head/Camera")]
        [SerializeField] private Transform head;

        [Header("Teleport Settings")]
        [Tooltip("If true, the player head will land exactly on the target position.")]
        [SerializeField] private bool teleportHeadExactlyToTarget = true;

        [Tooltip("If true, apply the target Y rotation when teleporting.")]
        [SerializeField] private bool matchTargetYaw = true;

        [Header("Menu Snap")]
        [SerializeField] private Transform menuRoot;
        [SerializeField] private float followDistance = 1.2f;
        [SerializeField] private float followHeightOffset = -0.2f;
        [SerializeField] private float minWorldHeight = 1.0f;
        [SerializeField] private bool moveMenuAfterTeleport = true;

        [Header("Flow")]
        [SerializeField] private bool lockStageUntilTasksComplete = true;
        [SerializeField] private bool autoAdvanceWhenStageCompleted = false;
        [SerializeField] private bool clampAtLastStage = true;

        private int currentStepIndex = 0;

        private void Start()
        {
            if (stepList.Count == 0)
            {
                Debug.LogWarning("StepManager: Step list is empty.");
                return;
            }

            for (int i = 0; i < stepList.Count; i++)
            {
                if (stepList[i].stepObject != null)
                    stepList[i].stepObject.SetActive(i == currentStepIndex);

                stepList[i].completedTasks = Mathf.Clamp(
                    stepList[i].completedTasks,
                    0,
                    Mathf.Max(1, stepList[i].totalTasksRequired)
                );
            }

            EnterCurrentStep();

            if (moveMenuAfterTeleport)
                SnapMenuInFrontOfHead();
        }

        public void Next()
        {
            if (stepList.Count == 0)
                return;

            Step step = GetCurrentStep();

            if (lockStageUntilTasksComplete && step.requireTasksToAdvance && !IsCurrentStepComplete())
            {
                Debug.Log($"[StepManager] Cannot go next. Stage {step.stageNumber} is not complete yet.");
                UpdateUI();
                return;
            }

            if (currentStepIndex >= stepList.Count - 1)
            {
                if (clampAtLastStage)
                {
                    Debug.Log("[StepManager] Already at final stage.");
                    UpdateUI();
                    return;
                }

                ExitCurrentStep();
                currentStepIndex = 0;
                EnterCurrentStep();
                return;
            }

            ExitCurrentStep();
            currentStepIndex++;
            EnterCurrentStep();
        }

        public void Back()
        {
            if (stepList.Count == 0)
                return;

            if (currentStepIndex <= 0)
            {
                Debug.Log("[StepManager] Already at first stage.");
                UpdateUI();
                return;
            }

            ExitCurrentStep();
            currentStepIndex--;
            EnterCurrentStep();
        }

        public void GoToStep(int index)
        {
            if (stepList.Count == 0)
                return;

            if (index < 0 || index >= stepList.Count)
            {
                Debug.LogWarning($"StepManager: GoToStep index out of range: {index}");
                return;
            }

            if (index > currentStepIndex)
            {
                for (int i = currentStepIndex; i < index; i++)
                {
                    Step step = stepList[i];

                    if (lockStageUntilTasksComplete && step.requireTasksToAdvance && !IsStepComplete(i))
                    {
                        Debug.Log($"[StepManager] Cannot jump to Stage {index}. Stage {step.stageNumber} is incomplete.");
                        UpdateUI();
                        return;
                    }
                }
            }

            if (index == currentStepIndex)
            {
                UpdateUI();
                return;
            }

            ExitCurrentStep();
            currentStepIndex = index;
            EnterCurrentStep();
        }

        public void CompleteTask()
        {
            if (stepList.Count == 0)
                return;

            Step step = stepList[currentStepIndex];

            if (step.completedTasks >= step.totalTasksRequired)
            {
                Debug.Log($"[StepManager] Stage {step.stageNumber} already complete.");
                UpdateUI();
                return;
            }

            step.completedTasks++;
            Debug.Log($"[StepManager] Stage {step.stageNumber}: task completed ({step.completedTasks}/{step.totalTasksRequired})");

            UpdateUI();

            if (IsCurrentStepComplete())
            {
                Debug.Log($"[StepManager] Stage {step.stageNumber} complete.");

                if (autoAdvanceWhenStageCompleted)
                    Next();
            }
        }

        public void CompleteTaskByAmount(int amount)
        {
            if (amount <= 0)
                return;

            for (int i = 0; i < amount; i++)
                CompleteTask();
        }

        public void ResetCurrentStageTasks()
        {
            if (stepList.Count == 0)
                return;

            stepList[currentStepIndex].completedTasks = 0;
            Debug.Log($"[StepManager] Stage {GetCurrentStep().stageNumber} tasks reset.");
            UpdateUI();
        }

        public void ResetAllStageTasks()
        {
            if (stepList.Count == 0)
                return;

            for (int i = 0; i < stepList.Count; i++)
                stepList[i].completedTasks = 0;

            Debug.Log("[StepManager] All stage tasks reset.");
            UpdateUI();
        }

        public void SetCurrentStageTaskCount(int completed)
        {
            if (stepList.Count == 0)
                return;

            Step step = GetCurrentStep();
            step.completedTasks = Mathf.Clamp(completed, 0, step.totalTasksRequired);
            UpdateUI();
        }

        public bool IsCurrentStepComplete()
        {
            return IsStepComplete(currentStepIndex);
        }

        public bool IsStepComplete(int index)
        {
            if (index < 0 || index >= stepList.Count)
                return false;

            Step step = stepList[index];
            return step.completedTasks >= step.totalTasksRequired;
        }

        public int GetCurrentStageIndex()
        {
            return currentStepIndex;
        }

        public int GetCurrentStageNumber()
        {
            if (stepList.Count == 0)
                return -1;

            return stepList[currentStepIndex].stageNumber;
        }

        private Step GetCurrentStep()
        {
            return stepList[currentStepIndex];
        }

        private void ExitCurrentStep()
        {
            Step step = stepList[currentStepIndex];
            if (step.stepObject != null)
                step.stepObject.SetActive(false);
        }

        private void EnterCurrentStep()
        {
            Step step = stepList[currentStepIndex];

            if (step.stepObject != null)
                step.stepObject.SetActive(true);

            UpdateUI();
            HandleTeleport(step);
        }

        private void UpdateUI()
        {
            if (stepList.Count == 0)
                return;

            Step step = stepList[currentStepIndex];

            if (stepDescriptionTextField != null)
                stepDescriptionTextField.text = step.description;

            if (stageTitleTextField != null)
                stageTitleTextField.text = $"{step.stageName} {step.stageNumber}";

            if (taskProgressTextField != null)
            {
                taskProgressTextField.text = step.requireTasksToAdvance
                    ? $"Tasks: {step.completedTasks} / {step.totalTasksRequired}"
                    : "Free navigation";
            }

            if (stageStatusTextField != null)
            {
                if (!step.requireTasksToAdvance)
                {
                    stageStatusTextField.text = "You can press Go Next at any time.";
                }
                else
                {
                    stageStatusTextField.text = IsCurrentStepComplete()
                        ? "Stage complete. You can go to the next stage."
                        : "Complete all required tasks to unlock Go Next.";
                }
            }
        }

        private void HandleTeleport(Step step)
        {
            if (!step.teleportOnEnter)
                return;

            if (step.teleportTarget == null)
                return;

            TeleportTo(step.teleportTarget);
        }

        private void TeleportTo(Transform target)
        {
            if (xrOrigin == null)
            {
                Debug.LogError("StepManager: XR Origin not assigned.");
                return;
            }

            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (disableCharacterControllerDuringTeleport && cc != null)
                cc.enabled = false;

            if (matchTargetYaw)
            {
                float targetYaw = target.eulerAngles.y;
                float currentHeadYaw = 0f;

                if (head != null)
                    currentHeadYaw = head.eulerAngles.y;

                float yawDelta = targetYaw - currentHeadYaw;
                xrOrigin.Rotate(0f, yawDelta, 0f, Space.World);
            }

            if (teleportHeadExactlyToTarget && head != null)
            {
                Vector3 offset = xrOrigin.position - head.position;
                xrOrigin.position = target.position + offset;
            }
            else
            {
                xrOrigin.position = target.position;
            }

            if (disableCharacterControllerDuringTeleport && cc != null)
                cc.enabled = true;

            Debug.Log($"[StepManager] Teleported to: {target.name}");

            if (moveMenuAfterTeleport)
                SnapMenuInFrontOfHead();
        }

        private void SnapMenuInFrontOfHead()
        {
            if (menuRoot == null || head == null)
                return;

            Vector3 forward = head.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Vector3 targetPos = head.position + forward * followDistance;

            float desiredY = head.position.y + followHeightOffset;
            targetPos.y = Mathf.Max(desiredY, minWorldHeight);

            menuRoot.position = targetPos;
            menuRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        public void BringMenuHere()
        {
            SnapMenuInFrontOfHead();
        }

        public void RestartScene()
        {
            Debug.Log("[StepManager] Restarting entire simulation.");
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void EndSimulation()
        {
            Debug.Log("[StepManager] Ending simulation.");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}