using System;
using UnityEngine;

[Serializable]
public struct DefenseEventChoice
{
    [SerializeField] private string _label;
    [SerializeField] private string _resultId;

    public string Label => _label;
    public string ResultId => _resultId;
    public bool IsValid => !string.IsNullOrWhiteSpace(_label) && !string.IsNullOrWhiteSpace(_resultId);
}
