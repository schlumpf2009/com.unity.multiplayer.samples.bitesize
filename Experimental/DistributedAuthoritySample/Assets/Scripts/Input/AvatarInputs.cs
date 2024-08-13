using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

[assembly: InternalsVisibleTo("com.unity.multiplayer.samples.socialhub.player")]
namespace com.unity.multiplayer.samples.socialhub.input
{
    class AvatarInputs : MonoBehaviour
    {
        [SerializeField]
        InputActionReference m_InteractActionReference;

        [Header("Character Input Values")]
        [SerializeField]
        internal Vector2 move;
        [SerializeField]
        internal Vector2 look;
        [SerializeField]
        internal bool jump;
        [SerializeField]
        internal bool sprint;

        [Header("Movement Settings")]
        [SerializeField]
        internal bool analogMovement;

        [Header("Mouse Cursor Settings")]
        [SerializeField]
        internal bool cursorLocked = true;
        [SerializeField]
        internal bool cursorInputForLook = true;

        internal event Action InteractTapped;
        internal event Action<double> InteractHeld;

        // tracking when a Hold interaction has started/ended
        bool m_HoldingInteractionPerformed;
        float m_HoldStartTime;

        void Start()
        {
            if (m_InteractActionReference == null)
            {
                Debug.LogWarning("Assign Interact ActionReference to this MonoBehaviour!", this);
                return;
            }

            m_InteractActionReference.action.performed += OnInteractPerformed;
            m_InteractActionReference.action.canceled += OnInteractCanceled;
            m_InteractActionReference.action.Enable();
        }

        void OnDestroy()
        {
            if (m_InteractActionReference != null)
            {
                m_InteractActionReference.action.performed -= OnInteractPerformed;
                m_InteractActionReference.action.canceled -= OnInteractCanceled;
                m_InteractActionReference.action.Disable();
            }
        }

        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            switch (context.interaction)
            {
                case HoldInteraction:
                    m_HoldingInteractionPerformed = true;
                    break;
                case TapInteraction:
                    InteractTapped?.Invoke();
                    break;
            }
        }

        void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (context.interaction is HoldInteraction)
            {
                if (m_HoldingInteractionPerformed)
                {
                    InteractHeld?.Invoke(context.duration);
                }
                m_HoldingInteractionPerformed = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }
#endif

        void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
