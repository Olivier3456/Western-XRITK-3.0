using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GunBulletChamber : MonoBehaviour
{
    private XRSocketInteractor xRSocketInteractor;
    
    // public XRSocketInteractor XRSocketInteractor
    // {
    //     get => xRSocketInteractor;
    // }

    public bool hasSelection => xRSocketInteractor.hasSelection;

    public GunBullet GunBullet
    {
        get
        {
            if (!xRSocketInteractor.hasSelection)
            {
                return null;
            }

            return xRSocketInteractor.interactablesSelected[0].transform.GetComponent<GunBullet>();
        }
    }


    private void Awake()
    {
        xRSocketInteractor = GetComponent<XRSocketInteractor>();
    }
}


