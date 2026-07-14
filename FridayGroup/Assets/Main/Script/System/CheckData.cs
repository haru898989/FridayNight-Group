using UnityEngine;

public class CheckData : MonoBehaviour
{
    void Start()
    {
        Debug.Log("CheckDataŠJn");

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager‚ª‘¶İ‚µ‚Ü‚¹‚ñ");
            return;
        }

        Debug.Log("GameManager‘¶İ");

        // 1Pî•ñæ“¾
        GameManager.PlayerData player1 =
            GameManager.Instance.GetPlayerData(0);

        // 2Pî•ñæ“¾
        GameManager.PlayerData player2 =
            GameManager.Instance.GetPlayerData(1);


        Debug.Log(
            "1P : ID=" + player1.playerID +
            " Controller=" + player1.useController +
            " Object=" + player1.objectName
        );


        Debug.Log(
            "2P : ID=" + player2.playerID +
            " Controller=" + player2.useController +
            " Object=" + player2.objectName
        );
    }
}