using System.Collections;
using UnityEngine;

/// <summary>
/// Single presentation director for a merchant caravan visit — building descent, bird/merchant
/// landing, dwell, and the reverse departure — as one Coroutine. Not a Worker/NPCType role: no
/// stat, no selector, no action queue. Not pooled: there is only ever one caravan, so a single
/// persistent scene GameObject enabled/disabled by MerchantArrivalScheduler is enough.
///
/// The caravan prefab instance's world position is the landing anchor. The building and bird
/// local positions authored in the prefab are their landed pose. The merchant anchor is parented
/// to the building at its deck position, and its Animator moves only Merchant/Visual between the
/// deck and ground. No scene-placed arrival/dock/departure Transforms are needed.
/// </summary>
public sealed class MerchantCaravan : MonoBehaviour
{
    private const float MinimumLiftHeight = 0.1f;
    private const float MinimumStepDuration = 0.1f;
    private const float MinimumDwellDuration = 1f;
    private const float ArrivalDistanceSquared = 0.0001f; // 0.01 world units
    private const int BaseLayerIndex = 0;

    private static readonly int MerchantDeckIdleStateHash = Animator.StringToHash("Base Layer.DeckIdle");
    private static readonly int MerchantLandingStateHash = Animator.StringToHash("Base Layer.Landing");
    private static readonly int MerchantLandedIdleStateHash = Animator.StringToHash("Base Layer.LandedIdle");
    private static readonly int MerchantBoardingStateHash = Animator.StringToHash("Base Layer.Boarding");

    [SerializeField] private MerchantVisual _visual;
    [SerializeField] private Transform _building;
    [SerializeField] private Animator _merchantAnimator;
    [SerializeField] private TransportBird[] _birds;

    [SerializeField, Min(MinimumLiftHeight)] private float _buildingLiftHeight = 12f;
    [SerializeField, Min(MinimumLiftHeight)] private float _birdLiftHeight = 7f;

    [SerializeField, Min(MinimumStepDuration)] private float _buildingLandDuration = 2.5f;
    [SerializeField, Min(MinimumStepDuration)] private float _crewLandDuration = 1.8f;
    [SerializeField, Min(MinimumStepDuration)] private float _birdGatherSpeed = 3f;
    [SerializeField, Min(MinimumStepDuration)] private float _crewLiftDuration = 1.8f;
    [SerializeField, Min(MinimumStepDuration)] private float _buildingLiftDuration = 2.5f;
    [SerializeField, Min(MinimumDwellDuration)] private float _dwellDuration = 30f;
    [SerializeField] private AnimationCurve _easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _visitRoutine;
    private Vector3 _buildingGround;
    private Vector3[] _birdGrounds;
    private bool _isConfigured;
    private bool _hasLoggedConfigurationFailure;

    public bool IsVisiting => _visitRoutine != null;

    private void Awake()
    {
        if (!_visual || !_building || !_merchantAnimator || !_merchantAnimator.runtimeAnimatorController ||
            _birds == null || _birds.Length == 0)
        {
            ReportConfigurationFailure(
                "missing a required reference (_visual/_building/_merchantAnimator with controller/_birds)");
            return;
        }

        for (int i = 0; i < _birds.Length; ++i)
        {
            if (!_birds[i])
            {
                ReportConfigurationFailure("_birds contains a missing element");
                return;
            }
        }

        // The pose authored on the prefab IS the landed pose. Captured once here so every
        // subsequent visit — including a lift/land cycle that overwrites these Transforms every
        // frame — always resets from the same fixed reference rather than drifting.
        _buildingGround = _building.localPosition;
        _birdGrounds = new Vector3[_birds.Length];
        for (int i = 0; i < _birds.Length; ++i)
            _birdGrounds[i] = _birds[i].transform.localPosition;

        _isConfigured = true;
    }

    public void BeginVisit()
    {
        if (IsVisiting)
            return;

        // SetActive must precede the _isConfigured check: the caravan sits disabled between
        // visits, so Awake has not run yet on the first visit. SetActive(true) runs Awake/OnEnable
        // synchronously, so _isConfigured is safe to read immediately after.
        gameObject.SetActive(true);

        if (!_isConfigured)
        {
            gameObject.SetActive(false);
            return;
        }

        _visitRoutine = StartCoroutine(VisitRoutine());
    }

    private void OnDisable()
    {
        // Unity has already stopped the Coroutine (and every bird's ground-presentation
        // Coroutine, transitively, once their GameObjects lose activation). Only the handle needs
        // clearing so IsVisiting reports correctly whether this was a normal finish or a forced
        // deactivation mid-visit.
        _visitRoutine = null;
    }

