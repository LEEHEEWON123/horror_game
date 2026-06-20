using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arcade-style run manager. Progression is portal-only (no keys/items).
/// Nothing is written to disk — map state resets on every map load and on death.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private readonly GameState _state = new GameState();
    private string _returnMapScene;

    public int Lives => _state.Lives;
    public string ReturnMapScene => _returnMapScene;

    public bool IsReturnMap(string sceneName) =>
        !string.IsNullOrEmpty(_returnMapScene)
        && !string.IsNullOrEmpty(sceneName)
        && _returnMapScene == sceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetMapState()
    {
        _state.Reset();
    }

    public void ResetRun()
    {
        _state.Reset();
        _returnMapScene = null;
        Map04RespawnState.UseRandomSpawn = false;
        PortalTransitionState.Reset();
    }

    public void PlayerDied()
    {
        ResetRun();
        NoclipEntryState.Clear();
        PortalTransitionState.Reset();
        RealityEscapeState.Clear();
        DimensionWakeState.SetActive(false);

        SceneTransitioner.Instance.LoadScene("Map_00");
    }

    public void BeginNewRun(string entryScene = "Map_00")
    {
        ResetRun();
        _returnMapScene = DimensionMapPool.PickReturnMap();
        SceneTransitioner.Instance.LoadScene(entryScene);
    }

    public void EnterRandomDimension()
    {
        EnsureReturnMapAssigned();
        ResetMapState();
        string nextScene = DimensionMapPool.PickRandom(null);

        if (SceneManager.GetActiveScene().name == "Map_00")
        {
            NoclipEntryState.MarkPending(autoStand: true);
            SceneTransitioner.Instance.LoadSceneNoclip(nextScene);
        }
        else
        {
            SceneTransitioner.Instance.LoadScene(nextScene);
        }
    }

    public void CompleteMap()
    {
        EnsureReturnMapAssigned();
        ResetMapState();

        string current = SceneManager.GetActiveScene().name;
        if (current == _returnMapScene)
        {
            RealityEscapeState.MarkPending();
            SceneTransitioner.Instance.LoadSceneRealityEscape(DimensionMapPool.RealityScene);
            return;
        }

        string nextScene = PortalTransitionState.HasNext
            ? PortalTransitionState.NextScene
            : DimensionMapPool.PickRandom(current);

        PortalTransitionState.UseGenerationSeed();
        PortalTransitionState.MarkPendingEntry(Vector3.forward);
        SceneTransitioner.Instance.LoadSceneThroughPortal(nextScene);
    }

    public void CompleteMapThroughPortal(Transform portal)
    {
        EnsureReturnMapAssigned();
        ResetMapState();

        if (!PortalTransitionState.HasNext)
            PortalTransitionState.Prepare(SceneManager.GetActiveScene().name);

        PortalTransitionState.UseGenerationSeed();
        Vector3 forward = portal != null ? portal.forward : Vector3.forward;
        PortalTransitionState.MarkPendingEntry(forward);

        if (portal != null)
        {
            var preview = portal.GetComponent<PortalNextMapPreview>();
            preview?.UnloadPreview();
        }

        SceneTransitioner.Instance.LoadSceneThroughPortal(PortalTransitionState.NextScene);
    }

    private void EnsureReturnMapAssigned()
    {
        if (!string.IsNullOrEmpty(_returnMapScene)) return;
        _returnMapScene = DimensionMapPool.PickReturnMap();
    }
}
