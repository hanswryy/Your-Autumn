using UnityEngine;
using Fungus;

public class OverworldManager : MonoBehaviour
{
    public GameObject playerPrefab; // Reference to your player prefab if needed

    [Header("Scene Transition")]
    public float fadeInDuration = 1f;

    void Start()
    {
        // Reveal the scene from the black that the cutscene's Fungus "Fade Screen" left behind.
        // CameraManager persists across the scene load, so we fade its alpha back to 0 here.
        FungusManager.Instance.CameraManager.Fade(0f, fadeInDuration, null);

        // NOTE: returning from battle is no longer handled here. Battles are loaded
        // additively (see GameState.SuspendOverworldForBattle / ReturnFromBattle), so
        // the overworld is never reloaded and this Start() runs only on first entry.
        // The player keeps their position and the world is never regenerated.
    }
}
