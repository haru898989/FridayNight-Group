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

    protected virtual bool UsePlayerInput => true;


    // Start is called before the first frame update
    public override void Spawned()
    {
        if (!HasInputAuthority) return;

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
        }
    }

    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {
        if (!UsePlayerInput) return;
        if (!HasInputAuthority) return;
        if (testplayerControl == null) return;
        if (!isSelectingStamp)
        {
            //移動
            Vector2 input = testplayerControl.Player.Move.ReadValue<Vector2>();
            Vector3 move = new Vector3(input.x, 0, input.y);
            transform.position += move * moveSpeed * Runner.DeltaTime;
            Vector2 lookInput = testplayerControl.Player.Look.ReadValue<Vector2>();
            // 左右を見る（プレイヤー回転）
            transform.Rotate(Vector3.up * lookInput.x * lookSpeed * Runner.DeltaTime);
            // 上下を見る（カメラ回転）
            cameraRotationX -= lookInput.y * lookSpeed * Runner.DeltaTime;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
            playerCamera.localRotation = Quaternion.Euler(cameraRotationX, 0, 0);
        }

        // スタンプ選択中のみ十字キーを読む
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
        if (index < 0 || index >= stampSprites.Length)
            return;

        if (index < 0 || index >= stampObjects.Length)
            return;

        for (int i = 0; i < stampObjects.Length; i++)
        {
            stampObjects[i].SetActive(i == index);
        }

        StartCoroutine(HideStampAfterTime());
    }

    IEnumerator HideStampAfterTime()
    {
        yield return new WaitForSeconds(2f);
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
        isSelectingStamp = true;

        if (stampMenu != null)
        {
            stampMenu.SetActive(true);
        }

        Debug.Log("スタンプ選択開始");
    }


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

    //大塚駅北口は空いてないby Taiga Sato
    private void OnDisable()
    {
        if (testplayerControl != null)
        {
            testplayerControl.Disable();
        }
    }
}
