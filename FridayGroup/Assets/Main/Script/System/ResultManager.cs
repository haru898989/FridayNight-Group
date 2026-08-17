using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public void BackToTitle()
    {
<<<<<<< HEAD
        if (Perfect_Online.Instance != null)
        {
            Perfect_Online.Instance.ReturnToTitle();
=======
        if (OnlineStageFlow.Instance != null && OnlineStageFlow.Instance.IsConnected)
        {
            OnlineStageFlow.Instance.ReturnToTitle();
>>>>>>> develop
            return;
        }

        SceneManager.LoadScene("Title");
    }
<<<<<<< HEAD
=======

    public void BackToStageSelect()
    {
        OnlineStageFlow.Instance?.ReturnToStageSelect();
    }

    public void GoToNextStage()
    {
        OnlineStageFlow.Instance?.ContinueToNextStage();
    }
>>>>>>> develop
}
