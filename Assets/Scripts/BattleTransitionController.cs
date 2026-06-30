using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cinemachine;

// Persona-style "battle initiated" transition.
//
// When a battle starts in the overworld we don't cut straight to the battle
// scene. Instead we take the camera away from Cinemachine, drop the game into
// slow motion, and slowly dolly/zoom in on the enemy while the battle scene
// loads in the background. Once it has finished loading we freeze the zoomed
// frame and play a radial blur on it, then cut to the battle scene.
public class BattleTransitionController : MonoBehaviour
{
    [Header("Scene")]
    public string battleSceneName = "BattleScene";

    [Header("Slow Motion")]
    [Tooltip("Time scale applied while the camera zooms toward the enemy (1 = normal speed).")]
    [Range(0.01f, 1f)]
    public float slowMotionScale = 0.15f;

    [Header("Camera Zoom")]
    [Tooltip("How fast the camera dollies toward the enemy (world units / real second).")]
    public float zoomSpeed = 6f;
    [Tooltip("Closest the camera is allowed to get to the enemy, so it doesn't clip through.")]
    public float minDistanceToEnemy = 3f;
    [Tooltip("Field of view the camera narrows to as it zooms in (smaller = tighter punch-in).")]
    public float targetFOV = 25f;
    [Tooltip("How fast the field of view narrows (degrees / real second).")]
    public float fovSpeed = 30f;
    [Tooltip("How quickly the camera rotates to keep the enemy framed.")]
    public float lookSpeed = 5f;

    [Header("Timing")]
    [Tooltip("Minimum length of the transition, even if the battle scene loads instantly.")]
    public float minTransitionTime = 1.2f;

    [Header("Radial Blur")]
    [Tooltip("Play a radial blur on the frozen frame once the battle scene has loaded.")]
    public bool useRadialBlur = true;
    [Tooltip("Optional material using the Hidden/BattleRadialBlur shader. If left empty one is created at runtime.")]
    public Material blurMaterial;
    [Tooltip("Length of the radial blur effect, in real seconds.")]
    public float blurDuration = 0.5f;
    [Tooltip("Sorting order of the blur canvas — keep it above any world UI.")]
    public int blurSortingOrder = 999;

    // Guards against a second battle trigger firing while a transition is already playing.
    public static bool IsTransitioning { get; private set; }

    public void BeginTransition(GameObject enemy)
    {
        if (IsTransitioning) return;
        IsTransitioning = true;
        StartCoroutine(TransitionRoutine(enemy));
    }

    IEnumerator TransitionRoutine(GameObject enemy)
    {
        Camera cam = Camera.main;

        // Take manual control of the camera away from Cinemachine for the duration.
        CinemachineBrain brain = cam != null ? cam.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
            brain.enabled = false;

        // Start loading the battle scene now, but hold the swap until we're done zooming.
        AsyncOperation load = SceneManager.LoadSceneAsync(battleSceneName);
        load.allowSceneActivation = false;

        // Drop into slow motion. Note: timeScale survives the scene load, so we
        // MUST restore it before activating the battle scene.
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * slowMotionScale;

        float elapsed = 0f;
        Vector3 enemyPos = enemy != null
            ? enemy.transform.position
            : (cam != null ? cam.transform.position + cam.transform.forward * 5f : Vector3.zero);

        while (true)
        {
            // Use unscaled time so the camera move plays at a steady real-world
            // pace regardless of the slow-motion timeScale.
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            if (cam != null)
            {
                if (enemy != null) enemyPos = enemy.transform.position;

                // Dolly toward the enemy, stopping short so we don't clip through them.
                Vector3 toEnemy = enemyPos - cam.transform.position;
                float distance = toEnemy.magnitude;
                if (distance > minDistanceToEnemy)
                {
                    Vector3 dir = toEnemy / distance;
                    float step = Mathf.Min(zoomSpeed * dt, distance - minDistanceToEnemy);
                    cam.transform.position += dir * step;
                }

                // Keep the enemy framed and narrow the FOV for the punch-in.
                if (toEnemy.sqrMagnitude > 0.0001f)
                {
                    cam.transform.rotation = Quaternion.Slerp(
                        cam.transform.rotation,
                        Quaternion.LookRotation(enemyPos - cam.transform.position),
                        dt * lookSpeed);
                }
                cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, targetFOV, fovSpeed * dt);
            }

            // Once the battle scene has finished loading (progress caps at 0.9
            // while activation is held) and the zoom has played long enough, move
            // on to the shatter.
            bool sceneReady = load.progress >= 0.9f;
            if (sceneReady && elapsed >= minTransitionTime)
                break;

            yield return null;
        }

