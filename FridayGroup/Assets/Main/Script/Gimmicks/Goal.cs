using UnityEngine;
using UnityEngine.SceneManagement;

public class Goal : MonoBehaviour
{
    private bool hasRequestedStageClear;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasRequestedStageClear && other.CompareTag("Player"))
        {
            hasRequestedStageClear = true;

            if (OnlineStageFlow.Instance != null && OnlineStageFlow.Instance.IsConnected)
            {
                PlayerBase player = other.GetComponentInParent<PlayerBase>();
                if (player != null)
                {
                    player.BeginGoalSpectatorMode();
                    player.ReportGoalReachedToAllPlayers();
                }
                else
                {
                    OnlineStageFlow.Instance.RequestStageClear();
                }
                return;
            }

            SceneManager.LoadScene("Result");
        }
    }
}
