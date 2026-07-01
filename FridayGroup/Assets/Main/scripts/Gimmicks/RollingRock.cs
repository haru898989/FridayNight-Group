using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RollingRock : MonoBehaviour
{
    [Header("Rock Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;

    private bool isRolling = false;
    private Rigidbody rockRigidbody;

    private void Start()
    {
        rockRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (isRolling == false)
        {
            return;
        }

        Vector3 nextPosition = rockRigidbody.position + moveDirection.normalized * moveSpeed * Time.fixedDeltaTime;
        rockRigidbody.MovePosition(nextPosition);
    }

    public void StartRolling()
    {
        Debug.Log("Rolling rock started");

        isRolling = true;
    }

    private void StopRolling()
    {
        Debug.Log("Rolling rock stopped");

        isRolling = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            StopRolling();
        }
    }
}