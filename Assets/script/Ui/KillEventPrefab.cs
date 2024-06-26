using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class KillEventPrefab : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI killerText;
    [SerializeField] public Image weaponImage;
    [SerializeField] public TextMeshProUGUI beKillerText;


    private void Start()
    {
        Invoke(nameof(DestroyMyself),5f);
    }

    private void DestroyMyself()
    {
        Destroy(gameObject);
    }
}   


