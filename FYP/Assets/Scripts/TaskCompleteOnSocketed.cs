using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TaskCompleteOnSocketed : MonoBehaviour
{
    [SerializeField] private StageTaskManager taskManager;
    [SerializeField] private string taskId = "ReturnRemote";
    [SerializeField] private XRSocketInteractor socket;
    [SerializeField] private Transform expectedObject;

    private void Awake()
    {
        if (socket == null)
            socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        if (socket != null)
            socket.selectEntered.AddListener(OnSocketed);
    }

    private void OnDisable()
    {
        if (socket != null)
            socket.selectEntered.RemoveListener(OnSocketed);
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        if (taskManager == null) return;
        if (!taskManager.IsCurrentTask(taskId)) return;

        if (expectedObject != null && args.interactableObject.transform != expectedObject)
            return;

        taskManager.CompleteTask(taskId);
    }
}