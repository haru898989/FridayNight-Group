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
    public float lookSpeed = 80f; // 視点の移動の速度
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
    private GameObject stampDisplay;
    //public GameObject[] stampObjects; // スタンプの数
    public float stampDisplayTime = 2f; // スタンプ表示時間（秒）
    private Coroutine stampCoroutine; // コルーチン管理用
    [SerializeField]
    private Animator animator;
    private int selectedIndex = 0;
    private GameObject[] stampMenuObjects;//UIようのはいれつ

    [Header("Character Visual")]
    [SerializeField] private GameObject characterModelPrefab;
    [SerializeField] private Vector3 characterModelLocalPosition = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 characterModelLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 characterModelLocalScale = Vector3.one;
    [SerializeField] private Transform stampAnchor;
    [SerializeField] private GameObject[] stampObjects;
    [SerializeField] private GameObject[] stampMenuIcons;

    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private Camera playerViewCamera;
    private AudioListener playerViewAudioListener;
    private PitfallCameraController[] pitfallCameraControllers;
    private GameObject characterModelInstance;
    private Vector3 previousRenderPosition;
    private bool hasPreviousRenderPosition;
    private bool isGoalSpectating;
    private bool isGoalSequenceStarted;
    private Coroutine goalPresentationCoroutine;
    private Transform goalSpectateTarget;
    private Vector3 defaultPlayerCameraLocalPosition;
    private Quaternion defaultPlayerCameraLocalRotation;
    private bool hasDefaultPlayerCameraTransform;

    public bool canMove = true;
    public bool IsGoalSpectating => isGoalSpectating;

    protected virtual bool UsePlayerInput => true;

    // GameManagerから同期される情報の格納用変数
    private int myPlayerId;
    private bool usesGamepad;

    public override void Spawned()
    {
        Debug.Log($"Spawned実行: {gameObject.name}, InputAuthority={HasInputAuthority}");

        characterController = GetComponent<CharacterController>();
        playerRigidbody = GetComponent<Rigidbody>();

        // StampAnchorを自動取得
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform t in children)
        {
            if (t.name == "StampAnchor")
            {
                stampAnchor = t;
                break;
            }
        }

        if (stampAnchor == null)
        {
            Debug.LogError("StampAnchorが見つかりません");
        }
        else
        {
            // 子オブジェクトを自動登録
            stampObjects = new GameObject[stampAnchor.childCount];

            for (int i = 0; i < stampAnchor.childCount; i++)
            {
                stampObjects[i] = stampAnchor.GetChild(i).gameObject;
                stampObjects[i].SetActive(false);
            }

            Debug.Log($"スタンプを {stampObjects.Length} 個登録しました");
        }

        GameObject[] objs = FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in objs)
        {
            if (obj.name.Contains("Stamp"))
            {
                Debug.Log("見つかった: " + obj.name);
            }
        }
        for (int i = 0; i < stampObjects.Length; i++)
        {
            if (stampObjects[i] != null)
                stampObjects[i].SetActive(false);
        }
        stampDisplay = GameObject.Find("StampDisplay");

        // 各クライアントでは、自分が操作するプレイヤーだけをギミックの対象にする。
        // これによりリモートプレイヤーの接触でローカル演出が二重起動するのを防ぐ。
        gameObject.tag = HasInputAuthority ? "Player" : "Untagged";

        CreateCharacterVisual();
        previousRenderPosition = transform.position;
        hasPreviousRenderPosition = true;

        // 入力を持たないリモートプレイヤーが、ローカルのキー入力で
        // 石板やクリスタルを操作しないようにする。
        StoneTabletCarrier tabletCarrier = GetComponent<StoneTabletCarrier>();
        if (tabletCarrier != null)
        {
            tabletCarrier.enabled = HasInputAuthority;
        }

        PlayerCrystalHolder crystalHolder = GetComponent<PlayerCrystalHolder>();
        if (crystalHolder != null)
        {
            crystalHolder.enabled = HasInputAuthority;
        }

        Camera childCam = GetComponentInChildren<Camera>();
        if (childCam != null)
        {
            // 自分が操作するキャラ(HasInputAuthorityがtrue)だけカメラをONにする
            childCam.enabled = HasInputAuthority;

            if (HasInputAuthority)
            {
                playerViewCamera = childCam;
                defaultPlayerCameraLocalPosition = childCam.transform.localPosition;
                defaultPlayerCameraLocalRotation = childCam.transform.localRotation;
                hasDefaultPlayerCameraTransform = true;
                pitfallCameraControllers = FindObjectsOfType<PitfallCameraController>(true);
                DisableOtherMainCameras(childCam);
                childCam.gameObject.tag = "MainCamera";
                stampMenu = GameObject.Find("PlayerStampMenu");

                if (stampMenu == null)
                {
                    Debug.LogError("PlayerStampMenuが見つかりません");
                    return;
                }
                Debug.Log("StampMenu = " + stampMenu.name);
                Debug.Log("子オブジェクト数 = " + stampMenu.transform.childCount);

                for (int i = 0; i < stampMenu.transform.childCount; i++)
                {
                    Debug.Log($"子[{i}] = {stampMenu.transform.GetChild(i).name}");
                }
                stampMenuObjects = new GameObject[stampMenu.transform.childCount - 1];

                int menuIndex = 0;

                foreach (Transform child in stampMenu.transform)
                {
                    if (child.name == "StampBackground")
                        continue;

                    stampMenuObjects[menuIndex] = child.gameObject;
                    menuIndex++;
                }

            }

            // Unityの警告防止のため、AudioListener（耳）も同様に設定する
            AudioListener listener = childCam.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = HasInputAuthority;

                if (HasInputAuthority)
                {
                    playerViewAudioListener = listener;
                }
            }
            if (stampMenu == null)
            {
                stampMenu = GameObject.Find("StampMenu");
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

    private void LateUpdate()
    {
        if (playerViewCamera == null)
        {
            return;
        }

        if (isGoalSpectating)
        {
            UpdateGoalSpectatorCamera();
        }

        bool isPitfallCameraActive = false;
        if (pitfallCameraControllers != null)
        {
            for (int i = 0; i < pitfallCameraControllers.Length; i++)
            {
                PitfallCameraController controller = pitfallCameraControllers[i];
                Camera pitfallCamera = controller != null ? controller.GetComponent<Camera>() : null;

                if (pitfallCamera != null && pitfallCamera.enabled)
                {
                    isPitfallCameraActive = true;
                    break;
                }
            }
        }

        playerViewCamera.enabled = !isPitfallCameraActive;

        if (playerViewAudioListener != null)
        {
            playerViewAudioListener.enabled = !isPitfallCameraActive;
        }
    }

    public override void Render()
    {
        if (animator == null || HasInputAuthority)
        {
            return;
        }

        if (!hasPreviousRenderPosition)
        {
            previousRenderPosition = transform.position;
            hasPreviousRenderPosition = true;
            return;
        }

        float movedDistance = (transform.position - previousRenderPosition).sqrMagnitude;
        animator.SetBool("run", movedDistance > 0.000001f);
        previousRenderPosition = transform.position;
    }

    private void CreateCharacterVisual()
    {
        if (characterModelPrefab == null || characterModelInstance != null)
        {
            return;
        }

        characterModelInstance = Instantiate(characterModelPrefab, transform);
        characterModelInstance.name = characterModelPrefab.name + "_Visual";
        characterModelInstance.transform.localPosition = characterModelLocalPosition;
        characterModelInstance.transform.localRotation = Quaternion.Euler(characterModelLocalEulerAngles);
        characterModelInstance.transform.localScale = characterModelLocalScale;

        // 見た目用モデル側の物理コンポーネントはPlayerルートの判定と重複させない。
        Collider[] visualColliders = characterModelInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < visualColliders.Length; i++)
        {
            visualColliders[i].enabled = false;
        }

        Rigidbody[] visualRigidbodies = characterModelInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < visualRigidbodies.Length; i++)
        {
            visualRigidbodies[i].isKinematic = true;
            visualRigidbodies[i].detectCollisions = false;
        }

        Animator modelAnimator = characterModelInstance.GetComponentInChildren<Animator>(true);
        if (modelAnimator != null)
        {
            animator = modelAnimator;
            animator.SetBool("run", false);
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }

        MeshRenderer placeholderRenderer = GetComponent<MeshRenderer>();
        if (placeholderRenderer != null)
        {
            placeholderRenderer.enabled = false;
        }
    }

    private static void DisableOtherMainCameras(Camera ownedCamera)
    {
        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera sceneCamera = cameras[i];
            if (sceneCamera == null || sceneCamera == ownedCamera || !sceneCamera.CompareTag("MainCamera"))
            {
                continue;
            }

            sceneCamera.enabled = false;

            AudioListener listener = sceneCamera.GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
    }

    public void BeginGoalSpectatorMode()
    {
        if (!HasInputAuthority || isGoalSequenceStarted)
        {
            return;
        }

        isGoalSequenceStarted = true;
        isGoalSpectating = false;
        goalSpectateTarget = null;
        canMove = false;

        if (animator != null)
        {
            animator.SetBool("run", false);
        }

        goalPresentationCoroutine = StartCoroutine(ShowGoalCelebrationThenSpectate());
    }

    public void ReportGoalReachedToAllPlayers()
    {
        if (HasInputAuthority)
        {
            RPC_ReportGoalReached();
        }
    }

    private IEnumerator ShowGoalCelebrationThenSpectate()
    {
        GoalPresentationUI presentationUI = GoalPresentationUI.Instance;
        float presentationDuration = presentationUI != null
            ? presentationUI.CelebrationDuration
            : 2f;

        if (presentationUI != null)
        {
            presentationUI.PlayGoalCelebration();
        }

        Debug.Log("Local player reached the goal. Playing the goal celebration.");
        yield return new WaitForSecondsRealtime(presentationDuration);

        isGoalSpectating = true;
        goalSpectateTarget = null;
        goalPresentationCoroutine = null;

        if (presentationUI != null)
        {
            presentationUI.ShowSpectatorFrame();
        }

        Debug.Log("Goal celebration finished. Spectating the other player.");
    }

    public void ResetGoalSpectatorMode()
    {
        if (!HasInputAuthority)
        {
            return;
        }

        isGoalSpectating = false;
        isGoalSequenceStarted = false;
        goalSpectateTarget = null;
        canMove = true;

        if (goalPresentationCoroutine != null)
        {
            StopCoroutine(goalPresentationCoroutine);
            goalPresentationCoroutine = null;
        }

        if (GoalPresentationUI.Instance != null)
        {
            GoalPresentationUI.Instance.HideAll();
        }

        if (playerViewCamera != null && hasDefaultPlayerCameraTransform)
        {
            playerViewCamera.transform.localPosition = defaultPlayerCameraLocalPosition;
            playerViewCamera.transform.localRotation = defaultPlayerCameraLocalRotation;
            cameraRotationX = NormalizeSignedAngle(defaultPlayerCameraLocalRotation.eulerAngles.x);
        }
    }

    private void UpdateGoalSpectatorCamera()
    {
        if (goalSpectateTarget == null)
        {
            goalSpectateTarget = FindOtherPlayerCamera();
        }

        if (goalSpectateTarget != null)
        {
            playerViewCamera.transform.SetPositionAndRotation(
                goalSpectateTarget.position,
                goalSpectateTarget.rotation
            );
        }
    }

    private Transform FindOtherPlayerCamera()
    {
        NetworkRunner activeRunner = OnlineStageFlow.Instance != null
            ? OnlineStageFlow.Instance.Runner
            : Runner;

        if (activeRunner == null || !activeRunner.IsRunning)
        {
            return null;
        }

        foreach (PlayerRef player in activeRunner.ActivePlayers)
        {
            if (player == activeRunner.LocalPlayer)
            {
                continue;
            }

            if (!activeRunner.TryGetPlayerObject(player, out NetworkObject playerObject) ||
                playerObject == null)
            {
                continue;
            }

            PlayerBase otherPlayer = playerObject.GetComponent<PlayerBase>();
            if (otherPlayer != null)
            {
                return otherPlayer.playerCamera != null
                    ? otherPlayer.playerCamera
                    : otherPlayer.transform;
            }
        }

        return null;
    }

    private static float NormalizeSignedAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    public override void FixedUpdateNetwork()
    {
        if (!UsePlayerInput) return;
        if (!HasInputAuthority) return;
        if (testplayerControl == null) return;
        if (!canMove) return;

        // スタンプ選択中のみ十字キー（D-Pad）を読む
        if (isSelectingStamp)
        {
            Vector2 stampInput = testplayerControl.Player.StampSelect.ReadValue<Vector2>();
            Debug.Log("StampInput = " + stampInput);
            if (stampInput.magnitude > 0.5f)
            {
                float angle = Mathf.Atan2(stampInput.y, stampInput.x) * Mathf.Rad2Deg;

                if (angle < 0)
                    angle += 360;

                int index = Mathf.FloorToInt(angle / (360f / stampObjects.Length));

                selectedIndex = index;

                HighlightStamp(index);
                Debug.Log("Highlight : " + index);
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

        Vector3 move = transform.forward * input.y + transform.right * input.x;

        // BearTrapはRigidbodyをFreezeAllにするため、同じ状態を
        // CharacterController移動にも反映してプレイヤーを停止させる。
        if (playerRigidbody != null && playerRigidbody.constraints == RigidbodyConstraints.FreezeAll)
        {
            if (animator != null) animator.SetBool("run", false);
            return;
        }

        // ==========================================
        // ↓↓↓ 変更前（壁をすり抜ける） ↓↓↓
        // transform.position += move * moveSpeed * Runner.DeltaTime;
        // ==========================================

        // ==========================================
        // ↓↓↓ 変更後（壁でちゃんと止まる） ↓↓↓
        // ==========================================
        if (characterController != null && characterController.enabled)
        {
            // transform.positionの代わりに、cc.Moveを使うだけ！
            characterController.Move(move * moveSpeed * Runner.DeltaTime);
        }
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ReportGoalReached(RpcInfo info = default)
    {
        if (OnlineStageFlow.Instance == null)
        {
            return;
        }

        PlayerRef player = info.Source;
        if (player == PlayerRef.None && Object != null)
        {
            player = Object.InputAuthority;
        }

        OnlineStageFlow.Instance.ReportPlayerReachedGoal(player);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ShowStamp(int index)
    {
        ShowStamp(index);
    }

    /// <summary>
    /// スタンプの実装部分
    /// </summary>
    void ShowStamp(int index)
    {
        foreach (GameObject obj in stampObjects)
        {
            if (obj != null)
                Debug.Log($"OFF : {obj.name}");
            obj.SetActive(false);
        }

        // 選択したスタンプだけ表示
        if (index >= 0 && index < stampObjects.Length)
        {
            stampObjects[index].SetActive(true);
        }

        // 前のコルーチンを停止
        if (stampCoroutine != null)
        {
            StopCoroutine(stampCoroutine);
        }

        if (index >= 0 &&
        index < stampObjects.Length &&
        stampObjects[index] != null)
        {
            stampObjects[index].SetActive(true);
            stampCoroutine = StartCoroutine(HideStampAfterTime());
        }
    }

    void CloseStampMenu()
    {
        isSelectingStamp = false;

        if (stampMenu != null)
        {
            stampMenu.SetActive(false);

            selectedIndex = -1;

            HighlightStamp(selectedIndex);

        }

        Debug.Log("スタンプ決定");
    }

    IEnumerator HideStampAfterTime()
    {
        yield return new WaitForSeconds(stampDisplayTime);

        foreach (GameObject obj in stampObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    /// <summary>
    /// スタンプ選択開始
    /// </summary>
    private void OnStampStarted(InputAction.CallbackContext context)
    {
        isSelectingStamp = !isSelectingStamp;

        Debug.Log("Stampボタンが押された");

        if (stampMenu != null)
        {
            stampMenu.SetActive(isSelectingStamp);
            Debug.Log("StampMenu active = " + stampMenu.activeSelf);
        }

        Debug.Log("スタンプ選択開始");
    }

    /// <summary>
    /// スタンプ選択終了
    /// </summary>
    private void OnStampCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("selectedStampIndex = " + selectedIndex);
        // 何もしない
        isSelectingStamp = false;

        if (stampMenu != null)
        {
            stampMenu.SetActive(false);
        }

        Debug.Log("スタンプ選択終了");
        Debug.Log($"決定したIndex = {selectedIndex}");
        RPC_ShowStamp(selectedIndex);
        CloseStampMenu();
    }

    void HighlightStamp(int index)
    {
        for (int i = 0; i < stampMenuObjects.Length; i++)
        {
            if (stampMenuObjects[i] == null) continue;

            stampMenuObjects[i].transform.localScale =
                (i == index) ? Vector3.one * 1.3f : Vector3.one;
        }
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
