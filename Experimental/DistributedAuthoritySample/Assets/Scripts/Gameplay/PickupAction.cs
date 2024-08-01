using System;
using com.unity.multiplayer.samples.distributed_authority.input;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace com.unity.multiplayer.samples.distributed_authority.gameplay
{
    [RequireComponent(typeof(AvatarInputs))]
    public class PickupAction : NetworkBehaviour
    {
        [SerializeField]
        NetworkRigidbody m_NetworkRigidbody;

        [SerializeField]
        Transform m_HoldTransform;

        [SerializeField]
        Collider m_InteractCollider;

        AvatarActions m_AvatarActions;

        Collider[] m_Results = new Collider[1];

        LayerMask m_PickupableLayerMask;

        bool m_Holding;
        NetworkRigidbody m_HoldingRigidbody;

        void Awake()
        {
            m_AvatarActions = new AvatarActions();
            m_PickupableLayerMask = 1 << LayerMask.NameToLayer("Pickupable");
        }

        public override void OnNetworkSpawn()
        {
            if (!HasAuthority)
            {
                return;
            }

            m_AvatarActions.Player.Interact.started += OnInteractStarted;
            m_AvatarActions.Player.Interact.performed += OnInteractPerformed;
            m_AvatarActions.Player.Interact.canceled += OnInteractCanceled;
            m_AvatarActions.Player.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (m_AvatarActions != null)
            {
                m_AvatarActions.Player.Interact.started -= OnInteractStarted;
                m_AvatarActions.Player.Interact.performed -= OnInteractPerformed;
                m_AvatarActions.Player.Interact.canceled -= OnInteractCanceled;
                m_AvatarActions.Player.Disable();
            }
        }

        void OnInteractStarted(InputAction.CallbackContext context)
        {
            if (m_Holding)
            {
                m_HoldingRigidbody.DetachFromFixedJoint();
                m_Holding = false;
            }
            else
            {
                if (Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
                {
                    if (m_Results[0].TryGetComponent(out NetworkRigidbody otherNetworkRigidbody))
                    {
                        m_Holding = otherNetworkRigidbody.AttachToFixedJoint(m_NetworkRigidbody, m_HoldTransform.position, massScale: 0.00001f);
                        if (m_Holding)
                        {
                            m_HoldingRigidbody = otherNetworkRigidbody;
                        }
                    }
                }
            }

            if (context.interaction is HoldInteraction)
            {
                Debug.Log("Started: Interaction was a hold");
            }
            else
            {
                Debug.Log("Interaction was a simple press");
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                Debug.Log("Performed: Interaction was a hold");
            }
            else
            {
                Debug.Log("Interaction was a simple press");
            }
        }

        void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                Debug.Log("Canceled: Interaction was a hold");
            }
            else
            {
                Debug.Log("Interaction was a simple press");
            }
        }

    }
}
