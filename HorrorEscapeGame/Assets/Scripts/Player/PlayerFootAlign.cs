using UnityEngine;

public static class PlayerFootAlign
{
    public static void AlignVisualToFloor(GameObject visualRoot)
    {
        if (visualRoot == null) return;

        float footY = GetFootWorldY(visualRoot);
        if (float.IsNaN(footY)) return;

        float delta = MapFloor.WalkY - footY;
        if (Mathf.Abs(delta) < 0.001f) return;

        visualRoot.transform.localPosition += Vector3.up * delta;
        Debug.Log($"[PlayerFootAlign] Visual raised by {delta:F3}m (feet -> WalkY {MapFloor.WalkY:F3})");
    }

    private static float GetFootWorldY(GameObject visualRoot)
    {
        var animator = visualRoot.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (left != null && right != null)
                return Mathf.Min(left.position.y, right.position.y);
        }

        var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return float.NaN;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.min.y;
    }
}
