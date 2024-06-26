
using System;
using System.Collections;
using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Ui : NetworkBehaviour
{
    private FPSMovement _fpsMovement;
    private FPSController _fpsController;
    private NetWorkPlayerControl _netWorkPlayerControl;
    
    [SerializeField] private Button Button1;

    [SerializeField] private GameObject playerUIObject;
    
    [Header("healthBar")]
    [SerializeField] private Slider shieldSlider;
    [SerializeField] private Slider healthSlider;
    [Header("Ammo")]
    [SerializeField] private TextMeshProUGUI ammo;
    [SerializeField] private TextMeshProUGUI totalAmmo;
    [Header("CrossHair")] 
    [SerializeField] private RectTransform crossHair;
    [HideInInspector]public float _nowOffset;
    [Header("Game")] 
    [SerializeField] private GameObject uiGameObject;
    [SerializeField] private TextMeshProUGUI team1Win;
    [SerializeField] private TextMeshProUGUI team2Win;
    [SerializeField] public TextMeshProUGUI time;
    [SerializeField] public Image bombImage;
    private GameManager _gameManager;

    [Header("Bomb")] 
    [SerializeField] public GameObject doingSlider;
    [SerializeField] public TextMeshProUGUI doingText;
    [SerializeField] public Slider timeSlider;
    
    
    
    [Header("BuyMenu")] 
    public BuyWeaponMenu buyWeaponMenu;
    
    [HideInInspector]public bool canUpdate = false;
    [HideInInspector]public float targetOffset;
    [HideInInspector]public Weapon weapon;
    
    
    
    void Start()
    {
        _gameManager = GameManager.Instance;
        Button1.onClick.AddListener(button1f);
        _fpsMovement = GetComponent<FPSMovement>();
        _fpsController = GetComponent<FPSController>();
        _netWorkPlayerControl = GetComponent<NetWorkPlayerControl>();
    }
    
    private void Buy(int value)
    {
        _fpsController.ChangeWeapon(value);
    }

    public void SetTeamWin(string value1,string value2)
    {
        if (!team1Win||!team2Win)return;
        team1Win.text = value1;
        team2Win.text = value2;
    }
    
    public void GetTimeUI()
    {
        if (!time)return;
        GameManager.Instance.GetTime(transform);
        Invoke(nameof(GetTimeUI),0.1f);
    }

    public void SetTimeUI(string timeText)
    {
        time.text = timeText;
    }

    public void SetUiActive(bool value)
    {
        if (!playerUIObject)return;
        playerUIObject.SetActive(value);
    }
    
    public void SetGameUiActive(bool value)
    {
        if (!uiGameObject)return;
        uiGameObject.SetActive(value);
    }

    public void SethHealthAndShield(float shield,float health)
    {
        if (!healthSlider||!shieldSlider)return;
        healthSlider.value = health;
        shieldSlider.value = shield;
    }
    
    private void button1f()
    {
        
    }

    public void SetAmmo(int value)
    {
        ammo.text = value.ToString();
    }
    
    public void SetTotalAmmo(int value)
    {
        totalAmmo.text = value.ToString();
    }

    private void Update()
    {
        if (!canUpdate) return;
            UpdateCrossHair();
    }

    public void AddOffset(float value)
    {
        _nowOffset += value;
    }

    private void UpdateCrossHair()
    {
        if (IsOwner)
        {
            if (_fpsMovement.MovementState == FPSMovementState.Idle)
            {
                targetOffset = weapon.idleOffset;
            }else if (_fpsMovement.MovementState == FPSMovementState.InAir)
            {
                targetOffset = weapon.airOffset;
            }else if (_fpsMovement.MovementState == FPSMovementState.Walking)
            {
                targetOffset = weapon.walkOffset;
            }else if (_fpsMovement.MovementState == FPSMovementState.Sliding)
            {
                targetOffset = weapon.slideOffset;
            }

            if (_fpsController.IsAiming())
            {
                targetOffset = 0;
            }

            if (_nowOffset < 0.1f)
            {
                crossHair.transform.parent.gameObject.SetActive(false);
            }
            else if (!_fpsController.IsAiming())
            {
                crossHair.transform.parent.gameObject.SetActive(true);
            }

            if (!weapon.firing) _nowOffset = Mathf.Lerp(_nowOffset, targetOffset, Time.deltaTime * 10);
            
            crossHair.sizeDelta = new Vector2(_nowOffset*16.4f-50, _nowOffset*16.4f-50);
        }
    }
}
