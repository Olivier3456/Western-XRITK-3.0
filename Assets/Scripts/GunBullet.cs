using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GunBullet : MonoBehaviour
{
    private bool hasBeenShot = false;
    public bool HasBeenShot => hasBeenShot;

    [SerializeField] private GameObject bulletProjectile;
    [SerializeField] private Collider bulletCollider;

    private Rigidbody rb;

    private XRGrabInteractable xrGrabInteractable;

    //public bool isInGun = false;

    public GunBulletChamber GunBulletChamber => gunBulletChamber;
    private GunBulletChamber gunBulletChamber = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
    }


    public void ExpulsionFromGunBarrel(Vector3 gunCurrentForward)
    {
        rb.isKinematic = false;
        gunBulletChamber = null;

        float expulsionForce = 1f;
        rb.AddForce(-gunCurrentForward * expulsionForce, ForceMode.Impulse);
    }

    public void Shoot()
    {
        hasBeenShot = true;
        bulletProjectile.SetActive(false);
    }

    public void EnableCollider()
    {
        bulletCollider.enabled = true;
    }

    public void DisableCollider()
    {
        bulletCollider.enabled = false;  // for now, idk how to disable "grabbable" property in XRGrabInteractable without making the bullet falling out of its socket interactor
    }

    public void SetRigidbodyKinematic(bool isKinematic)
    {
        rb.isKinematic = isKinematic;
    }

    public void SetGunBulletChamber(GunBulletChamber gunBulletChamber)
    {
        this.gunBulletChamber = gunBulletChamber;
    }
}
