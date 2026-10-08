using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class NPCDissatisfaction : MonoBehaviour
{
    private DissatisfactionState _state;
    private ResidentHousingState _residence;

    public void Initialize(NPCType npcType, DissatisfactionState state)
        => Initialize(npcType, state, null);

    public void Initialize(NPCType npcType, DissatisfactionState state, ResidentHousingState residence)
    {
        Unbind();
        _residence = residence;
        if (npcType == NPCType.Farmer || npcType == NPCType.Builder || npcType == NPCType.Guard)
            _state = state;
    }

    public bool TrySetCauseActive(DissatisfactionCause cause, bool active)
        => isActiveAndEnabled && _state != null && _state.TrySetCauseActive(cause, active);

    private void Update()
    {
        _state?.Tick(Time.deltaTime, _residence?.DissatisfactionRecoveryBonus ?? 0f);
    }

    public void Unbind()
    {
        _state?.Reset();
        _state = null;
        _residence = null;
    }

    private void OnDisable() => Unbind();
}
