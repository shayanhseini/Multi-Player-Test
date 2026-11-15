using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class VRRigReferences : MonoBehaviour
{
    public static VRRigReferences Instance;
    public Transform head, leftHand, rightHand;

    private LocomotionProvider[] locomotionProviders;
    private bool[] locomotionProviderStates;
    private bool locomotionLocked;
    private bool observerFlight;
    private Coroutine teleportRoutine;
    [SerializeField, Min(0.5f)] private float observerFlightSpeed = 4f;

    private void Awake()
    {
        Instance = this;
        locomotionProviders = GetComponentsInChildren<LocomotionProvider>(true);
        locomotionProviderStates = new bool[locomotionProviders.Length];
    }

    public void SetObserverFlight(bool enabled) => observerFlight = enabled;

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (teleportRoutine != null)
            StopCoroutine(teleportRoutine);

        ApplyTeleport(position, rotation);
        teleportRoutine = StartCoroutine(ConfirmTeleportAfterTrackingUpdate(position, rotation));
    }

    private IEnumerator ConfirmTeleportAfterTrackingUpdate(Vector3 position, Quaternion rotation)
    {
        // Locomotion and XR tracking can still write to the rig later in the frame
        // in which the round RPC arrives. Reapplying after that frame makes every
        // round start deterministic, not only the first one.
        yield return new WaitForEndOfFrame();
        ApplyTeleport(position, rotation);
        teleportRoutine = null;
    }

    private void ApplyTeleport(Vector3 position, Quaternion rotation)
    {
        CharacterController controller = GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;
        Quaternion yawOnly = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        transform.SetPositionAndRotation(position, yawOnly);
        Physics.SyncTransforms();
        if (controllerWasEnabled)
            controller.enabled = true;
    }

    private void Update()
    {
        if (!observerFlight || head == null)
            return;

        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 moveAxis);
        right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 verticalAxis);

        Vector3 forward = head.forward;
        Vector3 rightDirection = head.right;
        Vector3 movement =
            (forward * moveAxis.y) +
            (rightDirection * moveAxis.x) +
            (Vector3.up * verticalAxis.y);
        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null && controller.enabled)
            controller.Move(movement * (observerFlightSpeed * Time.deltaTime));
        else
            transform.position += movement * (observerFlightSpeed * Time.deltaTime);
    }

    public void SetLocomotionEnabled(bool enabled)
    {
        if (!enabled && !locomotionLocked)
        {
            for (int i = 0; i < locomotionProviders.Length; i++)
            {
                locomotionProviderStates[i] = locomotionProviders[i].enabled;
                locomotionProviders[i].enabled = false;
            }

            locomotionLocked = true;
            return;
        }

        if (!enabled || !locomotionLocked)
            return;

        for (int i = 0; i < locomotionProviders.Length; i++)
            locomotionProviders[i].enabled = locomotionProviderStates[i];

        locomotionLocked = false;
    }
}
