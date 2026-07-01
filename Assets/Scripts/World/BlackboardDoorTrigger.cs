using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BlackboardDoorTrigger : MonoBehaviour
{
    [Header("Wall Halves")]
    public Transform leftHalf;
    public Transform rightHalf;

    [Header("Slide Settings")]
    [Tooltip("How far each half slides open (world units)")]
    public float slideDistance = 2.5f;
    [Tooltip("Local-space axis the LEFT half slides along (negated on open)")]
    public Vector3 leftSlideAxis = Vector3.left;
    [Tooltip("Local-space axis the RIGHT half slides along")]
    public Vector3 rightSlideAxis = Vector3.right;

    [Header("Animation")]
    public float openDuration = 1.1f;
    public float closeDuration = 0.8f;
    public AnimationCurve openCurve  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Rumble Before Opening")]
    public bool doRumble = true;
    public float rumbleDuration   = 0.35f;
    public float rumbleIntensity  = 0.04f;
    public float rumbleFrequency  = 55f;

    [Header("Trigger Zone")]
    public float triggerRadius = 3f;

    // State
    private Vector3 leftOrigin;
    private Vector3 rightOrigin;
    private bool    isOpen;
    private bool    isAnimating;
    private bool    locked;
    private Coroutine activeRoutine;

    // Setup
    void Awake()
    {
        var col        = GetComponent<SphereCollider>();
        col.isTrigger  = true;
        col.radius     = triggerRadius;
    }

    void Start()
    {
        if (leftHalf)  leftOrigin  = leftHalf.position;
        if (rightHalf) rightOrigin = rightHalf.position;
    }

    // Trigger
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isOpen || isAnimating) return;
        Restart(OpenSequence());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !isOpen || isAnimating || locked) return;
        Restart(CloseSequence());
    }

    // Call during a cutscene so the door stays open when the player walks through.
    public void LockOpen()
    {
        locked = true;
    }

    void Restart(IEnumerator routine)
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(routine);
    }

    // Sequence
    IEnumerator OpenSequence()
    {
        isAnimating = true;

        if (doRumble)
            yield return StartCoroutine(Rumble());

        Vector3 leftTarget  = leftOrigin  + leftSlideAxis.normalized  * slideDistance;
        Vector3 rightTarget = rightOrigin + rightSlideAxis.normalized * slideDistance;

        yield return StartCoroutine(SlideHalves(
            leftHalf.position,  leftTarget,
            rightHalf.position, rightTarget,
            openDuration, openCurve));

        isOpen      = true;
        isAnimating = false;
    }

    IEnumerator CloseSequence()
    {
        isAnimating = true;

        yield return StartCoroutine(SlideHalves(
            leftHalf.position,  leftOrigin,
            rightHalf.position, rightOrigin,
            closeDuration, closeCurve));

        isOpen      = false;
        isAnimating = false;
    }

    // Anim helpers
    IEnumerator Rumble()
    {
        float t = 0f;
        while (t < rumbleDuration)
        {
            float offset = Mathf.Sin(t * rumbleFrequency) * rumbleIntensity;
            if (leftHalf)  leftHalf.position  = leftOrigin  + Vector3.up * offset;
            if (rightHalf) rightHalf.position = rightOrigin + Vector3.up * offset;
            t += Time.deltaTime;
            yield return null;
        }
        // reset before slide
        if (leftHalf)  leftHalf.position  = leftOrigin;
        if (rightHalf) rightHalf.position = rightOrigin;
    }

    IEnumerator SlideHalves(
        Vector3 leftStart,  Vector3 leftEnd,
        Vector3 rightStart, Vector3 rightEnd,
        float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = curve.Evaluate(elapsed / duration);
            if (leftHalf)  leftHalf.position  = Vector3.Lerp(leftStart,  leftEnd,  t);
            if (rightHalf) rightHalf.position = Vector3.Lerp(rightStart, rightEnd, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (leftHalf)  leftHalf.position  = leftEnd;
        if (rightHalf) rightHalf.position = rightEnd;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.18f);
        Gizmos.DrawSphere(transform.position, triggerRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        if (leftHalf)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftHalf.position,
                leftHalf.position + leftSlideAxis.normalized * slideDistance);
        }
        if (rightHalf)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightHalf.position,
                rightHalf.position + rightSlideAxis.normalized * slideDistance);
        }
    }
}
