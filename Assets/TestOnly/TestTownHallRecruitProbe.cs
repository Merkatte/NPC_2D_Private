using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Deterministic probe for TownHallRecruitment/NPCManager's reservation transaction, limited to
/// failure paths that never leave a partially-changed or hard-to-undo state (TownHallRecruitment's
/// cooldown timer is real wall-clock state with no test-only setter, and a successful
/// TryDispatchCandidate() commits a real NPC into the scene with no despawn API — this project
/// deliberately adds neither, so the success path is Play Mode manual verification instead; see
/// PublicMD/Systems/Town_Hall.md).
///
/// Every case checks its own precondition and reports SKIP rather than faking a PASS when this
/// Play session's current state can't construct the scenario (CodeConvention.md §15) — this
/// follows TestDecisionScenarioProbe/TestMerchantBatchTradeProbe's established pattern.
/// </summary>
public class TestTownHallRecruitProbe : MonoBehaviour
{
    private const float WindowWidth = 320f;
    private const float WindowHeight = 80f;

    [SerializeField] private TownHallRecruitment _recruitment;
    [SerializeField] private NPCManager _npcManager;
    [SerializeField] private GoldManager _goldManager;

    [Tooltip("Optional: a separate TownHallRecruitment instance deliberately missing a required " +
        "reference (e.g. no _goldManager), used only to prove TryDispatchCandidate refuses to run " +
        "when unconfigured. Leave unassigned to skip that case.")]
    [SerializeField] private TownHallRecruitment _misconfiguredRecruitment;

    private Rect _windowRect = new Rect(300f, 260f, WindowWidth, WindowHeight);
    private readonly StringBuilder _report = new StringBuilder();

    private int _passCount;
    private int _failCount;
    private int _skipCount;

    private void OnGUI()
    {
        _windowRect = GUI.Window(GetInstanceID(), _windowRect, DrawWindow, "Town Hall Recruit Probe");
    }

    private void DrawWindow(int windowId)
    {
        if (GUILayout.Button("Run Recruit Failure Scenarios", GUILayout.Height(32f)))
            RunAll();

        GUI.DragWindow();
    }

    private void RunAll()
    {
        if (!_recruitment || !_npcManager || !_goldManager)
        {
            Debug.LogError("TestTownHallRecruitProbe is missing one of its serialized references " +
                "(_recruitment/_npcManager/_goldManager).");
            return;
        }

        _report.Clear();
        _passCount = 0;
        _failCount = 0;
        _skipCount = 0;
        _report.AppendLine("=== Town hall recruit probe ===");

        CaseInitialRecruitingRejected();
        CaseUnregisteredRoleRejected();
        CaseInsufficientGoldRejected();
        CaseMisconfiguredRecruitmentRejected();

        _report.Append("Result: ").Append(_passCount).Append(" passed, ")
            .Append(_failCount).Append(" failed, ")
            .Append(_skipCount).Append(" skipped.");

        if (_failCount > 0)
            Debug.LogError(_report.ToString());
        else
            Debug.Log(_report.ToString());
    }

    // ---- cases ----

    private void CaseInitialRecruitingRejected()
    {
        if (_recruitment.Phase != RecruitPhase.Recruiting)
        {
            Skip("Case 1", "_recruitment.Phase is already CandidateReady at the time this ran " +
                "(cooldown elapsed before the button was pressed) — re-run right after entering Play Mode.");
            return;
        }

        int goldBefore = _goldManager.CurrentGold;

        RecruitResult result = _recruitment.TryDispatchCandidate();

        Check("Case 1", "calling TryDispatchCandidate while still Recruiting is rejected and gold is untouched",
            result == RecruitResult.NotReady && _goldManager.CurrentGold == goldBefore);
    }

    /// <summary>
    /// Not scene-specific: walks every NPCType and rents+immediately cancels a dry-run reservation
    /// until it finds one with no NPCManager creation entry, so this doesn't hardcode an assumption
    /// about which scene it runs in.
    /// </summary>
    private void CaseUnregisteredRoleRejected()
    {
        if (!TryFindUnregisteredNpcType(out NPCType npcType))
        {
            Skip("Case 2", "every NPCType has a creation entry registered in this scene; cannot " +
                "construct an unregistered-role case.");
            return;
        }

        int goldBefore = _goldManager.CurrentGold;

        bool reserved = _npcManager.TryReserveWorker(npcType, Vector3.zero, out WorkerReservation reservation);

        Check("Case 2", $"NPCType.{npcType} has no creation entry, so reservation is rejected and gold is untouched",
            !reserved && _goldManager.CurrentGold == goldBefore);
    }

    private void CaseInsufficientGoldRejected()
    {
        if (_recruitment.Phase != RecruitPhase.CandidateReady)
        {
            Skip("Case 3", "_recruitment.Phase is not CandidateReady yet (cooldown still running) — " +
                "re-run after the cooldown elapses.");
            return;
        }

        int goldBefore = _goldManager.CurrentGold;
        if (goldBefore > 0 && !_goldManager.TrySpend(goldBefore))
        {
            Skip("Case 3", "could not drain gold to zero for this case.");
            return;
        }

        try
        {
            RecruitResult result = _recruitment.TryDispatchCandidate();

            Check("Case 3", "dispatching with zero gold is rejected, leaving gold and phase untouched",
                result == RecruitResult.NotEnoughGold &&
                _goldManager.CurrentGold == 0 &&
                _recruitment.Phase == RecruitPhase.CandidateReady);
        }
        finally
        {
            if (goldBefore > 0)
                _goldManager.Add(goldBefore);
        }
    }

    private void CaseMisconfiguredRecruitmentRejected()
    {
        if (!_misconfiguredRecruitment)
        {
            Skip("Case 4", "_misconfiguredRecruitment is not assigned — wire a deliberately " +
                "under-configured TownHallRecruitment instance to exercise this case.");
            return;
        }

        int goldBefore = _goldManager.CurrentGold;

        RecruitResult result = _misconfiguredRecruitment.TryDispatchCandidate();

        Check("Case 4", "an unconfigured TownHallRecruitment refuses to dispatch and gold is untouched",
            result == RecruitResult.NotReady && _goldManager.CurrentGold == goldBefore);
    }

    // ---- helpers ----

    private bool TryFindUnregisteredNpcType(out NPCType npcType)
    {
        foreach (NPCType candidate in Enum.GetValues(typeof(NPCType)))
        {
            if (!_npcManager.TryReserveWorker(candidate, Vector3.zero, out WorkerReservation reservation))
            {
                npcType = candidate;
                return true;
            }

            _npcManager.CancelReservation(reservation);
        }

        npcType = default;
        return false;
    }

    private void Check(string id, string description, bool passed)
    {
        if (passed)
            _passCount++;
        else
            _failCount++;

        _report.Append(passed ? "  PASS " : "  FAIL ").Append(id).Append(" - ").AppendLine(description);
    }

    private void Skip(string id, string reason)
    {
        _skipCount++;
        _report.Append("  SKIP ").Append(id).Append(" - ").AppendLine(reason);
    }
}