    private IEnumerator VisitRoutine()
    {
        ResetPresentation();

        yield return MoveBuilding(_buildingGround + Vector3.up * _buildingLiftHeight, _buildingGround, _buildingLandDuration);
        yield return MoveCrew(isLanding: true, _crewLandDuration);

        _visual.SetTradeAvailable(true);
        StartBirdGroundPresentation();

        yield return new WaitForSeconds(_dwellDuration);

        _visual.SetTradeAvailable(false);
        StopBirdGroundPresentation();

        yield return GatherBirds();
        yield return MoveCrew(isLanding: false, _crewLiftDuration);
        yield return MoveBuilding(_buildingGround, _buildingGround + Vector3.up * _buildingLiftHeight, _buildingLiftDuration);

        _visitRoutine = null; // before SetActive(false) so IsVisiting reads correctly
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Guarantees no position, rotation, or Animator state survives from a previous visit. Runs at
    /// the start of every visit, first one included.
    /// </summary>
    private void ResetPresentation()
    {
        _visual.SetTradeAvailable(false);

        _building.localPosition = _buildingGround + Vector3.up * _buildingLiftHeight;

        _merchantAnimator.gameObject.SetActive(true);
        _merchantAnimator.speed = 1f;
        _merchantAnimator.Play(MerchantDeckIdleStateHash, BaseLayerIndex, 0f);
        _merchantAnimator.Update(0f);

        for (int i = 0; i < _birds.Length; ++i)
        {
            _birds[i].transform.localPosition = _birdGrounds[i] + Vector3.up * _birdLiftHeight;
            _birds[i].ResetToFlying();
        }
    }

    private IEnumerator MoveBuilding(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = _easing.Evaluate(Mathf.Clamp01(elapsed / duration));
            _building.localPosition = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        _building.localPosition = to; // snap so curve overshoot never leaves a visible offset
    }

    /// <summary>
    /// Moves the merchant and all birds together on one shared progress value, so the whole crew
    /// lands/lifts in the same frame window. isLanding true = birds land while the merchant jumps
    /// from the deck; false = birds lift while the merchant jumps back onto the deck.
    /// </summary>
    private IEnumerator MoveCrew(bool isLanding, float duration)
    {
        int merchantStateHash = isLanding ? MerchantLandingStateHash : MerchantBoardingStateHash;
        _merchantAnimator.speed = 0f;

        if (!isLanding)
        {
            for (int i = 0; i < _birds.Length; ++i)
                _birds[i].PlayFlying();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            SampleMerchantAnimation(merchantStateHash, normalizedTime);
            ApplyBirdPose(isLanding, _easing.Evaluate(normalizedTime));
            yield return null;
        }

        SampleMerchantAnimation(merchantStateHash, 1f);
        ApplyBirdPose(isLanding, 1f);
        _merchantAnimator.speed = 1f;

        if (isLanding)
        {
            _merchantAnimator.Play(MerchantLandedIdleStateHash, BaseLayerIndex, 0f);

            for (int i = 0; i < _birds.Length; ++i)
                _birds[i].PlayStanding();
        }
        else
        {
            // Boarding returns the merchant to the deck. The merchant anchor is parented under
            // the building, so the following building lift carries both of them together.
            _merchantAnimator.Play(MerchantDeckIdleStateHash, BaseLayerIndex, 0f);
        }
    }

    private void SampleMerchantAnimation(int stateHash, float normalizedTime)
    {
        _merchantAnimator.Play(stateHash, BaseLayerIndex, normalizedTime);
        _merchantAnimator.Update(0f);
    }

    private void ApplyBirdPose(bool isLanding, float t)
    {
        float landedWeight = isLanding ? t : 1f - t;

        for (int i = 0; i < _birds.Length; ++i)
        {
            Vector3 air = _birdGrounds[i] + Vector3.up * _birdLiftHeight;
            _birds[i].transform.localPosition = Vector3.LerpUnclamped(air, _birdGrounds[i], landedWeight);
        }
    }

    private void StartBirdGroundPresentation()
    {
        for (int i = 0; i < _birds.Length; ++i)
            _birds[i].StartGroundPresentation();
    }

    private void StopBirdGroundPresentation()
    {
        for (int i = 0; i < _birds.Length; ++i)
            _birds[i].StopGroundPresentation();
    }

    /// <summary>
    /// Birds scattered randomly during dwell, so this cannot be the recorded arrival animation
    /// played backwards — it is a separate, logically-reversed walk from wherever each bird
    /// currently stands back to its own ground anchor.
    /// </summary>
    private IEnumerator GatherBirds()
    {
        bool anyBirdMoving;
        do
        {
            anyBirdMoving = false;
            for (int i = 0; i < _birds.Length; ++i)
            {
                Transform birdTransform = _birds[i].transform;
                Vector3 target = _birdGrounds[i];
                if ((birdTransform.localPosition - target).sqrMagnitude <= ArrivalDistanceSquared)
                    continue;

                anyBirdMoving = true;
                birdTransform.localPosition =
                    Vector3.MoveTowards(birdTransform.localPosition, target, _birdGatherSpeed * Time.deltaTime);
            }

            if (anyBirdMoving)
                yield return null;
        } while (anyBirdMoving);

        for (int i = 0; i < _birds.Length; ++i)
        {
            _birds[i].transform.localPosition = _birdGrounds[i];
            _birds[i].PlayStanding();
        }
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"MerchantCaravan '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }

    private void OnValidate()
    {
        _buildingLiftHeight = Mathf.Max(MinimumLiftHeight, _buildingLiftHeight);
        _birdLiftHeight = Mathf.Max(MinimumLiftHeight, _birdLiftHeight);
        _buildingLandDuration = Mathf.Max(MinimumStepDuration, _buildingLandDuration);
        _crewLandDuration = Mathf.Max(MinimumStepDuration, _crewLandDuration);
        _birdGatherSpeed = Mathf.Max(MinimumStepDuration, _birdGatherSpeed);
        _crewLiftDuration = Mathf.Max(MinimumStepDuration, _crewLiftDuration);
        _buildingLiftDuration = Mathf.Max(MinimumStepDuration, _buildingLiftDuration);
        _dwellDuration = Mathf.Max(MinimumDwellDuration, _dwellDuration);
    }
}
