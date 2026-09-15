using UnityEngine;
using UnityEngine.Events;

public class SetRandomColor : MonoBehaviour
{
    public Renderer[] renderers;
    public static Color randomColor;
    
    public static UnityEvent<Color> OnLocalColorChanged =  new UnityEvent<Color>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetRandom();
    }

    public void SetRandom()
    {
        randomColor = Random.ColorHSV();
        ApplyColor(randomColor);
        
        OnLocalColorChanged?.Invoke(randomColor);
    }
    
    public void ApplyColor(Color color)
    {
        foreach (Renderer r in renderers)
        {
            r.material.color = color;
        }
    }
}
