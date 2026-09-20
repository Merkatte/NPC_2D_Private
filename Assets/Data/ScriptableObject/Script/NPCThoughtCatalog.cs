using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Thought Catalog")]
public sealed class NPCThoughtCatalog : ScriptableObject
{
    public const float DefaultHoldDuration = 4f;
    public const float DefaultNeedThreshold = 0.6f;

    public enum NeedType { Hunger, Thirst, Fatigue }

    [Serializable]
    public sealed class ActionThought
    {
        [SerializeField] private ActionType _actionType;
        [SerializeField] private LocalizeKey[] _keys = Array.Empty<LocalizeKey>();

        public ActionType ActionType => _actionType;
        public IReadOnlyList<LocalizeKey> Keys => _keys;

        public ActionThought(ActionType actionType, params LocalizeKey[] keys)
        {
            _actionType = actionType;
            _keys = keys != null ? (LocalizeKey[])keys.Clone() : Array.Empty<LocalizeKey>();
        }
    }

    [Serializable]
    public sealed class NeedThought
    {
        [SerializeField] private NeedType _needType;
        [SerializeField, Range(0f, 1f)] private float _threshold = DefaultNeedThreshold;
        [SerializeField] private LocalizeKey[] _keys = Array.Empty<LocalizeKey>();

        public NeedType NeedType => _needType;
        public float Threshold => _threshold;
        public IReadOnlyList<LocalizeKey> Keys => _keys;

        public NeedThought(NeedType needType, float threshold, params LocalizeKey[] keys)
        {
            _needType = needType;
            _threshold = threshold;
            _keys = keys != null ? (LocalizeKey[])keys.Clone() : Array.Empty<LocalizeKey>();
        }

        internal void Validate()
        {
            _threshold = float.IsNaN(_threshold) ? DefaultNeedThreshold : Mathf.Clamp01(_threshold);
        }
    }

    [SerializeField] private ActionThought[] _actionThoughts =
    {
        new ActionThought(ActionType.Farming, LocalizeKey.NPC_Thought_Farming_1, LocalizeKey.NPC_Thought_Farming_2),
        new ActionThought(ActionType.Harvest, LocalizeKey.NPC_Thought_Harvest_1, LocalizeKey.NPC_Thought_Harvest_2),
        new ActionThought(ActionType.Deposit, LocalizeKey.NPC_Thought_Deposit_1, LocalizeKey.NPC_Thought_Deposit_2),
        new ActionThought(ActionType.Guard, LocalizeKey.NPC_Thought_Guard_1, LocalizeKey.NPC_Thought_Guard_2),
        new ActionThought(ActionType.Attack, LocalizeKey.NPC_Thought_Attack_1, LocalizeKey.NPC_Thought_Attack_2),
        new ActionThought(ActionType.Eat, LocalizeKey.NPC_Thought_Eat_1, LocalizeKey.NPC_Thought_Eat_2),
        new ActionThought(ActionType.Drink, LocalizeKey.NPC_Thought_Drink_1, LocalizeKey.NPC_Thought_Drink_2),
        new ActionThought(ActionType.Sleep, LocalizeKey.NPC_Thought_Sleep_1, LocalizeKey.NPC_Thought_Sleep_2),
        new ActionThought(ActionType.Idle, LocalizeKey.NPC_Thought_Idle_1, LocalizeKey.NPC_Thought_Idle_2),
    };
    [SerializeField] private NeedThought[] _needThoughts =
    {
        new NeedThought(NeedType.Hunger, DefaultNeedThreshold, LocalizeKey.NPC_Thought_Hungry, LocalizeKey.NPC_Thought_Hungry_2),
        new NeedThought(NeedType.Thirst, DefaultNeedThreshold, LocalizeKey.NPC_Thought_Thirsty_1, LocalizeKey.NPC_Thought_Thirsty_2),
        new NeedThought(NeedType.Fatigue, DefaultNeedThreshold, LocalizeKey.NPC_Thought_Tired_1, LocalizeKey.NPC_Thought_Tired_2),
    };
    [SerializeField] private LocalizeKey _fallbackKey = LocalizeKey.NPC_Thought_Default;
    [SerializeField, Min(0.01f)] private float _holdDuration = DefaultHoldDuration;

    public IReadOnlyList<ActionThought> ActionThoughts => _actionThoughts;
    public IReadOnlyList<NeedThought> NeedThoughts => _needThoughts;
    public LocalizeKey FallbackKey => _fallbackKey;
    public float HoldDuration => _holdDuration;

    private void OnValidate()
    {
        if (float.IsNaN(_holdDuration) || float.IsInfinity(_holdDuration) || _holdDuration <= 0f)
            _holdDuration = DefaultHoldDuration;
        if (_needThoughts == null)
            return;
        foreach (NeedThought thought in _needThoughts)
            thought?.Validate();
    }
}
