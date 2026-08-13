using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StageSelectシーン上に配置されたCanvasを制御します。
/// ステージ数に応じて、StageContent内のテンプレートだけを複製します。
/// </summary>
public sealed class StageSelectController : MonoBehaviour
{
    private const float NodeSpacing = 240f;
    private const float CursorSyncInterval = 0.25f;

    [Header("Stage list")]
    [SerializeField] private RectTransform stageContent;
    [SerializeField] private Button stageNodeTemplate;
    [SerializeField] private Image stageLineTemplate;

    [Header("Stage information")]
    [SerializeField] private Text groupText;
    [SerializeField] private Text stageNameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Text previewPlaceholderText;

    [Header("Operation")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text confirmButtonText;
    [SerializeField] private Text statusText;

    private readonly List<Image> nodeImages = new List<Image>();
    private readonly List<Button> nodeButtons = new List<Button>();
    private readonly List<float> nodePositions = new List<float>();

    private List<StageCatalogEntry> stages = new List<StageCatalogEntry>();
    private OnlineStageFlow stageFlow;
    private int selectedIndex;
    private float previousHorizontalAxis;
    private float nextAxisRepeatTime;
    private float nextCursorSyncTime;
    private bool isConfirming;
    private Sprite runtimePreviewSprite;
    private Coroutine contentMoveCoroutine;

    private void Start()
    {
        if (!HasRequiredSceneUI())
        {
            Debug.LogError("StageSelectシーンのUI参照が不足しています。シーン上のStageSelectControllerを確認してください。");
            enabled = false;
            return;
        }

        stageFlow = OnlineStageFlow.Instance;
        stages = StageCatalog.Load();
        BuildStageNodes();

        if (stageFlow != null)
        {
            stageFlow.StageCursorChanged += OnStageCursorChanged;
            stageFlow.StateChanged += RefreshOnlineState;
            stageFlow.OperationMessageChanged += OnOperationMessageChanged;
            stageFlow.RefreshStageCursorFromSession();
        }

        if (stages.Count == 0)
        {
            statusText.text = "NO STAGE DATA";
            confirmButton.interactable = false;
            return;
        }

        int synchronizedIndex = FindStageIndex(stageFlow?.CurrentStageCursorResourcePath);
        selectedIndex = synchronizedIndex >= 0 ? synchronizedIndex : 0;
        ApplySelection(false, true);

        if (CanControlSelection())
        {
            stageFlow.BroadcastStageCursor(stages[selectedIndex].resourcePath);
        }
    }

    private void OnDestroy()
    {
        if (stageFlow != null)
        {
            stageFlow.StageCursorChanged -= OnStageCursorChanged;
            stageFlow.StateChanged -= RefreshOnlineState;
            stageFlow.OperationMessageChanged -= OnOperationMessageChanged;
        }

        if (runtimePreviewSprite != null)
        {
            Destroy(runtimePreviewSprite);
        }
    }

    private void Update()
    {
        if (stageFlow != null && Time.unscaledTime >= nextCursorSyncTime)
        {
            nextCursorSyncTime = Time.unscaledTime + CursorSyncInterval;
            stageFlow.RefreshStageCursorFromSession();
            ApplyCurrentFlowCursor();
        }

        if (!CanControlSelection() || stages.Count == 0 || isConfirming)
        {
            return;
        }

        int direction = ReadHorizontalDirection();
        if (direction != 0)
        {
            MoveSelection(direction);
        }

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            ConfirmSelection();
        }
    }

    private bool HasRequiredSceneUI()
    {
        return stageContent != null &&
               stageNodeTemplate != null &&
               stageLineTemplate != null &&
               groupText != null &&
               stageNameText != null &&
               descriptionText != null &&
               previewImage != null &&
               previewPlaceholderText != null &&
               confirmButton != null &&
               confirmButtonText != null &&
               statusText != null;
    }

