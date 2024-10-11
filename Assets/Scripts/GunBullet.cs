using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GunBullet : MonoBehaviour
{
    private bool hasBeenShot = false;
    public bool HasBeenShot => hasBeenShot;

    [SerializeField] private GameObject bulletProjectile;

    private Rigidbody rb;

    private XRGrabInteractable xrGrabInteractable;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
    }

    // public Rigidbody Rigidbody
    // {
    //     get => rb;
    // }

    // public XRGrabInteractable XRGrabInteractable
    // {
    //     get => xrGrabInteractable;
    // }

    public void ExpulsionFromGunBarrel(Vector3 gunCurrentForward)
    {
        xrGrabInteractable.enabled = false;

        rb.isKinematic = false;

        float expulsionForce = 1f;
        rb.AddForce(-gunCurrentForward * expulsionForce, ForceMode.Impulse);
    }

    public void Shoot()
    {
        hasBeenShot = true;
        bulletProjectile.SetActive(false);
    }
}
