using System;
using System.Collections;
using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using FishNet.Object;
using UnityEngine;
using KINEMATION.FPSAnimationFramework.Runtime.Core;
using KINEMATION.FPSAnimationFramework.Runtime.Playables;
using KINEMATION.FPSAnimationFramework.Runtime.Recoil;
using KINEMATION.KAnimationCore.Runtime.Input;
using UnityEngine.InputSystem;

public class NetWorkPlayerControl : NetworkBehaviour
{
    private UserInputController _userInputController;
    private FPSAnimator _fpsAnimator;
    private RecoilAnimation _recoilAnimation;
    private FPSMovement _fpsMovement;
    private PlayerInput _playerInput;
    private FPSPlayablesController _fpsPlayablesController;
    private RecoilPattern _recoilPattern;
    private FPSController _fpsController;
    private CharacterController _characterController;
    private UiSever _uiSever;
    private InteractorUI _interactorUI;
    [HideInInspector]public GameManager _gameManager;
    private Ui _ui;

    private bool _initialized;

    public GameManager.TeamSide playerSide;

    [HideInInspector]public int money;

     public int team;
     public int teamIndex;

    public bool haveBomb;
    [HideInInspector]public bool canPlant;
    
   [HideInInspector] public int mainWeaponIndex = -1;
   [HideInInspector] public int secondWeaponIndex = -1;
   [HideInInspector] public int meleeWeaponIndex = -1;

   [SerializeField] private Transform changerCharacterTransform;
   [SerializeField] public AudioSource severAudioSource;

   private Health _health;
   
    public override void OnStartClient()
    {
        base.OnStartClient();
        GameObject.Find("Canvas").GetComponent<Canvas>().enabled = false;
        if (IsOwner) gameObject.name = "MyPlayer";
        _uiSever = GetComponent<UiSever>();
        _gameManager = GameManager.Instance;
        _gameManager.teamUi.SetActive(true);
        if (IsOwner)_gameManager.GetComponent<UiSever>().player = transform;
        SetCanvas();
        
        // Test();
        
        // for (int i = 0; i < NetworkManager.SpawnablePrefabs.GetObjectCount(); i++)
        // {
        //     var prefab = NetworkManager.SpawnablePrefabs.GetObject(true, i);
        //     Debug.Log($"Prefab [{i}]: {prefab.name}");
        // }

        
    }

    private void Start()
    {

    }

    private void Test()
    {
        InitializedPlayer(true);
        _fpsMovement.canMove = true;
        haveBomb = true;
    }

    public void InitializedPlayer(bool value)
    {
        _userInputController = GetComponent<UserInputController>();
        _fpsAnimator = GetComponent<FPSAnimator>();
        _recoilAnimation = GetComponent<RecoilAnimation>();
        _fpsMovement = GetComponent<FPSMovement>();
        _playerInput = GetComponent<PlayerInput>();
        _fpsPlayablesController = GetComponent<FPSPlayablesController>();
        _recoilPattern = GetComponent<RecoilPattern>();
        _fpsController = GetComponent<FPSController>();
        _characterController = GetComponent<CharacterController>();
        _ui = GetComponent<Ui>();
        _interactorUI = GetComponent<InteractorUI>();
        _health = GetComponent<Health>();
        _userInputController.enabled = value;
        _fpsAnimator.enabled = value;
        _recoilAnimation.enabled = value;
        _fpsMovement.enabled = value;
        _playerInput.enabled = value;
        _fpsPlayablesController.enabled = value;
        _recoilPattern.enabled = value;
        _fpsController.enabled = value;
        _characterController.enabled = value;
        if (IsOwner) _ui.SetUiActive(value);

        mainWeaponIndex = -1;
        secondWeaponIndex = 0;
        meleeWeaponIndex = -1;    
        
        CameraSetInitialize();
        
        _initialized = true;
    }
    
    public void SetDie()
    {
        if (!IsOwner)return;
        _ui.SetUiActive(false);
        transform.position = GameObject.Find("DieTransform").transform.position;
        _gameManager.PlayerDie(team);
        InitializedPlayer(false);
        CameraSet(false);
    }
    
