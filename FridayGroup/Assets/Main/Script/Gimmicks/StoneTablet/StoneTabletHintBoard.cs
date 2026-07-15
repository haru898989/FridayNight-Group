using UnityEngine;
using TMPro;

public class StoneTabletHintBoard : MonoBehaviour
{
    [Header("Hint Text Settings")]
    [SerializeField] private TMP_Text hintText;

    /// <summary>
    /// 正解の石板IDをヒント表示する関数
    /// </summary>
    public void ShowHint(int[] correctTabletIds)
    {
        // 表示用テキストが設定されていない場合は処理しない
        if (hintText == null)
        {
            Debug.LogWarning("HintText is not set");
            return;
        }

        // 正解IDが設定されていない場合は処理しない
        if (correctTabletIds == null || correctTabletIds.Length == 0)
        {
            hintText.text = "No hint";
            return;
        }

        string message = "Stone Tablet Hint\n";

        // 型枠ごとの正解IDを文字列にする
        for (int i = 0; i < correctTabletIds.Length; i++)
        {
            message += "Frame " + i + " : Tablet " + correctTabletIds[i] + "\n";
        }

        // ヒント板に表示する
        hintText.text = message;
    }
}