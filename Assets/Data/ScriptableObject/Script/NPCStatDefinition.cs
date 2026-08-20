using UnityEngine;

public abstract class NPCStatDefinition : ScriptableObject
{
    public abstract NPCStat CreateRuntimeStat();
}
