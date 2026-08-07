using UnityEngine;

public class FarmingAction : DefaultAction
{
    private float _workingTime = 10f;
    private float _currentWorkingTime = 0f;
    
    public FarmingAction() : base(ActionType.Farming)
    {
    }

    public override void Tick()
    {
        throw new System.NotImplementedException();
    }

    protected override void UpdateCompletion()
    {
        throw new System.NotImplementedException();
    }
}
