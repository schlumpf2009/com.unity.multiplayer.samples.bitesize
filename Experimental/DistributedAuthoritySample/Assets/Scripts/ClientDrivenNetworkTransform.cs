using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Component inheriting from <see cref="ClientNetworkTransform"/>, where server-driven player position changes are
/// applied to the owning client.
/// </summary>
/// <remarks>
/// Handling movement inside this component's OnNetworkSpawn method only ensures the mitigation of race condition issues
/// arising due to the execution order of other NetworkBehaviours' OnNetworkSpawn methods.
/// </remarks>
[DisallowMultipleComponent]
public class ClientDrivenNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient && IsOwner)
        {
            // TODO: add spawn points
            SetPosition(Vector3.zero);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    void SetPosition(Vector3 newValue)
    {
        Teleport(newValue, Quaternion.identity, Vector3.one);
    }
}
