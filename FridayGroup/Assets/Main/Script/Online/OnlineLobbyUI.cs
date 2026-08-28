using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// OnlineConnectの既存Canvasへ、接続人数と接続状態を表示します。
/// シーン上のButtonはそのまま利用するため、Inspectorの再設定は不要です。
/// </summary>
public class OnlineLobbyUI : MonoBehaviour
{
    private OnlineStageFlow stageFlow;
    private GameObject startButtonUI;
    private Text statusText;
    private Text playerCountText;
    private bool isInitialized;
    private readonly List<GameObject> lobbyObjects = new List<GameObject>();

    private void Update()
    {
        if (isInitialized)
        {
            Refresh();
        }
    }

    public void Initialize(OnlineStageFlow onlineStageFlow, GameObject stageSelectButton)
    {
        if (isInitialized)
        {
            return;
        }

        stageFlow = onlineStageFlow;
        startButtonUI = stageSelectButton;
        BuildLobbyPanel();

        if (stageFlow != null)
        {
            stageFlow.StateChanged += Refresh;
            stageFlow.OperationMessageChanged += OnOperationMessageChanged;
        }

        isInitialized = true;
        Refresh();
    }

    private void OnDestroy()
    {
        if (stageFlow != null)
        {
            stageFlow.StateChanged -= Refresh;
            stageFlow.OperationMessageChanged -= OnOperationMessageChanged;
        }
    }

    private void BuildLobbyPanel()
    {
        if (startButtonUI == null)
        {
            Debug.LogWarning("OnlineLobbyUI: ステージ選択ボタンが設定されていません");
            return;
        }

        RectTransform buttonRect = startButtonUI.GetComponent<RectTransform>();
        if (buttonRect == null || buttonRect.parent == null)
        {
            return;
        }

        buttonRect.sizeDelta = new Vector2(260f, 46f);
        buttonRect.anchoredPosition = new Vector2(0f, -95f);

        TMP_Text buttonLabel = startButtonUI.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel != null)
        {
            buttonLabel.text = "STAGE SELECT";
            buttonLabel.fontSize = 24f;
        }

        Transform canvasTransform = buttonRect.parent;

        Text titleText = FindSceneText(canvasTransform, "LobbyTitle");
        playerCountText = FindSceneText(canvasTransform, "PlayerCount");
        statusText = FindSceneText(canvasTransform, "ConnectionStatus");

        AddLobbyObject(titleText);
        AddLobbyObject(playerCountText);
        AddLobbyObject(statusText);

        if (titleText == null || playerCountText == null || statusText == null)
        {
            Debug.LogWarning("OnlineLobbyUI: OnlineConnectシーンのロビー表示が不足しています");
        }
    }

    private void Refresh()
    {
        if (stageFlow == null)
        {
            return;
        }

        bool isOnlineConnectScene = SceneManager.GetActiveScene().name == "OnlineConnect";
        foreach (GameObject lobbyObject in lobbyObjects)
        {
            if (lobbyObject != null && lobbyObject.activeSelf != isOnlineConnectScene)
            {
                lobbyObject.SetActive(isOnlineConnectScene);
            }
        }

        if (!isOnlineConnectScene)
        {
            if (startButtonUI != null)
            {
                startButtonUI.SetActive(false);
            }

            return;
        }

        if (playerCountText != null)
        {
            string role = stageFlow.IsConnected
                ? (stageFlow.IsSharedModeMasterClient ? "HOST" : "PLAYER 2")
                : "OFFLINE";
            playerCountText.text = $"PLAYERS {stageFlow.ConnectedPlayerCount} / {stageFlow.NeededPlayerCount}   [{role}]";
        }

        if (statusText != null)
        {
            statusText.text = stageFlow.OperationMessage;
        }

        if (startButtonUI != null)
        {
            startButtonUI.SetActive(stageFlow.CanOpenStageSelect);
        }
    }

    private void OnOperationMessageChanged(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void AddLobbyObject(Text text)
    {
        if (text != null)
        {
            lobbyObjects.Add(text.gameObject);
        }
    }

    private static Text FindSceneText(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        return child != null ? child.GetComponent<Text>() : null;
    }
}
