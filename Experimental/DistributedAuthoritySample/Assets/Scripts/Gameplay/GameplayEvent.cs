using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("com.unity.multiplayer.samples.socialhub.player")]
namespace com.unity.multiplayer.samples.socialhub.gameplay
{
    enum GameplayEvent
    {
        Despawned,
        OwnershipChange
    }
}
