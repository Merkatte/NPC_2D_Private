using System;
using UnityEngine;

public class TestNPCSpawnWindow : MonoBehaviour
{
    private const float WindowWidth = 220f;
    private const float WindowHeight = 90f;

    [SerializeField] private NPCManager _npcManager;

    private readonly NPCType[] _npcTypes = (NPCType[])Enum.GetValues(typeof(NPCType));
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
        if (GUILayout.Button("Create Random NPC", GUILayout.Height(32f)))
        {
            CreateRandomNPC();
        }

        GUI.DragWindow();
    }

    private void CreateRandomNPC()
    {
        if (!_npcManager || _npcTypes.Length == 0)
        {
            return;
        }

        int index = UnityEngine.Random.Range(0, _npcTypes.Length);
        _npcManager.CreateNPC(_npcTypes[index]);
    }
}
