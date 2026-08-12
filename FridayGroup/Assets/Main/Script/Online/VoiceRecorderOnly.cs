using Fusion;
using Photon.Voice.Unity;
using UnityEngine;

public class VoiceRecorderOnly : NetworkBehaviour
{
    public override void Spawned()
    {
        // Shared Mode では State Authority を持つプレイヤーだけが
        // この端末のローカルプレイヤーです。
        if (Object.HasStateAuthority)
        {
            return;
        }

        // 他人のプレイヤーはマイク録音を開始させない。
        Recorder[] recorders = GetComponentsInChildren<Recorder>(true);

        foreach (Recorder recorder in recorders)
        {
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
        }
    }
}