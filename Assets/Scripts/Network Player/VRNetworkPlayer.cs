using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class VRNetworkPlayer : NetworkBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    public List<Renderer> localRendererToHide;
    
    public NetworkVariable<PlayerRole> role =
        new NetworkVariable<PlayerRole>(
            PlayerRole.Guest,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
    
    public event Action<PlayerRole> OnRoleReady;

    public void SetRole(PlayerRole newRole)
    {
        role.Value = newRole;
        OnRoleReady?.Invoke(newRole);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            role.Value = UIManager.SelectedRole;

            Debug.Log($"My Role: {role.Value}");

            foreach (Renderer rend in localRendererToHide)
            {
                rend.enabled = false;
            }
        }
    }
    
    void Update()
    {
        if (IsOwner)
        {
            head.transform.position = VRRigReferences.Instance.head.position;
            head.transform.rotation = VRRigReferences.Instance.head.rotation;
            
            leftHand.transform.position = VRRigReferences.Instance.leftHand.position;
            leftHand.transform.rotation = VRRigReferences.Instance.leftHand.rotation;
            
            rightHand.transform.position = VRRigReferences.Instance.rightHand.position;
            rightHand.transform.rotation = VRRigReferences.Instance.rightHand.rotation;
        }
    }
    
}
