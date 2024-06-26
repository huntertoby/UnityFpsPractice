// Designed by KINEMATION, 2024.
using System.Collections.Generic;
using Demo.Scripts.Runtime.AttachmentSystem;
using Demo.Scripts.Runtime.Character;
using FishNet.Object;
using KINEMATION.FPSAnimationFramework.Runtime.Camera;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Layers.SwayLayer;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Demo.Scripts.Runtime.Item
{
    public enum OverlayType
    {
        Default,
        Pistol,
        Rifle
    }
    
    public class Weapon : FPSItem
    {
        [Header("General")]
        [SerializeField] [Range(0f, 120f)] private float fieldOfView = 90f;
        [SerializeField] public Texture2D weaponImage;
        
        [Header("Offset")]
        [SerializeField] public float idleOffset;
        [SerializeField] public float walkOffset;
        [SerializeField] public float airOffset;
        [SerializeField] public float slideOffset;
        
        [Header("Ammo")] 
        [SerializeField] public GameObject bulletPrefab;

        [SerializeField] private int magAmmo;
        [SerializeField] private int totalAmmo;
        
        private int _magAmmo ;
        private int _totalAmmo ;
        
        [SerializeField] [HideInInspector]public int ammo ;
        
        [Header("Ray")] 
        [SerializeField] private LayerMask layerMask;

        [Header("Muzzle")] 
        public Transform muzzleTransform;
        
        [Header("Animations")]
        [SerializeField] private FPSAnimationAsset reloadClip;
        [SerializeField] private FPSCameraAnimation cameraReloadAnimation;
        
        [SerializeField] private FPSAnimationAsset grenadeClip;
        [SerializeField] private FPSCameraAnimation cameraGrenadeAnimation;
        [SerializeField] private OverlayType overlayType;

        [Header("Recoil")]
        [SerializeField] private RecoilAnimData recoilData;
        [SerializeField] private RecoilPatternSettings recoilPatternSettings;
        [SerializeField] private FPSCameraShake cameraShake;
        private float _fireRate;
        [SerializeField] private float fireRate;
        [Min(0f)] [SerializeField] private float burstFireRate;

        [SerializeField] private bool supportsAuto;
        [SerializeField] private bool supportsBurst;
        [SerializeField] private int burstLength;

        [Header("Attachments")] 
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> barrelAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private AttachmentGroup<BaseAttachment> gripAttachments = new AttachmentGroup<BaseAttachment>();
        
        [SerializeField]
        private List<AttachmentGroup<ScopeAttachment>> scopeGroups = new List<AttachmentGroup<ScopeAttachment>>();
        
        //~ Controller references

        private FPSController _fpsController;
        private Animator _controllerAnimator;
        private UserInputController _userInputController;
        private IPlayablesController _playablesController;
        private FPSCameraController _fpsCameraController;
        
        private FPSAnimator _fpsAnimator;
        private FPSAnimatorEntity _fpsAnimatorEntity;

        private RecoilAnimation _recoilAnimation;
        private RecoilPattern _recoilPattern;

        private Camera _camera;

        private Bomb _bomb;
        
        //~ Controller references
        
        private Animator _weaponAnimator;
        private int _scopeIndex;
        
        private float _lastRecoilTime;
        private int _bursts;
        private FireMode _fireMode = FireMode.Semi;
        
        private static readonly int OverlayType = Animator.StringToHash("OverlayType");
        private static readonly int CurveEquip = Animator.StringToHash("CurveEquip");
        private static readonly int CurveUnequip = Animator.StringToHash("CurveUnequip");
        
        private NetWorkPlayerControl _netWorkPlayerControl;

        private ActionEndType _actionEnd;
        
        private enum ActionEndType
        {
            Reload,GrenadeThrow
        }

        private Ui _ui;

        [HideInInspector]public bool firing;
        
        


        private void Start()
        {
            if (GetComponent<Bomb>()) _bomb = GetComponent<Bomb>();
            FPSMovement fpsMovement = GetComponentInParent<FPSMovement>();
            if (fpsMovement!=null)_camera = fpsMovement.camera;
            _netWorkPlayerControl = GetComponentInParent<NetWorkPlayerControl>();
            _magAmmo = magAmmo;
            ammo = _magAmmo;
            _totalAmmo = totalAmmo;
        }
        
        private void OnActionEnded()
        {
            if (_fpsController == null) return;
            _fpsController.ResetActionState();

            if (_actionEnd == ActionEndType.Reload)
            {
                ReloadAmmo();
            }
            
        }

        public void ResetAmmo()
        {
            _magAmmo = magAmmo;
            ammo = _magAmmo;
            _totalAmmo = totalAmmo;
            _ui = GetComponentInParent<Ui>();   
            _ui.SetAmmo(ammo);
            _ui.SetTotalAmmo(_totalAmmo);
        }

        private void ReloadAmmo()
        {
            if (_totalAmmo < _magAmmo && (ammo + _totalAmmo) < _magAmmo)
            {
                ammo = _totalAmmo + ammo;
                _totalAmmo = 0;
            }
            else
            {
                _totalAmmo -= (_magAmmo-ammo);
                ammo = _magAmmo;
            }
                
            _ui.SetAmmo(ammo);
            _ui.SetTotalAmmo(_totalAmmo);
        }
        
        protected void UpdateTargetFOV(bool isAiming)
        {
            float fov = fieldOfView;
            float sensitivityMultiplier = 1f;
            
            if (isAiming && scopeGroups.Count != 0)
            {
                var scope = scopeGroups[_scopeIndex].GetActiveAttachment();
                fov *= scope.aimFovZoom;

                sensitivityMultiplier = scopeGroups[_scopeIndex].GetActiveAttachment().sensitivityMultiplier;
            }

            _userInputController.SetValue("SensitivityMultiplier", sensitivityMultiplier);
            _fpsCameraController.UpdateTargetFOV(fov);
        }

        protected void UpdateAimPoint()
        {
            if (scopeGroups.Count == 0) return;

            var scope = scopeGroups[_scopeIndex].GetActiveAttachment().aimPoint;
            _fpsAnimatorEntity.defaultAimPoint = scope;
        }
        
        protected void InitializeAttachments()
        {
            foreach (var attachmentGroup in scopeGroups)
            {
                attachmentGroup.Initialize(_fpsAnimator);
            }
            
            _scopeIndex = 0;
            if (scopeGroups.Count == 0) return;

            UpdateAimPoint();
            UpdateTargetFOV(false);
        }
        
        public override void OnEquip(GameObject parent)
        {
            if (parent == null) return;

            _fireRate = fireRate;
            
            _fpsAnimator = parent.GetComponent<FPSAnimator>();
            _fpsAnimatorEntity = GetComponent<FPSAnimatorEntity>();
            
            _fpsController = parent.GetComponent<FPSController>();
            _weaponAnimator = GetComponentInChildren<Animator>();
            
            _controllerAnimator = parent.GetComponent<Animator>();
            _userInputController = parent.GetComponent<UserInputController>();
            _playablesController = parent.GetComponent<IPlayablesController>();
            _fpsCameraController = parent.GetComponentInChildren<FPSCameraController>();
            
            InitializeAttachments();
            
            _recoilAnimation = parent.GetComponent<RecoilAnimation>();
            _recoilPattern = parent.GetComponent<RecoilPattern>();
            
            _controllerAnimator.SetFloat(OverlayType, (float) overlayType);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
            
            barrelAttachments.Initialize(_fpsAnimator);
            gripAttachments.Initialize(_fpsAnimator);
            
            _recoilAnimation.Init(recoilData, _fireRate, _fireMode);

            if (_recoilPattern != null)
            {
                _recoilPattern.Init(recoilPatternSettings);
            }
            
            _controllerAnimator.CrossFade(CurveEquip, 0.15f);
            
            _ui = GetComponentInParent<Ui>();    
            
            _ui.SetAmmo(magAmmo);
            _ui.SetTotalAmmo(totalAmmo);

            if (supportsBurst)
            {
                _recoilAnimation.fireMode = FireMode.Burst;
                _fireRate = burstFireRate;
            }
            if (supportsAuto)  _recoilAnimation.fireMode = FireMode.Auto; 
            
        }

        public override void OnUnEquip()
        {
            _controllerAnimator.CrossFade(CurveUnequip, 0.15f);
        }

        public override void OnUnarmedEnabled()
        {
            _controllerAnimator.SetFloat(OverlayType, 0);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 0f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 0f);
        }

        public override void OnUnarmedDisabled()
        {
            _controllerAnimator.SetFloat(OverlayType, (int) overlayType);
            _userInputController.SetValue(FPSANames.PlayablesWeight, 1f);
            _userInputController.SetValue(FPSANames.StabilizationWeight, 1f);
            _fpsAnimator.LinkAnimatorProfile(gameObject);
        }

        public override bool OnAimPressed()
        {
            _userInputController.SetValue(FPSANames.IsAiming, true);
            UpdateTargetFOV(true);
            _recoilAnimation.isAiming = true;

            foreach (var setting in _fpsAnimatorEntity.animatorProfile.settings)
            {
                SwayLayerSettings swayLayerSetting = setting as SwayLayerSettings;
                if (swayLayerSetting)
                {
                    swayLayerSetting.alpha = 0.0f;
                }
            }
            return true;
        }

        public override bool OnAimReleased()
        {
            _userInputController.SetValue(FPSANames.IsAiming, false);
            UpdateTargetFOV(false);
            _recoilAnimation.isAiming = false;

            foreach (var setting in _fpsAnimatorEntity.animatorProfile.settings)
            {
                SwayLayerSettings swayLayerSetting = setting as SwayLayerSettings;
                if (swayLayerSetting)
                {
                    swayLayerSetting.alpha = 1f;
                }
            }
            
            return true;
        }

        private float _bombSetTime;

        private float _bombSetTimeNeed = 4; 
        
        private void BombPlant()
        {
            if (!_netWorkPlayerControl.canPlant)
            {
                _bombSetTime = 0f;

                if (_ui.doingSlider&&_ui.doingText)
                {
                    _ui.doingSlider.SetActive(false);
                    _ui.doingText.text = "";
                }
                CancelInvoke(nameof(BombPlant));
                
                return;
            }
            
            _bombSetTime += 0.05f;

            _ui.timeSlider.value = _bombSetTime / _bombSetTimeNeed;
            
            if (_bombSetTime<_bombSetTimeNeed)
            {
                Invoke(nameof(BombPlant),0.05f);
            }

            if (_bombSetTime > _bombSetTimeNeed)
            {
                GameManager.Instance.CmdBombPlant(transform.root, gameObject);

                OnFireReleased();

                if (_netWorkPlayerControl.mainWeaponIndex >= 0)
                {
                    _fpsController.ChangeWeapon(_netWorkPlayerControl.mainWeaponIndex);
                }
                else
                {
                    _fpsController.ChangeWeapon(_netWorkPlayerControl.secondWeaponIndex);
                }
                
                _netWorkPlayerControl.haveBomb = false;
                _netWorkPlayerControl.severAudioSource.clip = _bomb.bombPlantedAudioClip;
                _netWorkPlayerControl.severAudioSource.Play();
            }

        }
        
        public override bool OnFirePressed()
        {
            if (_bomb && transform.root.GetComponent<NetworkObject>().IsOwner)
            {
                if (!_netWorkPlayerControl.canPlant) return false;

                if (GameManager.Instance._gameState == GameManager.State.BuyIng) return false;
                    
                if (_ui.doingSlider&&_ui.doingText)
                {
                    _ui.doingSlider.SetActive(true);
                    _ui.doingText.text = "Planting";
                }
                
                _bombSetTime = 0f;
                
                BombPlant();
                
                _netWorkPlayerControl.SetMovement(false);
                
                _netWorkPlayerControl.severAudioSource.clip = _bomb.bombPlantingAudioClip;
                
                _netWorkPlayerControl.severAudioSource.Play();
                
                _netWorkPlayerControl.CmdPlantingBomb();
                
                return false;
            }

            
            if (Time.unscaledTime - _lastRecoilTime < 60f / _fireRate)
            {
                return false;
            }

            if (ammo == 0) return false;
    
            if (_recoilAnimation.fireMode == FireMode.Burst && firing)
            {
                return false;
            }
    
            _lastRecoilTime = Time.unscaledTime;

            if (_recoilAnimation.fireMode == FireMode.Burst && !firing)
            {
                _bursts = burstLength;
            }

            OnFire();
            firing = true;
    
            return true;
        }


        public override bool OnFireReleased()
        {
            if (_bomb && transform.root.GetComponent<NetworkObject>().IsOwner)
            {
                if (GameManager.Instance._gameState == GameManager.State.BuyIng) return false;
                
                if (_ui.doingSlider&&_ui.doingText)
                {
                    _ui.doingSlider.SetActive(false);
                    _ui.doingText.text = "";
                }
                _bombSetTime = 0f;
                CancelInvoke(nameof(BombPlant));
                _netWorkPlayerControl.severAudioSource.Stop();
                _netWorkPlayerControl.CmdCloseSeverAudioSource();
                
                _netWorkPlayerControl.SetMovement(true);
                
                return false;
            }
            
            if (_bomb) return false;
            
            if (_recoilAnimation != null)
            {
                _recoilAnimation.Stop();
            }
    
            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireEnd();
            }
            
            if (_recoilAnimation.fireMode != FireMode.Burst || _bursts == 0)
            {
                firing = false;
                CancelInvoke(nameof(OnFire));
            }

            return true;
        }

        public override bool OnReload()
        {
            if (!FPSAnimationAsset.IsValid(reloadClip) || _totalAmmo == 0)
            {
                return false;
            }
            
            _playablesController.PlayAnimation(reloadClip, 0f);
            
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Rebind();
                _weaponAnimator.Play("Reload", 0);
            }

            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraReloadAnimation);
            }
            
            Invoke(nameof(OnActionEnded), reloadClip.clip.length * 0.85f);

            _actionEnd = ActionEndType.Reload;

            OnFireReleased();
            return true;
        }

        public override bool OnGrenadeThrow()
        {
            if (!FPSAnimationAsset.IsValid(grenadeClip))
            {
                return false;
            }

            _playablesController.PlayAnimation(grenadeClip, 0f);
            
            if (_fpsCameraController != null)
            {
                _fpsCameraController.PlayCameraAnimation(cameraGrenadeAnimation);
            }
            
            Invoke(nameof(OnActionEnded), grenadeClip.clip.length * 0.8f);
            
            _actionEnd = ActionEndType.GrenadeThrow;
            return true;
        }
        
        private void OnFire()
        {
            if (_weaponAnimator != null)
            {
                _weaponAnimator.Play("Fire", 0, 0f);
            }
            _fpsCameraController.PlayCameraShake(cameraShake);

            if (_recoilAnimation != null && recoilData != null)
            {
                _recoilAnimation.Play();
            }

            if (_recoilPattern != null)
            {
                _recoilPattern.OnFireStart();
            }
    
            ShootBullet();
    
            if (ammo == 0)
            {
                _bursts = 0;
                OnFireReleased();
                return;
            }
        
            if (_recoilAnimation.fireMode == FireMode.Semi)
            {
                Invoke(nameof(OnFireReleased), 60f / _fireRate);
                return;
            }
    
            if (_recoilAnimation.fireMode == FireMode.Burst)
            {
                _bursts--;

                if (_bursts == 0)
                {
                    OnFireReleased();
                    return;
                }
        
                Invoke(nameof(OnFire), 60f / 600); // 根據你的需求調整這個數值
        
                return;
            }
    
            Invoke(nameof(OnFire), 60f / _fireRate);
        }
        
        private void ShootBullet()
        {
            if (!transform.root.GetComponent<NetworkObject>().IsOwner)return;
                
            Vector2 screenCenterPoint = new Vector2(Screen.width/2f, Screen.height/2f);
            Ray _ray = _camera.ScreenPointToRay(screenCenterPoint);
            
            float maxOffset = _ui._nowOffset;

            // 生成隨機的x偏移
            float xOffset = Random.Range(-maxOffset, maxOffset);

            // 根據x偏移計算y的範圍
            float yRange = Mathf.Sqrt(maxOffset * maxOffset - xOffset * xOffset);
            float yOffset = Random.Range(-yRange, yRange);

            // 根據x和y偏移計算z的範圍
            float zRange = Mathf.Sqrt(maxOffset * maxOffset - xOffset * xOffset - yOffset * yOffset);
            float zOffset = Random.Range(-zRange, zRange);
            
            _ray.direction = Quaternion.Euler(xOffset, yOffset, zOffset) * _ray.direction;
            
            Vector3 _targetPoint;
            
            if (Physics.Raycast(_ray,out RaycastHit raycastHit,999f,layerMask)) 
            { _targetPoint = raycastHit.point; }
            else { _targetPoint = _ray.GetPoint(999f); }
                                    
            Vector3 shootDirection = (_targetPoint - muzzleTransform.position).normalized;
            
            var bulletInstance = Instantiate(bulletPrefab, muzzleTransform.position, Quaternion.LookRotation(shootDirection));

            bulletInstance.GetComponent<Bullet>().weapon = GetComponent<Weapon>();

            bulletInstance.GetComponent<Bullet>().canDealDamage = true;
    
            ammo--;
            
            _ui.SetAmmo(ammo);
            
            _netWorkPlayerControl.CmdShootBullet(muzzleTransform.position,shootDirection);

            if (_ui._nowOffset < walkOffset) { _ui.AddOffset(0.1f); }
        }
        


        public void ShotPeople(Transform toWho, float damage)
        {
            _netWorkPlayerControl.ShotPeople(transform.root,toWho, damage);
        }
    
        
        public override void OnCycleScope()
        {
            if (scopeGroups.Count == 0) return;
            
            _scopeIndex++;
            _scopeIndex = _scopeIndex > scopeGroups.Count - 1 ? 0 : _scopeIndex;
            
            UpdateAimPoint();
            UpdateTargetFOV(true);
        }

        private void CycleFireMode()
        {
            if (_fireMode == FireMode.Semi && supportsBurst)
            {
                _fireRate = burstFireRate;
                _fireMode = FireMode.Burst;
                _bursts = burstLength;
                return;
            }

            if (_fireMode != FireMode.Auto && supportsAuto)
            {
                _fireRate = fireRate;
                _fireMode = FireMode.Auto;
                return;
            }
            
            _fireRate = fireRate;
            _fireMode = FireMode.Semi;
        }
        
        public override void OnChangeFireMode()
        {
            CycleFireMode();
            _recoilAnimation.fireMode = _fireMode;
        }

        public override void OnAttachmentChanged(int attachmentTypeIndex)
        {
            if (attachmentTypeIndex == 1)
            {
                barrelAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (attachmentTypeIndex == 2)
            {
                gripAttachments.CycleAttachments(_fpsAnimator);
                return;
            }

            if (scopeGroups.Count == 0) return;
            scopeGroups[_scopeIndex].CycleAttachments(_fpsAnimator);
            UpdateAimPoint();
        }
    }
}