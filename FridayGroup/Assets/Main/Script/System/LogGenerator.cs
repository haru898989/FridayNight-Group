using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking; // 通信機能を使うための準備

public class NewLog : MonoBehaviour
{
    // どこからでも NewLog.Instance.SendLog(...) と呼び出せるようにする魔法
    public static NewLog Instance { get; private set; }

    [Header("GASの設定")]
    // GASのデプロイURLをここに貼り付けます
    [SerializeField] private string gasUrl = "https://script.google.com/macros/s/AKfycbypWruHJ3VEaVMMPapG-EpaS0y7ja_QX9VQxJUkBK1RoAGgbH8O263p-QKU_CKdQWbu/exec";

    // 何秒ごとにまとめてログを送るか
    [Header("ログ送信設定")]
    [SerializeField] private float sendInterval = 1.0f;

    //位置のログは0.5秒ごとに送る！！
    [Header("位置ログ設定")]
    [SerializeField] private float positionLogInterval = 0.5f;

    // 送信するデータを一時的に溜めておく箱（バッファ）
    private List<string[]> logBuffer = new List<string[]>();

    //ゲーム情報
    private string matchId;//どのマッチか
    private float gameStartTime;//ゲーム内の時間（ゲームが開始されてからの時間）
    private bool gameStarted = false;


    private void Awake()
    {
        // 準備
        Instance = this;
    }

    private void Start()
    {
        // 仮のMatchID
        // 後でGASから取得することもできる
        matchId = "M_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ゲーム開始と同時に、定期送信ループをスタート
        StartCoroutine(LogUploadRoutine());
    }

    //ゲーム開始
    public void StartGame()
{
    // ゲーム開始時刻を記録
    gameStartTime = Time.time;

    gameStarted = true;

    Debug.Log("ゲーム開始");

    // 0.5秒ごとの位置ログを開始
    StartCoroutine(PositionLogRoutine());


    // =====================================================
    // Player1のゲーム開始ログ
    // =====================================================

    GameObject player1 =
        GameObject.FindGameObjectWithTag("Player1");

    if (player1 != null)
    {
        AddLog(
            "Player1",
            "GameStart",
            "Start",
            player1.transform.position
        );
    }


    // =====================================================
    // Player2のゲーム開始ログ
    // =====================================================

    GameObject player2 =
        GameObject.FindGameObjectWithTag("Player2");

    if (player2 != null)
    {
        AddLog(
            "Player2",
            "GameStart",
            "Start",
            player2.transform.position
        );
    }
}

    //ゲーム内時間
    private float GetGameTime()
    {
        if (!gameStarted)
        {
            return 0f;
        }

        return Time.time - gameStartTime;
    }

    //イベントログ
    public void SendEventLog(string playerId,string eventType,string eventName,Vector3 position)
    {
        AddLog(playerId,eventType,eventName,position);
    }

    //位置ログ
     private IEnumerator PositionLogRoutine()
    {
        while (gameStarted)
        {
            yield return new WaitForSeconds(positionLogInterval);


            // Player1
            GameObject player1 = GameObject.FindGameObjectWithTag("Player1");
            if (player1 != null)
            {
                AddLog
                (
                    "Player1",
                    "Position",
                    "",
                    player1.transform.position
                );
            }


            // Player2
            GameObject player2 = GameObject.FindGameObjectWithTag("Player2");
            if (player2 != null)
            {
                AddLog
                (
                    "Player2",
                    "Position",
                    "",
                    player2.transform.position
                );
            }
        }
    }



    // =========================================================
    // 📝 ログを溜め込む関数（他のスクリプトから呼ばれる）
    // =========================================================
    /// <summary>
    /// ゲームの出来事を記録する
    /// </summary>
    private void AddLog(string playerId,string eventType,string eventName,Vector3 position)
    {
        string[] row =
        {
            matchId,playerId,GetGameTime().ToString("F2"),
            eventType,eventName,
            position.x.ToString("F2"),
            position.y.ToString("F2"),
            position.z.ToString("F2")
        };

        // 一時保管用のリストに一旦追加するだけ（まだ送らない！）
        logBuffer.Add(row);
    }

    // =========================================================
    // 🚀 一定間隔でまとめて送信するループ処理
    // =========================================================
    private IEnumerator LogUploadRoutine()
    {
        // while(true) でゲーム中ずっと繰り返す
        while (true)
        {
            // 設定した秒数（sendInterval）だけ待つ
            yield return new WaitForSeconds(sendInterval);

            // 溜まっているログがあるかチェック
            if (logBuffer.Count > 0)
            {
                // 送信用にデータをコピーして、元の箱は空っぽにする
                List<string[]> dataToSend = new List<string[]>(logBuffer);
                logBuffer.Clear();

                // 実際の送信処理（GASへ）を呼び出す
                yield return PostToGAS(dataToSend);
            }
        }
    }

    // =========================================================
    // 🌐 GASへ通信（POSTリクエスト）を送る処理
    // =========================================================
    private IEnumerator PostToGAS(List<string[]> data)
    {
        // データをインターネットで送りやすい形（JSONという文字の塊）に変換する
        string json = JsonHelper.ToJson(data);

        // 通信の準備
        using (UnityWebRequest www = new UnityWebRequest(gasUrl, "POST"))
        {
            // JSONの文字列をバイトデータ（コンピューターがわかる形）に変換してセット
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // 通信開始！終わるまで待機
            yield return www.SendWebRequest();

            // 結果の確認
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"通信失敗: {www.error}");
                // 送信失敗したログを戻す
                logBuffer.InsertRange(0, data);
            }
            else
            {
                Debug.Log($"{data.Count}件のログをスプレッドシートに送りました！");
            }
        }
    }


//ゲーム終了
public void EndGame()
{
    // 位置ログを停止
    gameStarted = false;

    Debug.Log("ゲーム終了");


    // =====================================================
    // Player1のゲーム終了ログ
    // =====================================================

    GameObject player1 =
        GameObject.FindGameObjectWithTag("Player1");

    if (player1 != null)
    {
        AddLog(
            "Player1",
            "GameEnd",
            "End",
            player1.transform.position
        );
    }


    // =====================================================
    // Player2のゲーム終了ログ
    // =====================================================

    GameObject player2 =
        GameObject.FindGameObjectWithTag("Player2");

    if (player2 != null)
    {
        AddLog(
            "Player2",
            "GameEnd",
            "End",
            player2.transform.position
        );
    }


    // =====================================================
    // 残っているログをGASへ送信
    // =====================================================

    if (logBuffer.Count > 0)
    {
        List<string[]> dataToSend =
            new List<string[]>(logBuffer);

        logBuffer.Clear();

        StartCoroutine(
            PostToGAS(dataToSend)
        );
    }
}
// =========================================================
// 🧩 C#のリストをJSON（文字の塊）に変換するお助けツール
// =========================================================
public static class JsonHelper
{
    public static string ToJson(List<string[]> list)
    {
        // 大量の文字をくっつける時は StringBuilder が高速！
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("[");
        for (int i = 0; i < list.Count; i++)
        {
            sb.Append("[");
            for (int j = 0; j < list[i].Length; j++)
            {
                // 文字列の中にダブルクォーテーション(")が入らないようにエスケープ処理
                sb.Append("\"" + list[i][j].Replace("\"", "\\\"") + "\"");
                if (j < list[i].Length - 1) sb.Append(",");
            }
            sb.Append("]");
            if (i < list.Count - 1) sb.Append(",");
        }
        sb.Append("]");
        return sb.ToString(); // 完成した文字の塊を返す
    }
}
}