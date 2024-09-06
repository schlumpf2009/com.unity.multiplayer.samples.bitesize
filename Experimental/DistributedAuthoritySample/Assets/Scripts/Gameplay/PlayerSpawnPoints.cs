using System;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class PlayerSpawnPoints : MonoBehaviour
    {
        internal static PlayerSpawnPoints Instance;

        [SerializeField]
        Transform[] m_PlayerSpawnPoints;

        void Awake()
        {
            Instance = this;
        }

        internal Transform GetRandomSpawnPoint(int playerId)
        {
            return m_PlayerSpawnPoints[playerId];
        }
    }
}
