# Horror Escape Game — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 2.5D Android 공포 탈출 게임 MVP — 1맵, 크리쳐 1종, 리틀나이트메어 스타일

**Architecture:** 씬 분리 방식 (MainMenu → Map_01 → ComingSoon). DontDestroyOnLoad GameManager/SceneTransitioner로 씬 간 상태 유지. NavMesh 기반 몬스터 3-상태 AI (Patrol/Chase/Search). Rigidbody 기반 2.5D 플레이어 이동.

**Tech Stack:** Unity 2022 LTS (3D URP), NavMesh, Unity Test Framework (NUnit EditMode), TextMeshPro, Android IL2CPP

---

## File Map

```
Assets/
├── Scripts/
│   ├── GameState.cs                  # 순수 C# 상태 로직 (목숨, 열쇠) — 테스트용
│   ├── GameManager.cs                # 싱글턴, DontDestroyOnLoad, 씬 전환 트리거
│   ├── Utility/
│   │   ├── SceneTransitioner.cs      # 페이드 인/아웃 + SceneManager.LoadScene
│   │   └── CameraFollow.cs           # X축만 플레이어 추적, Y/Z 고정
│   ├── Player/
│   │   ├── PlayerController.cs       # Rigidbody 2.5D 이동 (X + Z축)
│   │   ├── PlayerHealth.cs           # 피격 → TakeDamage → GameManager.PlayerDied
│   │   └── PlayerInteraction.cs      # 트리거 감지 → IInteractable 호출
│   ├── Monster/
│   │   ├── MonsterVision.cs          # FOV 부채꼴 감지 (OverlapSphere + Raycast)
│   │   └── MonsterAI.cs              # 상태 머신 (Patrol/Chase/Search) + NavMeshAgent
│   ├── Map/
│   │   ├── IInteractable.cs          # 인터페이스
│   │   ├── KeyItem.cs                # 줍기 → GameManager.PickUpKey
│   │   ├── LockObject.cs             # 열쇠 사용 → ExitDoor.Unlock
│   │   └── ExitDoor.cs               # 잠금 해제 후 → GameManager.CompleteMap
│   └── UI/
│       ├── VirtualJoystick.cs        # 터치 다이나믹 조이스틱
│       ├── HUDManager.cs             # 하트, 열쇠 아이콘, 상호작용 버튼
│       ├── MinimapController.cs      # 아이콘 위치 업데이트
│       ├── MainMenuUI.cs             # 시작/종료 버튼
│       └── ComingSoonUI.cs           # 메인메뉴 복귀 버튼
├── Tests/
│   └── EditMode/
│       ├── Tests.EditMode.asmdef
│       ├── GameStateTests.cs
│       └── MonsterVisionTests.cs
├── Scenes/
│   ├── MainMenu.unity
│   ├── Map_01.unity
│   └── ComingSoon.unity
└── Prefabs/
    ├── Player.prefab
    ├── Monster_Map01.prefab
    ├── SceneTransitioner.prefab
    ├── Items/
    │   ├── Key.prefab
    │   ├── Lock.prefab
    │   └── ExitDoor.prefab
    └── UI/
        └── HUD.prefab
```

---

## Task 1: Unity 프로젝트 셋업

**Files:**
- Create: `Assets/Scenes/MainMenu.unity`
- Create: `Assets/Scenes/Map_01.unity`
- Create: `Assets/Scenes/ComingSoon.unity`

- [ ] **Step 1: Unity 2022 LTS에서 새 3D (URP) 프로젝트 생성**

Unity Hub → New Project → **3D (URP)** 템플릿 선택 → 프로젝트명: `HorrorEscapeGame` → 경로: 이 레포 디렉토리

- [ ] **Step 2: 씬 3개 생성**

File → New Scene (Basic URP) 으로 3개 씬을 `Assets/Scenes/`에 저장:
- `MainMenu.unity`
- `Map_01.unity`
- `ComingSoon.unity`

- [ ] **Step 3: Build Settings에 씬 등록**

File → Build Settings → Add Open Scenes로 순서대로 추가:
- index 0: `MainMenu`
- index 1: `Map_01`
- index 2: `ComingSoon`

- [ ] **Step 4: Android 플랫폼 전환**

File → Build Settings → Android 선택 → **Switch Platform**

Edit → Project Settings → Player → Android 탭:
- Minimum API Level: `Android 8.0 (API Level 26)`
- Scripting Backend: `IL2CPP`
- Target Architectures: `ARMv7` + `ARM64` 체크

- [ ] **Step 5: 폴더 구조 생성**

Project 창 우클릭 → Create Folder로 아래 폴더 생성:
```
Assets/Scripts/Utility
Assets/Scripts/Player
Assets/Scripts/Monster
Assets/Scripts/Map
Assets/Scripts/UI
Assets/Tests/EditMode
Assets/Prefabs/Items
Assets/Prefabs/UI
```

- [ ] **Step 6: 태그 등록**

Edit → Project Settings → Tags and Layers → Tags:
- `Player` 추가
- `Monster` 추가

- [ ] **Step 7: Commit**

```bash
git add Assets/ ProjectSettings/
git commit -m "feat: unity project setup with android build config"
```

---

## Task 2: GameState (순수 C# 로직 + 테스트)

