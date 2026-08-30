using Fusion;
using UnityEngine;

public class TransceiverController : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsTransmitting { get; private set; }

    public bool IsLocalTransmitting { get; private set; }

    private bool lastKeyState;

    private void Update()
    {
        if (Object == null || !Object.HasInputAuthority)
        {
            return;
        }

        TransceiverHolder holder = GetComponent<TransceiverHolder>();
        bool hasTransceiver = holder != null && holder.HasTransceiver();
        bool currentKeyState =
            hasTransceiver &&
            !VoiceChatMuteController.IsLocalMuted &&
            Input.GetKey(KeyCode.T);

        IsLocalTransmitting = currentKeyState;

        if (currentKeyState == lastKeyState)
        {
            return;
        }

        lastKeyState = currentKeyState;
        RPC_SetTransmitting(currentKeyState);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetTransmitting(bool active)
    {
        TransceiverHolder holder = GetComponent<TransceiverHolder>();
        IsTransmitting = active && holder != null && holder.HasTransceiver();
    }
}
