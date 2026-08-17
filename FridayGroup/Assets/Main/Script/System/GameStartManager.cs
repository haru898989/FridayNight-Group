using UnityEngine;

public class GameStartManager : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("ゲーム本編開始");


        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerなし");
            return;
        }


        Debug.Log("GameManager取得成功");


        var p1 = GameManager.Instance.GetPlayerData(0);
        var p2 = GameManager.Instance.GetPlayerData(1);



        Debug.Log(
            $"1P : Controller={p1.useController}, Object={p1.objectName}"
        );


        Debug.Log(
            $"2P : Controller={p2.useController}, Object={p2.objectName}"
        );
    }
}