**Files:**
- Create: `Assets/Scripts/GameState.cs`
- Create: `Assets/Tests/EditMode/Tests.EditMode.asmdef`
- Create: `Assets/Tests/EditMode/GameStateTests.cs`

- [ ] **Step 1: GameState.cs 작성**

```csharp
// Assets/Scripts/GameState.cs
using System;

public class GameState
{
    public const int MaxLives = 3;

    public int Lives { get; private set; } = MaxLives;
    public bool HasKey { get; private set; } = false;

    // Returns true if lives reached 0 (game over)
    public bool LoseLife()
    {
        Lives = Math.Max(0, Lives - 1);
        return Lives == 0;
    }

    public void Reset()
    {
        Lives = MaxLives;
        HasKey = false;
    }

    public void PickUpKey() => HasKey = true;
    public void UseKey() => HasKey = false;
}
```

- [ ] **Step 2: Test Assembly Definition 생성**

`Assets/Tests/EditMode/` 폴더 우클릭 → Create → Assembly Definition → 이름: `Tests.EditMode`

Inspector에서 설정:
- Platforms: `Editor` 만 체크
- Test Assemblies: 체크
- Override References: 체크 → Precompiled References에 `nunit.framework.dll` 추가

저장 후 Unity가 컴파일 완료될 때까지 대기.

- [ ] **Step 3: GameStateTests.cs 작성**

```csharp
// Assets/Tests/EditMode/GameStateTests.cs
using NUnit.Framework;

public class GameStateTests
{
    [Test]
    public void StartsWith3Lives()
    {
        var state = new GameState();
        Assert.AreEqual(3, state.Lives);
    }

    [Test]
    public void LoseLife_DecrementsLives()
    {
        var state = new GameState();
        bool gameOver = state.LoseLife();
        Assert.AreEqual(2, state.Lives);
        Assert.IsFalse(gameOver);
    }

    [Test]
    public void LoseAllLives_ReturnsTrue()
    {
        var state = new GameState();
        state.LoseLife();
        state.LoseLife();
        bool gameOver = state.LoseLife();
        Assert.AreEqual(0, state.Lives);
        Assert.IsTrue(gameOver);
    }

    [Test]
    public void LivesNeverGoBelowZero()
    {
        var state = new GameState();
        state.LoseLife(); state.LoseLife(); state.LoseLife();
        state.LoseLife(); // 4번째
        Assert.AreEqual(0, state.Lives);
    }

    [Test]
    public void Reset_Restores3LivesAndNoKey()
    {
        var state = new GameState();
        state.LoseLife();
        state.PickUpKey();
        state.Reset();
        Assert.AreEqual(3, state.Lives);
        Assert.IsFalse(state.HasKey);
    }

    [Test]
    public void PickUpKey_SetsHasKeyTrue()
    {
        var state = new GameState();
        state.PickUpKey();
        Assert.IsTrue(state.HasKey);
    }

    [Test]
    public void UseKey_SetsHasKeyFalse()
    {
        var state = new GameState();
        state.PickUpKey();
        state.UseKey();
        Assert.IsFalse(state.HasKey);
    }
}
```

- [ ] **Step 4: 테스트 실행 — 7개 모두 통과 확인**

