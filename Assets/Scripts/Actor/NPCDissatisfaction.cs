using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class NPCDissatisfaction : MonoBehaviour
{
    private DissatisfactionState _state;

    public void Initialize(NPCType npcType, DissatisfactionState state)
    {
        Unbind();
        if (npcType == NPCType.Farmer || npcType == NPCType.Builder || npcType == NPCType.Guard)
            _state = state;
    }

    public bool TrySetCauseActive(DissatisfactionCause cause, bool active)
        => isActiveAndEnabled && _state != null && _state.TrySetCauseActive(cause, active);

    private void Update()
    {
        _state?.Tick(Time.deltaTime);
    }

    public void Unbind()
    {
        _state?.Reset();
        _state = null;
    }

    private void OnDisable() => Unbind();
}
