using System;
using UnityEngine;

public class VRRigReferences : MonoBehaviour
{
    public static VRRigReferences Instance;
    public Transform head, leftHand, rightHand;
    private void Awake()
    {
        Instance = this; 
    }
}
