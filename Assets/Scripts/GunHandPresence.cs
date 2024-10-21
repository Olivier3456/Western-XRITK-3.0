using UnityEngine;

public class GunHandPresence : MonoBehaviour
{
    [SerializeField] private Gun gun;

    [SerializeField] private GameObject leftHandOnGun;
    [SerializeField] private GameObject rightHandOnGun;
    [SerializeField] private GameObject leftHandOnController;
    [SerializeField] private GameObject rightHandOnController;

    void Start()
    {
        gun.OnGunGrabbed += Gun_OnGunGrabbed;
        gun.OnGunDropped += Gun_OnGunDropped;
    }

    private void Gun_OnGunGrabbed(object obj, Gun.CurrentController currentController)
    {
        if (currentController == Gun.CurrentController.Left)
        {
            leftHandOnGun.SetActive(true);
            leftHandOnController.SetActive(false);
        }
        else
        {
            rightHandOnGun.SetActive(true);
            rightHandOnController.SetActive(false);
        }
    }


    private void Gun_OnGunDropped(object obj, Gun.CurrentController currentController)
    {
        if (currentController == Gun.CurrentController.Left)
        {
            leftHandOnGun.SetActive(false);
            leftHandOnController.SetActive(true);
        }
        else
        {
            rightHandOnGun.SetActive(false);
            rightHandOnController.SetActive(true);
        }
    }
}
