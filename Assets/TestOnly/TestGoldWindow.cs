using UnityEngine;

public class TestGoldWindow : MonoBehaviour
{
    private const float WindowWidth = 260f;
    private const float WindowHeight = 190f;
    private const int EarnAmount = 10;
    private const int AffordableSpendAmount = 10;
    private const int UnaffordableSpendAmount = 1000;

    [SerializeField] private GoldManager _goldManager;

    private Rect _windowRect = new Rect(560f, 20f, WindowWidth, WindowHeight);

    private bool _hasLastResult;
    private string _lastResultLabel;

    private void Awake()
    {
        if (!_goldManager)
        {
            _goldManager = FindFirstObjectByType<GoldManager>();
        }
    }

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Gold");
    }

    private void DrawWindow(int windowId)
    {
        if (!_goldManager)
        {
            GUILayout.Label("Missing GoldManager reference.");
            GUI.DragWindow();
            return;
        }

        GUILayout.Label($"Gold: {_goldManager.CurrentGold}");

        if (GUILayout.Button($"Add {EarnAmount} Gold", GUILayout.Height(28f)))
        {
            Earn(EarnAmount);
        }

        if (GUILayout.Button($"Try Spend {AffordableSpendAmount} Gold", GUILayout.Height(28f)))
        {
            Spend(AffordableSpendAmount);
        }

        if (GUILayout.Button($"Try Spend {UnaffordableSpendAmount} Gold", GUILayout.Height(28f)))
        {
            Spend(UnaffordableSpendAmount);
        }

        if (_hasLastResult)
        {
            GUILayout.Label(_lastResultLabel);
        }

        GUI.DragWindow();
    }

    private void Earn(int amount)
    {
        int previousGold = _goldManager.CurrentGold;
        _goldManager.Add(amount);
        RecordResult($"Add({amount}): {previousGold} -> {_goldManager.CurrentGold}");
    }

    private void Spend(int amount)
    {
        int previousGold = _goldManager.CurrentGold;
        bool success = _goldManager.TrySpend(amount);
        RecordResult($"TrySpend({amount})={success}: {previousGold} -> {_goldManager.CurrentGold}");
    }

    // Logged once per button press, never per frame — OnGUI fires on every GUI event, so the
    // on-screen label is the live view and this single Debug.Log per click is the console audit trail.
    private void RecordResult(string label)
    {
        _lastResultLabel = label;
        _hasLastResult = true;
        Debug.Log($"TestGoldWindow: {label}", this);
    }
}
