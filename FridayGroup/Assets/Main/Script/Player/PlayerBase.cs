using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class PlayerBase : MonoBehaviour
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


    // Start is called before the first frame update
    protected virtual void Start()
    {

        testplayerControl = new PlayerInputAction();
        testplayerControl.Player.OnActionB.started += OnBStarted;
        testplayerControl.Player.OnActionB.canceled += OnBCanceled;
        testplayerControl.Player.Stamp.started += OnStampStarted;
        testplayerControl.Player.Stamp.canceled += OnStampCanceled;
        stampMenu.SetActive(false);
        if (animator == null)
        {
            Debug.LogError("Animatorがありません");
            return;
        }
        Debug.Log(animator.gameObject.name);
        Debug.Log(animator.runtimeAnimatorController.name);
        for (int i = 0; i < stampObjects.Length; i++)
        {
            stampObjects[i].SetActive(false);
        }

        testplayerControl.Enable();
    }

    // Update is called once per frame
    public virtual void Update()
    {
        Vector2 input = Vector2.zero;
        // スタンプ選択中のみ十字キーを読む
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
        //移動
        input = testplayerControl.Player.Move.ReadValue<Vector2>();
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
    }
    /// <summary>
    /// 長押しの処理
    /// </summary>
    private void OnBStarted(InputAction.CallbackContext context)
    {
        pressStartTime = Time.time;
    }

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
    /// <param name="other"></param>
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
    /// <param name="other"></param>
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
    /// <param name="index"></param>
    void ShowStamp(int index)
    {   
        if (index < 0 || index >= stampObjects.Length)
            return;
        // 全部非表示
        for (int i = 0; i < stampObjects.Length; i++)
        {
            stampObjects[i].SetActive(i == index);
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
        yield return new WaitForSeconds(stampDisplayTime);
        for (int i = 0; i < stampObjects.Length; i++)
        {
            stampObjects[i].SetActive(false);
        }
    }


    /// <summary>
    /// スタンプ選択の処理
    /// </summary>
    /// <param name="context"></param>
    private void OnStampStarted(InputAction.CallbackContext context)
    {
        isSelectingStamp = !isSelectingStamp;
        stampMenu.SetActive(isSelectingStamp);
        Debug.Log("スタンプ選択開始");
    }


    private void OnStampCanceled(InputAction.CallbackContext context)
    {
        //何もしない
        Debug.Log("スタンプ選択終了");
    }
    //大塚駅北口は空いてないby Taiga Sato
}
