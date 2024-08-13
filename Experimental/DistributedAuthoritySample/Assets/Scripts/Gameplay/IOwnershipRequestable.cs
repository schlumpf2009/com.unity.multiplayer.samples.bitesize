using System;
using Unity.Netcode;
using UnityEngine;

namespace com.unity.multiplayer.samples.socialhub.gameplay
{
    interface IOwnershipRequestable
    {
        event Action<NetworkObject, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;
    }
}
