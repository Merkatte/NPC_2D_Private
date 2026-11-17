using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Sequence/Definition")]
public sealed class SequenceDefinition : ScriptableObject
{
    [SerializeField] private string _entryStepId;
    [SerializeField] private SequenceStep[] _steps;
    public string EntryStepId => _entryStepId;
    public IReadOnlyList<SequenceStep> Steps => _steps;

    public bool TryBuildIndex(Dictionary<string, SequenceStep> index, out string error)
    {
        index.Clear();
        error = null;
        if (_steps == null || _steps.Length == 0) error = "Sequence has no steps.";
        else foreach (SequenceStep step in _steps)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.Id) || index.ContainsKey(step.Id))
            { error = "Sequence has an empty or duplicate step ID."; break; }
            if (!step.IsValid) { error = "Invalid step: " + step.Id; break; }
            index.Add(step.Id, step);
        }
        if (error != null) return false;
        if (string.IsNullOrEmpty(_entryStepId) || !index.ContainsKey(_entryStepId))
        { error = "Sequence entry step is missing."; return false; }
        foreach (SequenceStep step in _steps)
        {
            if (!string.IsNullOrEmpty(step.NextId) && !index.ContainsKey(step.NextId))
            { error = "Missing next step at " + step.Id; return false; }
            if (step.Kind != SequenceStepKind.Choice) continue;
            var choiceIds = new HashSet<string>();
            foreach (SequenceChoice choice in step.Choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.Id) || !choiceIds.Add(choice.Id) ||
                    !Enum.IsDefined(typeof(LocalizeKey), choice.TextKey) ||
                    (!string.IsNullOrEmpty(choice.NextId) && !index.ContainsKey(choice.NextId)))
                { error = "Invalid choice at " + step.Id; return false; }
            }
        }
        return true;
    }
}
