using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class NPCDissatisfaction : MonoBehaviour
{
    private DissatisfactionState _state;
    private ResidentHousingState _residence;
    private bool _isSuspended;

    public void SetSuspended(bool suspended) { _isSuspended = suspended; }

    public void Initialize(NPCType npcType, DissatisfactionState state)
        => Initialize(npcType, state, null);

    public void Initialize(NPCType npcType, DissatisfactionState state, ResidentHousingState residence)
    {
        Unbind();
        _residence = residence;
        if (npcType == NPCType.Farmer || npcType == NPCType.Builder || npcType == NPCType.Guard || npcType == NPCType.Archer)
            _state = state;
    }

    public bool TrySetCauseActive(DissatisfactionCause cause, bool active)
        => isActiveAndEnabled && _state != null && _state.TrySetCauseActive(cause, active);

    private void Update()
    {
        if (!_isSuspended)
            _state?.Tick(Time.deltaTime, _residence?.DissatisfactionRecoveryBonus ?? 0f);
    }

    public void Unbind()
    {
        _state?.Reset();
        _state = null;
        _residence = null;
        _isSuspended = false;
    }

    private void OnDisable() => Unbind();
}
