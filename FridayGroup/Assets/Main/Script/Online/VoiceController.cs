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

            voiceAudioSource =
                speaker.GetComponent<AudioSource>();

            if (voiceAudioSource == null)
            {
                voiceAudioSource =
                    speaker.GetComponentInChildren<AudioSource>(true);
            }

            if (voiceAudioSource == null)
            {
                return;
            }

            voiceAudioSource.spatialBlend = 1f;
            voiceAudioSource.dopplerLevel = 0f;
        }

        // 自分のPlayerを取得
        GameObject localPlayer = GetLocalPlayer();

        if (localPlayer == null)
        {
            return;
        }

        // 自分自身の音声
        if (localPlayer == gameObject)
        {
            voiceAudioSource.mute = false;
            return;
        }

        // ==========================================
        // トランシーバー情報
        // ==========================================

        TransceiverHolder localHolder =
            localPlayer.GetComponent<TransceiverHolder>();

        bool localHasTransceiver =
            localHolder != null &&
            localHolder.HasTransceiver();

        TransceiverHolder remoteHolder =
            GetComponent<TransceiverHolder>();

        bool remoteHasTransceiver =
            remoteHolder != null &&
            remoteHolder.HasTransceiver();


        TransceiverController remoteController =
            GetComponent<TransceiverController>();

        bool remoteIsTransmitting =
            remoteController != null &&
            remoteController.IsTransmitting;


        // ==========================================
        // トランシーバー通信
        // ==========================================

        if (localHasTransceiver &&
            remoteHasTransceiver &&
            remoteIsTransmitting)
        {
            // Radio audio is non-positional while the remote player is transmitting.
            voiceAudioSource.spatialBlend = 0f;
            voiceAudioSource.mute = false;

            return;
        }

        // ==========================================
        // 通常のボイスチャット
        // ==========================================

        voiceAudioSource.spatialBlend = 1f;

        float distance = Vector3.Distance(
            localPlayer.transform.position,
            transform.position
        );

        voiceAudioSource.mute = distance > voiceRange;
    }

    private GameObject GetLocalPlayer()
    {
        GameObject[] players =
            GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            VoiceController controller =
                player.GetComponent<VoiceController>();

            if (controller != null)
            {
                Photon.Voice.Fusion.VoiceNetworkObject voiceObject =
                    player.GetComponent<
                        Photon.Voice.Fusion.VoiceNetworkObject>();

                if (voiceObject != null && voiceObject.IsLocal)
                {
                    return player;
                }
            }
        }

        return null;
    }
}