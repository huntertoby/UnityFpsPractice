using System;
using System.Collections;
using System.Collections.Generic;
using Demo.Scripts.Runtime.Character;
using Demo.Scripts.Runtime.Item;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

public class InteractorUI : NetworkBehaviour
{
    
    [SerializeField] private LayerMask layerMask;
    private Camera camera;
    private Ui _ui;
    private FPSController _fpsController;
    [HideInInspector]public bool defusing;

    public AudioClip defusingAudioClip;
    public AudioClip defusedAudioClip;

    [HideInInspector] public bool canDefuse;
    private float _bombDefuseTime;
    private float _defaultDefuseTime = 5f;

    private NetWorkPlayerControl _netWorkPlayerControl;
    
    
    private void Start()
    {
        _ui = GetComponent<Ui>();
        camera = GetComponentInChildren<Camera>();
        _fpsController = GetComponent<FPSController>();
        _netWorkPlayerControl = GetComponent<NetWorkPlayerControl>();
    }

    private Bomb _bomb;
    
    void Update()
    {
        if(!IsOwner)return;
        Vector2 screenCenterPoint = new Vector2(Screen.width/2f, Screen.height/2f);
        Ray _ray = camera.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(_ray, out RaycastHit raycastHit, 5f, layerMask))
        {
            if (raycastHit.collider.GetComponentInParent<Bomb>())
            {
                _bomb = raycastHit.collider.GetComponentInParent<Bomb>();
                if (_bomb.defused)
                {
                    _ui.doingSlider.SetActive(false);
                    return;
                }
                if (!_bomb.canBeDefused)return;
                _ui.doingSlider.SetActive(true);
                _ui.doingText.text = "Defusing";
                canDefuse = true;
            }
            else
            {
                if (!_fpsController.GetActiveItem())return;
                if (_fpsController.GetActiveItem().gameObject.GetComponent<Bomb>()) return;
                _ui.doingSlider.SetActive(false);
                _ui.doingText.text = "";
                _bombDefuseTime = 0f;
                canDefuse = false;
                if (defusing) CancelBombDefuse();
            }
        }
        
    }
    public void CancelBombDefuse()
    {
        _ui.timeSlider.value = 0;
        _bombDefuseTime = 0;
        defusing = false;
        CancelInvoke(nameof(BombInteract));
    }

    public void BombInteract()
    {
        defusing = true;
        
        if (!canDefuse || !_bomb.canBeDefused)
        {
            CancelBombDefuse();
            return;
        }
        _bombDefuseTime += 0.05f;

        _ui.timeSlider.value = _bombDefuseTime / _defaultDefuseTime;

        if (_bombDefuseTime < _defaultDefuseTime)
        {
            Invoke(nameof(BombInteract),0.05f);
        }

        if (_bombDefuseTime > _defaultDefuseTime)
        {
            _netWorkPlayerControl._gameManager.bomb = _bomb;
            _bomb.defused = true;
            _netWorkPlayerControl.severAudioSource.clip = defusedAudioClip;
            _netWorkPlayerControl.severAudioSource.Play();
            CmdBombDefusedAudio();
        }
    }
    
    [ObserversRpc]
    private void RpcBombDefusedAudio()
    {
        if(IsOwner)return;
        Debug.Log(_netWorkPlayerControl._gameManager.bomb);
        Debug.Log(_netWorkPlayerControl._gameManager.bomb.name);
        _netWorkPlayerControl._gameManager.bomb.defused = true;
        _netWorkPlayerControl.severAudioSource.clip = defusedAudioClip;
        _netWorkPlayerControl.severAudioSource.Play();
    }

    [ServerRpc]
    private void CmdBombDefusedAudio()
    {
        RpcBombDefusedAudio();
    }
    
    [ObserversRpc]
    private void RpcDefusingBombAudio()
    {
        if(IsOwner)return;
        _netWorkPlayerControl.severAudioSource.clip = defusingAudioClip;
        _netWorkPlayerControl.severAudioSource.Play();
    }

    [ServerRpc]
    private void CmdDefusingBombAudio()
    {
        RpcDefusingBombAudio();
    }

    public void DefusingBombAudio()
    {
        CmdDefusingBombAudio();
        _netWorkPlayerControl.severAudioSource.clip = defusingAudioClip;
        _netWorkPlayerControl.severAudioSource.Play();
    }
}
