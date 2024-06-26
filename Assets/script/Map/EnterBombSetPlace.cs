using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnterBombSetPlace : MonoBehaviour
{
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>())
        {
            other.GetComponent<NetWorkPlayerControl>().canPlant = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<NetWorkPlayerControl>())
        {
            other.GetComponent<NetWorkPlayerControl>().canPlant = false;
        }
    }
}
