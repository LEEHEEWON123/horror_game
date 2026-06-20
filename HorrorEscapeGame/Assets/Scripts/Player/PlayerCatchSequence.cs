using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCatchSequence : MonoBehaviour
{
    [SerializeField] private float pullDuration = 0.38f;
    [SerializeField] private float holdDuration = 1.65f;
    [SerializeField] private float targetFov = 44f;
    [SerializeField] private float faceDistance = 0.36f;
    [SerializeField] private float lookHeightOffset = 0.03f;
    [SerializeField] private float cameraHeightOffset = -0.04f;
    [SerializeField] private float nearClipPlane = 0.08f;

    private float _entityScaleOverride;
    private Image _vignette;
    private bool _playing;

    public void Configure(
        float faceDistance,
        float targetFov,
        float lookHeightOffset,
        float cameraHeightOffset,
        float entityScale,
        float nearClip = 0.08f)
    {
        this.faceDistance = faceDistance;
        this.targetFov = targetFov;
        this.lookHeightOffset = lookHeightOffset;
        this.cameraHeightOffset = cameraHeightOffset;
        _entityScaleOverride = entityScale;
        nearClipPlane = nearClip;
    }

    public void Play(MonsterAI attacker, PlayerHealth health)
    {
        if (_playing || health == null) return;
        StartCoroutine(Run(attacker, health));
    }

    private IEnumerator Run(MonsterAI attacker, PlayerHealth health)
    {
        _playing = true;

        var controller = GetComponent<PlayerController>();
        var interaction = GetComponent<PlayerInteraction>();
        var footAlign = GetComponent<PlayerFootAlignRunner>();
        var fpCamera = GetComponentInChildren<FirstPersonCamera>();
        var rb = GetComponent<Rigidbody>();
        var cam = Camera.main;

        if (controller != null) controller.enabled = false;
        if (interaction != null) interaction.enabled = false;
        if (footAlign != null) footAlign.enabled = false;
        if (fpCamera != null) fpCamera.enabled = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;

        MonsterAI.PauseAllForCatch(attacker, transform);

        Light flashlight = null;
        float flashIntensity = 0f;
        if (cam != null)
        {
            flashlight = cam.GetComponentInChildren<Light>();
            if (flashlight != null)
            {
                flashIntensity = flashlight.intensity;
                flashlight.intensity = 0.06f;
            }
        }

        EnsureOverlay();
        _vignette.color = new Color(0.08f, 0f, 0f, 0f);

        if (cam == null)
        {
            health.CompleteCatchDeath();
            yield break;
        }

        Transform camTransform = cam.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;
        float startFov = cam.fieldOfView;
        float startNearClip = cam.nearClipPlane;
        cam.nearClipPlane = nearClipPlane;

        camTransform.SetParent(null, true);

        float pullElapsed = 0f;
        while (pullElapsed < pullDuration)
        {
            pullElapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, pullElapsed / pullDuration);
            GetFaceFrame(attacker, out _, out Vector3 camTarget, out Quaternion camLook);

            camTransform.position = Vector3.Lerp(startPos, camTarget, t);
            camTransform.rotation = Quaternion.Slerp(startRot, camLook, t);
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
            _vignette.color = new Color(0.12f, 0f, 0.02f, t * 0.5f);

            yield return null;
        }

        float holdElapsed = 0f;
        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            GetFaceFrame(attacker, out _, out Vector3 camTarget, out Quaternion camLook);

            float shake = Mathf.Sin(Time.time * 39f) * 0.011f + Mathf.Sin(Time.time * 67f) * 0.007f;
            Vector3 shakeOffset = camLook * Vector3.right * shake + Vector3.up * shake * 0.35f;
            camTransform.position = camTarget + shakeOffset;
            camTransform.rotation = camLook;
            cam.fieldOfView = targetFov + Mathf.Sin(Time.time * 2.4f) * 0.8f;

            float pulse = 0.52f + Mathf.Sin(holdElapsed * 5.5f) * 0.08f;
            _vignette.color = new Color(0.2f, 0.01f, 0.02f, pulse);

            yield return null;
        }

        if (flashlight != null)
            flashlight.intensity = flashIntensity;

        health.CompleteCatchDeath();
    }

    private void GetFaceFrame(MonsterAI attacker, out Vector3 facePoint, out Vector3 camPos, out Quaternion camRot)
    {
        facePoint = GetFacePoint(attacker);

        Vector3 forward = attacker != null ? attacker.transform.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        camPos = facePoint + forward * faceDistance + Vector3.up * cameraHeightOffset;
        Vector3 lookTarget = facePoint + Vector3.up * lookHeightOffset;
        camRot = Quaternion.LookRotation(lookTarget - camPos, Vector3.up);
    }

    private Vector3 GetFacePoint(MonsterAI attacker)
    {
        if (attacker == null)
            return Vector3.zero;

        var smilerFace = attacker.GetComponent<SmilerFaceAnchor>();
        if (smilerFace != null)
            return smilerFace.FacePoint;

        var animator = attacker.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                return head.position;
        }

        var headBone = FindNamedTransform(attacker.transform, "pumpkinHead", "Character1_Head", "head", "Head", "Head 1");
        if (headBone != null)
            return headBone.position;

        float scale = _entityScaleOverride > 0f ? _entityScaleOverride : EntityVisualSetup.VisualScale;
        return attacker.transform.position + Vector3.up * (1.55f * scale);
    }

    private static Transform FindNamedTransform(Transform root, params string[] names)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in names)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }
        }

        return null;
    }

    private void EnsureOverlay()
    {
        if (_vignette != null) return;

        var canvasGo = new GameObject("CatchOverlay");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _vignette = GameUIBuilder.CreateFadeOverlay(canvasGo.transform);
    }
}
