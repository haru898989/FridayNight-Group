using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StartGameButton : MonoBehaviour
{
    [SerializeField] private Perfect_Online online;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(StartGame);
        OnlineStageFlow.EnsureExists(gameObject);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(StartGame);
        }
    }

    public void StartGame()
    {
        Debug.Log("[StartGameButton] StartGame() called");

        if (online == null)
        {
            online = Perfect_Online.Instance;
        }

        Debug.Log($"[StartGameButton] online = {online}");

        if (online == null)
        {
            Debug.LogError("Perfect_Onlineが見つかりません");
            return;
        }

        OnlineStageFlow stageFlow = OnlineStageFlow.EnsureExists(gameObject);

        Debug.Log($"[StartGameButton] stageFlow = {stageFlow}");

        if (stageFlow == null)
        {
            Debug.LogError("[StartGameButton] stageFlow is NULL");
            return;
        }

        bool result = stageFlow.LoadStageSelect();

        Debug.Log($"[StartGameButton] LoadStageSelect result = {result}");

        if (!result)
        {
            Debug.LogError("ステージ選択画面へ移動できません");
        }
    }
}
