using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Fusion;

public class PlayerBase : NetworkBehaviour
{
    private PlayerInputAction testplayerControl;
    private Vector2 moveInput;   // 入力値
    public float moveSpeed = 5f; // 移動速度
    public Transform playerCamera;
    public float lookSpeed = 100f;//視点の移動の速度
    private float cameraRotationX = 0f;//カメラの位置
    public float holdThreshold = 0.5f;//ボタンの長押し判定
    private float pressStartTime;
    private bool isSelectingStamp = false;
    private int selectedStamp = 0;
    private GameObject heldObject;    // 持っている物
    public GameObject nearbyObject;   // 近くにある持てる物
    public GameObject selectableObject;    // 決定できる対象
    public float groundY = 0f; //地面の座標
    public Sprite[] stampSprites;//スタンプ画像
    public GameObject stampMenu;//スタンプの選択
    public GameObject[] stampObjects;//スタンプの数

    // GameManagerから同期される情報の格納用変数
    private int myPlayerId;
    private bool usesGamepad;

    public override void Spawned()
    {
        // 入力権限がないクライアントは初期化しない
        if (!HasInputAuthority) return;

        testplayerControl = new PlayerInputAction();

        // アクションのイベント登録
        testplayerControl.Player.OnActionB.started += OnBStarted;
        testplayerControl.Player.OnActionB.canceled += OnBCanceled;
        testplayerControl.Player.Stamp.started += OnStampStarted;
        testplayerControl.Player.Stamp.canceled += OnStampCanceled;

        // UIなどの初期非表示設定
        if (stampMenu != null)
        {
            stampMenu.SetActive(false);
        }

        if (stampObjects != null)
        {
            for (int i = 0; i < stampObjects.Length; i++)
            {
                if (stampObjects[i] != null)
                {
                    stampObjects[i].SetActive(false);
                }
            }
        }

        testplayerControl.Enable();
        Debug.Log("PlayerBase入力開始");

        // カメラの自動取得処理
        if (playerCamera == null)
        {
            // まずは子オブジェクトからカメラを探す
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
            {
                playerCamera = childCam.transform;
            }
            // 子オブジェクトになければ、メインカメラを取得する
            else if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("プレイヤーカメラが設定されておらず、シーン内にもカメラが見つかりません。");
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;
        if (testplayerControl == null) return;

        // 移動・カメラ回転（スタンプ選択中でない場合のみ）
        if (!isSelectingStamp)
        {
            // 移動処理
            Vector2 input = testplayerControl.Player.Move.ReadValue<Vector2>();
            Vector3 move = new Vector3(input.x, 0, input.y);
            transform.position += move * moveSpeed * Runner.DeltaTime;

            // 視点（カメラ回転）処理
            if (playerCamera != null)
            {
                Vector2 lookInput = testplayerControl.Player.Look.ReadValue<Vector2>();

                // 左右を見る（プレイヤー自身の回転）
                transform.Rotate(Vector3.up * lookInput.x * lookSpeed * Runner.DeltaTime);

                // 上下を見る（カメラ単体の回転）
                cameraRotationX -= lookInput.y * lookSpeed * Runner.DeltaTime;
                cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
                playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
            }
        }

        // スタンプ選択中のみ十字キー（D-Pad）を読む
        if (isSelectingStamp)
        {
            Vector2 stampinput = testplayerControl.Player.StampSelect.ReadValue<Vector2>();

            if (stampinput.y > 0.5f)
            {
                selectedStamp = 0;
                Debug.Log("↑ good");
            }
            else if (stampinput.y < -0.5f)
            {
                selectedStamp = 1;
                Debug.Log("↓ bad");
            }
            else if (stampinput.x < -0.5f)
            {
                selectedStamp = 2;
                Debug.Log("← ??");
            }
            else if (stampinput.x > 0.5f)
            {
                selectedStamp = 3;
                Debug.Log("→ die");
            }
        }
    }

    /// <summary>
    /// 長押しの判定（ボタンが押された瞬間）
    /// </summary>
    private void OnBStarted(InputAction.CallbackContext context)
    {
        pressStartTime = Time.time;
    }

    /// <summary>
    /// 長押しの判定（ボタンが離された瞬間）
    /// </summary>
    private void OnBCanceled(InputAction.CallbackContext context)
    {
        float pressDuration = Time.time - pressStartTime;
        bool isHolding = heldObject != null;

        if (pressDuration >= holdThreshold)
        {
            Debug.Log("B長押し");
            HoldObject();
        }
        else
        {
            if (!isHolding)
            {
                Debug.Log("B短押し");
                ConfirmSelection();
            }
            else
            {
                Debug.Log("物を持っているので短押し無効");
            }
        }
    }

    /// <summary>
    /// オブジェクトの所持とオブジェクトとの距離計算
    /// </summary>
    void HoldObject()
    {
        if (heldObject == null)
        {
            if (nearbyObject != null)
            {
                float distance = Vector3.Distance(
                    transform.position,
                    nearbyObject.transform.position
                );

                if (distance > 2f)
                {
                    Debug.Log("遠すぎて持てない");
                    return;
                }

                heldObject = nearbyObject;
                heldObject.transform.SetParent(transform);
                heldObject.transform.localPosition = new Vector3(0, 1, 1);

                Collider col = heldObject.GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;

                Debug.Log("物を持った");
            }
        }
        else
        {
            Collider col = heldObject.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            heldObject.transform.SetParent(null);
            Vector3 pos = heldObject.transform.position;
            pos.y = groundY;
            heldObject.transform.position = pos;
            heldObject = null;

            Debug.Log("物を離した");
        }
    }

    void ConfirmSelection()
    {
        if (selectableObject != null)
        {
            Debug.Log("決定！");
            // 例: ドア開ける、会話する
        }
    }

    /// <summary>
    /// オブジェクトに近いときのみ持てる
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            nearbyObject = other.gameObject;
            Debug.Log("Pickupに接触");
        }
    }

    /// <summary>
    /// オブジェクトが遠いと持てない
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pickup"))
        {
            if (nearbyObject == other.gameObject)
            {
                nearbyObject = null;
                Debug.Log("Pickupから離れた");
            }
        }
    }

    /// <summary>
    /// スタンプの実装部分
    /// </summary>
    void ShowStamp(int index)
    {
        if (stampSprites == null || index < 0 || index >= stampSprites.Length)
            return;

        if (stampObjects == null || index < 0 || index >= stampObjects.Length)
            return;

        for (int i = 0; i < stampObjects.Length; i++)
        {
            if (stampObjects[i] != null)
            {
                stampObjects[i].SetActive(i == index);
            }
        }

        StartCoroutine(HideStampAfterTime());
    }

    IEnumerator HideStampAfterTime()
    {
        yield return new WaitForSeconds(2f);
        if (stampObjects != null)
        {
            for (int i = 0; i < stampObjects.Length; i++)
            {
                if (stampObjects[i] != null)
                {
                    stampObjects[i].SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// スタンプ選択開始
    /// </summary>
    private void OnStampStarted(InputAction.CallbackContext context)
    {
        isSelectingStamp = true;

        if (stampMenu != null)
        {
            stampMenu.SetActive(true);
        }

        Debug.Log("スタンプ選択開始");
    }

    /// <summary>
    /// スタンプ選択終了
    /// </summary>
    private void OnStampCanceled(InputAction.CallbackContext context)
    {
        isSelectingStamp = false;
        ShowStamp(selectedStamp);

        if (stampMenu != null)
        {
            stampMenu.SetActive(false);
        }

        Debug.Log("スタンプ選択終了");
    }

    /// <summary>
    /// GameManagerなどからプレイヤー情報を受け取るメソッド
    /// （コンパイルエラーを防ぐために引数を int, bool に統一しています）
    /// </summary>
    /// <param name="playerId">プレイヤーのID（1Pか2Pか）</param>
    /// <param name="useController">コントローラー（ゲームパッド）を使用するかどうか</param>
    public void SetPlayerDevice(int playerId, bool useController)
    {
        this.myPlayerId = playerId;
        this.usesGamepad = useController;

        Debug.Log($"PlayerBase: プレイヤー {myPlayerId}P のデバイス設定を適用しました。Controller={usesGamepad}");
    }

    // 大塚駅北口は空いてないby Taiga Sato
    private void OnDisable()
    {
        if (testplayerControl != null)
        {
            testplayerControl.Disable();
        }
    }
}