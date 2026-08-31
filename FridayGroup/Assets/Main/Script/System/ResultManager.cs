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
        SoundManager.Instance.PlaySE(0);
        SceneManager.LoadScene("Title");
    }

    public void BackToStageSelect()
    {
        SoundManager.Instance.PlaySE(0);
        OnlineStageFlow.Instance?.ReturnToStageSelect();
    }

    public void GoToNextStage()
    {
        SoundManager.Instance.PlaySE(1);
        OnlineStageFlow.Instance?.ContinueToNextStage();
    }
}