Window → General → Test Runner → EditMode 탭 → Run All
예상: 7개 테스트 모두 초록 (PASS)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/GameState.cs Assets/Tests/
git commit -m "feat: GameState pure logic with 7 passing unit tests"
```

---

## Task 3: GameManager

**Files:**
- Create: `Assets/Scripts/GameManager.cs`

- [ ] **Step 1: GameManager.cs 작성**

```csharp
// Assets/Scripts/GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private GameState _state = new GameState();

    public int Lives => _state.Lives;
    public bool HasKey => _state.HasKey;

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

    public void PlayerDied()
    {
        bool gameOver = _state.LoseLife();
        HUDManager.Instance?.UpdateHearts(_state.Lives);

        if (gameOver)
        {
            _state.Reset();
            SceneTransitioner.Instance.LoadScene("Map_01");
        }
        else
        {
            // 현재 맵 처음부터 재시작
            SceneTransitioner.Instance.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void PickUpKey()
    {
        _state.PickUpKey();
        HUDManager.Instance?.UpdateKeyDisplay(true);
    }

    public void UseKey()
    {
        _state.UseKey();
        HUDManager.Instance?.UpdateKeyDisplay(false);
    }

    public void CompleteMap()
    {
        _state.UseKey();
        SceneTransitioner.Instance.LoadScene("ComingSoon");
    }
}
```

- [ ] **Step 2: MainMenu 씬에 GameManager 배치**

MainMenu.unity 씬 열기 → Hierarchy 우클릭 → Create Empty → 이름: `GameManager` → Inspector에서 GameManager 컴포넌트 추가

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/GameManager.cs Assets/Scenes/MainMenu.unity
git commit -m "feat: GameManager singleton with DontDestroyOnLoad"
```

---

## Task 4: SceneTransitioner (페이드 인/아웃)

**Files:**
- Create: `Assets/Scripts/Utility/SceneTransitioner.cs`

- [ ] **Step 1: SceneTransitioner.cs 작성**

```csharp
// Assets/Scripts/Utility/SceneTransitioner.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance { get; private set; }

    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(FadeIn());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(sceneName);
        yield return StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color c = Color.black;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Color c = Color.black;
        c.a = 1f;
        fadePanel.color = c;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }
    }
}
```

- [ ] **Step 2: MainMenu 씬에 SceneTransitioner 설정**

MainMenu.unity에서:
1. Hierarchy 우클릭 → Create Empty → 이름: `SceneTransitioner` → SceneTransitioner 컴포넌트 추가
2. Hierarchy → UI → Canvas 생성:
   - Render Mode: Screen Space - Overlay
   - Sort Order: 99 (항상 최상단)
3. Canvas 하위에 UI → Image 생성 → 이름: `FadePanel`:
   - Anchor: stretch 전체 (Alt+클릭으로 사방 stretch 선택)
   - Color: (0, 0, 0, 0) — 검정 투명
4. SceneTransitioner 컴포넌트의 `Fade Panel` 필드에 FadePanel Image 드래그 연결
5. `Assets/Prefabs/SceneTransitioner.prefab`으로 저장

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Utility/SceneTransitioner.cs Assets/Prefabs/SceneTransitioner.prefab Assets/Scenes/MainMenu.unity
git commit -m "feat: SceneTransitioner with black fade in/out coroutine"
```

---

## Task 5: IInteractable + 맵 아이템 (열쇠, 자물쇠, 출구)

**Files:**
- Create: `Assets/Scripts/Map/IInteractable.cs`
- Create: `Assets/Scripts/Map/KeyItem.cs`
- Create: `Assets/Scripts/Map/LockObject.cs`
- Create: `Assets/Scripts/Map/ExitDoor.cs`

- [ ] **Step 1: IInteractable.cs 작성**

```csharp
// Assets/Scripts/Map/IInteractable.cs
public interface IInteractable
{
    bool CanInteract();
    void Interact();
    string GetInteractionLabel();
}
```

- [ ] **Step 2: KeyItem.cs 작성**

```csharp
// Assets/Scripts/Map/KeyItem.cs
using UnityEngine;

public class KeyItem : MonoBehaviour, IInteractable
{
    public bool CanInteract() => true;

    public void Interact()
    {
        GameManager.Instance.PickUpKey();
        MinimapController.Instance?.HideKeyIcon();
        gameObject.SetActive(false);
    }

    public string GetInteractionLabel() => "열쇠 줍기";
}
```

- [ ] **Step 3: LockObject.cs 작성**

```csharp
// Assets/Scripts/Map/LockObject.cs
using UnityEngine;

public class LockObject : MonoBehaviour, IInteractable
{
    [SerializeField] private ExitDoor linkedDoor;

    public bool CanInteract() => GameManager.Instance.HasKey;

    public void Interact()
    {
        if (!GameManager.Instance.HasKey) return;
        GameManager.Instance.UseKey();
        linkedDoor.Unlock();
        MinimapController.Instance?.HideLockIcon();
        gameObject.SetActive(false);
    }

    public string GetInteractionLabel() =>
        GameManager.Instance.HasKey ? "자물쇠 해제" : "열쇠 필요";
}
```

- [ ] **Step 4: ExitDoor.cs 작성**

```csharp
// Assets/Scripts/Map/ExitDoor.cs
using UnityEngine;

public class ExitDoor : MonoBehaviour, IInteractable
{
    private bool _isUnlocked = false;

    public bool CanInteract() => _isUnlocked;

    public void Unlock()
    {
        _isUnlocked = true;
        // 잠금 해제 시각 피드백: 머티리얼 색상 초록으로 변경
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.green;
    }

    public void Interact()
    {
        if (!_isUnlocked) return;
        GameManager.Instance.CompleteMap();
    }

    public string GetInteractionLabel() => _isUnlocked ? "탈출" : "잠겨있음";
}
```

- [ ] **Step 5: 아이템 Prefab 3개 생성**

Map_01.unity 씬에서 임시로 오브젝트 생성 후 Prefab 저장:

**Key Prefab:**
- 3D Object → Cube → 이름: `Key`, Scale: (0.3, 0.3, 0.3), Color: 노랑
- BoxCollider → IsTrigger: true
- KeyItem 컴포넌트 추가
- `Assets/Prefabs/Items/Key.prefab` 저장

**Lock Prefab:**
- 3D Object → Cube → 이름: `Lock`, Scale: (0.5, 0.5, 0.3), Color: 빨강
- BoxCollider → IsTrigger: true
- LockObject 컴포넌트 추가
- `Assets/Prefabs/Items/Lock.prefab` 저장

**ExitDoor Prefab:**
- 3D Object → Cube → 이름: `ExitDoor`, Scale: (1, 2, 0.3), Color: 회색
- BoxCollider → IsTrigger: true
- ExitDoor 컴포넌트 추가
- `Assets/Prefabs/Items/ExitDoor.prefab` 저장

- [ ] **Step 6: Lock → ExitDoor 연결**

Map_01 씬에서 Lock 오브젝트의 LockObject 컴포넌트 `Linked Door` 필드에 ExitDoor 드래그

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Map/ Assets/Prefabs/Items/
git commit -m "feat: IInteractable interface and map items key/lock/exitdoor"
```

---

## Task 6: CameraFollow

**Files:**
- Create: `Assets/Scripts/Utility/CameraFollow.cs`

- [ ] **Step 1: CameraFollow.cs 작성**

```csharp
// Assets/Scripts/Utility/CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothSpeed = 5f;

    // Start 시점의 Y, Z를 고정값으로 사용
    // 씬 배치 시 카메라 Y, Z를 원하는 위치에 두면 그 값으로 고정
    private float _fixedY;
    private float _fixedZ;

    private void Start()
    {
        _fixedY = transform.position.y;
        _fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(target.position.x, _fixedY, _fixedZ);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
```

- [ ] **Step 2: Map_01 씬 카메라 설정**

Map_01.unity의 Main Camera:
- Position: `(0, 8, -12)` — 맵 중앙, 위에서 약간 비스듬히
- Rotation: `(20, 0, 0)` — 살짝 아래를 바라봄
- CameraFollow 컴포넌트 추가
- Target 필드는 Task 8 Player 배치 후 연결

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Utility/CameraFollow.cs Assets/Scenes/Map_01.unity
git commit -m "feat: CameraFollow tracks player X axis, Y/Z fixed"
```

---

## Task 7: VirtualJoystick

**Files:**
- Create: `Assets/Scripts/UI/VirtualJoystick.cs`

- [ ] **Step 1: VirtualJoystick.cs 작성**

```csharp
// Assets/Scripts/UI/VirtualJoystick.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float maxHandleDistance = 60f;

    private Vector2 _input = Vector2.zero;
    private Canvas _canvas;

    public Vector2 Input => _input;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        background.gameObject.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            _canvas.worldCamera,
            out Vector2 localPoint);
        background.anchoredPosition = localPoint;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            _canvas.worldCamera,
            out Vector2 localPoint);

        _input = localPoint.magnitude > maxHandleDistance
            ? localPoint.normalized
            : localPoint / maxHandleDistance;

        handle.anchoredPosition = _input * maxHandleDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
        background.gameObject.SetActive(false);
    }
}
```

- [ ] **Step 2: Map_01 씬에 UI Canvas + Joystick 구성**

Map_01.unity에 Canvas 생성:
- Render Mode: Screen Space - Camera
- Render Camera: Main Camera
- Plane Distance: 1

Canvas 하위 UI 계층:
```
Canvas
└── JoystickArea (Image, alpha=0, 화면 좌측 하단 절반)
    ├── JoystickBackground (Image, 원형 Sprite, 120x120)
    └── JoystickHandle (Image, 원형 Sprite, 60x60)
