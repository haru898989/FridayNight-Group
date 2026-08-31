using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Fusion;
using UnityEngine.InputSystem.Switch;

public class PlayerBase : NetworkBehaviour
{
    private PlayerInputAction testplayerControl;
    public float moveSpeed = 5f; // 移動速度
    public Transform playerCamera;
    public float lookSpeed = 80f; // 視点の移動の速度
    private float cameraRotationX = 0f; // カメラの位置
    public float holdThreshold = 0.5f; // ボタンの長押し判定
    private float pressStartTime;
    private bool keyboardPickupHandled;
    private bool isSelectingStamp = false;
    protected GameObject heldObject; // 持っている物
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
    private float previousStampNavigationAxis;
    private float nextStampNavigationTime;

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

    //NPC
    public enum StampCommand
    {
        Patrol,  //自由行動
        FollowPlayer,  //プレイヤーについてくる
        MoveToTarget,  //指示された場所へ移動
        Action,  //指示された行動
        Stop,  //止まる
        MoveForward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        SolveOtherGimmick
    }

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
                stampMenuObjects = new GameObject[9];

                foreach (Transform child in stampMenu.transform)
                {
                    if (child.name == "StampBackground")
                        continue;

                    int menuIndex = GetStampMenuIndex(child.name);
                    if (menuIndex >= 0 && menuIndex < stampMenuObjects.Length)
                    {
                        stampMenuObjects[menuIndex] = child.gameObject;
                    }
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

        if (HasInputAuthority)
        {
            HideLocalPlayerHat();
        }

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

    /// <summary>
    /// 一人称視点を遮る帽子だけを、このクライアント上の自分のモデルから非表示にする。
    /// リモートプレイヤーのモデルは変更しないため、相手からは帽子が見える。
    /// </summary>
    private void HideLocalPlayerHat()
    {
        Transform[] visualTransforms = characterModelInstance.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < visualTransforms.Length; i++)
        {
            Transform visualTransform = visualTransforms[i];
            string objectName = visualTransform.name.ToLowerInvariant();

            if (!objectName.Contains("hat") && !objectName.Contains("fedora"))
            {
                continue;
            }

            Renderer[] hatRenderers = visualTransform.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < hatRenderers.Length; rendererIndex++)
            {
                hatRenderers[rendererIndex].enabled = false;
            }
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

        // スタンプ選択中は横入力またはマウスホイールで順番に選ぶ。
        if (isSelectingStamp)
        {
            Vector2 stampInput = testplayerControl.Player.StampSelect.ReadValue<Vector2>();
            float navigationAxis = Mathf.Abs(stampInput.x) >= 0.45f
                ? stampInput.x
                : stampInput.y;
            bool isPressed = Mathf.Abs(navigationAxis) >= 0.5f;
            bool wasReleased = Mathf.Abs(previousStampNavigationAxis) < 0.5f;

            if (isPressed &&
                (wasReleased || Time.unscaledTime >= nextStampNavigationTime) &&
                stampMenuObjects != null && stampMenuObjects.Length > 0)
            {
                // 右入力で次のスタンプ、左入力で前のスタンプへ移動する。
                int direction = navigationAxis > 0f ? 1 : -1;
                selectedIndex = (selectedIndex + direction + stampMenuObjects.Length) %
                                stampMenuObjects.Length;
                HighlightStamp(selectedIndex);
                nextStampNavigationTime = Time.unscaledTime + (wasReleased ? 0.3f : 0.16f);
            }

            previousStampNavigationAxis = isPressed ? navigationAxis : 0f;

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
        if (!canMove)
        {
            return;
        }

        // キーボードのEは長押し待ちをせず、押した瞬間に持つ／離す。
        // 持てる物がない場合は、既存どおり短押し決定として扱う。
        // ゲームパッドのBは既存どおり長押し操作を維持する。
        if (context.control.device is Keyboard)
        {
            GameObject pickup = heldObject == null ? FindClosestPickup() : null;
            if (heldObject != null || pickup != null)
            {
                if (pickup != null)
                {
                    nearbyObject = pickup;
                }

                keyboardPickupHandled = true;
                HoldObject();
                return;
            }

            pressStartTime = Time.time;
            return;
        }

        pressStartTime = Time.time;
    }

    /// <summary>
    /// 長押しの判定（ボタンが離された瞬間）
    /// </summary>
    private void OnBCanceled(InputAction.CallbackContext context)
    {
        if (keyboardPickupHandled)
        {
            keyboardPickupHandled = false;
            return;
        }

        if (!canMove)
        {
            return;
        }

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
            if (nearbyObject == null)
            {
                nearbyObject = FindClosestPickup();
            }

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

                SetHeldObjectCollidersEnabled(heldObject, false);

                Debug.Log("物を持った");
            }
        }
        else
        {
            GameObject objectToDrop = heldObject;
            Vector3 dropPosition = FindDropPosition(objectToDrop);

            objectToDrop.transform.SetParent(null);
            objectToDrop.transform.position = dropPosition;
            SetHeldObjectCollidersEnabled(objectToDrop, true);
            heldObject = null;

            Debug.Log("物を離した");
        }
    }

    private GameObject FindClosestPickup()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            2f,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );

        GameObject closestPickup = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Transform current = nearbyColliders[i].transform;
            while (current != null && !current.CompareTag("Pickup"))
            {
                current = current.parent;
            }

            if (current == null)
            {
                continue;
            }

            float sqrDistance = (current.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestPickup = current.gameObject;
            }
        }

        return closestPickup;
    }

    private Vector3 FindDropPosition(GameObject target)
    {
        Vector3 rayOrigin = transform.position + Vector3.up;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            10f,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;
        bool foundFloor = false;
        Vector3 floorPoint = transform.position + Vector3.down;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].collider.transform;
            if (hitTransform.IsChildOf(transform) || hitTransform.IsChildOf(target.transform))
            {
                continue;
            }

            if (hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                floorPoint = hits[i].point;
                foundFloor = true;
            }
        }

        float bottomOffset = 0.08f;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            float lowestPoint = renderers[0].bounds.min.y;
            for (int i = 1; i < renderers.Length; i++)
            {
                lowestPoint = Mathf.Min(lowestPoint, renderers[i].bounds.min.y);
            }

            bottomOffset = Mathf.Max(0.01f, target.transform.position.y - lowestPoint);
        }

        Vector3 dropPosition = new Vector3(transform.position.x, floorPoint.y, transform.position.z);
        dropPosition.y += bottomOffset;

        if (!foundFloor)
        {
            dropPosition.y = transform.position.y - 1f + bottomOffset;
        }

        return dropPosition;
    }

    protected static void SetHeldObjectCollidersEnabled(GameObject target, bool isEnabled)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = isEnabled;
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
    protected virtual void OnTriggerEnter(Collider other)
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
    protected virtual void OnTriggerExit(Collider other)
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

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_RemoveBearTrap(Vector3 trapPosition)
    {
        BearTrap.ConsumeAtPosition(trapPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ShowStamp(int index)
    {
        ShowStamp(index);

        StampIndicatorUI indicator = StampIndicatorUI.Instance;
        if (indicator == null)
        {
            indicator = FindObjectOfType<StampIndicatorUI>(true);
        }

        if (indicator == null)
        {
            Debug.LogWarning("StampIndicatorUIがMapシーンに見つかりません");
            return;
        }

        if (HasInputAuthority)
        {
            indicator.ShowSentFeedback(index);
        }
        else
        {
            indicator.ShowRemoteStamp(transform, Camera.main, index, stampDisplayTime);
        }
    }

    /// <summary>
    /// スタンプの実装部分
    /// </summary>
    protected void ShowStamp(int index)
    {
        if (stampObjects == null)
        {
            return;
        }

        foreach (GameObject obj in stampObjects)
        {
            if (obj != null)
            {
                Debug.Log($"OFF : {obj.name}");
                obj.SetActive(false);
            }
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendStampCommand(int command, RpcInfo info = default)
    {
        StampCommand stampCommand = (StampCommand)command;

        Debug.Log($"StampCommand受信: {stampCommand}");

        NPCBase npc = FindObjectOfType<NPCBase>();
        if(npc == null)
        {
            Debug.Log("現在NPCはいません．");
            return;
        }
        npc.RPC_ReceiveStampCommand((int)stampCommand);
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
        isSelectingStamp = true;
        selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;
        previousStampNavigationAxis = 0f;
        nextStampNavigationTime = 0f;

        Debug.Log("Stampボタンが押された");

        if (stampMenu != null)
        {
            stampMenu.SetActive(isSelectingStamp);
            Debug.Log("StampMenu active = " + stampMenu.activeSelf);
        }

        HighlightStamp(selectedIndex);

        Debug.Log("スタンプ選択開始");
    }

    /// <summary>
    /// スタンプ選択終了
    /// </summary>
    private void OnStampCanceled(InputAction.CallbackContext context)
    {
        if (!isSelectingStamp)
        {
            return;
        }

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
        StampCommand command = GetStampCommand(selectedIndex);
        RPC_SendStampCommand((int)command);
        CloseStampMenu();
    }


    //Stampの種類
    private StampCommand GetStampCommand(int index)
    {
        switch(index)
        {
            case 0:
                return StampCommand.FollowPlayer;
            case 1:
                return StampCommand.Stop;
            case 2:
                return StampCommand.Action;
            case 3:
                return StampCommand.Patrol;
            case 4:
                return StampCommand.MoveForward;
            case 5:
                return StampCommand.MoveBackward;
            case 6:
                return StampCommand.MoveLeft;
            case 7:
                return StampCommand.MoveRight;
            case 8:
                return StampCommand.SolveOtherGimmick;
            default:
                return StampCommand.Patrol;
        }
    }

    void HighlightStamp(int index)
    {
        if (stampMenuObjects == null)
        {
            return;
        }

        for (int i = 0; i < stampMenuObjects.Length; i++)
        {
            if (stampMenuObjects[i] == null) continue;

            stampMenuObjects[i].transform.localScale =
                (i == index) ? Vector3.one * 1.35f : Vector3.one;

            Image image = stampMenuObjects[i].GetComponent<Image>();
            if (image != null)
            {
                image.color = i == index
                    ? new Color(1f, 0.9f, 0.3f, 1f)
                    : new Color(0.65f, 0.65f, 0.65f, 1f);
            }
        }
    }

    private static int GetStampMenuIndex(string objectName)
    {
        switch (objectName)
        {
            case "Come On":
                return 0;
            case "Stop":
                return 1;
            case "Gimmik":
                return 2;
            case "Free":
                return 3;
            case "Up":
                return 4;
            case "Down":
                return 5;
            case "Left":
                return 6;
            case "Right":
                return 7;
            case "Other Gimmick":
                return 8;
            default:
                return -1;
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


    private void OnDisable()
    {
        if (testplayerControl != null)
        {
            testplayerControl.Disable();
        }
    }
}
