using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyWeaponMenu : MonoBehaviour
{
    [SerializeField] private GameObject buyMenu;
    [SerializeField] private GameObject typeMenu;
    [SerializeField] private Button pistolsButton;
    [SerializeField] private Button smgButton;
    [SerializeField] private Button riflesButton;
    [SerializeField] private GameObject pistolsMenu;
    [SerializeField] private GameObject riflesMenu;
    [SerializeField] private GameObject msgMenu;
    [SerializeField] private Button backButton;
    
    [SerializeField] private Button[] mainWeaponButtons;
    [SerializeField] private Button[] secondWeaponButtons;
    [SerializeField] private Button[] meleeWeaponButtons;
    
    private NetWorkPlayerControl _netWorkPlayerControl;

    private int BuyMoney;
    
    public enum WeaponType
    {
        Main,Second,Melee
    }
    

    private void Start()
    {
        pistolsButton.onClick.AddListener( () =>PistolsButtonMenu(true));
        smgButton.onClick.AddListener( () =>SmgButtonMenu(true));
        riflesButton.onClick.AddListener( () =>RiflesButtonMenu(true));
        backButton.onClick.AddListener( () =>BackButton());
        
        _netWorkPlayerControl = GetComponent<NetWorkPlayerControl>();

        foreach (var button in mainWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Main));
        }
        foreach (var button in secondWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Second));
        }
        foreach (var button in meleeWeaponButtons)
        {
            button.onClick.AddListener(() => Buy(button.GetComponent<BuyWeaponButton>().weaponIndex,WeaponType.Melee));
        }
    }

    private void Buy(int index,WeaponType weaponType)
    {
        if (weaponType == WeaponType.Main)
        {
            _netWorkPlayerControl.BuyWeapon(index,1);
        }
        else if (weaponType == WeaponType.Second)
        {
            _netWorkPlayerControl.BuyWeapon(index,2);
        }
        else if (weaponType == WeaponType.Melee)
        {
            _netWorkPlayerControl.BuyWeapon(index,3);
        }
        
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
    }

    public void PistolsButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        pistolsMenu.SetActive(value);            
    }
    
    public void RiflesButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        riflesMenu.SetActive(value);            
    }
    
    public void SmgButtonMenu(bool value)
    {
        if (value) typeMenu.SetActive(false);
        msgMenu.SetActive(value);            
    }
    
}
