using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GunBulletChamber : MonoBehaviour
{
    private XRSocketInteractor xRSocketInteractor;

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

    public void EnableSocketInteractor()
    {
        xRSocketInteractor.socketActive = true;
    }

    public void DisableSocketInteractor(bool onlyIfEmpty)
    {
        if (!onlyIfEmpty)
        {
            xRSocketInteractor.socketActive = false;
            return;
        }

        if (!xRSocketInteractor.hasSelection)
        {
            xRSocketInteractor.socketActive = false;
        }
    }
}