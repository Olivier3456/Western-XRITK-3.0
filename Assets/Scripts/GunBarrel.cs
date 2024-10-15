using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GunBarrel : MonoBehaviour
{
    [SerializeField] private GunBulletChamber[] gunBulletChambers;
    [SerializeField] private LayerMask bulletLayerMask;


    //private int firstFoundFreeBulletChamber;

    //private GunBullet bulletMovingToChamber;


    [SerializeField] private float bulletSnapMoveSpeed = 5f;
    [SerializeField] private float bulletSnapRotationSpeed = 5f;

    [SerializeField] private GameObject validChamberVisualDebugObject;

    [SerializeField] private Collider bulletsDetectionTriggerCollider;

    //private bool isMovingBullet;


    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return;

        // We want to know when this bullet will be dropped by player:
        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.AddListener(BulletGrabInteractable_selectExited);

        validChamberVisualDebugObject.SetActive(FindEmptyChamber() >= 0);
    }



    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return; // Not the good layer.

        validChamberVisualDebugObject.SetActive(false);

        // No need to know anymore when this bullet is dropped by player:
        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.RemoveListener(BulletGrabInteractable_selectExited);
    }


    private void BulletGrabInteractable_selectExited(SelectExitEventArgs args)
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

        validChamberVisualDebugObject.SetActive(false);

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


    private int FindEmptyChamber()
    {
        int firstFoundFreeBulletChamber = -1;

        for (int i = 0; i < gunBulletChambers.Length; i++)
        {
            if (gunBulletChambers[i].GunBullet == null)
            {
                firstFoundFreeBulletChamber = i;
                break;
            }
        }

        return firstFoundFreeBulletChamber;
    }


    public void DisableBarrelBulletDetectionCollider()
    {
        bulletsDetectionTriggerCollider.enabled = false;
    }

    public void EnableBulletDetectionCollider()
    {
        bulletsDetectionTriggerCollider.enabled = true;
    }
}
