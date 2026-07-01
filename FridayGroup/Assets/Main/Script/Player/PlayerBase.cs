using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBase : MonoBehaviour
{
    private PlayerInputAction testplayerControl;
    private Vector2 moveInput;   // 入力値
    public float moveSpeed = 5f; // 移動速度
    public float lookSpeed = 100f;//視点の移動速度
    public float holdThreshold = 0.5f;//ボタンの長押し判定
    private float pressStartTime;
    private GameObject heldObject;    // 持っている物
    public GameObject nearbyObject;   // 近くにある持てる物
    public GameObject selectableObject;    // 決定できる対象

    // Start is called before the first frame update
    protected virtual void Start()
    {
     
        testplayerControl = new PlayerInputAction();
        testplayerControl.Player.OnActionB.started += OnBStarted;
        testplayerControl.Player.OnActionB.canceled += OnBCanceled;

        testplayerControl.Enable();
    }

    // Update is called once per frame
    public virtual void Update()
    {
        //移動
        Vector2 input = testplayerControl.Player.Move.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        transform.position += move * moveSpeed * Time.deltaTime;

        //視点の移動
        Vector2 lookInput = testplayerControl.Player.Look.ReadValue<Vector2>();
        transform.Rotate(0, lookInput.x * lookSpeed * Time.deltaTime, 0);

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

        if (pressDuration >= holdThreshold)
        {
            Debug.Log("B長押し");
            HoldObject();
        }
        else
        {
            Debug.Log("B短押し");
            ConfirmSelection();
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
    /// 移動の入力
    /// </summary>
    /// <param name="context"></param>
    public virtual void OnMove(InputAction.CallbackContext context)
    {
        // スティックの傾き具合をVector2(X, Y)で受け取る
        moveInput = context.ReadValue<Vector2>();
    }
    //void Move()
    //{
        // 2D入力 → 3D座標に変換
      //  Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        // 移動
        //transform.position += move * moveSpeed * Time.deltaTime;
    //}

    /// <summary>
    /// Aボタンが押された時の実行
    /// </summary>
    /// <param name="context"></param>

}
