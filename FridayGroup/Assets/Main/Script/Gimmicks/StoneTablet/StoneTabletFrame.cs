using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StoneTabletFrame : MonoBehaviour
{
    [Header("Frame Settings")]
    [SerializeField] private int requiredTabletId = 0;
    [SerializeField] private GameObject fittedTabletObject;

    [Header("Input Settings")]
    [SerializeField] private KeyCode fitKey = KeyCode.E;

    private bool isFilled = false;
    private StoneTabletPuzzleManager puzzleManager;
    private StoneTabletCarrier currentCarrier;

    /// <summary>
    /// コンポーネント追加時にColliderをTriggerにする関数
    /// </summary>
    private void Reset()
    {
        // 型枠はプレイヤーが近づいたことをTriggerで判定する
        Collider frameCollider = GetComponent<Collider>();
        frameCollider.isTrigger = true;
    }

    /// <summary>
    /// 必要な石板IDを設定する関数
    /// </summary>
    public void SetRequiredTabletId(int tabletId)
    {
        // この型枠に必要な石板IDを設定する
        requiredTabletId = tabletId;
    }

    /// <summary>
    /// パズル管理クラスを設定する関数
    /// </summary>
    public void SetPuzzleManager(StoneTabletPuzzleManager manager)
    {
        // 正解判定用の管理クラスを保存する
        puzzleManager = manager;
    }

    /// <summary>
    /// 正しい石板がはまっているかを返す関数
    /// </summary>
    public bool IsFilled()
    {
        // 型枠が埋まっているか返す
        return isFilled;
    }

    /// <summary>
    /// プレイヤーが型枠の近くに入ったときに呼ばれる関数
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグ以外なら処理しない
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        StoneTabletCarrier carrier = other.GetComponent<StoneTabletCarrier>();

        // 石板所持スクリプトがあれば，現在近くにいるプレイヤーとして保存する
        if (carrier != null)
        {
            currentCarrier = carrier;
            Debug.Log("Press " + fitKey + " to fit tablet");
        }
    }

    /// <summary>
    /// プレイヤーが型枠の近くから離れたときに呼ばれる関数
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Playerタグ以外なら処理しない
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        StoneTabletCarrier carrier = other.GetComponent<StoneTabletCarrier>();

        // 離れたプレイヤーが現在の対象なら解除する
        if (carrier != null && carrier == currentCarrier)
        {
            currentCarrier = null;
        }
    }

    /// <summary>
    /// 型枠の近くでEキーが押されたか確認する関数
    /// </summary>
    private void Update()
    {
        // すでに石板がはまっている場合は処理しない
        if (isFilled)
        {
            return;
        }

        // 近くにプレイヤーがいない場合は処理しない
        if (currentCarrier == null)
        {
            return;
        }

        // 指定キーが押されていない場合は処理しない
        if (Input.GetKeyDown(fitKey) == false)
        {
            return;
        }

        // 石板をはめる処理を行う
        TryFitTablet();
    }

    /// <summary>
    /// プレイヤーが持っている石板を型枠にはめる関数
    /// </summary>
    private void TryFitTablet()
    {
        // プレイヤーが石板を持っていない場合は処理しない
        if (currentCarrier.HasTablet() == false)
        {
            Debug.Log("Player does not have a tablet");
            return;
        }

        int currentTabletId = currentCarrier.GetCurrentTabletId();

        // 正しい石板IDか確認する
        if (currentTabletId == requiredTabletId)
        {
            isFilled = true;
            currentCarrier.ClearTablet();

            Debug.Log("Correct tablet fitted. ID: " + currentTabletId);

            // はまった石板の見た目を表示する
            if (fittedTabletObject != null)
            {
                fittedTabletObject.SetActive(true);
            }

            // パズル全体のクリア判定を行う
            if (puzzleManager != null)
            {
                puzzleManager.CheckPuzzleClear();
            }
        }
        else
        {
            Debug.Log("Wrong tablet. Need: " + requiredTabletId + " / Current: " + currentTabletId);
        }
    }
}