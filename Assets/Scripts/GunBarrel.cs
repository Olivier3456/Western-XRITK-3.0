using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GunBarrel : MonoBehaviour
{
    [SerializeField] private GunBulletChamber[] gunBulletChambers;
    [SerializeField] private LayerMask bulletLayerMask;


    private int firstFoundFreeBulletChamber;

    private GunBullet bulletMovingToChamber;


    [SerializeField] private float bulletSnapMoveSpeed = 5f;
    [SerializeField] private float bulletSnapRotationSpeed = 5f;

    [SerializeField] private GameObject validChamberVisualDebugObject;

    private bool isMovingBullet;


    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return;

        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.AddListener(BulletGrabInteractable_selectExited);   // We want to know when this bullet will be dropped by player.

        firstFoundFreeBulletChamber = -1;

        for (int i = 0; i < gunBulletChambers.Length; i++)
        {
            if (gunBulletChambers[i].GunBullet == null)
            {
                firstFoundFreeBulletChamber = i;
                break;
            }
        }

        validChamberVisualDebugObject.SetActive(firstFoundFreeBulletChamber > -1);
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & bulletLayerMask) == 0) return;                                     // Not the good layer.

        validChamberVisualDebugObject.SetActive(false);

        if (isMovingBullet && bulletMovingToChamber == other.gameObject.GetComponent<GunBullet>()) return;    // The bullet is the one currently snapping.

        XRGrabInteractable bulletGrabInteractable = other.gameObject.GetComponent<XRGrabInteractable>();
        bulletGrabInteractable.selectExited.RemoveListener(BulletGrabInteractable_selectExited);

        Debug.Log($"Bullet {bulletMovingToChamber} has exited barrel trigger.");

        bulletMovingToChamber = null;
    }


    private void BulletGrabInteractable_selectExited(SelectExitEventArgs args)
    {
        bulletMovingToChamber = args.interactableObject.transform.GetComponent<GunBullet>();
    }


    private void Update()
    {
        if (bulletMovingToChamber == null) return;

        if (firstFoundFreeBulletChamber < 0)
        {
            return;
        }

        if (!isMovingBullet)
        {
            XRGrabInteractable bulletGrabInteractable = bulletMovingToChamber.gameObject.GetComponent<XRGrabInteractable>();
            bulletGrabInteractable.selectExited.RemoveListener(BulletGrabInteractable_selectExited);

            bulletMovingToChamber.SetRigidbodyKinematic(true);
            bulletMovingToChamber.DisableCollider();
            validChamberVisualDebugObject.SetActive(false);
            isMovingBullet = true;
        }


        //Debug.Log($"Snapping bullet {bulletToSnapToChamber} to chamber {firstFoundFreeBulletChamber}...");


        Transform bulletTransform = bulletMovingToChamber.transform;
        Transform chamberTransform = gunBulletChambers[firstFoundFreeBulletChamber].transform;

        float remainingDistance = Vector3.Distance(bulletTransform.position, chamberTransform.position);
        float distanceFactor = 1f / remainingDistance;
        float lerp = Time.deltaTime * bulletSnapMoveSpeed * distanceFactor;

        bulletTransform.position = Vector3.Lerp(bulletTransform.position, chamberTransform.position, lerp);
        bulletTransform.rotation = Quaternion.Slerp(bulletTransform.rotation, chamberTransform.rotation, Time.deltaTime * bulletSnapRotationSpeed);

        float distanceToAnchor = Vector3.Distance(bulletTransform.position, chamberTransform.position);
        float dotToAnchorForward = Vector3.Dot(bulletTransform.eulerAngles.normalized, chamberTransform.eulerAngles.normalized);

        if (distanceToAnchor < 0.001f) // && dotToAnchorForward > 0.99f)
        {
            Debug.Log($"Bullet {bulletMovingToChamber} is arrived to its chamber.");

            bulletTransform.position = chamberTransform.position;
            bulletTransform.rotation = chamberTransform.rotation;
            bulletTransform.parent = chamberTransform;
            gunBulletChambers[firstFoundFreeBulletChamber].AddBullet(bulletMovingToChamber);
            bulletMovingToChamber = null;
            isMovingBullet = false;
        }

        //Debug.Log("Gun is moving and rotating to anchor. distanceToAnchor = " + distanceToAnchor + " and dotToAnchorForward = " + dotToAnchorForward);        
    }

    public void DisableSocket()
    {

    }
}
