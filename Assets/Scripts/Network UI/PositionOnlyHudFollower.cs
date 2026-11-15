using UnityEngine;

public sealed class PositionOnlyHudFollower : MonoBehaviour
{
    [SerializeField] private Transform trackedHead;
    [SerializeField] private Vector3 viewOffset = new(0f, -0.32f, 1.55f);

    private Transform trackedOrigin;
    private bool initialized;

    private void Start() => Initialize();

    private void LateUpdate()
    {
        if (!initialized) Initialize();
        if (trackedHead == null || trackedOrigin == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(trackedHead.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(trackedOrigin.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        Quaternion yawRotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 horizontalOffset = yawRotation * new Vector3(viewOffset.x, 0f, viewOffset.z);
        Vector3 position = trackedHead.position + horizontalOffset;
        position.y = trackedHead.position.y + viewOffset.y;
        transform.SetPositionAndRotation(position, yawRotation);
    }

    private void Initialize()
    {
        if (trackedHead == null && VRRigReferences.Instance != null)
            trackedHead = VRRigReferences.Instance.head;
        if (trackedHead == null) return;

        trackedOrigin = VRRigReferences.Instance != null
            ? VRRigReferences.Instance.transform
            : trackedHead.root;

        // The HUD is authored below the headset for convenient scene editing, but
        // at runtime it belongs to the XR origin. Its position follows the head,
        // while its rotation remains yaw-only and ignores head pitch and roll.
        if (transform.parent != trackedOrigin)
            transform.SetParent(trackedOrigin, true);
        initialized = true;
    }
}