    [ObserversRpc]
    private void RpcBuyWeapon(int index,int type)
    {
        if (!IsOwner)
        {
            if (type == 1)
            {
                mainWeaponIndex = index;
                _fpsController.ChangeWeapon(index);
            }else if (type == 2)
            {
                secondWeaponIndex = index;
                _fpsController.ChangeWeapon(index);
            }else if (type == 3)
            {
                meleeWeaponIndex = index;
                _fpsController.ChangeWeapon(index);
            }

        }
    }
    


    [ServerRpc]
    private void CmdBuyWeapon(int index,int type)
    {
        RpcBuyWeapon(index,type);
    }
    
    public void ShotPeople(Transform toWho, float damage)
    {
        if(!IsOwner) return;
        Debug.Log(damage);
        _gameManager.CmdDealDamage(toWho, damage);
    }
    
    public void BuyWeapon(int index,int type)
    {
        if (!IsOwner)return;
            
        if (type == 1)
        {
            mainWeaponIndex = index;
            _fpsController.ChangeWeapon(index);
        }else if (type == 2)
        {
            secondWeaponIndex = index;
            _fpsController.ChangeWeapon(index);
        }else if (type == 3)
        {
            meleeWeaponIndex = index;
            _fpsController.ChangeWeapon(index);
        }

        CmdBuyWeapon(index,type);
    }
    
    public void SetMovement(bool value)
    {
        _fpsMovement.canMove = value;
        if (!_ui.doingSlider) return;
        if (_ui.doingSlider.activeSelf) _fpsMovement.canMove = false;
    }
    public void CameraSet(bool value)
    {
        Camera camera = GetComponentInChildren<Camera>();
        AudioListener audioListener = GetComponentInChildren<AudioListener>();
        camera.enabled = value;
        audioListener.enabled = value;
    }

    private void SetCanvas()
    {
        if (!IsOwner && GetComponentInChildren<Canvas>().gameObject) Destroy(GetComponentInChildren<Canvas>().gameObject);
    }
    
    private void CameraSetInitialize()
    {
        Camera camera = GetComponentInChildren<Camera>();
        AudioListener audioListener = GetComponentInChildren<AudioListener>();
        if (IsOwner)
        {
            if (camera != null) camera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
            foreach (Transform child in changerCharacterTransform)
            {
                Renderer renderer = child.GetComponent<Renderer>();
            
                if (!renderer) break;
                
                foreach (Material material in renderer.materials)
                {
                    if (material.name.Contains("head"))
                    {
                        material.shader = Shader.Find("Transparent/Invisible Shadow Caster");
                    }
                }
            
            }
        }
        else
        {
            if (camera != null) camera.enabled = false;
            if (audioListener != null) audioListener.enabled = false;
        }
    }
    
    [ObserversRpc]
    private void RpcShootBullet(Vector3 muzzlePosition,Vector3 shootDirection)
    {
        if (IsOwner)return;
        
        Weapon weapon = _fpsController.GetActiveItem() as Weapon;
        if (weapon)
        {
            var  bulletInstance = Instantiate(weapon.bulletPrefab,muzzlePosition,Quaternion.LookRotation(shootDirection, Vector3.up));
            bulletInstance.GetComponent<Bullet>().canDealDamage = false;
        }
        
    }

    [ServerRpc]
    public void CmdShootBullet(Vector3 muzzlePosition,Vector3 shootDirection)
    {
        RpcShootBullet(muzzlePosition,shootDirection);
    }
    
    [ObserversRpc]
    private void RpcCloseSeverAudioSource()
    {
        if(IsOwner)return;
        severAudioSource.Stop();
    }

    [ServerRpc]
    public void CmdCloseSeverAudioSource()
    {
        RpcCloseSeverAudioSource();
    }
    
    [ObserversRpc]
    private void RpcPlantBomb()
    {
        if(IsOwner)return;
        var weapon = _fpsController.GetActiveItem()as Weapon;
        severAudioSource.PlayOneShot(weapon.gameObject.GetComponent<Bomb>().bombPlantingAudioClip);
    }

    [ServerRpc]
    public void CmdPlantingBomb()
    {
        RpcPlantBomb();
    }
}
