using UnityEngine;
using System.Collections.Generic;

public class StoneTabletPuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [SerializeField] private StoneTabletPiece[] tabletPieces;
    [SerializeField] private StoneTabletFrame[] tabletFrames;
    [SerializeField] private StoneTabletDoor targetDoor;
    [SerializeField] private StoneTabletHintBoard hintBoard;

    [Header("Sound Settings")]
    [SerializeField] private int clearSeIndex = 7;

    private bool isCleared = false;

    /// <summary>
    /// 石板パズルの初期設定を行う関数
    /// </summary>
    private void Start()
    {
        // 石板と型枠の正解を設定する
        SetupPuzzle();
    }

    /// <summary>
    /// 石板の正解組み合わせをランダムに決定する関数
    /// </summary>
    private void SetupPuzzle()
    {
        // 手動設定されていない場合はシーン内から探す
        if (tabletPieces == null || tabletPieces.Length == 0)
        {
            tabletPieces = FindObjectsOfType<StoneTabletPiece>();
        }

        // 型枠が設定されていない場合は処理しない
        if (tabletFrames == null || tabletFrames.Length == 0)
        {
            Debug.LogWarning("StoneTabletFrame is not set");
            return;
        }

        // 石板ID一覧を作成する
        List<int> tabletIds = new List<int>();

        for (int i = 0; i < tabletPieces.Length; i++)
        {
            int id = tabletPieces[i].GetTabletId();

            // 重複しないIDだけ追加する
            if (tabletIds.Contains(id) == false)
            {
                tabletIds.Add(id);
            }
        }

        // 型枠の数だけ石板IDがない場合は警告を出す
        if (tabletIds.Count < tabletFrames.Length)
        {
            Debug.LogWarning("Not enough tablet IDs for frames");
            return;
        }

        // IDの順番をランダムに入れ替える
        for (int i = 0; i < tabletIds.Count; i++)
        {
            int randomIndex = Random.Range(i, tabletIds.Count);

            int temporaryId = tabletIds[i];
            tabletIds[i] = tabletIds[randomIndex];
            tabletIds[randomIndex] = temporaryId;
        }

        // ヒント表示用に正解IDを保存する配列を作る
        int[] correctTabletIds = new int[tabletFrames.Length];

        // 型枠に正解IDを設定する
        for (int i = 0; i < tabletFrames.Length; i++)
        {
            tabletFrames[i].SetRequiredTabletId(tabletIds[i]);
            tabletFrames[i].SetPuzzleManager(this);

            // ヒント表示用の配列にも正解IDを保存する
            correctTabletIds[i] = tabletIds[i];

            Debug.Log("Frame " + i + " requires tablet ID: " + tabletIds[i]);
        }

        // ヒント板が設定されていれば，正解IDを表示する
        if (hintBoard != null)
        {
            hintBoard.ShowHint(correctTabletIds);
        }
    }

    /// <summary>
    /// すべての型枠に正しい石板がはまったか確認する関数
    /// </summary>
    public void CheckPuzzleClear()
    {
        // すでにクリア済みなら処理しない
        if (isCleared)
        {
            return;
        }

        // すべての型枠が埋まっているか確認する
        for (int i = 0; i < tabletFrames.Length; i++)
        {
            if (tabletFrames[i].IsFilled() == false)
            {
                return;
            }
        }

        // すべて正解ならクリア処理を行う
        isCleared = true;
        Debug.Log("Stone tablet puzzle cleared");

        // クリア音を再生する
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySE(clearSeIndex);
        }

        // 対象ドアを開く
        if (targetDoor != null)
        {
            targetDoor.OpenDoor();
        }
    }
}