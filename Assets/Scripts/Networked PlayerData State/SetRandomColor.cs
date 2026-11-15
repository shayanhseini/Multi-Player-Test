using UnityEngine;
using UnityEngine.Events;

public class SetRandomColor : MonoBehaviour
{
    public Renderer[] renderers;
    public static Color randomColor;
    
    public static UnityEvent<Color> OnLocalColorChanged =  new UnityEvent<Color>();
    
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
