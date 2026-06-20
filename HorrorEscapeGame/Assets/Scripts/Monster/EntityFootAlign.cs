using UnityEngine;

public static class EntityFootAlign
{
    private const float FloorSink = 0.02f;

    public static void AlignToFloor(Transform entityRoot, GameObject visualRoot)
    {
        PinVisualToFloor(entityRoot, visualRoot, forcePoseSync: true);
    }

    public static void PinVisualToFloor(Transform entityRoot, GameObject visualRoot, bool forcePoseSync = false)
    {
        if (entityRoot == null || visualRoot == null) return;

        float footY = GetFootWorldY(visualRoot, forcePoseSync);
        if (float.IsNaN(footY)) return;

        float delta = GetFloorY(entityRoot) - footY - FloorSink;
        if (Mathf.Abs(delta) > 1.25f)
            return;

        if (Mathf.Abs(delta) < 0.0005f) return;

        var local = visualRoot.transform.localPosition;
        local.y += delta;
        visualRoot.transform.localPosition = local;
    }

    private static float GetFloorY(Transform entityRoot)
    {
        // NavMeshAgent transform sits on the walk surface; baseOffset shifts the agent center, not the floor.
        return entityRoot.position.y;
    }

    private static float GetFootWorldY(GameObject visualRoot, bool forcePoseSync)
    {
        var animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            if (forcePoseSync)
                animator.Update(0f);

            if (animator.isHuman)
            {
                var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (left != null && right != null)
                    return Mathf.Min(left.position.y, right.position.y);
            }
        }

        var leftNamed = FindTransform(
            visualRoot.transform,
            "foot.l", "foot_l", "leftfoot", "ArtStore3D_LeftFoot", "LeftFoot");
        var rightNamed = FindTransform(
            visualRoot.transform,
            "foot.r", "foot_r", "rightfoot", "ArtStore3D_RightFoot", "RightFoot");
        if (leftNamed != null && rightNamed != null)
            return Mathf.Min(leftNamed.position.y, rightNamed.position.y);

        return float.NaN;
    }

    private static Transform FindTransform(Transform root, params string[] names)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in names)
            {
                if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(child.name.Replace(" ", ""), name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }
}
