using UnityEngine;
using System.Collections.Generic;

public class RidePlatformParent : MonoBehaviour
{
    [Header("The transform that moves")]
    [SerializeField] private Transform movingPlatform;

    [Header("XR rig root")]
    [SerializeField] private Transform xrOriginRoot;

    [Header("CharacterController on the XR rig")]
    [SerializeField] private CharacterController characterController;

    [Header("Only colliders on these layers can ride")]
    [SerializeField] private LayerMask riderLayers;

    [Header("How close above the platform top counts as standing on it")]
    [SerializeField] private float maxRideHeight = 0.35f;

    [Header("How far outside the trigger bounds is allowed")]
    [SerializeField] private float boundsPadding = 0.05f;

    [Header("Optional")]
    [SerializeField] private bool autoFindCharacterController = true;
    [SerializeField] private bool useFixedUpdate = false;

    private readonly HashSet<Collider> validColliders = new();
    private Collider triggerCol;

    private bool riding;
    private Vector3 lastPlatformPos;

    private void Awake()
    {
        triggerCol = GetComponent<Collider>();

        if (triggerCol == null || !triggerCol.isTrigger)
            Debug.LogError("RidePlatformParent needs a Trigger collider on the platform top.");

        if (autoFindCharacterController && characterController == null && xrOriginRoot != null)
            characterController = xrOriginRoot.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (movingPlatform != null)
            lastPlatformPos = movingPlatform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAllowedLayer(other)) return;
        if (!IsActuallyOnPlatform(other)) return;

        validColliders.Add(other);

        if (!riding)
        {
            riding = true;
            lastPlatformPos = movingPlatform.position;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsAllowedLayer(other)) return;

        if (IsActuallyOnPlatform(other))
        {
            validColliders.Add(other);

            if (!riding)
            {
                riding = true;
                lastPlatformPos = movingPlatform.position;
            }
        }
        else
        {
            validColliders.Remove(other);
            RefreshRidingState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowedLayer(other)) return;

        validColliders.Remove(other);
        RefreshRidingState();
    }

    private void Update()
    {
        if (!useFixedUpdate)
            ApplyPlatformMotion();
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
            ApplyPlatformMotion();
    }

    private void ApplyPlatformMotion()
    {
        if (!riding || movingPlatform == null || xrOriginRoot == null)
            return;

        Vector3 platformDelta = movingPlatform.position - lastPlatformPos;

        if (platformDelta.sqrMagnitude <= Mathf.Epsilon)
            return;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(platformDelta);
        }
        else
        {
            xrOriginRoot.position += platformDelta;
        }

        lastPlatformPos = movingPlatform.position;
    }

    private bool IsAllowedLayer(Collider other)
    {
        return (riderLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private bool IsActuallyOnPlatform(Collider other)
    {
        if (triggerCol == null) return false;

        Bounds b = triggerCol.bounds;
        Bounds ob = other.bounds;
        Vector3 p = ob.center;

        bool insideXZ =
            p.x >= b.min.x - boundsPadding &&
            p.x <= b.max.x + boundsPadding &&
            p.z >= b.min.z - boundsPadding &&
            p.z <= b.max.z + boundsPadding;

        if (!insideXZ) return false;

        float platformTop = b.max.y;
        float colliderBottom = ob.min.y;

        bool aboveTop = colliderBottom >= platformTop - 0.02f;
        bool closeEnough = colliderBottom <= platformTop + maxRideHeight;

        return aboveTop && closeEnough;
    }

    private void RefreshRidingState()
    {
        if (validColliders.Count > 0) return;

        riding = false;

        if (movingPlatform != null)
            lastPlatformPos = movingPlatform.position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (autoFindCharacterController && characterController == null && xrOriginRoot != null)
            characterController = xrOriginRoot.GetComponent<CharacterController>();
    }
#endif
}