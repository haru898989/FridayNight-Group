using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public void BackToTitle()
    {
        if (OnlineStageFlow.Instance != null && OnlineStageFlow.Instance.IsConnected)
        {
            OnlineStageFlow.Instance.ReturnToTitle();
            return;
        }

        SceneManager.LoadScene("Title");
    }

    public void BackToStageSelect()
    {
        OnlineStageFlow.Instance?.ReturnToStageSelect();
    }

    public void GoToNextStage()
    {
        OnlineStageFlow.Instance?.ContinueToNextStage();
    }
}
