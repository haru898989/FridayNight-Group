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
        if (online == null)
        {
            online = Perfect_Online.Instance;
        }

        if (online == null)
        {
            Debug.LogError("Perfect_Onlineが見つかりません");
            return;
        }

        online.LoadMap();
    }
}
