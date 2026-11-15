using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class ReusableGrenadePickup : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Transform holsterPoint;
    [SerializeField, Min(0.1f)] private float returnDelay = 1f;
    [SerializeField] private Vector3 throwDirectionAxis = Vector3.forward;

    private NetworkShooter shooter;
    private Rigidbody body;
    private bool returning;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        shooter = GetComponentInParent<NetworkShooter>();
        grabInteractable?.activated.AddListener(OnActivated);
    }

    private void OnDestroy()
    {
        grabInteractable?.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (returning || shooter == null)
            return;

        Vector3 direction = transform.TransformDirection(throwDirectionAxis).normalized;
        if (!shooter.TryThrowGrenade(transform.position, direction))
            return;

        if (grabInteractable != null && grabInteractable.interactionManager != null)
            grabInteractable.interactionManager.CancelInteractableSelection(
                (IXRSelectInteractable)grabInteractable);
        StartCoroutine(ReturnToHolster());
    }

    private IEnumerator ReturnToHolster()
    {
        returning = true;
        if (grabInteractable != null)
            grabInteractable.enabled = false;
        foreach (Renderer item in GetComponentsInChildren<Renderer>())
            item.enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        yield return new WaitForSeconds(returnDelay);

        if (holsterPoint != null)
            transform.SetPositionAndRotation(holsterPoint.position, holsterPoint.rotation);
        foreach (Renderer item in GetComponentsInChildren<Renderer>())
            item.enabled = true;
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
        }
        if (grabInteractable != null)
            grabInteractable.enabled = true;
        returning = false;
    }
}
