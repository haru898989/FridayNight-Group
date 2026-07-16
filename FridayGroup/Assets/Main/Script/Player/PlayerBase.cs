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
    public float lookSpeed = 100f; // 視点の移動の速度
    private float cameraRotationX = 0f; // カメラの位置
    public float holdThreshold = 0.5f; // ボタンの長押し判定
    private float pressStartTime;
    private bool isSelectingStamp = false;
    private GameObject heldObject; // 持っている物
    public GameObject nearbyObject; // 近くにある持てる物
    public GameObject selectableObject; // 決定できる対象
    public float groundY = 0f; // 地面の座標
    public Sprite[] stampSprites; // スタンプ画像
    public GameObject stampMenu; // スタンプの選択
    public GameObject[] stampObjects; // スタンプの数
    public float stampDisplayTime = 2f; // スタンプ表示時間（秒）
    private Coroutine stampCoroutine; // コルーチン管理用
    [SerializeField]
    private Animator animator;

    protected virtual bool UsePlayerInput => true;

    // GameManagerから同期される情報の格納用変数
    private int myPlayerId;
    private bool usesGamepad;

    public override void Spawned()
    {
        Debug.Log($"Spawned実行: {gameObject.name}, InputAuthority={HasInputAuthority}");

        Camera childCam = GetComponentInChildren<Camera>();
        if (childCam != null)
        {
            // 自分が操作するキャラ(HasInputAuthorityがtrue)だけカメラをONにする
            childCam.enabled = HasInputAuthority;

            // Unityの警告防止のため、AudioListener（耳）も同様に設定する
            AudioListener listener = childCam.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = HasInputAuthority;
            }
        }

        Debug.Log($"Spawned実行 Player={gameObject.name}, InputAuthority={HasInputAuthority}");

        // 入力権限がないクライアントは初期化しない
        if (!HasInputAuthority)
        {
            Debug.Log("入力権限がないためPlayerBase初期化終了");
            return;
        }

        testplayerControl = new PlayerInputAction();

        // アクションのイベント登録
        testplayerControl.Player.OnActionB.started += OnBStarted;
        testplayerControl.Player.OnActionB.canceled += OnBCanceled;
        testplayerControl.Player.Stamp.started += OnStampStarted;
        testplayerControl.Player.Stamp.canceled += OnStampCanceled;

        testplayerControl.Player.Enable();

        Debug.Log("PlayerBase入力開始");

        // UIなどの初期非表示設定
        if (stampMenu != null)
            stampMenu.SetActive(false);

        if (animator == null)
        {
            Debug.Log("Animatorがありません。アニメーションなしで続行します。");
        }
        else
        {
            Debug.Log(animator.gameObject.name);
            Debug.Log(animator.runtimeAnimatorController?.name);
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


    }

    public override void FixedUpdateNetwork()
    {
        //Debug.Log("Authority : " + Object.HasStateAuthority);

        if (!UsePlayerInput) return;
        if (!HasInputAuthority) return;
        if (testplayerControl == null) return;

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

            // スタンプ選択中は移動処理を行わない
            if (animator != null) animator.SetBool("run", false);
            return;
        }

        // --- 移動処理 ---
        Vector2 input = testplayerControl.Player.Move.ReadValue<Vector2>();

        if (input != Vector2.zero)
        {
            Debug.Log($"Move入力:{input}");
        }
        float speed = input.magnitude;


        if (animator != null)
        {
            animator.SetBool("run", speed > 0.1f);
        }

        // プレイヤーの向きを基準にした移動 (Time.deltaTime ではなく Runner.DeltaTime を使用)
        Vector3 move = transform.forward * input.y + transform.right * input.x;
        transform.position += move * moveSpeed * Runner.DeltaTime;

        // --- 視点（カメラ回転）処理 ---
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
        if (stampObjects == null || index < 0 || index >= stampObjects.Length)
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

        if (stampMenu != null)
        {
            stampMenu.SetActive(false);
        }

        Debug.Log("スタンプ決定");
    }

    IEnumerator HideStampAfterTime()
    {
        yield return new WaitForSeconds(stampDisplayTime);

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
        isSelectingStamp = !isSelectingStamp;

        if (stampMenu != null)
        {
            stampMenu.SetActive(isSelectingStamp);
        }

        Debug.Log("スタンプ選択開始");
    }

    /// <summary>
    /// スタンプ選択終了
    /// </summary>
    private void OnStampCanceled(InputAction.CallbackContext context)
    {
        // 何もしない
        isSelectingStamp = false;

        if (stampMenu != null)
        {
            stampMenu.SetActive(false);
        }

        Debug.Log("スタンプ選択終了");
    }

    /// <summary>
    /// GameManagerなどからプレイヤー情報を受け取るメソッド
    /// </summary>
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