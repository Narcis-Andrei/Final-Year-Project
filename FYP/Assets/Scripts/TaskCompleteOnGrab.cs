using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TaskCompleteOnGrab : MonoBehaviour
{
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string taskId = "GrabRemote";
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private bool completeOnlyOnce = true;

    private bool done;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (done && completeOnlyOnce) return;
        if (taskManager == null) return;
        if (!taskManager.IsCurrentTask(taskId)) return;

        done = true;
        taskManager.CompleteTask(taskId);
    }
}