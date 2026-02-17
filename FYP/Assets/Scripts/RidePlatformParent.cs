using UnityEngine;

public class RidePlatformParent : MonoBehaviour
{
    [Header("The transform that moves (LiftPlatform or Platform)")]
    [SerializeField] private Transform movingPlatform;

    [Header("Your XR rig root (XR Origin Hands (XR Rig))")]
    [SerializeField] private Transform xrOriginRoot;

    [Header("Only colliders on these layers can activate riding (NOT hands)")]
    [SerializeField] private LayerMask riderLayers;

    private int contacts = 0;
    private bool riding = false;

    private Vector3 lastPlatformPos;

    private void OnTriggerEnter(Collider other)
    {
        if ((riderLayers.value & (1 << other.gameObject.layer)) == 0) return;

        contacts++;
        if (contacts == 1)
        {
            riding = true;
            lastPlatformPos = movingPlatform.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((riderLayers.value & (1 << other.gameObject.layer)) == 0) return;

        contacts--;
        if (contacts <= 0)
        {
            contacts = 0;
            riding = false;
        }
    }

    private void LateUpdate()
    {
        if (!riding || movingPlatform == null || xrOriginRoot == null) return;

        Vector3 platformDelta = movingPlatform.position - lastPlatformPos;
        xrOriginRoot.position += platformDelta;

        lastPlatformPos = movingPlatform.position;
    }
}