```

JoystickArea 설정:
- Anchor: Bottom-Left, Pivot: (0, 0)
- Width: 540 (화면 절반), Height: 540
- VirtualJoystick 컴포넌트 추가 → Background/Handle 필드 연결

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/VirtualJoystick.cs Assets/Scenes/Map_01.unity
git commit -m "feat: dynamic virtual joystick with touch drag input"
```

---

## Task 8: PlayerController + PlayerHealth

**Files:**
- Create: `Assets/Scripts/Player/PlayerController.cs`
- Create: `Assets/Scripts/Player/PlayerHealth.cs`

- [ ] **Step 1: PlayerController.cs 작성**

```csharp
// Assets/Scripts/Player/PlayerController.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float depthSpeedMultiplier = 0.6f; // Z축은 살짝 느리게

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        Vector2 input = joystick.Input;
        _rb.velocity = new Vector3(
            input.x * moveSpeed,
            _rb.velocity.y,                             // 중력 유지
            input.y * moveSpeed * depthSpeedMultiplier  // 앞뒤는 살짝 느리게
        );
    }
}
```

- [ ] **Step 2: PlayerHealth.cs 작성**

```csharp
// Assets/Scripts/Player/PlayerHealth.cs
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private bool _isDead = false;

    public void TakeDamage()
    {
        if (_isDead) return;
        _isDead = true;
        GetComponent<PlayerController>().enabled = false;
        GetComponent<PlayerInteraction>().enabled = false;
        GameManager.Instance.PlayerDied();
    }
}
```

- [ ] **Step 3: Player Prefab 생성**

Map_01.unity에서:
1. Create Empty → 이름: `Player`, Tag: `Player`
2. 자식으로 Capsule 추가 (임시 형태, Height: 1.8)
3. Player 루트에 컴포넌트 추가:
   - `Rigidbody` (Mass: 1, Use Gravity: true)
   - `CapsuleCollider` (Height: 1.8, Radius: 0.3)
   - `PlayerController` (Move Speed: 5, Depth Speed Multiplier: 0.6)
   - `PlayerHealth`
4. PlayerController의 Joystick 필드 → VirtualJoystick 연결
5. CameraFollow의 Target → Player Transform 연결
6. `Assets/Prefabs/Player.prefab` 저장

- [ ] **Step 4: Play 테스트 — 이동 확인**

