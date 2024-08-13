using Unity.Netcode;
using UnityEngine;

namespace com.unity.multiplayer.samples.socialhub.gameplay
{
    interface IGameplayEventInvokable
    {
        event System.Action<NetworkObject, GameplayEvent> OnGameplayEvent;
    }
}
