using System.Collections;
using UnityEngine;

/// <summary>
/// One transport bird's Animator and simple ground-wander presentation. Not a general NPC — only
/// expression commands are exposed (Play*/Start*/Stop*), never the current motion state, so
/// nothing outside this component reads or branches on it. MerchantCaravan owns the synchronized
/// flight lift/land of all three birds together; this component only owns what a single bird does
/// once it is standing on the ground during dwell.
/// </summary>
public sealed class TransportBird : MonoBehaviour
{
    // Mirrors TransportBird.controller's MotionState AnyState transition thresholds.
    private const int StandingMotion = 0;
    private const int WalkingMotion = 1;
    private const int FeedingMotion = 2;
    private const int FlyingMotion = 3;

    private const int GroundBehaviourCount = 3;
    private const int WalkChoice = 1;
    private const int FeedChoice = 2;
    private const float ArrivalDistanceSquared = 0.0001f; // 0.01 world units
    private const float MinimumRoamRadius = 0.1f;
    private const float MinimumWalkSpeed = 0.1f;
    private const float MinimumPauseDuration = 0.1f;

    private const int BaseLayerIndex = 0;

    private static readonly int MotionStateHash = Animator.StringToHash("MotionState");
    private static readonly int FlyingStateHash = Animator.StringToHash("Base Layer.Flying");

    [SerializeField] private Animator _animator;
    [SerializeField, Min(MinimumRoamRadius)] private float _roamRadius = 1.2f;
    [SerializeField, Min(MinimumWalkSpeed)] private float _walkSpeed = 0.8f;
    [SerializeField, Min(MinimumPauseDuration)] private float _minimumPauseDuration = 0.8f;
    [SerializeField, Min(MinimumPauseDuration)] private float _maximumPauseDuration = 2.5f;

    private Coroutine _groundRoutine;
    private Vector3 _groundAnchor;
    private bool _hasLoggedConfigurationFailure;

    private void Awake()
    {
        if (!_animator)
        {
            ReportConfigurationFailure("missing Animator reference");
        }
    }

    /// <summary>
    /// Snaps straight to Flying with no transition. MerchantCaravan reuses the same bird instance
    /// visit after visit, and re-enabling the caravan rebinds the Animator to its default state
    /// (Standing) — this forces the correct pose from frame one instead of showing a stray frame.
    /// Also stops any in-progress ground presentation so a new visit never inherits an old wander.
    /// </summary>
    public void ResetToFlying()
    {
        StopGroundPresentation();

        if (!_animator)
            return;

        _animator.SetInteger(MotionStateHash, FlyingMotion);
        _animator.Play(FlyingStateHash, BaseLayerIndex, 0f);
        _animator.Update(0f);
    }

    public void PlayFlying() => SetMotion(FlyingMotion);
    public void PlayStanding() => SetMotion(StandingMotion);
    public void PlayWalking() => SetMotion(WalkingMotion);
    public void PlayFeeding() => SetMotion(FeedingMotion);

    /// <summary>
    /// Starts an unbounded loop of standing/walking/feeding within _roamRadius of wherever the
    /// bird is standing right now (its post-landing ground position). Call only after the bird has
    /// actually landed.
    /// </summary>
    public void StartGroundPresentation()
    {
        if (_groundRoutine != null)
            return;

        _groundAnchor = transform.localPosition;
        _groundRoutine = StartCoroutine(GroundPresentationRoutine());
    }

    public void StopGroundPresentation()
    {
        if (_groundRoutine != null)
        {
            StopCoroutine(_groundRoutine);
            _groundRoutine = null;
        }

        PlayStanding();
    }

    private void OnDisable()
    {
        _groundRoutine = null;
    }

    private IEnumerator GroundPresentationRoutine()
    {
        while (true)
        {
            // Purely cosmetic randomness with no gameplay effect and no determinism requirement,
            // so UnityEngine.Random is used directly rather than an injected IRandomSource.
            int choice = Random.Range(0, GroundBehaviourCount);
            if (choice == WalkChoice)
            {
                Vector2 offset = Random.insideUnitCircle * _roamRadius;
                Vector3 target = _groundAnchor + new Vector3(offset.x, offset.y, 0f);

                PlayWalking();
                while ((transform.localPosition - target).sqrMagnitude > ArrivalDistanceSquared)
                {
                    transform.localPosition =
                        Vector3.MoveTowards(transform.localPosition, target, _walkSpeed * Time.deltaTime);
                    yield return null;
                }

                PlayStanding();
            }
            else if (choice == FeedChoice)
            {
                PlayFeeding();
            }
            else
            {
                PlayStanding();
            }

            yield return new WaitForSeconds(Random.Range(_minimumPauseDuration, _maximumPauseDuration));
        }
    }

    private void SetMotion(int motion)
    {
        if (!_animator)
            return;

        _animator.SetInteger(MotionStateHash, motion);
    }

    private void ReportConfigurationFailure(string reason)
    {
        if (_hasLoggedConfigurationFailure)
            return;

        Debug.LogError($"TransportBird '{name}': {reason}.", this);
        _hasLoggedConfigurationFailure = true;
    }

    private void OnValidate()
    {
        _roamRadius = Mathf.Max(MinimumRoamRadius, _roamRadius);
        _walkSpeed = Mathf.Max(MinimumWalkSpeed, _walkSpeed);
        _minimumPauseDuration = Mathf.Max(MinimumPauseDuration, _minimumPauseDuration);
        _maximumPauseDuration = Mathf.Max(_minimumPauseDuration, _maximumPauseDuration);
    }
}
