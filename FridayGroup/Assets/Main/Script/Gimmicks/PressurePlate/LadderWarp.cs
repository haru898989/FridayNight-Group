using System.Collections;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LadderWarp : MonoBehaviour
{
    [Header("Warp Settings")]
    [SerializeField] private Vector3 exitOffset = new Vector3(0.0f, 5.0f, 0.0f);
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float blackScreenTime = 0.15f;

    private bool isWarping;
    private float fadeAlpha;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isWarping)
        {
            return;
        }

        NPCBase npc = other.GetComponentInParent<NPCBase>();

        if(npc != null)
        {
            return;
        }

        GameObject playerObject = FindPlayerRoot(other);
        if (playerObject == null)
        {
            return;
        }

        NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
        if (networkObject != null && !networkObject.HasStateAuthority)
        {
            return;
        }

        StartCoroutine(WarpToUpperFloor(playerObject, networkObject));
    }

    private IEnumerator WarpToUpperFloor(GameObject playerObject, NetworkObject networkObject)
    {
        isWarping = true;

        CharacterController controller = playerObject.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
        {
            controller.enabled = false;
        }

        PlayerBase playerBase = playerObject.GetComponent<PlayerBase>();
        bool playerCouldMove = playerBase != null && playerBase.canMove;
        if (playerBase != null)
        {
            playerBase.canMove = false;
        }

        yield return FadeTo(1.0f);

        Vector3 targetPosition = transform.position + exitOffset;
        Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = targetPosition;
        }

        NetworkTransform networkTransform = playerObject.GetComponent<NetworkTransform>();
        if (networkObject != null && networkObject.HasStateAuthority && networkTransform != null)
        {
            networkTransform.Teleport(targetPosition, playerObject.transform.rotation);
        }

        playerObject.transform.position = targetPosition;
        Physics.SyncTransforms();

        yield return new WaitForSeconds(blackScreenTime);

        if (controllerWasEnabled)
        {
            controller.enabled = true;
        }

        if (playerBase != null)
        {
            playerBase.canMove = playerCouldMove;
        }

        yield return FadeTo(0.0f);
        isWarping = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = fadeAlpha;

        if (fadeDuration <= 0.0f)
        {
            fadeAlpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0.0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeAlpha = targetAlpha;
    }

    private static GameObject FindPlayerRoot(Collider other)
    {
        Transform current = other.transform;

        while (current != null)
        {
            if (current.GetComponent<PlayerBase>() != null || current.CompareTag("Player"))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private void OnGUI()
    {
        if (fadeAlpha <= 0.0f)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.depth = -1000;
        GUI.color = new Color(0.0f, 0.0f, 0.0f, fadeAlpha);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    //NPC
    public void UseByNpc(NPCBase npc)
    {

        if(npc == null)
        {
            return;
        }

        Vector3 destination = transform.position + exitOffset;
        npc.WarpToNavMesh(destination);
    }
}
