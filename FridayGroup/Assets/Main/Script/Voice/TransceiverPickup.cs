using Fusion;
using UnityEngine;

public class TransceiverPickup : NetworkBehaviour
{
    [Networked]
    private NetworkBool IsPickedUp { get; set; }

    [Networked]
    private PlayerRef Owner { get; set; }

    private Collider pickupCollider;
    private Renderer[] pickupRenderers;
    private bool lastVisualPickedUp;

    public override void Spawned()
    {
        pickupCollider = GetComponent<Collider>();
        pickupRenderers = GetComponentsInChildren<Renderer>(true);
        ApplyVisualState(IsPickedUp);
        lastVisualPickedUp = IsPickedUp;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || IsPickedUp)
        {
            return;
        }

        TransceiverHolder holder = other.GetComponentInParent<TransceiverHolder>();
        if (holder == null || holder.HasTransceiver())
        {
            return;
        }

        // Physics triggers are observed on every peer. Only the player that
        // owns this player object is allowed to request the pickup.
        if (!holder.Object.HasStateAuthority)
        {
            return;
        }

        RPC_RequestPickup();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPickup(RpcInfo info = default)
    {
        // State Authority accepts only the first request. The owner is then
        // replicated to every peer, so the same pickup cannot be awarded twice.
        if (IsPickedUp)
        {
            return;
        }

        IsPickedUp = true;
        Owner = info.Source;
    }

    public override void Render()
    {
        if (IsPickedUp != lastVisualPickedUp)
        {
            lastVisualPickedUp = IsPickedUp;
            ApplyVisualState(IsPickedUp);
        }

        GrantToOwner();
    }

    private void GrantToOwner()
    {
        if (!IsPickedUp || Owner != Runner.LocalPlayer)
        {
            return;
        }

        TransceiverHolder[] holders =
            FindObjectsByType<TransceiverHolder>(FindObjectsSortMode.None);

        foreach (TransceiverHolder holder in holders)
        {
            if (holder.Object != null && holder.Object.HasStateAuthority)
            {
                holder.TryGrantTransceiver();
                return;
            }
        }
    }

    private void ApplyVisualState(bool pickedUp)
    {
        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider>();
        }

        if (pickupRenderers == null)
        {
            pickupRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled = !pickedUp;
        }

        foreach (Renderer pickupRenderer in pickupRenderers)
        {
            pickupRenderer.enabled = !pickedUp;
        }
    }
}