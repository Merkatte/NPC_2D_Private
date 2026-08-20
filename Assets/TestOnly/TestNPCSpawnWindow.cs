using UnityEngine;

public class TestNPCSpawnWindow : MonoBehaviour
{
    private const float WindowWidth = 220f;
    private const float WindowHeight = 130f;

    [SerializeField] private NPCManager _npcManager;

    private Rect _windowRect = new Rect(20f, 20f, WindowWidth, WindowHeight);

    private void Awake()
    {
        if (!_npcManager)
        {
            _npcManager = FindFirstObjectByType<NPCManager>();
        }
    }

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Test NPC");
    }

    private void DrawWindow(int windowId)
    {
        if (GUILayout.Button("Create Farmer NPC", GUILayout.Height(32f)))
        {
            CreateNPC(NPCType.Farmer);
        }

        if (GUILayout.Button("Create Guard NPC", GUILayout.Height(32f)))
        {
            CreateNPC(NPCType.Guard);
        }

        GUI.DragWindow();
    }

    private void CreateNPC(NPCType npcType)
    {
        if (!_npcManager)
        {
            return;
        }

        _npcManager.CreateNPC(npcType);
    }
}
