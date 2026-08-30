using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class TransceiverController : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsTransmitting { get; private set; }

    public bool IsLocalTransmitting { get; private set; }

    private bool lastKeyState;
    private InputAction transmitAction;

    private void Awake()
    {
        transmitAction = new InputAction("Transceiver", InputActionType.Button);
        transmitAction.AddBinding("<Keyboard>/t");
        // Nintendo系コントローラーのXはGamepadの上ボタンです。
        transmitAction.AddBinding("<Gamepad>/buttonNorth");
    }

    private void OnEnable()
    {
        transmitAction?.Enable();
    }

    private void OnDisable()
    {
        transmitAction?.Disable();
        IsLocalTransmitting = false;
        lastKeyState = false;
    }

    private void OnDestroy()
    {
        transmitAction?.Dispose();
    }

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
            transmitAction != null &&
            transmitAction.IsPressed();

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
