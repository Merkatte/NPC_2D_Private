public interface IAction
{
    void Init(ActionContext context);
    void Start();
    void Tick();
    void Pause();
    void Resume();
    void Stop();
    void Clear();
    bool CheckComplete();
    ActionType GetMyActionType();
}