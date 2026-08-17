using Photon.Voice.Unity;
using UnityEngine;

public class VoiceController : MonoBehaviour
{
    [SerializeField] private float voiceRange = 5f;

    private AudioSource voiceAudioSource;
    private AudioListener localListener;

    private void Update()
    {
        if (voiceAudioSource == null)
        {
            Speaker speaker = GetComponent<Speaker>();

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

            // 話しているプレイヤーの位置から鳴る3D音声にする。
            voiceAudioSource.spatialBlend = 1f;
            voiceAudioSource.rolloffMode = AudioRolloffMode.Linear;
            voiceAudioSource.minDistance = 1f;
            voiceAudioSource.maxDistance = voiceRange;
            voiceAudioSource.dopplerLevel = 0f;
        }

        // 有効になっている、自分の AudioListener だけを取得する。
        if (localListener == null || !localListener.enabled)
        {
            localListener = GetLocalAudioListener();
        }

        if (localListener == null)
        {
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            localListener.transform.position
        );

        // 5 Unity Unit を超えたら完全に聞こえなくする。
        voiceAudioSource.mute = distance > voiceRange;
    }

    private AudioListener GetLocalAudioListener()
    {
        AudioListener[] listeners =
            FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        foreach (AudioListener listener in listeners)
        {
            if (listener.enabled && listener.gameObject.activeInHierarchy)
            {
                return listener;
            }
        }

        return null;
    }
}