Map_01 씬 Play → 조이스틱 드래그 시 Player가 X+Z 방향으로 이동, 카메라가 X축 추적 확인

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Player/PlayerController.cs Assets/Scripts/Player/PlayerHealth.cs Assets/Prefabs/Player.prefab
git commit -m "feat: PlayerController rigidbody 2.5D movement and PlayerHealth"
```

---

## Task 9: PlayerInteraction

**Files:**
- Create: `Assets/Scripts/Player/PlayerInteraction.cs`

- [ ] **Step 1: PlayerInteraction.cs 작성**

```csharp
// Assets/Scripts/Player/PlayerInteraction.cs
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private IInteractable _currentInteractable;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            _currentInteractable = interactable;
            HUDManager.Instance?.ShowInteractionButton(
                interactable.CanInteract(),
                interactable.GetInteractionLabel());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 열쇠 습득 후 같은 프레임에 LockObject CanInteract()가 true가 되는 경우 처리
        if (_currentInteractable != null && other.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            HUDManager.Instance?.ShowInteractionButton(
                interactable.CanInteract(),
                interactable.GetInteractionLabel());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out _))
        {
            _currentInteractable = null;
            HUDManager.Instance?.ShowInteractionButton(false, "");
        }
    }

    public void TryInteract()
    {
        if (_currentInteractable == null) return;
        if (_currentInteractable.CanInteract())
            _currentInteractable.Interact();
    }
}
```

- [ ] **Step 2: Player에 InteractionZone 추가**

Player Prefab 열기:
1. 자식으로 Create Empty → 이름: `InteractionZone`
2. SphereCollider 추가 → Radius: 1.5, IsTrigger: true
3. Player 루트에 PlayerInteraction 컴포넌트 추가

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Player/PlayerInteraction.cs Assets/Prefabs/Player.prefab
git commit -m "feat: PlayerInteraction trigger-based item detection"
```

---

## Task 10: MonsterVision + 테스트

**Files:**
- Create: `Assets/Scripts/Monster/MonsterVision.cs`
- Create: `Assets/Tests/EditMode/MonsterVisionTests.cs`

- [ ] **Step 1: MonsterVision.cs 작성**

```csharp
// Assets/Scripts/Monster/MonsterVision.cs
using UnityEngine;

public class MonsterVision : MonoBehaviour
{
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    public bool CanSeePlayer(out Transform target)
    {
        target = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        foreach (Collider hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;

            if (!IsInFieldOfView(transform.forward, dirToTarget, viewAngle))
                continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            if (!Physics.Raycast(transform.position, dirToTarget, distance, obstacleMask))
            {
                target = hit.transform;
                return true;
            }
        }
        return false;
    }

    // Physics 없이 순수 각도 계산 — 테스트 가능
    public static bool IsInFieldOfView(Vector3 monsterForward, Vector3 dirToTarget, float viewAngle)
    {
        float angle = Vector3.Angle(monsterForward, dirToTarget.normalized);
        return angle < viewAngle / 2f;
    }
}
```

- [ ] **Step 2: MonsterVisionTests.cs 작성**

```csharp
// Assets/Tests/EditMode/MonsterVisionTests.cs
using NUnit.Framework;
using UnityEngine;

public class MonsterVisionTests
{
    [Test]
    public void PlayerDirectlyAhead_IsInFOV()
    {
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, Vector3.forward, 90f);
        Assert.IsTrue(result);
    }

    [Test]
    public void PlayerDirectlyBehind_IsNotInFOV()
    {
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, Vector3.back, 90f);
        Assert.IsFalse(result);
    }

    [Test]
    public void PlayerAt44Degrees_IsInsideFOV90()
    {
        Vector3 dir = Quaternion.Euler(0, 44, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 90f);
        Assert.IsTrue(result);
    }

    [Test]
    public void PlayerAt46Degrees_IsOutsideFOV90()
    {
        Vector3 dir = Quaternion.Euler(0, 46, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 90f);
        Assert.IsFalse(result);
    }

    [Test]
    public void PlayerAt89Degrees_IsInsideFOV180()
    {
        Vector3 dir = Quaternion.Euler(0, 89, 0) * Vector3.forward;
        bool result = MonsterVision.IsInFieldOfView(Vector3.forward, dir, 180f);
        Assert.IsTrue(result);
    }
}
```

- [ ] **Step 3: 테스트 실행 — 5개 모두 통과**

