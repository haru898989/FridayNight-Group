using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Fusion;

public class PlayerBase : NetworkBehaviour
{
    private PlayerInputAction testplayerControl;
    public float moveSpeed = 5f; // 移動速度
    public Transform playerCamera;
    public float lookSpeed = 100f;//視点の移動の速度
    private float cameraRotationX = 0f;//カメラの位置
    public float holdThreshold = 0.5f;//ボタンの長押し判定
    private float pressStartTime;
    private bool isSelectingStamp = false;
    private GameObject heldObject;    // 持っている物
    public GameObject nearbyObject;   // 近くにある持てる物
    public GameObject selectableObject;    // 決定できる対象
    public float groundY = 0f; //地面の座標
    public Sprite[] stampSprites;//スタンプ画像
    public GameObject stampMenu;//スタンプの選択
    public GameObject[] stampObjects;//スタンプの数
    public float stampDisplayTime = 2f;   // スタンプ表示時間（秒）
    private Coroutine stampCoroutine;     // コルーチン管理用
    [SerializeField]
    private Animator animator;

    protected virtual bool UsePlayerInput => true;

    // GameManagerから同期される情報の格納用変数
    private int myPlayerId;
    private bool usesGamepad;

    public override void Spawned()
    {
        // 入力権限がないクライアントは初期化しない
        if (!HasInputAuthority) return;

        testplayerControl = new PlayerInputAction();
<<<<<<< HEAD

        // アクションのイベント登録
=======
>>>>>>> develop
        testplayerControl.Player.OnActionB.started += OnBStarted;
        testplayerControl.Player.OnActionB.canceled += OnBCanceled;
        testplayerControl.Player.Stamp.started += OnStampStarted;
        testplayerControl.Player.Stamp.canceled += OnStampCanceled;
<<<<<<< HEAD

        // UIなどの初期非表示設定
        if (stampMenu != null)
=======
        stampMenu.SetActive(false);
        if (animator == null)
>>>>>>> develop
        {
            Debug.LogError("Animatorがありません");
            return;
        }
<<<<<<< HEAD

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
=======
        Debug.Log(animator.gameObject.name);
        Debug.Log(animator.runtimeAnimatorController.name);
        for (int i = 0; i < stampObjects.Length; i++)

        if (UsePlayerInput) if (!HasInputAuthority) return;

        {

            testplayerControl = new PlayerInputAction();

            testplayerControl.Player.OnActionB.started += OnBStarted;
            testplayerControl.Player.OnActionB.canceled += OnBCanceled;
            testplayerControl.Player.Stamp.started += OnStampStarted;
            testplayerControl.Player.Stamp.canceled += OnStampCanceled;

            if (stampMenu != null)
            {
                stampMenu.SetActive(false);
            }

            for (int i = 0; i < stampObjects.Length; i++)
            {
                stampObjects[i].SetActive(false);
            }

            testplayerControl.Enable();
            playerCamera = Camera.main.transform;
>>>>>>> develop
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!UsePlayerInput) return;
        if (!HasInputAuthority) return;
        if (testplayerControl == null) return;
<<<<<<< HEAD

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
=======
>>>>>>> develop

        // スタンプ選択中のみ十字キー（D-Pad）を読む
        if (isSelectingStamp)
        {
            Vector2 stampInput = testplayerControl.Player.StampSelect.ReadValue<Vector2>();
            if (stampInput.y > 0.5f)
            {
                ShowStamp(0);
                CloseStampMenu();
            }
            else if (stampInput.y < -0.5f)
            {
                ShowStamp(1);
                CloseStampMenu();
            }
            else if (stampInput.x < -0.5f)
            {
                ShowStamp(2);
                CloseStampMenu();
            }
            else if (stampInput.x > 0.5f)
            {
                ShowStamp(3);
                CloseStampMenu();
            }
            return;
        }
<<<<<<< HEAD
=======
        //移動
        Vector2 input = testplayerControl.Player.Move.ReadValue<Vector2>();
        Debug.Log(input);
        float speed = input.magnitude;
        animator.SetBool("run", speed > 0.1f);
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        transform.position += move * moveSpeed * Time.deltaTime;
        Vector2 lookInput = testplayerControl.Player.Look.ReadValue<Vector2>();
        // 左右を見る（プレイヤー回転）
        transform.Rotate(Vector3.up * lookInput.x * lookSpeed * Time.deltaTime);
        // 上下を見る（カメラ回転）
        cameraRotationX -= lookInput.y * lookSpeed * Time.deltaTime;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
        playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
        animator.SetBool("run", speed > 0.1f);
        Debug.Log(animator.GetBool("run"));
        Debug.Log(animator.runtimeAnimatorController.name);
>>>>>>> develop
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
<<<<<<< HEAD
    {
        if (stampSprites == null || index < 0 || index >= stampSprites.Length)
            return;

        if (stampObjects == null || index < 0 || index >= stampObjects.Length)
=======
    {   
        if (index < 0 || index >= stampObjects.Length)
>>>>>>> develop
            return;
        // 全部非表示
        for (int i = 0; i < stampObjects.Length; i++)
        {
            if (stampObjects[i] != null)
            {
                stampObjects[i].SetActive(i == index);
            }
        }
        // 前のコルーチンを停止
        if (stampCoroutine != null)
        {
            StopCoroutine(stampCoroutine);
        }

        // 新しく表示時間をカウント
        stampCoroutine = StartCoroutine(HideStampAfterTime());
    }

    void CloseStampMenu()
    {
        isSelectingStamp = false;
        stampMenu.SetActive(false);

        Debug.Log("スタンプ決定");
    }

    IEnumerator HideStampAfterTime()
    {
<<<<<<< HEAD
        yield return new WaitForSeconds(2f);
        if (stampObjects != null)
=======
        yield return new WaitForSeconds(stampDisplayTime);
        for (int i = 0; i < stampObjects.Length; i++)
>>>>>>> develop
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

        isSelectingStamp = !isSelectingStamp;
        stampMenu.SetActive(isSelectingStamp);

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

        //何もしない

        isSelectingStamp = false;

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