using UnityEngine;
using System.Collections;

public class BearTrap : GimmickBase
{
    [Header("とらばさみ設定")]
    [SerializeField] private float stopTime = 2.0f;

    protected override void OnPlayerHit(GameObject playerObject)
    {
        Debug.Log("とらばさみ発動");

        StartCoroutine(StopPlayer(playerObject));
    }

    private IEnumerator StopPlayer(GameObject playerObject)
    {
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();

        if (playerRigidbody == null)
        {
            yield break;
        }

        RigidbodyConstraints oldConstraints = playerRigidbody.constraints;

        playerRigidbody.velocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;

        yield return new WaitForSeconds(stopTime);

        playerRigidbody.constraints = oldConstraints;
    }
}