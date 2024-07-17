using Unity.Netcode;
using UnityEngine;

public class NetworkObjectSpawner : NetworkBehaviour
{
    [SerializeField]
    NetworkObject m_NetworkObjectToSpawn;

    NetworkVariable<bool> m_Initialized = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // TODO: fetch item from pool

        if (IsSessionOwner && !m_Initialized.Value)
        {
            SpawnNetworkObject();
            m_Initialized.Value = true;
        }
        base.OnNetworkSpawn();
    }

    void SpawnNetworkObject()
    {
        m_NetworkObjectToSpawn.InstantiateAndSpawn(NetworkManager.Singleton, position: transform.position);
    }
}
