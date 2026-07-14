using UnityEngine;

public class SetTestData : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager‚ª‘¶İ‚µ‚Ü‚¹‚ñ");
            return;
        }

        // 1Pî•ñİ’è
        GameManager.Instance.players[0].playerID = 1;
        GameManager.Instance.players[0].useController = true;
        GameManager.Instance.players[0].objectName = "A";


        // 2Pî•ñİ’è
        GameManager.Instance.players[1].playerID = 2;
        GameManager.Instance.players[1].useController = false;
        GameManager.Instance.players[1].objectName = "B";


        Debug.Log("î•ñ•Û‘¶‚µ‚Ü‚µ‚½");

        Debug.Log(
            "1P : Controller=" +
            GameManager.Instance.players[0].useController +
            " Object=" +
            GameManager.Instance.players[0].objectName
        );

        Debug.Log(
            "2P : Controller=" +
            GameManager.Instance.players[1].useController +
            " Object=" +
            GameManager.Instance.players[1].objectName
        );
    }
}