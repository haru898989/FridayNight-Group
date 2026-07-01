using UnityEngine;
using System.Collections;

public class Pitfall : GimmickBase
{
    [Header("Pitfall Settings")]
    [SerializeField] private StageController stageController;
    [SerializeField] private float fallTime = 1.0f;
    [SerializeField] private float fallDistance = 3.0f;

    private bool isWarping = false;

    protected override void OnPlayerHit(GameObject playerObject)
    {
        if (isWarping)
        {
            return;
        }

        StartCoroutine(FallAndWarp(playerObject));
    }

    private IEnumerator FallAndWarp(GameObject playerObject)
    {
        isWarping = true;

        Debug.Log("Pitfall started");

        Collider playerCollider = playerObject.GetComponent<Collider>();
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        Vector3 startPosition = playerObject.transform.position;
        Vector3 fallPosition = startPosition + Vector3.down * fallDistance;

        float timer = 0.0f;

        while (timer < fallTime)
        {
            timer += Time.deltaTime;

            float rate = timer / fallTime;
            playerObject.transform.position = Vector3.Lerp(startPosition, fallPosition, rate);

            yield return null;
        }

        if (stageController == null)
        {
            Debug.LogWarning("StageController is not set");

            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }

            isWarping = false;
            yield break;
        }

        Vector3 warpPosition = stageController.GetPitfallWarpPosition();

        PlayerWarp playerWarp = playerObject.GetComponent<PlayerWarp>();

        if (playerWarp != null)
        {
            playerWarp.WarpToPosition(warpPosition.x, warpPosition.y, warpPosition.z);
        }
        else
        {
            playerObject.transform.position = warpPosition;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        Debug.Log("Pitfall finished");

        isWarping = false;
    }
}