        // Loading is done: freeze the zoomed frame and play the radial blur.
        if (useRadialBlur)
            yield return PlayRadialBlur(cam, enemyPos);

        // Restore normal time before handing off to the battle scene.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        IsTransitioning = false;
        load.allowSceneActivation = true;
    }

    // The capture from the previous transition. We can't destroy a frame the
    // moment its blur finishes — the overlay keeps drawing it for a frame or two
    // until the battle scene activates, and a destroyed texture renders black. So
    // we let it linger and free it at the start of the next transition instead.
    static Texture2D s_lingeringFrame;

    // Captures the current (zoomed) frame and plays a radial blur on it via a
    // full-screen Screen Space Overlay RawImage.
    IEnumerator PlayRadialBlur(Camera cam, Vector3 enemyPos)
    {
        // Free the capture left over from a previous transition.
        if (s_lingeringFrame != null)
        {
            Destroy(s_lingeringFrame);
            s_lingeringFrame = null;
        }

        // Grab the rendered frame once everything for this frame has been drawn.
        yield return new WaitForEndOfFrame();
        Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
        s_lingeringFrame = frame;

        // Resolve the material (instance, so we never mutate a shared asset).
        Material mat = blurMaterial != null ? new Material(blurMaterial) : null;
        if (mat == null)
        {
            Shader shader = Shader.Find("Hidden/BattleRadialBlur");
            if (shader == null)
            {
                Debug.LogWarning("[BattleTransition] Blur shader 'Hidden/BattleRadialBlur' not found; skipping blur.");
                Destroy(frame);
                yield break;
            }
            mat = new Material(shader);
        }

        // Aim the blur at the enemy's position on screen.
        Vector2 center = new Vector2(0.5f, 0.5f);
        if (cam != null)
        {
            Vector3 vp = cam.WorldToViewportPoint(enemyPos);
            if (vp.z > 0f) center = new Vector2(vp.x, vp.y);
        }
        mat.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
        mat.SetFloat("_Progress", 0f);

        // Build a full-screen overlay showing the frozen frame.
        GameObject canvasObj = new GameObject("BattleBlurOverlay");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = blurSortingOrder;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject imgObj = new GameObject("Frame");
        imgObj.transform.SetParent(canvasObj.transform, false);
        RawImage img = imgObj.AddComponent<RawImage>();
        img.texture = frame;
        img.material = mat;
        img.raycastTarget = false;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Drive the effect with unscaled time so it's unaffected by slow motion.
        float t = 0f;
        while (t < blurDuration)
        {
            t += Time.unscaledDeltaTime;
            mat.SetFloat("_Progress", blurDuration > 0f ? Mathf.Clamp01(t / blurDuration) : 1f);
            yield return null;
        }
        mat.SetFloat("_Progress", 1f);

        // Deliberately do NOT destroy `frame` here: the overlay must keep showing
        // it until the battle scene activates (a frame later), otherwise the
        // RawImage draws a destroyed texture as a black screen. It is freed at the
        // start of the next transition via s_lingeringFrame. The overlay canvas
        // itself is destroyed with the overworld scene on activation.
    }
}
