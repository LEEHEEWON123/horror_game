using UnityEngine;

public class MonsterGroundPin : MonoBehaviour
{
    private Transform _entityRoot;
    private GameObject _visualRoot;

    public void Configure(Transform entityRoot, GameObject visualRoot)
    {
        _entityRoot = entityRoot;
        _visualRoot = visualRoot;
        EntityFootAlign.AlignToFloor(entityRoot, visualRoot);
    }

    private void LateUpdate()
    {
        if (_entityRoot == null || _visualRoot == null) return;
        EntityFootAlign.PinVisualToFloor(_entityRoot, _visualRoot);
    }
}
