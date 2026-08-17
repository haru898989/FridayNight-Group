using Photon.Voice.Unity;
using UnityEngine;

public class VoiceController : MonoBehaviour
{
    [SerializeField] private float voiceRange = 5f;

    private AudioSource voiceAudioSource;
    private Speaker speaker;

    private void Update()
    {
        // Speaker / AudioSourceを取得
        if (voiceAudioSource == null)
        {
            speaker = GetComponent<Speaker>();

            if (speaker == null)
            {
                return;
            }

            voiceAudioSource = speaker.GetComponent<AudioSource>();

            if (voiceAudioSource == null)
            {
                voiceAudioSource =
                    speaker.GetComponentInChildren<AudioSource>(true);
            }

            if (voiceAudioSource == null)
            {
                return;
            }

            // 3D音声
            voiceAudioSource.spatialBlend = 1f;
            voiceAudioSource.dopplerLevel = 0f;
        }

        // 自分自身のPlayerを取得
        GameObject localPlayer = GetLocalPlayer();

        if (localPlayer == null)
        {
            return;
        }

        // 自分自身の場合は音声を聞こえる状態にする
        if (localPlayer == gameObject)
        {
            voiceAudioSource.mute = false;
            return;
        }

        // 自分と発話者との距離
        float distance = Vector3.Distance(
            localPlayer.transform.position,
            transform.position
        );

        // 5m以内なら聞こえる
        // 5mを超えたらミュート
        voiceAudioSource.mute = distance > voiceRange;
    }

    private GameObject GetLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            // Player側に付いているVoiceControllerを探す
            VoiceController controller =
                player.GetComponent<VoiceController>();

            if (controller != null)
            {
                // 自分のPlayerかどうかを判定
                Photon.Voice.Fusion.VoiceNetworkObject voiceObject =
                    player.GetComponent<Photon.Voice.Fusion.VoiceNetworkObject>();

                if (voiceObject != null && voiceObject.IsLocal)
                {
                    return player;
                }
            }
        }

        return null;
    }
}