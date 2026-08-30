using Fusion;

public class TransceiverHolder : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsHoldingTransceiver { get; private set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsHoldingTransceiver = false;
        }
    }

    public bool HasTransceiver()
    {
        return IsHoldingTransceiver;
    }

    public bool TryGrantTransceiver()
    {
        if (Object == null || !Object.HasStateAuthority || IsHoldingTransceiver)
        {
            return false;
        }

        IsHoldingTransceiver = true;
        return true;
    }

    public void RemoveTransceiver()
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            return;
        }

        IsHoldingTransceiver = false;
    }
}
