using UnityEngine;
using Unity.Netcode;

public class HighFiveDetector : NetworkBehaviour
{
    public GameObject highFiveParticlePrefab;

    public float cooldown = 1;
    private float lastHighFiveTime;
    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;
        if (!other.TryGetComponent(out HighFiveDetector otherHand))
            return;
        if (otherHand.OwnerClientId == OwnerClientId)
            return;
        if (OwnerClientId > otherHand.OwnerClientId)
            return; 
        if (Time.time - lastHighFiveTime < cooldown)
            return;
        Vector3 contactPos = (transform.position + other.transform.position) / 2;
        lastHighFiveTime = Time.time;
        
        PlayHighFiveEffectRpc(contactPos);

        if (HighFiveCounter.Instance != null)
            HighFiveCounter.Instance.RequestIncrement();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayHighFiveEffectRpc(Vector3 position)
    {
        GameObject effect = Instantiate(highFiveParticlePrefab, position, Quaternion.identity);
        Destroy(effect, 2f);
    }
}
