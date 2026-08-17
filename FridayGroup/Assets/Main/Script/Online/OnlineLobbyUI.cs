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
    private TMP_Text statusText;
    private TMP_Text playerCountText;
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

        buttonRect.sizeDelta = new Vector2(280f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -95f);

        TMP_Text buttonLabel = startButtonUI.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel != null)
        {
            buttonLabel.text = "STAGE SELECT";
            buttonLabel.fontSize = 28f;
        }

        Transform canvasTransform = buttonRect.parent;

        GameObject panelObject = CreateUIObject("OnlineStatusPanel", canvasTransform);
        lobbyObjects.Add(panelObject);
        panelObject.transform.SetAsFirstSibling();
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        SetCenteredRect(panelRect, new Vector2(560f, 260f), new Vector2(0f, 30f));

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.07f, 0.13f, 0.90f);
        panelImage.raycastTarget = false;

        TMP_Text titleText = CreateText("LobbyTitle", canvasTransform, "ONLINE LOBBY", 42f, FontStyles.Bold);
        lobbyObjects.Add(titleText.gameObject);
        SetCenteredRect(titleText.rectTransform, new Vector2(520f, 58f), new Vector2(0f, 125f));
        titleText.color = new Color(1f, 0.86f, 0.25f);

        playerCountText = CreateText("PlayerCount", canvasTransform, "PLAYERS 0 / 2", 32f, FontStyles.Bold);
        lobbyObjects.Add(playerCountText.gameObject);
        SetCenteredRect(playerCountText.rectTransform, new Vector2(520f, 52f), new Vector2(0f, 57f));
        playerCountText.color = Color.white;

        statusText = CreateText("ConnectionStatus", canvasTransform, "CONNECTING...", 23f, FontStyles.Normal);
        lobbyObjects.Add(statusText.gameObject);
        SetCenteredRect(statusText.rectTransform, new Vector2(520f, 50f), new Vector2(0f, 8f));
        statusText.color = new Color(0.55f, 0.90f, 1f);
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

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.layer = LayerMask.NameToLayer("UI");
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize, FontStyles style)
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
