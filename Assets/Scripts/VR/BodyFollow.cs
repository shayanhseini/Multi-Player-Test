using UnityEngine;

public class BodyFollow : MonoBehaviour
{
    public Transform head;
    public float yOffset = -0.6f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = head.position + Vector3.up * yOffset;
        
        Vector3 horizontalForward = head.forward;
        horizontalForward.y = 0;

        if (horizontalForward.magnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(horizontalForward.normalized);
        }
    }
} 
