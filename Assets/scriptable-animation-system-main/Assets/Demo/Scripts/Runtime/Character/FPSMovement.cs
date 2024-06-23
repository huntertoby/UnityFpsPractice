// Designed by KINEMATION, 2024.

using KINEMATION.KAnimationCore.Runtime.Core;
using KINEMATION.KAnimationCore.Runtime.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

using System.Collections.Generic;
using Demo.Scripts.Runtime.Item;
using FishNet.Object;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSMovementState
    {
        Idle,
        Walking,
        Sprinting,
        InAir,
        Sliding
    }

    public enum FPSPoseState
    {
        Standing,
        Crouching,
        Prone
    }
    
    public class FPSMovement : NetworkBehaviour
    {
        public delegate bool ConditionDelegate();
        
        [SerializeField] private FPSMovementSettings movementSettings;
        [SerializeField] public Transform rootBone;
        
        [Header("footSteps")]
        [SerializeField] private AudioClip walkAudioClip;
        [SerializeField] private AudioClip runAudioClip;
        [SerializeField] private AudioClip jumpAudioClip;
        [SerializeField] private AudioSource footStepsAudioSource;
        
        [Header("UnityEvent")]
        
        [SerializeField] public UnityEvent onStartMoving;
        [SerializeField] public UnityEvent onStopMoving;
        
        [SerializeField] public UnityEvent onSprintStarted;
        [SerializeField] public UnityEvent onSprintEnded;

        [SerializeField] public UnityEvent onCrouch;
        [SerializeField] public UnityEvent onUncrouch;
        
        [SerializeField] public UnityEvent onProneStarted;
        [SerializeField] public UnityEvent onProneEnded;

        [SerializeField] public UnityEvent onJump;
        [SerializeField] public UnityEvent onLanded;

        [SerializeField] public UnityEvent onSlideStarted;
        [SerializeField] public UnityEvent onSlideEnded;

        public ConditionDelegate slideCondition;
        public ConditionDelegate proneCondition;
        public ConditionDelegate sprintCondition;
        
        public FPSMovementState MovementState { get; private set; }
        public FPSPoseState PoseState { get; private set; }

        public Vector2 AnimatorVelocity { get; private set; }


        private FPSController _fpsController;
        private CharacterController _controller;
        private Animator _animator;
        private Vector2 _inputDirection;

        private FPSMovementState _cachedMovementState;

        public Vector3 MoveVector { get; private set; }
        
        private Vector3 _velocity;

        private float _originalHeight;
        private Vector3 _originalCenter;
        
        private GaitSettings _desiredGait;
        private float _slideProgress = 0f;

        private Vector3 _prevPosition;
        private Vector3 _velocityVector;

        private static readonly int InAir = Animator.StringToHash("InAir");
        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int Velocity = Animator.StringToHash("Velocity");
        private static readonly int Moving = Animator.StringToHash("Moving");
        private static readonly int Crouching = Animator.StringToHash("Crouching");
        private static readonly int Sliding = Animator.StringToHash("Sliding");
        private static readonly int Sprinting = Animator.StringToHash("Sprinting");
        private static readonly int Proning = Animator.StringToHash("Proning");

        private float _sprintAnimatorInterp = 8f;
        
        private bool _wasMoving = false;

        private bool _isSprinting = false;

        private UserInputController _inputController;

        [HideInInspector]public Camera camera;

        [HideInInspector]public bool canMove;

        private InteractorUI _interactorUI;

        
        
        public bool IsInAir()
        {
            return !_controller.isGrounded;
        }
        
        private bool IsMoving()
        {
            return !Mathf.Approximately(_inputDirection.normalized.magnitude, 0f);
        }

        private float GetSpeedRatio()
        {
            return _velocity.magnitude / _desiredGait.velocity;
        }
        
        

        private bool CanSlide()
        {
            return MovementState == FPSMovementState.Sprinting && PoseState == FPSPoseState.Standing && (slideCondition == null || slideCondition.Invoke());
        }

        private bool CanSprint()
        {
            bool conditionCheck = false;
            if (sprintCondition != null)
            {
                conditionCheck = sprintCondition.Invoke();
            }
            
            return PoseState == FPSPoseState.Standing && conditionCheck;
        }

        private bool CanProne()
        {
            return proneCondition == null || proneCondition.Invoke(); 
        }
        
        private bool CanUnCrouch()
        {
            float height = _originalHeight - _controller.radius * 2f;
            Vector3 position = rootBone.TransformPoint(_originalCenter + Vector3.up * height / 2f);
            return !Physics.CheckSphere(position, _controller.radius);
        }

        private void EnableProne()
        {
            Crouch();
            PoseState = FPSPoseState.Prone;
            _animator.SetBool(Crouching, false);
            _animator.SetBool(Proning, true);
            
            onProneStarted?.Invoke();
            _desiredGait = movementSettings.prone;
        }

        private void CancelProne()
        {
            if (!CanUnCrouch()) return;
            UnCrouch();
            PoseState = FPSPoseState.Standing;
            _animator.SetBool(Proning, false);
            
            onProneEnded?.Invoke();
            _desiredGait = movementSettings.walking;
        }

        private void Crouch()
        {
            float crouchedHeight = _originalHeight * movementSettings.crouchRatio;
            float heightDifference = _originalHeight - crouchedHeight;

            _controller.height = crouchedHeight;

            // Adjust the center position so the bottom of the capsule remains at the same position
            Vector3 crouchedCenter = _originalCenter;
            crouchedCenter.y -= heightDifference / 2;
            _controller.center = crouchedCenter;

            PoseState = FPSPoseState.Crouching;
            
            _animator.SetBool(Crouching, true);
            _animator.SetFloat("CrouchWeight",1f);
            onCrouch.Invoke();
        }

        private void UnCrouch()
        {
            _controller.height = _originalHeight;
            _controller.center = _originalCenter;
            
            PoseState = FPSPoseState.Standing;
            
            _animator.SetBool(Crouching, false);
            _animator.SetFloat("CrouchWeight",0f);
            onUncrouch.Invoke();
        }
        
        private void UpdateMovementState()
        {
            if (MovementState == FPSMovementState.Sliding && !Mathf.Approximately(_slideProgress, 1f))
            {
                // Consume input, but do not allow cancelling sliding.
                return;
            }

            if (MovementState == FPSMovementState.InAir)
            {
                return;
            }
            
            if (MovementState == FPSMovementState.Sprinting && _inputDirection.y > 0f && !_fpsController.IsAiming() && PoseState != FPSPoseState.Prone && !_fpsController.firing)
            {
                if (PoseState == FPSPoseState.Crouching)UnCrouch();
                return;
            }

            if (!IsMoving())
            {
                MovementState = FPSMovementState.Idle;
                return;
            }
            
            if (_isSprinting && _inputDirection.y > 0f && !_fpsController.IsAiming() && PoseState != FPSPoseState.Prone && !_fpsController.firing)
            {
                if (PoseState == FPSPoseState.Crouching)UnCrouch();
                MovementState = FPSMovementState.Sprinting;
                return;
            }
            
            MovementState = FPSMovementState.Walking;
            
        }

        private void OnMovementStateChanged()
        {
            
            if (_cachedMovementState == FPSMovementState.InAir)
            {
                onLanded.Invoke();
                if (_isSprinting) { MovementState = FPSMovementState.Sprinting; }
            }

            if (_cachedMovementState == FPSMovementState.Sprinting)
            {
                onSprintEnded?.Invoke();
                _sprintAnimatorInterp = 7f;
            }

            if (_cachedMovementState == FPSMovementState.Sliding)
            {
                _sprintAnimatorInterp = 15f;
                onSlideEnded.Invoke();

                if (CanUnCrouch())
                {
                    UnCrouch();
                }
            }
            
            if (MovementState == FPSMovementState.Idle)
            {
                float prevVelocity = _desiredGait.velocity;
                _desiredGait = movementSettings.idle;
                _desiredGait.velocity = prevVelocity;
                return;
            }

            if (MovementState == FPSMovementState.InAir)
            {
                _velocity.y = movementSettings.jumpHeight;
                onJump.Invoke();
                return;
            }

            if (MovementState == FPSMovementState.Sprinting)
            {
                onSprintStarted?.Invoke();
                _desiredGait = movementSettings.sprinting;
                return;
            }

            if (MovementState == FPSMovementState.Sliding)
            {
                _desiredGait.velocitySmoothing = movementSettings.slideDirectionSmoothing;
                _slideProgress = 0f;
                onSlideStarted.Invoke();
                Crouch();
                return;
            }

            if (PoseState == FPSPoseState.Crouching)
            {
                _desiredGait = movementSettings.crouching;
                return;
            }

            if (PoseState == FPSPoseState.Prone)
            {
                _desiredGait = movementSettings.prone;
                return;
            }
            
            // Walking state
            _desiredGait = movementSettings.walking;
        }

        private void UpdateSliding()
        {
            // 1. Extract the slide animation.
            float slideAmount = movementSettings.slideCurve.Evaluate(_slideProgress);
            
            // 2. Apply sliding to both current and desired velocity vectors.
            // Here we just want to interpolate between the same velocities, but different directions.

            _velocity *= slideAmount;

            Vector3 desiredVelocity = _velocity;
            desiredVelocity.y = -movementSettings.gravity;
            MoveVector = desiredVelocity;
            
            _slideProgress = Mathf.Clamp01(_slideProgress + Time.deltaTime * movementSettings.slideSpeed);
        }
        
        private void UpdateGrounded()
        {
            var normInput = _inputDirection.normalized;
            var desiredVelocity = rootBone.right * normInput.x + rootBone.forward * normInput.y;

            desiredVelocity *= _desiredGait.velocity;

            desiredVelocity = Vector3.Lerp(_velocity, desiredVelocity, 
                KMath.ExpDecayAlpha(_desiredGait.velocitySmoothing, Time.deltaTime));
            
            _velocity = desiredVelocity;

            desiredVelocity.y = -movementSettings.gravity;
            MoveVector = desiredVelocity;
        }
        
        private void UpdateInAir()
        {
            var normInput = _inputDirection.normalized;
            _velocity.y -= movementSettings.gravity * Time.deltaTime;
            _velocity.y = Mathf.Max(-movementSettings.maxFallVelocity, _velocity.y);
            
            var desiredVelocity = rootBone.right * normInput.x + rootBone.forward * normInput.y;
            desiredVelocity *= _desiredGait.velocity;

            desiredVelocity = Vector3.Lerp(_velocity, desiredVelocity * movementSettings.airFriction, 
                KMath.ExpDecayAlpha(movementSettings.airVelocity, Time.deltaTime));

            desiredVelocity.y = _velocity.y;
            _velocity = desiredVelocity;
            
            MoveVector = desiredVelocity;
        }
        
        private void UpdateMovement()
        {
            _controller.Move(MoveVector * Time.deltaTime);
        }

        private void UpdateAnimatorParams()
        {
            var animatorVelocity = _inputDirection;
            animatorVelocity *= MovementState == FPSMovementState.InAir ? 0f : 1f;

            AnimatorVelocity = Vector2.Lerp(AnimatorVelocity, animatorVelocity, 
                KMath.ExpDecayAlpha(_desiredGait.velocitySmoothing, Time.deltaTime));

            _animator.SetFloat(MoveX, AnimatorVelocity.x);
            _animator.SetFloat(MoveY, AnimatorVelocity.y);
            _animator.SetFloat(Velocity, AnimatorVelocity.magnitude);
            _animator.SetBool(InAir, IsInAir());
            _animator.SetBool(Moving, IsMoving());
            
            // Sprinting needs to be blended manually
            float a = _animator.GetFloat(Sprinting);
            float b = MovementState == FPSMovementState.Sprinting ? 1f : 0f;

            a = Mathf.Lerp(a, b, KMath.ExpDecayAlpha(_sprintAnimatorInterp, Time.deltaTime));

            _animator.SetFloat(Sprinting, a);
            
            _inputController.SetValue("MoveInput", new Vector4(AnimatorVelocity.x, AnimatorVelocity.y));
        }

        private void Start()
        {
            _fpsController = GetComponent<FPSController>();
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _inputController = GetComponent<UserInputController>();
            _interactorUI = GetComponent<InteractorUI>();
            
            _originalHeight = _controller.height;
            _originalCenter = _controller.center;
            
            MovementState = FPSMovementState.Idle;
            PoseState = FPSPoseState.Standing;

            _desiredGait = movementSettings.walking;

            camera = GetComponentInChildren<Camera>();
        }
        private void Update()
        {
            
            
            UpdateMovementState();

            if (_cachedMovementState != MovementState)
            {
                OnMovementStateChanged();
            }

            bool isMoving = IsMoving();
            
            if (_wasMoving != isMoving)
            {
                if (isMoving)
                {
                    onStartMoving?.Invoke();
                }
                else
                {
                    onStopMoving?.Invoke();
                }
            }
            
            _wasMoving = isMoving;

            if (MovementState == FPSMovementState.InAir)
            {
                UpdateInAir();
            }
            else if (MovementState == FPSMovementState.Sliding)
            {
                UpdateSliding();
            }
            else
            {
                UpdateGrounded();
            }
            
            if (!canMove || _interactorUI.defusing)return;

            if (IsOwner) UpdateMovement();
            if (IsOwner) UpdateAnimatorParams();
            
            _cachedMovementState = MovementState;
            if (MovementState == FPSMovementState.InAir && !IsInAir())
            {
                MovementState = FPSMovementState.Idle;
            }
        }

        private bool _isFootStepsPlaying =false;
        
        private void UpdateFootSteps()
        {
            if (!IsMoving())
            {
                _isFootStepsPlaying = false;
                return;
            }

            CancelInvoke(nameof(UpdateFootSteps));  // 確保只有一個Invoke存在
            
            if (MovementState == FPSMovementState.Idle)
            {
                footStepsAudioSource.clip = null;
                Invoke(nameof(UpdateFootSteps), 0.1f);
            }
            else if (MovementState == FPSMovementState.InAir)
            {
                footStepsAudioSource.clip = null;
                Invoke(nameof(UpdateFootSteps), 0.1f);
            }
            else if (MovementState == FPSMovementState.Walking)
            {
                footStepsAudioSource.clip = walkAudioClip;
                Invoke(nameof(UpdateFootSteps), 0.5f);
            }
            else if (MovementState == FPSMovementState.Sprinting)
            {
                footStepsAudioSource.clip = runAudioClip;
                Invoke(nameof(UpdateFootSteps), 0.3f);
            }

            CmdUpdateFoodSteps();
            footStepsAudioSource.Play();
        }
        
        [ObserversRpc]
        private void RpcUpdateFoodSteps()
        {
            if (!IsOwner)
            {
                if (MovementState == FPSMovementState.Idle)
                {
                    footStepsAudioSource.clip = null;
                }
                else if (MovementState == FPSMovementState.Walking)
                {
                    footStepsAudioSource.clip = walkAudioClip;
                }
                else if (MovementState == FPSMovementState.Sprinting)
                {
                    footStepsAudioSource.clip = runAudioClip;
                }
                
                footStepsAudioSource.Play();
            }
        }
        
        [ServerRpc]
        private void CmdUpdateFoodSteps()
        {
            RpcUpdateFoodSteps();
        }
        
        
#if ENABLE_INPUT_SYSTEM
        
        [ObserversRpc]
        private void RpcOnMove(Vector2 value)
        {
            if (!IsOwner) _inputDirection = value;
        }
        
        [ServerRpc]
        private void CmdOnMove(Vector2 value)
        {
            RpcOnMove(value);
        }
        
        public void OnMove(InputValue value)
        {
            if (!IsOwner) return;
            CmdOnMove(value.Get<Vector2>());
            _inputDirection = value.Get<Vector2>();
            if (!_isFootStepsPlaying) UpdateFootSteps();
            _isFootStepsPlaying = true;
        }
        
        [ObserversRpc]
        private void RpcOnCrouch()
        {
            if (!IsOwner)
            {
                if (_animator.GetFloat("OverlayType") < 1f) return;
            
                if (MovementState is not (FPSMovementState.Idle or FPSMovementState.Walking))
                {
                    return;
                }
            
                if (PoseState == FPSPoseState.Standing)
                {
                    Crouch();
                    _desiredGait = movementSettings.crouching;
                    return;
                }

                if (!CanUnCrouch())
                {
                    return;
                }
            
                UnCrouch();
                _desiredGait = movementSettings.walking;
            }
        }

        [ServerRpc]
        private void CmdOnCrouch()
        {
            RpcOnCrouch();
        }

        public void OnCrouch()
        {
            if (!IsOwner) return;
            
            CmdOnCrouch();
            
            if (_animator.GetFloat("OverlayType") < 1f) return;
            
            if (MovementState is not (FPSMovementState.Idle or FPSMovementState.Walking))
            {
                return;
            }
            
            if (PoseState == FPSPoseState.Standing)
            {
                Crouch();
                _desiredGait = movementSettings.crouching;
                return;
            }

            if (!CanUnCrouch())
            {
                return;
            }

            UnCrouch();
            _desiredGait = movementSettings.walking;
        }
        
        [ObserversRpc]
        private void RpcOnProne()
        {
            if (!IsOwner)
            {
                if (_animator.GetFloat("OverlayType") < 1f) return;
            
                if (MovementState is FPSMovementState.Sprinting or FPSMovementState.InAir)
                {
                    return;
                }

                if (!CanProne())
                {
                    return;
                }
            
                if (PoseState == FPSPoseState.Prone)
                {
                    CancelProne();
                    return;
                }
            
                EnableProne();
            }
        }

        [ServerRpc]
        private void CmdOnProne()
        {
            RpcOnProne();
        }

        public void OnProne()
        {
            if (!IsOwner) return;
            CmdOnProne();
            
            if (_animator.GetFloat("OverlayType") < 1f) return;
            
            if (MovementState is FPSMovementState.Sprinting or FPSMovementState.InAir)
            {
                return;
            }

            if (!CanProne())
            {
                return;
            }
            
            if (PoseState == FPSPoseState.Prone)
            {
                CancelProne();
                return;
            }
            
            EnableProne();
        }
        
                
        [ObserversRpc]
        private void RpcOnJump()
        {
            if (!IsOwner)
            {
                if (IsInAir() || PoseState == FPSPoseState.Crouching)
                {
                    return;
                }

                if (PoseState == FPSPoseState.Prone)
                {
                    CancelProne();
                    return;
                }
                MovementState = FPSMovementState.InAir;
            }
            
        }

        [ServerRpc]
        private void CmdOnJump()
        {
            RpcOnJump();
        }

        public void OnJump()
        {
            if (!IsOwner) return;

            CmdOnJump();
            
            if (IsInAir() || PoseState == FPSPoseState.Crouching)
            {
                return;
            }

            if (PoseState == FPSPoseState.Prone)
            {
                CancelProne();
                return;
            }
            MovementState = FPSMovementState.InAir;
        }
        
        
        [ObserversRpc]
        private void RpcOnSprint(bool value)
        {
            if (!IsOwner)
            {
                _isSprinting = !_isSprinting;
                
                if (MovementState is FPSMovementState.InAir or FPSMovementState.Sliding)
                {
                    return;
                }
            
                bool enableSprint = value && CanSprint();

                if (enableSprint)
                {
                    MovementState = FPSMovementState.Sprinting;
                    return;
                }
            
                MovementState = FPSMovementState.Walking;
            }
        }

        [ServerRpc]
        private void CmdOnSprint(bool value)
        {
            RpcOnSprint(value);
        }
        

        public void OnSprint(InputValue value)
        {
            if (!IsOwner) return;
            
            CmdOnSprint(value.isPressed);
            
            _isSprinting = !_isSprinting;
                
            if (MovementState is FPSMovementState.InAir or FPSMovementState.Sliding)
            {
                return;
            }
            
            bool enableSprint = value.isPressed && CanSprint();

            if (enableSprint)
            {
                MovementState = FPSMovementState.Sprinting;
                return;
            }
            
            MovementState = FPSMovementState.Walking;
            
            
        }
        
        
        [ObserversRpc]
        private void RpcOnSlide()
        {
            if (!IsOwner)
            {
                if (!CanSlide())
                {
                    return;
                }
                _slideProgress = 0f;
                MovementState = FPSMovementState.Sliding;
            }
        }

        [ServerRpc]
        private void CmdOnSlide()
        {
            RpcOnSlide();
        }

        public void OnSlide()
        {
            if (!IsOwner) return;
            CmdOnSlide();
            if (!CanSlide())
            {
                return;
            }
            _slideProgress = 0f;
            MovementState = FPSMovementState.Sliding;

        }
#endif
    }
}