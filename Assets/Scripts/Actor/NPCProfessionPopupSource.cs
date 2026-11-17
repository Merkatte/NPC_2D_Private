using UnityEngine;

[DisallowMultipleComponent]
public sealed class NPCProfessionPopupSource : MonoBehaviour, IClickPopupSource
{
    [SerializeField] private WorkerNPC _worker;

    private void Awake()
    {
        if (!_worker)
        {
            Debug.LogError($"NPCProfessionPopupSource '{name}': missing _worker.", this);
            enabled = false;
        }
    }

    public bool TryGetClickPopup(out PopupType popupType)
    {
        popupType = TryGetProgress(out _, out _, out _) ? PopupType.NPCProfession : PopupType.None;
        return popupType != PopupType.None;
    }

    public bool TryGetProgress(out NPCType profession, out ProfessionProgress progress, out uint lifetimeVersion)
    {
        profession = default;
        progress = default;
        lifetimeVersion = 0;
        if (!isActiveAndEnabled || !_worker || !_worker.isActiveAndEnabled
            || _worker.ProfessionGrowth == null
            || !_worker.ProfessionGrowth.TryGetProgress(_worker.Profession, out progress))
            return false;

        profession = _worker.Profession;
        lifetimeVersion = _worker.LifetimeVersion;
        return true;
    }
}
