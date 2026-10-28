using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SceneGunPickup : MonoBehaviour
{
    [SerializeField] private Transform gunRoot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject bulletTemplate;

    private XRGrabInteractable grabInteractable;
    private Rigidbody gunBody;
    private NetworkShooter localShooter;
    private bool networkReady;

    public static SceneGunPickup Instance { get; private set; }
    public bool IsHeld => grabInteractable != null && grabInteractable.isSelected;
    public Transform GunTransform => gunRoot;

    private void Awake()
    {
        Instance = this;
        if (bulletTemplate != null)
            bulletTemplate.SetActive(false);
        if (gunRoot == null)
            return;

        gunBody = gunRoot.GetComponent<Rigidbody>();
        if (gunBody == null)
            gunBody = gunRoot.gameObject.AddComponent<Rigidbody>();
        gunBody.mass = 0.4f;
        gunBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        gunBody.isKinematic = true;

        grabInteractable = gunRoot.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            grabInteractable = gunRoot.gameObject.AddComponent<XRGrabInteractable>();
        grabInteractable.enabled = false;
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.activated.AddListener(OnActivated);
    }

    private void Update()
    {
        bool connected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        if (connected == networkReady || grabInteractable == null)
            return;
        networkReady = connected;
        grabInteractable.enabled = connected;
        gunBody.isKinematic = !connected;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        localShooter = FindLocalShooter();
        localShooter?.BindPickup(this);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        localShooter?.UnbindPickup(this);
        localShooter = null;
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (localShooter != null && muzzle != null)
            localShooter.TryShoot(muzzle.position, muzzle.right);
    }

    private static NetworkShooter FindLocalShooter()
    {
        foreach (NetworkShooter shooter in FindObjectsByType<NetworkShooter>(FindObjectsSortMode.None))
            if (shooter.IsOwner)
                return shooter;
        return null;
    }

    public GameObject CreateVisualClone()
    {
        if (gunRoot == null)
            return null;
        GameObject clone = Instantiate(gunRoot.gameObject);
        clone.name = "Remote Player Gun";
        foreach (Collider item in clone.GetComponentsInChildren<Collider>()) Destroy(item);
        foreach (Rigidbody item in clone.GetComponentsInChildren<Rigidbody>()) Destroy(item);
        foreach (XRGrabInteractable item in clone.GetComponentsInChildren<XRGrabInteractable>()) Destroy(item);
        return clone;
    }

    public GameObject CreateBulletClone(Vector3 position)
    {
        if (bulletTemplate == null)
            return null;
        GameObject clone = Instantiate(bulletTemplate, position, Quaternion.identity);
        clone.name = "Bullet (Shot)";
        clone.SetActive(true);
        foreach (Collider item in clone.GetComponentsInChildren<Collider>()) Destroy(item);
        foreach (Rigidbody item in clone.GetComponentsInChildren<Rigidbody>()) Destroy(item);
        return clone;
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
            grabInteractable.activated.RemoveListener(OnActivated);
        }
        if (Instance == this) Instance = null;
    }
}