Window → Test Runner → EditMode → Run All
예상: 전체 12개 (Task 2 7개 + Task 10 5개) 모두 PASS

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Monster/MonsterVision.cs Assets/Tests/EditMode/MonsterVisionTests.cs
git commit -m "feat: MonsterVision FOV detection with 5 unit tests"
```

---

## Task 11: MonsterAI (NavMesh 순찰/추격/수색)

**Files:**
- Create: `Assets/Scripts/Monster/MonsterAI.cs`

- [ ] **Step 1: MonsterAI.cs 작성**

```csharp
// Assets/Scripts/Monster/MonsterAI.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterVision))]
public class MonsterAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Search }

    [Header("Speed")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;

    [Header("Search")]
    [SerializeField] private float searchDuration = 3f;

    private NavMeshAgent _agent;
    private MonsterVision _vision;
    private State _currentState = State.Patrol;
    private int _waypointIndex = 0;
    private float _searchTimer = 0f;
    private Vector3 _lastSeenPosition;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _vision = GetComponent<MonsterVision>();
    }

    private void Start()
    {
        if (waypoints.Length > 0)
            _agent.SetDestination(waypoints[0].position);
    }

    private void Update()
    {
        switch (_currentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase:  UpdateChase();  break;
            case State.Search: UpdateSearch(); break;
        }
    }

    private void UpdatePatrol()
    {
        _agent.speed = patrolSpeed;
        if (_vision.CanSeePlayer(out _)) { _currentState = State.Chase; return; }

        if (!_agent.pathPending && _agent.remainingDistance < 0.5f && waypoints.Length > 0)
        {
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            _agent.SetDestination(waypoints[_waypointIndex].position);
        }
    }

    private void UpdateChase()
    {
        _agent.speed = chaseSpeed;
        if (_vision.CanSeePlayer(out Transform player))
        {
            _lastSeenPosition = player.position;
            _agent.SetDestination(player.position);
        }
        else
        {
            _agent.SetDestination(_lastSeenPosition);
            _searchTimer = searchDuration;
            _currentState = State.Search;
        }
    }

    private void UpdateSearch()
    {
        _agent.speed = patrolSpeed;
        if (_vision.CanSeePlayer(out _)) { _currentState = State.Chase; return; }

        _searchTimer -= Time.deltaTime;
        if (_searchTimer <= 0f) _currentState = State.Patrol;
    }

    // 몬스터 자식 AttackZone 오브젝트의 Trigger Collider가 플레이어와 접촉 시 호출
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerHealth>()?.TakeDamage();
    }
}
```

- [ ] **Step 2: Monster Prefab 생성**

Map_01.unity에서:
1. Create Empty → 이름: `Monster_Map01`, Tag: `Monster`
2. 자식으로 Capsule 추가 (Height: 2, Scale: 1x1x1), Color: 어두운 보라
3. 루트에 컴포넌트 추가:
   - `NavMeshAgent` (Speed: 2, Stopping Distance: 0.5, Angular Speed: 360)
   - `MonsterVision` (View Radius: 10, View Angle: 90)
   - `MonsterAI` (Patrol Speed: 2, Chase Speed: 5, Search Duration: 3)
4. 자식으로 Create Empty → 이름: `AttackZone`:
   - SphereCollider (Radius: 0.8, IsTrigger: true)
   - MonsterAI는 루트에 있으므로 AttackZone의 OnTriggerEnter 대신 루트 MonsterAI의 OnTriggerEnter가 작동
5. MonsterVision 설정:
   - Target Mask: `Player` 레이어 선택
   - Obstacle Mask: `Default` 레이어 선택
6. `Assets/Prefabs/Monster_Map01.prefab` 저장

- [ ] **Step 3: 웨이포인트 배치**

Map_01 씬에서:
1. Create Empty × 4 → 이름: `WP_01`, `WP_02`, `WP_03`, `WP_04`
2. 맵 네 구석에 배치 (몬스터가 맵 전체를 순찰하도록)
3. MonsterAI의 Waypoints 배열에 WP_01~04 순서대로 연결

- [ ] **Step 4: NavMesh Bake**

1. 맵 바닥(Plane)과 벽(Cube) 오브젝트 선택 → Inspector → Static 드롭다운 → `Navigation Static` 체크
2. Window → AI → Navigation → Bake 탭 → **Bake** 클릭
3. 씬 뷰에 파란 NavMesh 오버레이 확인

- [ ] **Step 5: Play 테스트**

Map_01 씬 Play → 아래 확인:
1. Monster가 WP_01 → WP_02 → ... 순찰
2. Player를 10 유닛 이내로 접근 시 Chase 전환 (달리기)
3. Player가 몬스터 시야에서 벗어나면 Search → Patrol 복귀
4. Monster가 Player에 닿으면 씬 리로드 (목숨 1 차감)

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Monster/ Assets/Prefabs/Monster_Map01.prefab Assets/Scenes/Map_01.unity
git commit -m "feat: MonsterAI state machine Patrol/Chase/Search with NavMesh"
```

---

## Task 12: HUDManager + MinimapController

**Files:**
- Create: `Assets/Scripts/UI/HUDManager.cs`
- Create: `Assets/Scripts/UI/MinimapController.cs`

- [ ] **Step 1: HUDManager.cs 작성**

```csharp
// Assets/Scripts/UI/HUDManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Hearts")]
    [SerializeField] private Image[] heartImages;  // 3개
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartEmpty;

    [Header("Key")]
    [SerializeField] private GameObject keyIndicator;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionButton;
    [SerializeField] private TMP_Text interactionLabel;

    private void Awake()
    {
        Instance = this;
        int lives = GameManager.Instance != null ? GameManager.Instance.Lives : 3;
        bool hasKey = GameManager.Instance != null && GameManager.Instance.HasKey;
        UpdateHearts(lives);
        UpdateKeyDisplay(hasKey);
        ShowInteractionButton(false, "");
    }

    public void UpdateHearts(int lives)
    {
        for (int i = 0; i < heartImages.Length; i++)
            heartImages[i].sprite = i < lives ? heartFull : heartEmpty;
    }

    public void UpdateKeyDisplay(bool hasKey)
    {
        keyIndicator.SetActive(hasKey);
    }

    public void ShowInteractionButton(bool show, string label)
    {
        interactionButton.SetActive(show);
        if (interactionLabel != null)
            interactionLabel.text = label;
    }
}
```

- [ ] **Step 2: MinimapController.cs 작성**

