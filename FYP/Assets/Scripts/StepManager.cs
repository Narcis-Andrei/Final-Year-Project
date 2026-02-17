using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Unity.VRTemplate
{
    public class StepManager : MonoBehaviour
    {
        [Serializable]
        private class Step
        {
            [Header("UI")]
            public GameObject stepObject;
            public string buttonText;

            [TextArea(2, 6)]
            public string description;

            [Header("Optional Teleport")]
            public bool teleportOnEnter;
            public Transform teleportTarget;
        }

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI stepButtonTextField;

        [Tooltip("This should be your 'Modal Text' TMP field (the big text area on the card/menu).")]
        [SerializeField] private TextMeshProUGUI stepDescriptionTextField;

        [SerializeField] private List<Step> stepList = new List<Step>();

        [Header("XR Rig Root (XR Origin, NOT the camera)")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private bool disableCharacterControllerDuringTeleport = true;

        [Header("Menu Snap (teleports with you, still movable after)")]
        [SerializeField] private Transform menuRoot;                 // StageMenuPanel / CoachingCardRoot
        [SerializeField] private Transform head;                     // Main Camera (HMD)
        [SerializeField] private float followDistance = 1.2f;        // distance in front of head
        [SerializeField] private float followHeightOffset = -0.2f;   // relative to head height
        [SerializeField] private float minWorldHeight = 1.0f;        // never place menu below this
        [SerializeField] private bool moveMenuAfterTeleport = true;

        private int currentStepIndex = 0;

        private void Start()
        {
            if (stepList.Count == 0)
            {
                Debug.LogWarning("StepManager: Step list is empty.");
                return;
            }

            // Ensure only current step is active at start
            for (int i = 0; i < stepList.Count; i++)
            {
                if (stepList[i].stepObject != null)
                    stepList[i].stepObject.SetActive(i == currentStepIndex);
            }

            UpdateUI();
            EnterCurrentStep();

            // Optional: bring menu to you on start
            if (moveMenuAfterTeleport)
                SnapMenuInFrontOfHead();
        }

        public void Next()
        {
            if (stepList.Count == 0) return;

            ExitCurrentStep();

            currentStepIndex++;
            if (currentStepIndex >= stepList.Count)
                currentStepIndex = 0;

            EnterCurrentStep();
        }

        public void Back()
        {
            if (stepList.Count == 0) return;

            ExitCurrentStep();

            currentStepIndex--;
            if (currentStepIndex < 0)
                currentStepIndex = stepList.Count - 1;

            EnterCurrentStep();
        }

        /// <summary>
        /// Optional: call this from your stage cards (Stage 1/2/3/4) so each card can jump directly.
        /// </summary>
        public void GoToStep(int index)
        {
            if (stepList.Count == 0) return;
            if (index < 0 || index >= stepList.Count)
            {
                Debug.LogWarning($"StepManager: GoToStep index out of range: {index}");
                return;
            }

            if (index == currentStepIndex)
            {
                // Still refresh UI (useful if text fields were assigned late)
                UpdateUI();
                return;
            }

            ExitCurrentStep();
            currentStepIndex = index;
            EnterCurrentStep();
        }

        private void ExitCurrentStep()
        {
            var step = stepList[currentStepIndex];
            if (step.stepObject != null)
                step.stepObject.SetActive(false);
        }

        private void EnterCurrentStep()
        {
            var step = stepList[currentStepIndex];

            if (step.stepObject != null)
                step.stepObject.SetActive(true);

            UpdateUI();
            HandleTeleport(step);
        }

        private void UpdateUI()
        {
            if (stepList.Count == 0) return;

            var step = stepList[currentStepIndex];

            if (stepButtonTextField != null)
                stepButtonTextField.text = step.buttonText;

            if (stepDescriptionTextField != null)
                stepDescriptionTextField.text = step.description;
        }

        private void HandleTeleport(Step step)
        {
            if (!step.teleportOnEnter) return;
            if (step.teleportTarget == null) return;

            TeleportTo(step.teleportTarget);
        }

        private void TeleportTo(Transform target)
        {
            if (xrOrigin == null)
            {
                Debug.LogError("StepManager: XR Origin not assigned.");
                return;
            }

            var cc = xrOrigin.GetComponent<CharacterController>();
            if (disableCharacterControllerDuringTeleport && cc != null)
                cc.enabled = false;

            xrOrigin.position = target.position;
            xrOrigin.rotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

            if (disableCharacterControllerDuringTeleport && cc != null)
                cc.enabled = true;

            Debug.Log($"[StepManager] Teleported to: {target.name}");

            // Snap menu once after teleport (so it comes with you)
            if (moveMenuAfterTeleport)
                SnapMenuInFrontOfHead();
        }

        private void SnapMenuInFrontOfHead()
        {
            if (menuRoot == null || head == null) return;

            // Flatten forward so menu doesn't tilt up/down
            Vector3 forward = head.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Vector3 targetPos = head.position + forward * followDistance;

            // Keep menu at a usable height and prevent floor placement
            float desiredY = head.position.y + followHeightOffset;
            targetPos.y = Mathf.Max(desiredY, minWorldHeight);

            menuRoot.position = targetPos;

            // Face user (yaw only)
            menuRoot.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // Optional: Hook this to a "Bring Menu Here" button if you ever lose the panel
        public void BringMenuHere()
        {
            SnapMenuInFrontOfHead();
        }
    }
}