    private void BuildStageNodes()
    {
        nodeImages.Clear();
        nodeButtons.Clear();
        nodePositions.Clear();

        stageNodeTemplate.gameObject.SetActive(false);
        stageLineTemplate.gameObject.SetActive(false);

        float centerOffset = Mathf.Max(0, stages.Count - 1) * NodeSpacing * 0.5f;
        stageContent.sizeDelta = new Vector2(
            Mathf.Max(1000f, Mathf.Max(0, stages.Count - 1) * NodeSpacing + 180f),
            stageContent.sizeDelta.y
        );

        for (int i = 0; i < stages.Count; i++)
        {
            float nodeX = i * NodeSpacing - centerOffset;
            nodePositions.Add(nodeX);

            if (i > 0)
            {
                Image line = Instantiate(stageLineTemplate, stageContent);
                line.name = "Line_" + (i - 1) + "_" + i;
                SetRect(line.rectTransform, new Vector2(NodeSpacing - 90f, 4f), new Vector2(nodeX - NodeSpacing * 0.5f, 0f));
                line.gameObject.SetActive(true);
            }

            StageCatalogEntry stage = stages[i];
            Button nodeButton = Instantiate(stageNodeTemplate, stageContent);
            nodeButton.name = "Stage_" + stage.stageFolder;
            SetRect(nodeButton.GetComponent<RectTransform>(), new Vector2(90f, 58f), new Vector2(nodeX, 0f));

            Text label = nodeButton.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = stage.stageFolder;
            }

            Text groupLabel = nodeButton.transform.Find("GroupLabel")?.GetComponent<Text>();
            if (groupLabel != null)
            {
                groupLabel.text = stage.groupFolder;
            }

            int capturedIndex = i;
            nodeButton.onClick.RemoveAllListeners();
            nodeButton.onClick.AddListener(() => SelectByIndex(capturedIndex));
            nodeButton.gameObject.SetActive(true);
            nodeButtons.Add(nodeButton);
            nodeImages.Add(nodeButton.image);
        }
    }

    private int ReadHorizontalDirection()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            previousHorizontalAxis = -1f;
            nextAxisRepeatTime = Time.unscaledTime + 0.35f;
            return -1;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            previousHorizontalAxis = 1f;
            nextAxisRepeatTime = Time.unscaledTime + 0.35f;
            return 1;
        }

        float horizontalAxis = Input.GetAxisRaw("Horizontal");
        bool axisPressed = Mathf.Abs(horizontalAxis) >= 0.6f;
        bool axisWasReleased = Mathf.Abs(previousHorizontalAxis) < 0.6f;
        int direction = 0;

        if (axisPressed && (axisWasReleased || Time.unscaledTime >= nextAxisRepeatTime))
        {
            direction = horizontalAxis > 0f ? 1 : -1;
            nextAxisRepeatTime = Time.unscaledTime + (axisWasReleased ? 0.35f : 0.14f);
        }

        previousHorizontalAxis = horizontalAxis;
        return direction;
    }

    private void MoveSelection(int direction)
    {
        int nextIndex = Mathf.Clamp(selectedIndex + direction, 0, stages.Count - 1);
        if (nextIndex == selectedIndex)
        {
            return;
        }

        selectedIndex = nextIndex;
        ApplySelection(true, false);
    }

    private void SelectByIndex(int index)
    {
        if (!CanControlSelection() || isConfirming || index < 0 || index >= stages.Count)
        {
            return;
        }

        selectedIndex = index;
        ApplySelection(true, false);
    }

    private void ApplySelection(bool broadcast, bool immediate)
    {
        if (stages.Count == 0)
        {
            return;
        }

        StageCatalogEntry selectedStage = stages[selectedIndex];
        int groupStageCount = stages.Count(stage => stage.groupFolder == selectedStage.groupFolder);
        groupText.text = selectedStage.groupFolder + "  (" + groupStageCount + ")";
        stageNameText.text = selectedStage.displayName;
        descriptionText.text = selectedStage.HasMapData
            ? selectedStage.description
            : selectedStage.description + "  [NO CSV]";

        UpdatePreview(selectedStage);
        UpdateNodeColors();
        UpdateConfirmState();
        MoveStageContent(immediate);

        if (broadcast && CanControlSelection())
        {
            stageFlow.BroadcastStageCursor(selectedStage.resourcePath);
        }
    }

    private void UpdatePreview(StageCatalogEntry stage)
    {
        if (runtimePreviewSprite != null)
        {
            Destroy(runtimePreviewSprite);
            runtimePreviewSprite = null;
        }

        Texture2D previewTexture = StageCatalog.LoadPreview(stage);
        if (previewTexture == null)
        {
            previewImage.enabled = false;
            previewPlaceholderText.enabled = true;
            return;
        }

        runtimePreviewSprite = Sprite.Create(
            previewTexture,
            new Rect(0f, 0f, previewTexture.width, previewTexture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        previewImage.sprite = runtimePreviewSprite;
        previewImage.enabled = true;
        previewPlaceholderText.enabled = false;
    }

    private void UpdateNodeColors()
    {
        for (int i = 0; i < nodeImages.Count; i++)
        {
            if (i == selectedIndex)
            {
                nodeImages[i].color = Color.white;
            }
            else
            {
                nodeImages[i].color = stages[i].HasMapData
                    ? new Color(0.72f, 0.72f, 0.72f, 1f)
                    : new Color(0.42f, 0.42f, 0.42f, 1f);
            }

            nodeButtons[i].interactable = CanControlSelection() && !isConfirming;
        }
    }

    private void UpdateConfirmState()
    {
        if (stages.Count == 0)
        {
            return;
        }

        bool hasCsv = stages[selectedIndex].HasMapData;
        confirmButton.interactable = CanControlSelection() && hasCsv && !isConfirming;

        if (isConfirming)
        {
            confirmButtonText.text = "LOADING";
            statusText.text = stageFlow != null ? stageFlow.OperationMessage : "LOADING";
        }
        else if (!hasCsv)
        {
            confirmButtonText.text = "NO CSV";
            statusText.text = "THIS STAGE HAS NO CSV DATA";
        }
        else if (!CanControlSelection())
        {
            confirmButtonText.text = "HOST ONLY";
            statusText.text = stageFlow == null ? "NO ONLINE SESSION" : stageFlow.OperationMessage;
        }
        else
        {
            confirmButtonText.text = "START";
            statusText.text = "SELECT A STAGE";
        }
    }

    private void ConfirmSelection()
    {
        if (stages.Count == 0 || !CanControlSelection() || isConfirming)
        {
            return;
        }

        StageCatalogEntry selectedStage = stages[selectedIndex];
        if (!selectedStage.HasMapData)
        {
            statusText.text = "THIS STAGE HAS NO CSV DATA";
            return;
        }

        if (stageFlow.ConfirmStageSelection(selectedStage.resourcePath))
        {
            isConfirming = true;
            UpdateNodeColors();
            UpdateConfirmState();
        }
    }

    private void OnStageCursorChanged(string resourcePath)
    {
        ApplyCursorPath(resourcePath);
    }

    private void ApplyCurrentFlowCursor()
    {
        if (stageFlow != null)
        {
            ApplyCursorPath(stageFlow.CurrentStageCursorResourcePath);
        }
    }

    private void ApplyCursorPath(string resourcePath)
    {
        int index = FindStageIndex(resourcePath);
        if (index < 0 || index == selectedIndex)
        {
            return;
        }

        selectedIndex = index;
        ApplySelection(false, false);
    }

    private void RefreshOnlineState()
    {
        UpdateNodeColors();
        UpdateConfirmState();
        ApplyCurrentFlowCursor();
    }

    private void OnOperationMessageChanged(string message)
    {
        if (statusText != null && (isConfirming || !CanControlSelection()))
        {
            statusText.text = message;
        }
    }

    private int FindStageIndex(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return -1;
        }

        return stages.FindIndex(stage => stage.resourcePath == resourcePath);
    }

    private bool CanControlSelection()
    {
        return stageFlow != null &&
               stageFlow.IsSharedModeMasterClient &&
               stageFlow.ConnectedPlayerCount >= stageFlow.NeededPlayerCount;
    }

    private void MoveStageContent(bool immediate)
    {
        if (selectedIndex < 0 || selectedIndex >= nodePositions.Count)
        {
            return;
        }

        Vector2 destination = new Vector2(-nodePositions[selectedIndex], 0f);

        if (contentMoveCoroutine != null)
        {
            StopCoroutine(contentMoveCoroutine);
        }

        if (immediate)
        {
            stageContent.anchoredPosition = destination;
            contentMoveCoroutine = null;
            return;
        }

        contentMoveCoroutine = StartCoroutine(MoveStageContentRoutine(destination));
    }

    private IEnumerator MoveStageContentRoutine(Vector2 destination)
    {
        Vector2 start = stageContent.anchoredPosition;
        const float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            stageContent.anchoredPosition = Vector2.Lerp(start, destination, t);
            yield return null;
        }

        stageContent.anchoredPosition = destination;
        contentMoveCoroutine = null;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }
}
