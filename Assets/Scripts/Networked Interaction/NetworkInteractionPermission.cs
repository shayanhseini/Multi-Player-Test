using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Legacy compatibility component. Role-based interaction restrictions were
// removed, so existing scene references no longer filter XR interactions.
public class NetworkInteractionPermission : MonoBehaviour, IXRSelectFilter
{
    public bool canProcess => isActiveAndEnabled;

    public bool Process(
        IXRSelectInteractor interactor,
        IXRSelectInteractable interactable)
    {
        return true;
    }
}
