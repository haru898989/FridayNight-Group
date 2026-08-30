using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mapシーンのポーズメニューです。
/// オンライン時はポーズ状態と選択位置を全員に同期します。
/// </summary>
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button stageSelectButton;
    [SerializeField] private Button titleButton;

    private readonly Dictionary<PlayerBase, bool> movementStates =
        new Dictionary<PlayerBase, bool>();
    private readonly Dictionary<NavMeshAgent, bool> agentStoppedStates =
        new Dictionary<NavMeshAgent, bool>();

    private InputAction pauseAction;
    private Button[] menuButtons;
    private OnlineStageFlow subscribedFlow;
    private bool isOpen;
    private bool isTransitioning;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    public static bool IsStagePaused { get; private set; }

    private void Awake()
    {
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.AddBinding("<Gamepad>/buttonEast");
        pauseAction.performed += OnPausePerformed;

        menuButtons = new[]
        {
            resumeButton,
            restartButton,
            stageSelectButton,
            titleButton
        };

        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(RestartStage);
        stageSelectButton.onClick.AddListener(ReturnToStageSelect);
        titleButton.onClick.AddListener(ReturnToTitle);
        menuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        pauseAction?.Enable();
        TrySubscribeStageFlow();
    }

    private void Update()
    {
        TrySubscribeStageFlow();

        if (!isOpen || subscribedFlow == null || !subscribedFlow.IsConnected)
        {
            return;
        }

        if (subscribedFlow.CanLocalControlStagePause)
        {
            int selectedIndex = GetSelectedButtonIndex();
            if (selectedIndex >= 0)
            {
                subscribedFlow.RequestStagePauseSelection(selectedIndex);
            }
        }
        else
        {
            SelectButton(subscribedFlow.StagePauseSelectionIndex);
        }
    }

    private void OnDisable()
    {
        pauseAction?.Disable();
        UnsubscribeStageFlow();

        if (isOpen)
        {
            ApplyPauseState(false, 0);
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

    private void TrySubscribeStageFlow()
    {
        OnlineStageFlow currentFlow = OnlineStageFlow.Instance;
        if (subscribedFlow == currentFlow)
        {
            return;
        }

        UnsubscribeStageFlow();
        subscribedFlow = currentFlow;

        if (subscribedFlow != null)
        {
            subscribedFlow.StagePauseChanged += OnStagePauseChanged;

            if (subscribedFlow.IsStagePaused)
            {
                ApplyPauseState(true, subscribedFlow.StagePauseSelectionIndex);
            }
        }
    }

    private void UnsubscribeStageFlow()
    {
        if (subscribedFlow != null)
        {
            subscribedFlow.StagePauseChanged -= OnStagePauseChanged;
            subscribedFlow = null;
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

        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            flow.RequestOpenStagePause();
            return;
        }

        ApplyPauseState(true, 0);
    }

    public void Resume()
    {
        if (!isOpen || isTransitioning || !CanControlMenu())
        {
            return;
        }

        OnlineStageFlow flow = OnlineStageFlow.Instance;
        if (flow != null && flow.IsConnected)
        {
            flow.RequestCloseStagePause();
            return;
        }

        ApplyPauseState(false, 0);
    }

    public void RestartStage()
    {
        if (!CanControlMenu() || !BeginTransition())
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
        if (!CanControlMenu() || !BeginTransition())
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
        if (!CanControlMenu() || !BeginTransition())
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

    private void OnStagePauseChanged(bool paused, int ownerPlayerId, int selectionIndex)
    {
        ApplyPauseState(paused, selectionIndex);
    }

    private void ApplyPauseState(bool paused, int selectionIndex)
    {
        if (paused)
        {
            if (!isOpen)
            {
                previousCursorLockMode = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                StoreAndStopPlayers();
            }

            isOpen = true;
            IsStagePaused = true;
            menuPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SelectButton(selectionIndex);
            return;
        }

        menuPanel.SetActive(false);
        isOpen = false;
        IsStagePaused = false;
        RestorePlayersAndCursor();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private bool CanControlMenu()
    {
        OnlineStageFlow flow = OnlineStageFlow.Instance;
        return flow == null || !flow.IsConnected || flow.CanLocalControlStagePause;
    }

    private bool BeginTransition()
    {
        if (isTransitioning)
        {
            return false;
        }

        isTransitioning = true;
        foreach (Button button in menuButtons)
        {
            button.interactable = false;
        }

        return true;
    }

    private void StoreAndStopPlayers()
    {
        movementStates.Clear();
        agentStoppedStates.Clear();
        PlayerBase[] players = FindObjectsByType<PlayerBase>(FindObjectsSortMode.None);

        foreach (PlayerBase player in players)
        {
            movementStates[player] = player.canMove;
            player.canMove = false;
        }

        NavMeshAgent[] agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
        foreach (NavMeshAgent agent in agents)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agentStoppedStates[agent] = agent.isStopped;
                agent.isStopped = true;
            }
        }
    }

    private void RestorePlayersAndCursor()
    {
        foreach (KeyValuePair<PlayerBase, bool> state in movementStates)
        {
            if (state.Key != null)
            {
                state.Key.canMove = state.Value;
            }
        }

        movementStates.Clear();

        foreach (KeyValuePair<NavMeshAgent, bool> state in agentStoppedStates)
        {
            if (state.Key != null && state.Key.enabled && state.Key.isOnNavMesh)
            {
                state.Key.isStopped = state.Value;
            }
        }

        agentStoppedStates.Clear();
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
    }

    private int GetSelectedButtonIndex()
    {
        if (EventSystem.current == null)
        {
            return -1;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null && menuButtons[i].gameObject == selected)
            {
                return i;
            }
        }

        return -1;
    }

    private void SelectButton(int index)
    {
        if (EventSystem.current == null || menuButtons == null || menuButtons.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, menuButtons.Length - 1);
        GameObject buttonObject = menuButtons[index].gameObject;
        if (EventSystem.current.currentSelectedGameObject != buttonObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(buttonObject);
        }
    }
}
