using UnityEngine;

[CreateAssetMenu(fileName = "WanderActionCost", menuName = "Scriptable Objects/ActionCost/WanderActionCost")]
public sealed class WanderActionCost : DefaultActionCost
{
    [SerializeField, Min(0.1f)] private float _moveSeconds = 5f;
    [SerializeField, Min(0.1f)] private float _restSeconds = 1f;
    [SerializeField, Min(0f)] private float _hungerPerSecond = 0.3f;
    [SerializeField, Min(0f)] private float _thirstPerSecond = 0.3f;
    [SerializeField, Min(0f)] private float _fatiguePerSecond = 0.3f;

    public override ActionType MyType => ActionType.Wander;
    public float MoveSeconds => Mathf.Max(0.1f, _moveSeconds);
    public float RestSeconds => Mathf.Max(0.1f, _restSeconds);

    public void ApplyElapsedNeeds(NPCStat stat, float seconds)
    {
        stat.ChangeHunger(Mathf.Max(0f, _hungerPerSecond) * seconds);
        stat.ChangeThirst(Mathf.Max(0f, _thirstPerSecond) * seconds);
        stat.ChangeFatigue(Mathf.Max(0f, _fatiguePerSecond) * seconds);
    }

    private void OnValidate()
    {
        _moveSeconds = Mathf.Max(0.1f, _moveSeconds);
        _restSeconds = Mathf.Max(0.1f, _restSeconds);
        _hungerPerSecond = Mathf.Max(0f, _hungerPerSecond);
        _thirstPerSecond = Mathf.Max(0f, _thirstPerSecond);
        _fatiguePerSecond = Mathf.Max(0f, _fatiguePerSecond);
    }
}