```csharp
// Assets/Scripts/UI/MinimapController.cs
using UnityEngine;

public class MinimapController : MonoBehaviour
{
    public static MinimapController Instance { get; private set; }

    [Header("Icons (RectTransform)")]
    [SerializeField] private RectTransform playerIcon;
    [SerializeField] private RectTransform keyIcon;
    [SerializeField] private RectTransform lockIcon;
    [SerializeField] private RectTransform exitIcon;

    [Header("World Targets")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform keyTransform;
    [SerializeField] private Transform lockTransform;
    [SerializeField] private Transform exitTransform;

    [Header("Map Bounds")]
    [SerializeField] private float mapWorldWidth = 50f;
    [SerializeField] private float mapWorldHeight = 10f;
    [SerializeField] private Vector2 mapWorldCenter = Vector2.zero;
    [SerializeField] private float minimapSize = 180f; // UI 픽셀 단위 미니맵 크기

    private void Awake() => Instance = this;

    private void Update()
    {
        SetIconPos(playerIcon, playerTransform);
        if (keyIcon.gameObject.activeSelf)  SetIconPos(keyIcon, keyTransform);
        if (lockIcon.gameObject.activeSelf) SetIconPos(lockIcon, lockTransform);
        SetIconPos(exitIcon, exitTransform);
    }

    private void SetIconPos(RectTransform icon, Transform worldTarget)
    {
        if (icon == null || worldTarget == null) return;
        float nx = (worldTarget.position.x - mapWorldCenter.x) / mapWorldWidth;
        float ny = (worldTarget.position.z - mapWorldCenter.y) / mapWorldHeight;
        icon.anchoredPosition = new Vector2(nx * minimapSize, ny * minimapSize);
    }

    public void HideKeyIcon()  => keyIcon.gameObject.SetActive(false);
    public void HideLockIcon() => lockIcon.gameObject.SetActive(false);
}
```

- [ ] **Step 3: HUD UI 계층 구성**

Map_01.unity의 Canvas에 추가:

```
Canvas
├── JoystickArea (Task 7)
├── HUD
│   ├── Hearts (Horizontal Layout Group)
│   │   ├── Heart_1 (Image, 40x40)
│   │   ├── Heart_2 (Image, 40x40)
│   │   └── Heart_3 (Image, 40x40)
│   ├── KeyIndicator (Image, 열쇠 아이콘, 초기 비활성)
│   └── InteractionButton (Button, 초기 비활성)
│       └── Label (TMP_Text) "상호작용"
└── Minimap
    ├── MinimapBG (Image, 원형 마스크, 200x200, 우상단 Anchor)
    ├── PlayerIcon (Image, 흰 점, 10x10)
    ├── KeyIcon (Image, 노란 점, 10x10)
    ├── LockIcon (Image, 빨간 점, 10x10)
    └── ExitIcon (Image, 초록 점, 10x10)
```

- HUD 오브젝트에 HUDManager 컴포넌트 추가 → 필드 연결
- Minimap 오브젝트에 MinimapController 컴포넌트 추가 → 필드 연결
- InteractionButton OnClick → Player 오브젝트의 `PlayerInteraction.TryInteract()` 연결

- [ ] **Step 4: Play 테스트**

Map_01 Play:
1. 하트 3개 표시 확인
2. Key 근처 가면 "열쇠 줍기" 버튼 출현
3. 버튼 클릭 → 키 사라짐, HUD 열쇠 아이콘 표시, 미니맵 키 아이콘 사라짐
4. Lock 근처 → "자물쇠 해제" 버튼 → 클릭 → ExitDoor 초록 변경
5. 피격 시 하트 1개 감소 확인

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/UI/HUDManager.cs Assets/Scripts/UI/MinimapController.cs Assets/Scenes/Map_01.unity
git commit -m "feat: HUDManager hearts/key/interaction and MinimapController"
```

---

## Task 13: Map_01 씬 완성

**Files:**
- Modify: `Assets/Scenes/Map_01.unity`

- [ ] **Step 1: 맵 구조물 배치**

Map_01.unity에서 3D 오브젝트로 기본 맵 구성:

| 오브젝트 | Position | Scale | 역할 |
|---------|---------|-------|------|
| Floor | (0, 0, 0) | (50, 0.2, 10) | 바닥 |
| WallLeft | (-25, 2, 0) | (0.5, 4, 10) | 왼쪽 벽 |
| WallRight | (25, 2, 0) | (0.5, 4, 10) | 오른쪽 벽 |
| WallBack | (0, 2, -5) | (50, 4, 0.5) | 뒷벽 |
| WallFront | (0, 2, 5) | (50, 4, 0.5) | 앞벽 |
| Obstacle_1 | (-10, 1, 0) | (2, 2, 2) | 장애물 |
| Obstacle_2 | (5, 1, 2) | (3, 2, 1) | 장애물 |
| Obstacle_3 | (15, 1, -2) | (2, 2, 3) | 장애물 |

- [ ] **Step 2: 아이템 배치**

| 아이템 | Position | 비고 |
|--------|---------|------|
| Key | (-15, 0.5, 0) | 맵 왼쪽 중간 |
| Lock | (20, 0.5, 0) | 출구 문 앞 |
| ExitDoor | (24, 1, 0) | 맵 오른쪽 끝 |

Lock의 `Linked Door` 필드 → ExitDoor 연결 확인

- [ ] **Step 3: GameManager, SceneTransitioner Map_01 단독 실행 보장**

Map_01.unity에도 GameManager, SceneTransitioner Prefab 배치:
- Awake의 `Instance != null` 체크로 중복 시 자신을 Destroy → 문제없음
- 이렇게 하면 Map_01 씬 단독으로 Play 버튼 눌러도 동작

- [ ] **Step 4: NavMesh 최종 Bake**

모든 구조물(Floor, Wall, Obstacle)에 Navigation Static 체크 후 Window → AI → Navigation → Bake

- [ ] **Step 5: MinimapController 바운드 설정**

MinimapController 컴포넌트:
- Map World Width: 50
- Map World Height: 10
- Map World Center: (0, 0)
- Player/Key/Lock/Exit Transform 필드 각 오브젝트 연결

- [ ] **Step 6: 전체 플레이 시나리오 테스트**

Map_01 Play → 아래 순서 전부 확인:
1. 조이스틱으로 X+Z 이동
2. Monster 시야 진입 → Chase (빠른 이동)
3. 숨으면 Search → Patrol 복귀
4. Key 줍기 → HUD 키 표시, 미니맵 키 아이콘 사라짐
5. Lock 해제 → ExitDoor 초록 변경, 미니맵 자물쇠 아이콘 사라짐
6. ExitDoor 상호작용 → 페이드 아웃 (ComingSoon은 Task 15)
7. 피격 → 하트 감소 → 씬 리로드
8. 하트 0 → Map_01 재시작 + 하트 3개

- [ ] **Step 7: Commit**

```bash
git add Assets/Scenes/Map_01.unity
git commit -m "feat: Map_01 complete scene with map layout, items, monster, HUD"
```

---

## Task 14: MainMenu 씬

**Files:**
- Create: `Assets/Scripts/UI/MainMenuUI.cs`
- Modify: `Assets/Scenes/MainMenu.unity`

- [ ] **Step 1: MainMenuUI.cs 작성**

```csharp
// Assets/Scripts/UI/MainMenuUI.cs
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneTransitioner.Instance.LoadScene("Map_01");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
```

- [ ] **Step 2: MainMenu UI 구성**

MainMenu.unity:
```
Canvas (Screen Space - Overlay)
├── Background (Image, 전체화면, 어두운 색)
├── Title (TMP_Text) — "HORROR ESCAPE", 큰 글씨, 상단
├── StartButton (Button) — "시작하기", 화면 중앙
└── QuitButton (Button) — "종료", 시작하기 아래
```

Canvas에 MainMenuUI 컴포넌트 추가:
- StartButton OnClick → `MainMenuUI.OnStartButton()`
- QuitButton OnClick → `MainMenuUI.OnQuitButton()`

- [ ] **Step 3: GameManager, SceneTransitioner Prefab 배치 확인**

MainMenu.unity에 GameManager와 SceneTransitioner Prefab이 배치되어 있는지 확인 (Task 3, 4에서 추가함)

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/MainMenuUI.cs Assets/Scenes/MainMenu.unity
git commit -m "feat: MainMenu scene with start and quit buttons"
```

