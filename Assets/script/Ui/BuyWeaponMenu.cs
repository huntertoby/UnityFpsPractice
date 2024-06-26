using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyWeaponMenu : MonoBehaviour
{
    [SerializeField] public GameObject buyMenu;
    [SerializeField] private GameObject typeMenu;
    [SerializeField] private Button pistolsButton;
    [SerializeField] private Button smgButton;
    [SerializeField] private Button riflesButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private GameObject pistolsMenu;
    [SerializeField] private GameObject riflesMenu;
    [SerializeField] private GameObject msgMenu;
    [SerializeField] private GameObject shieldMenu;
    [SerializeField] private Button backButton;
    
    [SerializeField] private Button[] mainWeaponButtons;
    [SerializeField] private Button[] secondWeaponButtons;
    [SerializeField] private Button[] meleeWeaponButtons;

    [SerializeField] private Button halfShieldButton;
    [SerializeField] private Button fullShieldButton;

    [SerializeField] private TextMeshProUGUI moneyText;
    
    private NetWorkPlayerControl _netWorkPlayerControl;
    
    public enum WeaponType
    {
        Main,Second,Melee
    }
    
    
    public void CloseBuyMenu()
    {
        buyMenu.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        BackButton();
    }
    
    public void OpenBuyMenu()
    {
        buyMenu.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        moneyText.text = "Money: "+_netWorkPlayerControl.money.ToString();
    }   
    

    private void Start()
    {
        pistolsButton.onClick.AddListener( () =>PistolsButtonMenu(true));
        smgButton.onClick.AddListener( () =>SmgButtonMenu(true));
        riflesButton.onClick.AddListener( () =>RiflesButtonMenu(true));
        shieldButton.onClick.AddListener( () =>ShieldButtonMenu(true));
        backButton.onClick.AddListener( () =>BackButton());
        
        halfShieldButton.onClick.AddListener(()=>BuyHalfShield());
        fullShieldButton.onClick.AddListener(()=>BuyFullShield());
        
        _netWorkPlayerControl = GetComponent<NetWorkPlayerControl>();

        foreach (var button in mainWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Main,button.GetComponent<BuyWeaponButton>().weaponMoney));
        }
        foreach (var button in secondWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Second,button.GetComponent<BuyWeaponButton>().weaponMoney));
        }
        foreach (var button in meleeWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Melee,button.GetComponent<BuyWeaponButton>().weaponMoney));
        }
    }

    private void Buy(int index,WeaponType weaponType,int weaponMoney)
    {
        if (weaponType == WeaponType.Main)
        {
            _netWorkPlayerControl.BuyWeapon(index,1,weaponMoney);
        }
        else if (weaponType == WeaponType.Second)
        {
            _netWorkPlayerControl.BuyWeapon(index,2,weaponMoney);
        }
        else if (weaponType == WeaponType.Melee)
        {
            _netWorkPlayerControl.BuyWeapon(index,3,weaponMoney);
        }

        moneyText.text = "Money: " + _netWorkPlayerControl.money.ToString();
        BackButton();
        BackButton();
    }
    

    public void BackButton()
    {
        if (typeMenu.activeSelf)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            buyMenu.SetActive(false);
        }
        typeMenu.SetActive(true);
        pistolsMenu.SetActive(false);
        riflesMenu.SetActive(false);
        msgMenu.SetActive(false);
        shieldMenu.SetActive(false);
    }

    private void BuyHalfShield()
    {
        _netWorkPlayerControl.SetShield(0.5f,400);
        BackButton();
        BackButton();
    }
    
    private void BuyFullShield()
    {
        _netWorkPlayerControl.SetShield(1f,800);
        BackButton();
        BackButton();
    }
    
    public void ShieldButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        shieldMenu.SetActive(value);
        
        if (400 > _netWorkPlayerControl.money)
            halfShieldButton.interactable = false;
        else halfShieldButton.interactable = true;
        
        if (800 > _netWorkPlayerControl.money)
            fullShieldButton.interactable = false;
        else fullShieldButton.interactable = true;
    }

    public void PistolsButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        pistolsMenu.SetActive(value);
        foreach (var button in secondWeaponButtons)
        {
            if (button.GetComponent<BuyWeaponButton>().weaponMoney > _netWorkPlayerControl.money)
                button.interactable = false;
            else button.interactable = true;
        }
    }
    
    public void RiflesButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        riflesMenu.SetActive(value);        
        foreach (var button in mainWeaponButtons)
        {
            if (button.GetComponent<BuyWeaponButton>().weaponMoney > _netWorkPlayerControl.money)
                button.interactable = false;
            else button.interactable = true;
        }
    }
    
    public void SmgButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        msgMenu.SetActive(value);     
        foreach (var button in meleeWeaponButtons)
        {
            if (button.GetComponent<BuyWeaponButton>().weaponMoney > _netWorkPlayerControl.money)
                button.interactable = false;
            else button.interactable = true;
        }
    }
    
}
