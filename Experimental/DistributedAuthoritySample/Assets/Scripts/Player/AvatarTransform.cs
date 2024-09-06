using System;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;
using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Input;
using Unity.Multiplayer.Samples.SocialHub.Physics;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    [RequireComponent(typeof(Rigidbody))]
    class AvatarTransform : PhysicsObjectMotion, INetworkUpdateSystem
    {
        [SerializeField]
        PlayerInput m_PlayerInput;
        [SerializeField]
        AvatarInputs m_AvatarInputs;
        [SerializeField]
        AvatarInteractions m_AvatarInteractions;
        [SerializeField]
        PhysicsPlayerController m_PhysicsPlayerController;

        Camera m_MainCamera;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            gameObject.name = $"[Client-{OwnerClientId}]{name}";

            if (!HasAuthority)
            {
                return;
            }

            m_PlayerInput.enabled = true;
            m_AvatarInputs.enabled = true;
            m_AvatarInputs.Jumped += OnJumped;
            m_AvatarInteractions.enabled = true;
            m_PhysicsPlayerController.enabled = true;
            Rigidbody.isKinematic = false;
            Rigidbody.freezeRotation = true;
            var spawnPoint = PlayerSpawnPoints.Instance.GetRandomSpawnPoint((int)OwnerClientId - 1);
            //new Vector3(53.7428741f,7.85612297f,-8.75020027f);
            //Quaternion.Euler(0f,143.263947f,0f)
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            Teleport(spawnPoint.position, spawnPoint.rotation, Vector3.one);
            Rigidbody.position = spawnPoint.position;
            Rigidbody.linearVelocity = Vector3.zero;

            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.Update);
            this.RegisterNetworkUpdate(updateStage: NetworkUpdateStage.FixedUpdate);

            var otherCamera = GameObject.FindWithTag("SpawnLocation").GetComponent<Camera>();
            otherCamera.enabled = false;

            m_MainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            var cameraControl = m_MainCamera.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(transform);
                m_MainCamera.enabled = true;
            }
            else
            {
                Debug.LogError("CameraControl not found on the Main Camera or Main Camera is missing.");
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (m_AvatarInputs != null)
            {
                m_AvatarInputs.Jumped -= OnJumped;
            }

            this.UnregisterAllNetworkUpdates();

            var cameraControl = Camera.main?.GetComponent<CameraControl>();
            if (cameraControl != null)
            {
                cameraControl.SetTransform(null);
            }
        }

        void OnJumped()
        {
            m_PhysicsPlayerController.SetJump(true);
        }

        void OnTransformUpdate()
        {
            if (m_MainCamera != null)
            {
                Vector3 forward = m_MainCamera.transform.forward;
                Vector3 right = m_MainCamera.transform.right;

                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 movement = forward * m_AvatarInputs.Move.y + right * m_AvatarInputs.Move.x;
                m_PhysicsPlayerController.SetMovement(movement);
                m_PhysicsPlayerController.SetSprint(m_AvatarInputs.Sprint);
            }
        }

        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            switch (updateStage)
            {
                case NetworkUpdateStage.Update:
                    OnTransformUpdate();

                    if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                    {
                        NetworkManager.Shutdown();
                        SceneManager.LoadScene("MainMenu");
                    }
                    break;
                case NetworkUpdateStage.FixedUpdate:
                    m_PhysicsPlayerController.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(updateStage), updateStage, null);
            }
        }
    }
}
