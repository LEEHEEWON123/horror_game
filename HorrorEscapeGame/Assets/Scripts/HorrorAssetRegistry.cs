using UnityEngine;

/// <summary>
/// Resources/HorrorAssetRegistry.asset 에 저장되는 중앙 에셋 레지스트리.
/// WebGL 빌드에서 AssetDatabase 대신 사용됩니다.
/// Tools > Create Horror Asset Registry 로 생성하세요.
/// </summary>
[CreateAssetMenu(fileName = "HorrorAssetRegistry", menuName = "Horror/Asset Registry")]
public class HorrorAssetRegistry : ScriptableObject
{
    private static HorrorAssetRegistry _instance;
    public static HorrorAssetRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<HorrorAssetRegistry>("HorrorAssetRegistry");
            return _instance;
        }
    }

    [Header("Map Prefabs")]
    public GameObject cityPrefab;
    public GameObject backroomsLevelPrefab;
    public GameObject sponzaPrefab;

    [Header("Map_03 Materials")]
    public Material sponzaFloorMaterial;
    public Material sponzaWallMaterial;
    public Material sponzaAltWallMaterial;
    public Material sponzaCeilingMaterial;

    [Header("Player")]
    public GameObject protectiveSuitVisualPrefab;

    [Header("Entity Visuals")]
    public GameObject mutantVisualPrefab;
    public GameObject pumpkinVisualPrefab;
    public GameObject priestVisualPrefab;
    public GameObject insurgentVisualPrefab;

    [Header("Entity Animators")]
    public RuntimeAnimatorController mutantAnimatorController;
    public RuntimeAnimatorController pumpkinAnimatorController;
    public RuntimeAnimatorController priestAnimatorController;
    public RuntimeAnimatorController insurgentAnimatorController;

    [Header("Smiler")]
    public Texture2D smilerTexture;

    [Header("Audio")]
    public AudioClip[] entityChaseClips;
}
