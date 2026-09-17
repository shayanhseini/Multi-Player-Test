using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Vivox;

public class ProximityChatPlayer : NetworkBehaviour
{
   public Transform head;

   private void LateUpdate()
   {
      if (!IsOwner)
         return;
      
      if (!ProximityVoiceManager.instance)
         return;  
      
      if (!ProximityVoiceManager.instance.HasJoinedChannel())
         return;  
      
      if (ProximityVoiceManager.instance.debugEcho)
         return; 

      string channelId = ProximityVoiceManager.instance.connectionManager.GetSessionID();
      
      VivoxService.Instance.Set3DPosition(
         head.position, head.position, head.forward, head.up, channelId);
   }
}
