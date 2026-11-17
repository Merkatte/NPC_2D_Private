using System;
using UnityEngine;

// Completion reports success/failure once; cancellation must suppress late completion callbacks.
public abstract class SequenceAction : MonoBehaviour
{
    public abstract bool TryBegin(SequenceStep step, SequenceBindings bindings, Action<bool> completed);
    public abstract void Cancel();
}
