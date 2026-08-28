using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mapシーンのポーズメニューです。
/// オンライン通信は止めず、自分のプレイヤー操作だけを一時停止します。
/// </summary>
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button stageSelectButton;
    [SerializeField] private Button titleButton;

    private InputAction pauseAction;
    private PlayerBase localPlayer;
    private bool localPlayerCouldMove;
    private bool isOpen;
    private bool isTransitioning;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    private void Awake()
    {
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.AddBinding("<Gamepad>/buttonEast");
        pauseAction.performed += OnPausePerformed;

        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(RestartStage);
        stageSelectButton.onClick.AddListener(ReturnToStageSelect);
        titleButton.onClick.AddListener(ReturnToTitle);
        menuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        pauseAction?.Enable();
    }

    private void OnDisable()
    {
        pauseAction?.Disable();

        if (isOpen && !isTransitioning)
        {
            RestoreLocalPlayerAndCursor();
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Dispose();
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (isTransitioning || SceneManager.GetActiveScene().name != "Map")
        {
            return;
        }

        // Bボタンはポーズを閉じるときだけ使います。
        // 通常プレイ中の既存アクション操作とは競合させません。
        if (!isOpen && Gamepad.current != null && context.control == Gamepad.current.buttonEast)
        {
            return;
        }

        if (isOpen)
        {
            Resume();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (isOpen || isTransitioning)
        {
            return;
        }

        localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            localPlayerCouldMove = localPlayer.canMove;
            localPlayer.canMove = false;
        }

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isOpen = true;
        menuPanel.SetActive(true);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void Resume()
    {
        if (!isOpen || isTransitioning)
        {
            return;
        }

        menuPanel.SetActive(false);
        isOpen = false;
        RestoreLocalPlayerAndCursor();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void RestartStage()
    {
        if (!BeginTransition())
        {
            return;
        }

        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            flow.RestartCurrentStage();
        }
        else
        {
            SceneManager.LoadScene("Map");
        }
    }

    public void ReturnToStageSelect()
    {
        if (!BeginTransition())
        {
            return;
        }

        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            flow.ReturnToStageSelect();
        }
        else
        {
            SceneManager.LoadScene("StageSelect");
        }
    }

    public void ReturnToTitle()
    {
        if (!BeginTransition())
        {
            return;
        }

        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            flow.ReturnToTitle();
        }
        else
        {
            SceneManager.LoadScene("Title");
        }
    }

    private bool BeginTransition()
    {
        if (isTransitioning)
        {
            return false;
        }

        isTransitioning = true;
        resumeButton.interactable = false;
        restartButton.interactable = false;
        stageSelectButton.interactable = false;
        titleButton.interactable = false;
        return true;
    }

    private void RestoreLocalPlayerAndCursor()
    {
        if (localPlayer != null)
        {
            localPlayer.canMove = localPlayerCouldMove;
        }

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        localPlayer = null;
    }

    private static PlayerBase FindLocalPlayer()
    {
        PlayerBase[] players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);
        foreach (PlayerBase player in players)
        {
            if (player.HasInputAuthority)
            {
                return player;
            }
        }

        return null;
    }
}
