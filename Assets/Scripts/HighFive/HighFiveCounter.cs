using System;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class HighFiveCounter : NetworkBehaviour
{
    private NetworkVariable<int> highFiveCount =  new NetworkVariable<int>();
    public TextMeshProUGUI tmp;
    
    public static HighFiveCounter Instance {get; private set;}

    private void Awake()
    {
        Instance = this;
    }

    public void IncreaseCount()
    {
        highFiveCount.Value += 1;
    }

    public override void OnNetworkSpawn()
    {
        UpdateText(highFiveCount.Value);
        highFiveCount.OnValueChanged += OnCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        highFiveCount.OnValueChanged -= OnCountChanged;
    }

    private void OnCountChanged(int preValue, int newValue)
    {
        UpdateText(newValue);
    }

    private void UpdateText(int count)
    {
        tmp.text = count.ToString();
    }
}