---

## Task 15: ComingSoon 씬

**Files:**
- Create: `Assets/Scripts/UI/ComingSoonUI.cs`
- Modify: `Assets/Scenes/ComingSoon.unity`

- [ ] **Step 1: ComingSoonUI.cs 작성**

```csharp
// Assets/Scripts/UI/ComingSoonUI.cs
using UnityEngine;

public class ComingSoonUI : MonoBehaviour
{
    public void OnMainMenuButton()
    {
        SceneTransitioner.Instance.LoadScene("MainMenu");
    }
}
```

- [ ] **Step 2: ComingSoon UI 구성**

ComingSoon.unity:
```
Canvas (Screen Space - Overlay)
├── Background (Image, 전체화면, 검정)
├── ClearText (TMP_Text) — "탈출 성공!", 상단, 흰색
├── Message (TMP_Text) — "추가 맵이 업데이트될 예정입니다.\n기다려주세요!", 중앙
└── MainMenuButton (Button) — "메인 메뉴로"
```

Canvas에 ComingSoonUI 컴포넌트 추가:
- MainMenuButton OnClick → `ComingSoonUI.OnMainMenuButton()`

SceneTransitioner Prefab도 이 씬에 배치 (Awake 중복 처리로 안전)

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/ComingSoonUI.cs Assets/Scenes/ComingSoon.unity
git commit -m "feat: ComingSoon splash screen with main menu return"
```

---

## Task 16: Android 빌드

- [ ] **Step 1: 빌드 설정 최종 확인**

File → Build Settings:
- Platform: Android ✓
- Scenes: MainMenu(0), Map_01(1), ComingSoon(2) ✓

Edit → Project Settings → Player → Android:
- Scripting Backend: IL2CPP ✓
- Target Architectures: ARMv7 + ARM64 ✓
- Internet Access: Not Required

- [ ] **Step 2: APK 빌드**

File → Build Settings → **Build** → 파일명: `HorrorEscapeGame.apk`

빌드 오류 발생 시 Console 탭에서 에러 확인 후 수정

- [ ] **Step 3: 기기 설치**

```bash
adb install -r HorrorEscapeGame.apk
```

- [ ] **Step 4: 기기 시나리오 테스트**

실제 안드로이드 기기에서 아래 확인:
1. 메인메뉴 표시 → "시작하기" 터치 → 페이드 인 후 Map_01
2. 가상 조이스틱 이동 정상 작동
3. 몬스터 AI (배회 → 추격 → 수색)
4. 열쇠 줍기 → 자물쇠 해제 → 출구 탈출 → ComingSoon 씬
5. 피격 하트 감소 → 0개 시 Map_01 재시작

- [ ] **Step 5: Final Commit**

```bash
git add .
git commit -m "feat: MVP complete - android build ready"
```
