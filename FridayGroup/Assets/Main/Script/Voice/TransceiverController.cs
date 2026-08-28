using Fusion;
using UnityEngine;

public class TransceiverController : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsTransmitting { get; private set; }

    public bool IsLocalTransmitting { get; private set; }

    private bool lastKeyState = false;

    private void Update()
    {
        // ©•ª‚ÌPlayer‚¾‚¯‚ª‘€ì‚·‚é
        if (Object == null || !Object.HasInputAuthority)
        {
            return;
        }

        TransceiverHolder holder = GetComponent<TransceiverHolder>();
        bool hasTransceiver = holder != null && holder.HasTransceiver();
        bool currentKeyState = hasTransceiver &&
                               !VoiceChatMuteController.IsLocalMuted &&
                               Input.GetKey(KeyCode.T);
        IsLocalTransmitting = currentKeyState;

        // TƒL[‚Ìó‘Ô‚ª•Ï‰»‚µ‚½‚Æ‚«‚¾‚¯ˆ—
        if (currentKeyState != lastKeyState)
        {
            lastKeyState = currentKeyState;

            Debug.Log(
                gameObject.name +
                " T key = " +
                currentKeyState
            );

            RPC_SetTransmitting(currentKeyState);
        }
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_SetTransmitting(bool active)
    {
        TransceiverHolder holder = GetComponent<TransceiverHolder>();
        IsTransmitting = active && holder != null && holder.HasTransceiver();

        Debug.Log(
            gameObject.name +
            " IsTransmitting = " +
            IsTransmitting
        );
    }

    private bool lastRenderState = false;

    public override void Render()
    {
        bool currentState = IsTransmitting;

        if (currentState != lastRenderState)
        {
            lastRenderState = currentState;

            Debug.Log(
                "yTransceiver“¯Šúz" +
                "\nPlayer = " + gameObject.name +
                "\nIsTransmitting = " + currentState +
                "\nInputAuthority = " + Object.InputAuthority +
                "\nStateAuthority = " + Object.StateAuthority +
                "\nLocalPlayer = " + Runner.LocalPlayer
            );
        }
    }
}