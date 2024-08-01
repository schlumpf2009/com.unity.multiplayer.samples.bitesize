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

        [SerializeField]
        float m_MinTossForce;

        [SerializeField]
        float m_MaxTossForce;

        AvatarActions m_AvatarActions;

        Collider[] m_Results = new Collider[1];

        LayerMask m_PickupableLayerMask;

        bool m_HoldingInteraction;

        bool m_AttachedRigidbody;

        NetworkRigidbody m_HoldingRigidbody;

        float m_HoldStartTime;

        const float k_MinDurationHeld = 0f;
        const float k_MaxDurationHeld = 2f;

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
            switch (context.interaction)
            {
                case HoldInteraction:
                    Debug.Log("Started: Interaction was a hold");
                    break;
                case TapInteraction:
                    Debug.Log("Started: Interaction was a tap");
                    break;
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    Debug.Log("Performed: Interaction was a hold");

                    // start capturing the hold time
                    m_HoldingInteraction = true;
                    m_HoldStartTime = Time.time;

                    break;
                case TapInteraction:
                    Debug.Log("Performed: Interaction was a tap");

                    if (m_AttachedRigidbody)
                    {
                        Drop();
                    }
                    else
                    {
                        PickUp();
                    }
                    break;
            }
        }

        void OnInteractCanceled(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    Debug.Log("Canceled: Interaction was a hold");

                    if (m_HoldingInteraction && m_AttachedRigidbody)
                    {
                        Toss(m_HoldStartTime);
                    }
                    m_HoldingInteraction = false;
                    break;
                case TapInteraction:
                    Debug.Log("Canceled: Interaction was a tap");
                    break;
            }
        }

        void PickUp()
        {
            Debug.Log(nameof(PickUp));

            if (Physics.OverlapBoxNonAlloc(m_InteractCollider.transform.position, m_InteractCollider.bounds.extents, m_Results, Quaternion.identity, mask: m_PickupableLayerMask) > 0)
            {
                if (m_Results[0].TryGetComponent(out NetworkRigidbody otherNetworkRigidbody))
                {
                    m_AttachedRigidbody = otherNetworkRigidbody.AttachToFixedJoint(m_NetworkRigidbody, m_HoldTransform.position, massScale: 0.00001f);
                    if (m_AttachedRigidbody)
                    {
                        m_HoldingRigidbody = otherNetworkRigidbody;
                    }

                    // TODO: is ownership transferred automatically with AttachToFixedJoint?
                }
            }
        }

        void Drop()
        {
            Debug.Log(nameof(Drop));

            m_HoldingRigidbody.DetachFromFixedJoint();
            m_HoldingRigidbody = null;
            m_AttachedRigidbody = false;
        }

        void Toss(float holdStartTime)
        {
            Debug.Log(nameof(Toss));

            float timeHeld = Time.time - holdStartTime;
            float timeHeldClamped = Mathf.Clamp(timeHeld, k_MinDurationHeld, k_MaxDurationHeld);
            float tossForce = Mathf.Lerp(m_MinTossForce, m_MaxTossForce, Mathf.Clamp(timeHeldClamped, 0f, 1f));

            m_HoldingRigidbody.DetachFromFixedJoint();

            m_HoldingRigidbody.GetComponent<Rigidbody>().AddForce(transform.forward * tossForce, ForceMode.Impulse);

            m_HoldingRigidbody = null;
            m_AttachedRigidbody = false;
        }
    }
}
