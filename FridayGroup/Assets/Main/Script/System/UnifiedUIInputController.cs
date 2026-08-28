using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// すべてのシーンのUI入力をInput Systemへ統一します。
/// シーン上のCanvasやButtonはそのまま使い、入力方式だけを共通化します。
/// </summary>
public sealed class UnifiedUIInputController : MonoBehaviour
{
    private const float SelectedScale = 1.08f;
    private static readonly Color NormalButtonColor = Color.white;
    private static readonly Color SelectedButtonColor = new Color(1f, 0.9f, 0.3f, 1f);
    private static readonly Color PressedButtonColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    private static UnifiedUIInputController instance;
    private Selectable highlightedSelectable;
    private Vector3 highlightedOriginalScale = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateOnce()
    {
        if (instance != null)
        {
            return;
        }

        GameObject inputObject = new GameObject(nameof(UnifiedUIInputController));
        instance = inputObject.AddComponent<UnifiedUIInputController>();
        DontDestroyOnLoad(inputObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(ConfigureSceneUI());
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            RestoreHighlight();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreHighlight();
        StartCoroutine(ConfigureSceneUI());
    }

    private IEnumerator ConfigureSceneUI()
    {
        yield return null;

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (EventSystem eventSystem in eventSystems)
        {
            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                oldModule.enabled = false;
            }

            InputSystemUIInputModule inputSystemModule =
                eventSystem.GetComponent<InputSystemUIInputModule>();

            if (inputSystemModule == null)
            {
                inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputSystemModule.AssignDefaultActions();
            }

            inputSystemModule.enabled = true;
            eventSystem.sendNavigationEvents = true;
        }

        ApplySharedButtonStyle();
    }

    private void LateUpdate()
    {
        // ステージ選択は専用Controllerが同じInput System入力でカーソルと
        // 横スクロールを同期しているため、EventSystem側の選択表示は重ねません。
        if (SceneManager.GetActiveScene().name == "StageSelect")
        {
            RestoreHighlight();
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            RestoreHighlight();
            return;
        }

        GameObject selectedObject = eventSystem.currentSelectedGameObject;
        Selectable selected = selectedObject != null
            ? selectedObject.GetComponent<Selectable>()
            : null;

        if (selected == null || !selected.IsActive() || !selected.IsInteractable())
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool shouldSelectAutomatically = sceneName != "Map" && sceneName != "StageSelect";

            if (shouldSelectAutomatically || WasNavigationPressedThisFrame())
            {
                selected = FindFirstAvailableSelectable();
                if (selected != null)
                {
                    eventSystem.SetSelectedGameObject(selected.gameObject);
                }
            }
            else
            {
                selected = null;
            }
        }

        UpdateHighlight(selected);
    }

    private static void ApplySharedButtonStyle()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button button in buttons)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = NormalButtonColor;
            colors.highlightedColor = SelectedButtonColor;
            colors.selectedColor = SelectedButtonColor;
            colors.pressedColor = PressedButtonColor;
            button.colors = colors;
        }
    }

    private static bool WasNavigationPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null &&
            (keyboard.upArrowKey.wasPressedThisFrame ||
             keyboard.downArrowKey.wasPressedThisFrame ||
             keyboard.leftArrowKey.wasPressedThisFrame ||
             keyboard.rightArrowKey.wasPressedThisFrame ||
             keyboard.wKey.wasPressedThisFrame ||
             keyboard.aKey.wasPressedThisFrame ||
             keyboard.sKey.wasPressedThisFrame ||
             keyboard.dKey.wasPressedThisFrame ||
             keyboard.tabKey.wasPressedThisFrame))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null &&
               (gamepad.dpad.up.wasPressedThisFrame ||
                gamepad.dpad.down.wasPressedThisFrame ||
                gamepad.dpad.left.wasPressedThisFrame ||
                gamepad.dpad.right.wasPressedThisFrame ||
                gamepad.leftStick.ReadValue().sqrMagnitude > 0.5f);
    }

    private static Selectable FindFirstAvailableSelectable()
    {
        return Selectable.allSelectablesArray
            .Where(selectable =>
                selectable != null &&
                selectable.IsActive() &&
                selectable.IsInteractable())
            .OrderByDescending(selectable => selectable.transform.position.y)
            .ThenBy(selectable => selectable.transform.position.x)
            .FirstOrDefault();
    }

    private void UpdateHighlight(Selectable selected)
    {
        if (highlightedSelectable == selected)
        {
            return;
        }

        RestoreHighlight();

        if (selected == null)
        {
            return;
        }

        highlightedSelectable = selected;
        highlightedOriginalScale = selected.transform.localScale;
        selected.transform.localScale = highlightedOriginalScale * SelectedScale;
    }

    private void RestoreHighlight()
    {
        if (highlightedSelectable != null)
        {
            highlightedSelectable.transform.localScale = highlightedOriginalScale;
        }

        highlightedSelectable = null;
    }
}
