using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GunBarrel : MonoBehaviour
{
    [SerializeField] private Gun gun;

    public GunBulletChamber[] GunBulletChambers => gunBulletChambers;
    [SerializeField] private GunBulletChamber[] gunBulletChambers;

    [SerializeField] private LayerMask bulletLayerMask;

    [SerializeField] private float bulletSnapMoveSpeed = 5f;
    [SerializeField] private float bulletSnapRotationSpeed = 5f;

    [SerializeField] private GameObject validChamberVisualDebugObject;

    [SerializeField] private Collider bulletsDetectionTriggerCollider;


    private WaitForSeconds waitOneSec = new WaitForSeconds(1f);


    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return;

        if (other.gameObject.GetComponent<GunBullet>().GunBulletChamber != null) return;

        // we want to know when this bullet will be dropped by player:
        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.AddListener(BulletGrabInteractable_selectExited);


        int emptyChamberFound = FindEmptyChamber();
        if (emptyChamberFound >= 0)
        {
            ShowBulletPlacementVisual(emptyChamberFound);
        }
    }



    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return; // Not the good layer.

        HideAllBulletChambersVisual();

        // No need to know anymore when this bullet is dropped by player:
        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.RemoveListener(BulletGrabInteractable_selectExited);
    }



    private void BulletGrabInteractable_selectExited(SelectExitEventArgs args)      // only for bullets which are in the barrel proximity trigger
    {
        GunBullet gunBullet = args.interactableObject.transform.GetComponent<GunBullet>();
        StartCoroutine(MoveBulletToChamberCoroutine(gunBullet, FindEmptyChamber()));
    }



    private IEnumerator MoveBulletToChamberCoroutine(GunBullet gunBullet, int chamberId)
    {
        // No need to know anymore when this bullet is dropped by player:
        XRGrabInteractable bulletGrabInteractable = gunBullet.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.RemoveListener(BulletGrabInteractable_selectExited);

        if (chamberId < 0) yield break; // No empty chamber found.

        gunBulletChambers[chamberId].AddBullet(gunBullet);

        gunBullet.SetRigidbodyKinematic(true);
        gunBullet.DisableCollider();

        HideAllBulletChambersVisual();

        Transform bulletTransform = gunBullet.transform;
        Transform chamberTransform = gunBulletChambers[chamberId].transform;

        float remainingDistance = float.MaxValue;
        while (remainingDistance > 0.001f)
        {
            remainingDistance = Vector3.Distance(bulletTransform.position, chamberTransform.position);
            float distanceFactor = 1f / remainingDistance;
            float lerp = Time.deltaTime * bulletSnapMoveSpeed * distanceFactor;

            bulletTransform.position = Vector3.Lerp(bulletTransform.position, chamberTransform.position, lerp);
            bulletTransform.rotation = Quaternion.Slerp(bulletTransform.rotation, chamberTransform.rotation, Time.deltaTime * bulletSnapRotationSpeed);

            yield return null;
        }

        bulletTransform.position = chamberTransform.position;
        bulletTransform.rotation = chamberTransform.rotation;
        bulletTransform.parent = chamberTransform;
    }


    public IEnumerator BulletsExpulsionCoroutine()
    {
        yield return waitOneSec;

        EnableBarrelBulletDetectionCollider();

        bool hasBullet = false;
        foreach (GunBulletChamber chamber in gunBulletChambers)
        {
            if (chamber.GunBullet != null)
            {
                hasBullet = true;
                break;
            }
        }
        if (!hasBullet) // no bullet in barrel: no need to continue the coroutine
        {
            yield break;
        }


        bool areBulletsOut = false;

        while (!areBulletsOut && gun.IsBarrelOut)
        {
            yield return null;

            Vector3 barrelCurrentForward = transform.forward;
            float verticalDot = Vector3.Dot(barrelCurrentForward, Vector3.up);

            float dotThreshold = 0.8f;
            if (verticalDot > dotThreshold) // the revolver is facing up
            {
                areBulletsOut = true;
                DisableBarrelBulletDetectionCollider(); // avoid bullets detection when bullets falling

                foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
                {
                    if (gunBulletChamber.GunBullet != null)
                    {
                        gunBulletChamber.GunBullet.ExpulsionFromGunBarrel(barrelCurrentForward);
                        gunBulletChamber.GunBullet.EnableCollider();
                        gunBulletChamber.GunBullet.transform.parent = null;
                        gunBulletChamber.RemoveBullet();
                    }
                }
            }
        }

        // wait a little to give enough time to the falling bullets to exit the gun barrel bullet detection trigger before re-enabling it:
        yield return waitOneSec;
        EnableBarrelBulletDetectionCollider();
    }


    private int FindEmptyChamber()
    {
        int firstFoundFreeBulletChamber = -1;

        // first search: starting to bullet chamber facing canon
        for (int i = gun.BulletChamberFacingCanon; i < gunBulletChambers.Length; i++)
        {
            if (gunBulletChambers[i].GunBullet == null)
            {
                firstFoundFreeBulletChamber = i;
                break;
            }
        }

        // if no empty chamber found: second search, starting to the beginning of the array
        if (firstFoundFreeBulletChamber < 0)
        {
            for (int i = 0; i < gun.BulletChamberFacingCanon; i++)
            {
                if (gunBulletChambers[i].GunBullet == null)
                {
                    firstFoundFreeBulletChamber = i;
                    break;
                }
            }
        }

        return firstFoundFreeBulletChamber;
    }


    public void DisableBarrelBulletDetectionCollider()
    {
        bulletsDetectionTriggerCollider.enabled = false;
    }

    public void EnableBarrelBulletDetectionCollider()
    {
        bulletsDetectionTriggerCollider.enabled = true;
    }

    public void EnableBulletsGrabbable()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            if (gunBulletChamber.GunBullet != null)
            {
                gunBulletChamber.GunBullet.EnableCollider();
            }
        }
    }

    public void DisableBulletsGrabbable()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            if (gunBulletChamber.GunBullet != null)
            {
                gunBulletChamber.GunBullet.DisableCollider();
            }
        }
    }

    private void ShowBulletPlacementVisual(int bulletChamberId) => gunBulletChambers[bulletChamberId].ShowBulletPlacementVisual();

    private void HideAllBulletChambersVisual()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            gunBulletChamber.HideBulletPlacementVisual();
        }
    }
}
