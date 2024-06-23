// Designed by KINEMATION, 2024.

using System.Collections.Generic;
using Demo.Scripts.Runtime.Item;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using KINEMATION.KAnimationCore.Runtime.Rig;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Scripts.Runtime.Character
{
    public enum FPSAimState
    {
        None,
        Ready,
        Aiming,
        PointAiming
    }

    public enum FPSActionState
    {
        None,
        PlayingAnimation,
        WeaponChange,
        AttachmentEditing
    }

    [RequireComponent(typeof(CharacterController), typeof(FPSMovement))]
    public class FPSController : NetworkBehaviour
    {
        //~ Legacy Controller Interface

        [SerializeField] private FPSControllerSettings settings;

        private FPSMovement _movementComponent;

        private Transform _weaponBone;
        private Vector2 _playerInput;

        private int _activeWeaponIndex;
        private int _previousWeaponIndex;

        private FPSAimState _aimState;
        private FPSActionState _actionState;
        
        private static readonly int TurnRight = Animator.StringToHash("TurnRight");
        private static readonly int TurnLeft = Animator.StringToHash("TurnLeft");
        
        private float _turnProgress = 1f;
        private bool _isTurning;

        private bool _isUnarmed;
        private Animator _animator;

        //~ Legacy Controller Interface

        // ~Scriptable Animation System Integration
        private FPSAnimator _fpsAnimator;
        private UserInputController _userInput;
        // ~Scriptable Animation System Integration

        private List<FPSItem> _instantiatedWeapons;
        private Vector2 _lookDeltaInput;

        private RecoilPattern _recoilPattern;

        private int _sensitivityMultiplierPropertyIndex;

        private NetWorkPlayerControl _netWorkPlayerControl;

        private InteractorUI _interactorUI;

        private Ui _ui;

        public bool firing;

        private void PlayTransitionMotion(FPSAnimatorLayerSettings layerSettings)
        {
            if (layerSettings == null)
            {
                return;
            }
            
            _fpsAnimator.LinkAnimatorLayer(layerSettings);
        }

        private bool IsSprinting()
        {
            return _movementComponent.MovementState == FPSMovementState.Sprinting;
        }
        
        private bool HasActiveAction()
        {
            return _actionState != FPSActionState.None;
        }

        public bool IsAiming()
        {
            return _aimState is FPSAimState.Aiming or FPSAimState.PointAiming;
        }

        private void InitializeMovement()
        {
            _movementComponent = GetComponent<FPSMovement>();
            
            _movementComponent.onJump.AddListener(() => { PlayTransitionMotion(settings.jumpingMotion); });
            _movementComponent.onLanded.AddListener(() => { PlayTransitionMotion(settings.jumpingMotion); });

            _movementComponent.onCrouch.AddListener(OnCrouch);
            _movementComponent.onUncrouch.AddListener(OnUncrouch);

            _movementComponent.onSprintStarted.AddListener(OnSprintStarted);
            _movementComponent.onSprintEnded.AddListener(OnSprintEnded);

            _movementComponent.onSlideStarted.AddListener(OnSlideStarted);

            _movementComponent.slideCondition += () => !HasActiveAction();
            _movementComponent.sprintCondition += () => !HasActiveAction();
            _movementComponent.proneCondition += () => !HasActiveAction();
            
            _movementComponent.onStartMoving.AddListener(() =>
            {
                if (_movementComponent.PoseState != FPSPoseState.Prone) return;
                _userInput.SetValue(FPSANames.PlayablesWeight, 0f);
            });
            
            _movementComponent.onStopMoving.AddListener(() =>
            {
                PlayTransitionMotion(settings.stopMotion);
                if (_movementComponent.PoseState != FPSPoseState.Prone) return;
                _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            });
        }
        
        private void InitializeWeapons()
        {
            _instantiatedWeapons = new List<FPSItem>();
            
            foreach (var prefab in settings.weaponPrefabs)
            { 
                var weapon = Instantiate(prefab, transform.position, Quaternion.identity);
                
                weapon.transform.position = transform.position;
                
                var weaponTransform = weapon.transform;

                weaponTransform.parent = _weaponBone;
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;

                _instantiatedWeapons.Add(weapon.GetComponent<FPSItem>());
                weapon.gameObject.SetActive(false);
            }
        }

        public void ResetWeaponAmmo()
        {
            foreach (var item in _instantiatedWeapons)
            {
                Weapon weapon = item as Weapon;
                weapon.ResetAmmo();    
            }
        }
        
        private void Start()
        {
            _ui = GetComponent<Ui>();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _weaponBone = GetComponentInChildren<KRigComponent>().GetRigTransform(settings.weaponBone);
            _fpsAnimator = GetComponent<FPSAnimator>();
            _userInput = GetComponent<UserInputController>();
            _animator = GetComponent<Animator>();
            _recoilPattern = GetComponent<RecoilPattern>();
            _netWorkPlayerControl = GetComponent<NetWorkPlayerControl>();
            _interactorUI = GetComponent<InteractorUI>();
            

            InitializeMovement();
            InitializeWeapons();

            _actionState = FPSActionState.None;
            EquipWeapon();

            _sensitivityMultiplierPropertyIndex = _userInput.GetPropertyIndex("SensitivityMultiplier");
            
            _ui.canUpdate = true;
        }




        private void UnequipWeapon()
        {
            DisableAim();
            _actionState = FPSActionState.WeaponChange;
            GetActiveItem().OnUnEquip();
        }

        public void ResetActionState()
        {
            _actionState = FPSActionState.None;
        }

        private void EquipWeapon()
        {
            if (_instantiatedWeapons.Count == 0) return;

            _instantiatedWeapons[_previousWeaponIndex].gameObject.SetActive(false);
            GetActiveItem().gameObject.SetActive(true);
            GetActiveItem().OnEquip(gameObject);
            _ui.weapon = GetActiveItem()as Weapon;
            _actionState = FPSActionState.None;
        }

        private void DisableAim()
        {
            if (GetActiveItem().OnAimReleased()) _aimState = FPSAimState.None;
        }
        
        private void OnFirePressed()
        {
            if (_instantiatedWeapons.Count == 0 || HasActiveAction()) return;
            if (_ui.buyMenu)if (_ui.buyMenu.activeSelf)return;

            GetActiveItem().OnFirePressed();
            firing = true;
        }

        private void OnFireReleased()
        {
            if (_instantiatedWeapons.Count == 0) return;
            GetActiveItem().OnFireReleased();
            firing = false;
        }
        
        public FPSItem GetActiveItem()
        {
            if (_instantiatedWeapons == null) return null;
            if (_instantiatedWeapons.Count == 0) return null;
            return _instantiatedWeapons[_activeWeaponIndex];
        }
        
        private void OnSlideStarted()
        {

            _animator.CrossFade("Sliding", 0.1f);
        }

        private void OnSprintStarted()
        {
            
            OnFireReleased();
            DisableAim();

            _aimState = FPSAimState.None;
            
            _userInput.SetValue(FPSANames.StabilizationWeight, 0f);
            _userInput.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }

        private void OnSprintEnded()
        {
            
            if (_animator.GetFloat("OverlayType") == 0) return;
            
            _userInput.SetValue(FPSANames.StabilizationWeight, 1f);
            _userInput.SetValue(FPSANames.PlayablesWeight, 1f);
            _userInput.SetValue("LookLayerWeight", 1f);
        }
        
        

        private void OnCrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
        }

        private void OnUncrouch()
        {
            PlayTransitionMotion(settings.crouchingMotion);
        }
        
        private bool _isLeaning;

        private void StartWeaponChange(int newIndex)
        {

            if (newIndex > _instantiatedWeapons.Count - 1)
            {
                return;
            }

            UnequipWeapon();

            OnFireReleased();
            Invoke(nameof(EquipWeapon), settings.equipDelay);

            _previousWeaponIndex = _activeWeaponIndex;
            _activeWeaponIndex = newIndex;
        }
        
        private void UpdateLookInput()
        {
            float scale = _userInput.GetValue<float>(_sensitivityMultiplierPropertyIndex);
            
            float deltaMouseX = _lookDeltaInput.x * settings.sensitivity * scale;
            float deltaMouseY = -_lookDeltaInput.y * settings.sensitivity * scale;
            
            _playerInput.y += deltaMouseY;
            _playerInput.x += deltaMouseX;
            
            if (_recoilPattern != null)
            {
                _playerInput += _recoilPattern.GetRecoilDelta();
            }

            float proneWeight = _animator.GetFloat("ProneWeight");
            Vector2 pitchClamp = Vector2.Lerp(new Vector2(-90f, 90f), new Vector2(-30, 0f), proneWeight);

            _playerInput.y = Mathf.Clamp(_playerInput.y, pitchClamp.x, pitchClamp.y);
            
            transform.rotation *= Quaternion.Euler(0f, deltaMouseX + (_recoilPattern != null ? _recoilPattern.GetRecoilDelta().x : 0f), 0f);
            _userInput.SetValue(FPSANames.MouseDeltaInput, new Vector4(deltaMouseX, deltaMouseY));
            _userInput.SetValue(FPSANames.MouseInput, new Vector4(_playerInput.x, _playerInput.y));
            
            CmdUpdateLookInput(_userInput.GetValue<Vector4>(_userInput.GetPropertyIndex("MouseDeltaInput")),_userInput.GetValue<Vector4>(_userInput.GetPropertyIndex("MouseInput")));
        }
        
        [ObserversRpc]
        private void RpcUpdateLookInput(Vector4 mouseDeltaInput,Vector4 mouseInput)
        {
            if (!IsOwner)
            {
                _userInput.SetValue(FPSANames.MouseDeltaInput,mouseDeltaInput);
                _userInput.SetValue(FPSANames.MouseInput,mouseInput);
            }
        }

        [ServerRpc]
        private void CmdUpdateLookInput(Vector4 mouseDeltaInput,Vector4 mouseInput)
        {
            RpcUpdateLookInput(mouseDeltaInput,mouseInput);
        }

        private void Update()
        {
            if (!IsOwner)return;
            if (!_ui.buyMenu.activeSelf) UpdateLookInput();
            Time.timeScale = settings.timeScale;
        }

#if ENABLE_INPUT_SYSTEM
        
        
        
        public void OnMouseLock()
        {
            Cursor.visible = !Cursor.visible;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Debug.Log(Cursor.lockState == CursorLockMode.Locked);
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Debug.Log(Cursor.lockState != CursorLockMode.Locked);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        
        [ObserversRpc]
        private void RpcOnReload()
        {
            if (!IsOwner)
            {
                if (IsSprinting() || HasActiveAction() || !GetActiveItem().OnReload()) return;
                _actionState = FPSActionState.PlayingAnimation;
            }
        }

        [ServerRpc]
        private void CmdOnReload()
        {
            RpcOnReload();
        }
        
        public void OnReload()
        {
            if (!IsOwner)return;
            CmdOnReload();
            if (IsSprinting() || HasActiveAction() || !GetActiveItem().OnReload()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        
        [ObserversRpc]
        private void RpcOnThrowGrenade()
        {
            if (!IsOwner)
            {
                if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
                _actionState = FPSActionState.PlayingAnimation;
            }
        }

        [ServerRpc]
        private void CmdOnThrowGrenade()
        {
            RpcOnThrowGrenade();
        }
        

        public void OnThrowGrenade()
        {
            if (!IsOwner)return;
            CmdOnThrowGrenade();
            if (IsSprinting()|| HasActiveAction() || !GetActiveItem().OnGrenadeThrow()) return;
            _actionState = FPSActionState.PlayingAnimation;
        }
        
        [ObserversRpc]
        private void RpcOnToggleUnarmed()
        {
            if (!IsOwner)
            {
                _isUnarmed = !_isUnarmed;

                if (_isUnarmed)
                {
                    GetActiveItem().gameObject.SetActive(false);
                    GetActiveItem().OnUnarmedEnabled();
                    _fpsAnimator.LinkAnimatorProfile(settings.unarmedProfile);
                    return;
                }

                GetActiveItem().gameObject.SetActive(true);
                GetActiveItem().OnUnarmedDisabled();
            }
        }

        [ServerRpc]
        private void CmdOnToggleUnarmed()
        {
            RpcOnToggleUnarmed();
        }

        public void OnToggleUnarmed()
        {
            if (!IsOwner)return;
            CmdOnToggleUnarmed();
            _isUnarmed = !_isUnarmed;

            if (_isUnarmed)
            {
                GetActiveItem().gameObject.SetActive(false);
                GetActiveItem().OnUnarmedEnabled();
                _fpsAnimator.LinkAnimatorProfile(settings.unarmedProfile);
                return;
            }
            GetActiveItem().gameObject.SetActive(true);
            GetActiveItem().OnUnarmedDisabled();
        }
        
        [ObserversRpc]
        private void RpcOnFire(bool value)
        {
            if (!IsOwner)
            {
                if (IsSprinting()) return;
            
                if (value)
                {
                    OnFirePressed();
                    return;
                }
            
                OnFireReleased();
            }
        }

        [ServerRpc]
        private void CmdOnFire(bool value)
        {
            RpcOnFire(value);
        }


        public void OnFire(InputValue value)
        {
            if (!IsOwner)return;
            CmdOnFire(value.isPressed);
            
            // if (IsSprinting()) return;
            
            if (value.isPressed)
            {
                OnFirePressed();
                return;
            }
            
            OnFireReleased();
        }
        
        [ObserversRpc]
        private void RpcDisableAim()
        {
            if (!IsOwner)
            {
                DisableAim(); 
            }
        }

        [ServerRpc]
        private void CmdDisableAim()
        {
            RpcDisableAim();
        }
        
        [ObserversRpc]
        private void RpcPlayAim()
        {
            if (!IsOwner)
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
            }
        }

        [ServerRpc]
        private void CmdPlayAim()
        {
            RpcPlayAim();
        }

        public void OnAim(InputValue value)
        {
            if (!IsOwner)return;
            
            if (value.isPressed && !IsAiming())
            {
                if (GetActiveItem().OnAimPressed()) _aimState = FPSAimState.Aiming;
                PlayTransitionMotion(settings.aimingMotion);
                CmdPlayAim();
                return;
            }
            if (!value.isPressed && IsAiming())
            {
                DisableAim();
                CmdDisableAim();
                PlayTransitionMotion(settings.aimingMotion);
            }
        }
        
        public void OnDefuse(InputValue value)
        {
            if (!IsOwner) return;
            
            if (value.isPressed)
            {
                _interactorUI.BombInteract();
                _interactorUI.DefusingBombAudio();
            }
            else
            {
                _interactorUI.CancelBombDefuse();
                if (_netWorkPlayerControl.severAudioSource.clip == _interactorUI.defusedAudioClip)return;
                _netWorkPlayerControl.CmdCloseSeverAudioSource();
                _netWorkPlayerControl.severAudioSource.Stop();
            }
        }
        
        [ObserversRpc]
        private void RpcChangeWeapon(int value)
        {
            if (!IsOwner)
            {
                if (_movementComponent.PoseState == FPSPoseState.Prone) return;
                if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;
            
                StartWeaponChange(value);
            }
        }
        
        [ServerRpc]
        private void CmdChangeWeapon(int value)
        {
            RpcChangeWeapon(value);
        }
        
        public void ChangeWeapon(int value)
        {
            if (!IsOwner)return;
            if (_movementComponent.PoseState == FPSPoseState.Prone) return;
            if (HasActiveAction() || _instantiatedWeapons.Count == 0) return;
            
            StartWeaponChange(value);
            CmdChangeWeapon(value);
        }
        
        [ObserversRpc]
        private void RpcOnLook(Vector2 value)
        {
            if (!IsOwner) _lookDeltaInput = value;
        }

        [ServerRpc]
        private void CmdOnLook(Vector2 value)
        {
            RpcOnLook(value);
        }

        public void OnLook(InputValue value)
        {
            if (!IsOwner)return;
            _lookDeltaInput = value.Get<Vector2>();
            CmdOnLook(value.Get<Vector2>());
        }
        
        
        [ObserversRpc]
        private void RpcOnLean(float value)
        {
            if (!IsOwner) _userInput.SetValue(FPSANames.LeanInput, value * settings.leanAngle);
            if (!IsOwner) PlayTransitionMotion(settings.leanMotion);
        }

        [ServerRpc]
        private void CmdOnLean(float value)
        {
            RpcOnLean(value);
        }

        public void OnLean(InputValue value)
        {
            if (!IsOwner)return;
            _userInput.SetValue(FPSANames.LeanInput, value.Get<float>() * settings.leanAngle);
            PlayTransitionMotion(settings.leanMotion);
            CmdOnLean(value.Get<float>());
        }
        
        public void OnCycleScope()
        {
            if (!IsOwner)return;
            if (!IsAiming()) return;
            
            GetActiveItem().OnCycleScope();
            PlayTransitionMotion(settings.aimingMotion);
        }

        public void OnChangeFireMode()
        {
            if (!IsOwner)return;
            GetActiveItem().OnChangeFireMode();
        }

        public void OnToggleAttachmentEditing()
        {
            if (!IsOwner)return;
            if (HasActiveAction() && _actionState != FPSActionState.AttachmentEditing) return;
            
            _actionState = _actionState == FPSActionState.AttachmentEditing 
                ? FPSActionState.None : FPSActionState.AttachmentEditing;

            if (_actionState == FPSActionState.AttachmentEditing)
            {
                _animator.CrossFade("InspectStart", 0.2f);
                return;
            }
            
            _animator.CrossFade("InspectEnd", 0.3f);
        }

        public void OnDigitAxis(InputValue value)
        {
            if (!IsOwner)return;
            if (!value.isPressed || _actionState != FPSActionState.AttachmentEditing) return;
            GetActiveItem().OnAttachmentChanged((int) value.Get<float>());
        }

        public void OnBuy()
        {
            if (!IsOwner)return;
            if(_netWorkPlayerControl._gameManager._gameState != GameManager.State.BuyIng) return;
            
            _ui.buyMenu.SetActive(!_ui.buyMenu.activeSelf);
            
            Cursor.visible = _ui.buyMenu.activeSelf;
            
            if (_ui.buyMenu.activeSelf)
            {
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void OnMainWeapon()
        {
            if (_netWorkPlayerControl.mainWeaponIndex>=0)
            {
                ChangeWeapon(_netWorkPlayerControl.mainWeaponIndex);
            }
        }

        public void OnSecondWeapon()
        {
            if (_netWorkPlayerControl.secondWeaponIndex>=0)
            {
                ChangeWeapon(_netWorkPlayerControl.secondWeaponIndex);
            }
        }

        public void OnBomb()
        {
            if (_netWorkPlayerControl.haveBomb)
            {
                ChangeWeapon(_instantiatedWeapons.Count-1);
            }
        }
        
#endif
    }
}