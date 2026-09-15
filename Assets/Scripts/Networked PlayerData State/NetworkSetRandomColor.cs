using UnityEngine;
using Unity.Netcode;

public class NetworkSetRandomColor : NetworkBehaviour
{
    public Renderer[] renderers;
    public NetworkVariable<Color> randomColor =  new NetworkVariable<Color>(Color.white, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetRandom();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        SetRandomColor.OnLocalColorChanged.RemoveListener(SetNewColor);
        randomColor.OnValueChanged -= OnColorChanged;
    }

    public void SetRandom()
    {
        if (IsOwner)
        {
            randomColor.Value = SetRandomColor.randomColor;
            SetRandomColor.OnLocalColorChanged.AddListener(SetNewColor);
        }
        ApplyColor(randomColor.Value);
        
        randomColor.OnValueChanged += OnColorChanged;
    }

    public void SetNewColor(Color color)
    {
        randomColor.Value = color;
    }
    
    public void ApplyColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            r.material.color = color;
        }
    }
    
    public void OnColorChanged(Color previousColor, Color newColor)
    {
        ApplyColor(newColor);
    }
}
