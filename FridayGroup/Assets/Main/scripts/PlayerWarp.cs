using UnityEngine;

public class PlayerWarp : MonoBehaviour
{
    private Rigidbody playerRigidbody;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    public void WarpToPosition(float x, float y, float z)
    {
        Vector3 targetPosition = new Vector3(x, y, z);

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = targetPosition;
        }
        else
        {
            transform.position = targetPosition;
        }
    }
}