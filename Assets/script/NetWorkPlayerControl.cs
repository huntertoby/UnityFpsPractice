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

    [HideInInspector]public int money;

    [HideInInspector] public int team;

    [HideInInspector] public bool haveBomb;
    [HideInInspector]public bool canPlant;
    
   [HideInInspector] public int mainWeaponIndex = -1;
   [HideInInspector] public int secondWeaponIndex = -1;
   [HideInInspector] public int meleeWeaponIndex = -1;

   [SerializeField] private Transform changerCharacterTransform;
   [SerializeField] public AudioSource severAudioSource;
   
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner) gameObject.name = "MyPlayer";
        _uiSever = GetComponent<UiSever>();
        if (IsOwner) _uiSever.player = transform;
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _gameManager.teamUi.SetActive(true);
        _gameManager.startersUi.SetActive(false);
        SetCanvas();

        // Test();

        



        // for (int i = 0; i < NetworkManager.SpawnablePrefabs.GetObjectCount(); i++)
        // {
        //     var prefab = NetworkManager.SpawnablePrefabs.GetObject(true, i);
        //     Debug.Log($"Prefab [{i}]: {prefab.name}");
        // }
    }

    private void Test()
    {
        InitializedPlayer(true);
        _fpsMovement.canMove = true;
        _gameManager.startersUi.SetActive(false);
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
        _ui.SetUiActive(false);
        transform.position = GameObject.Find("DieTransform").transform.position;
        _gameManager.PlayerDie(team);
        InitializedPlayer(false);
        CameraSet(false);
        Cmd(team);
    }
    
    void SetMyLayer(GameObject obj)
    {
        obj.layer = 31;
   
        foreach (Transform child in obj.transform)
        {
            SetMyLayer(child.gameObject);
        }
    }

    public void BuyWeapon(int index,int type)
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
    
    [ObserversRpc]
    private void Rpc(int value)
    {
        if (!IsOwner)
        {
            transform.position = GameObject.Find("DieTransform").transform.position;
            _gameManager.PlayerDie(value);
        }
    }

    [ServerRpc]
    private void Cmd(int value)
    {
        Rpc(value);
    }

    public void IsPlanting()
    {
        
    }
    
    public void SetMovement(bool value)
    {
        _fpsMovement.canMove = value;
        if (!_ui.doingSlider) return;
        if (_ui.doingSlider.activeSelf) _fpsMovement.canMove = false;
    }
    public void CameraSet(bool value)
    {
        Debug.Log("CameraSet");
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
            Instantiate(weapon.bulletPrefab,muzzlePosition,Quaternion.LookRotation(shootDirection, Vector3.up));
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
        if (severAudioSource.clip == _interactorUI.defusedAudioClip)return;
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
        Weapon weapon = _fpsController.GetActiveItem()as Weapon;
        if (weapon)
        {
            Bomb bomb = weapon.gameObject.GetComponent<Bomb>();
            severAudioSource.clip = bomb.bombPlantingAudioClip;
            severAudioSource.Play();
        }
    }

    [ServerRpc]
    public void CmdPlantingBomb()
    {
        RpcPlantBomb();
    }
    
    [ObserversRpc]
    private void RpcPlantedBomb()
    {
        if(IsOwner)return;
        Weapon weapon = _fpsController.GetActiveItem()as Weapon;
        if (weapon)
        {
            Debug.Log(weapon.gameObject.name);
            Bomb bomb = weapon.gameObject.GetComponent<Bomb>();
            var bombInstantiate = Instantiate(bomb.gameObject,transform.root.position,Quaternion.LookRotation(Vector3.up));
            if (mainWeaponIndex>=0) _fpsController.ChangeWeapon(mainWeaponIndex);
            else _fpsController.ChangeWeapon(secondWeaponIndex);
            haveBomb = false;
            severAudioSource.clip = bomb.bombPlantedAudioClip;
            severAudioSource.Play();
            bombInstantiate.GetComponent<Bomb>().BombStart();
            _gameManager.bomb = bombInstantiate.GetComponent<Bomb>();
            _gameManager.SetPlayersTimeOrBombUi("bomb");
        }
    }

    [ServerRpc]
    public void CmdPlantedBomb()
    {
        RpcPlantedBomb();
    }
    

    
    
}
