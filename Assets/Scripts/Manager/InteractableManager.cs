using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [SerializeField] private DataManager _dataManager;
    [SerializeField] private BaseInteractable[] _interactables;

    void Start()
    {
        var itemInfos = _dataManager.GetItemInfos();

        foreach (var interactable in _interactables)
        {
            if (!interactable)
                continue;

            interactable.Init(itemInfos);
        }
    }
}
