using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Gun : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable xRGrabInteractable;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform leftInteractorTransform;
    [SerializeField] private Transform rightInteractorTransform;

    [SerializeField] private InputActionReference leftActivateAction;   // shoot
    [SerializeField] private InputActionReference rightActivateAction;  // shoot

    [SerializeField] private InputActionReference leftPrimaryAction;    // reloading
    [SerializeField] private InputActionReference rightPrimaryAction;   // reloading

    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody barrelRigidbody;
    [SerializeField] private AudioClip shotAudioClip;
    [SerializeField] private AudioClip shotEmptyAudioClip;
    [SerializeField] private AudioClip barrelOutAudioClip;

    [SerializeField] private GunHammerJoint gunHammerJoint;

    [SerializeField] private Transform barrelRotationTransform;
    [SerializeField] private AnimationCurve barrelRotationSpeedCurve;

    [SerializeField] private GunBulletChamber[] gunBulletChambers;
    [SerializeField] private Collider hammerCollider;

    public event EventHandler OnShoot;

    public event EventHandler OnGunGrabbed;
    public event EventHandler OnGunDropped;



    private enum CurrentController { None, Left, Right }
    private CurrentController currentController;

    private bool shotDone;
    private bool shotDoneButNotFinished;
    private bool isBarrelOut;


    private bool isHammerArmed;

    private int bulletChamberFacingCanon = 0;


    private void Start()
    {
        xRGrabInteractable.selectEntered.AddListener(XRGrabInteractable_SelectEntered);
        xRGrabInteractable.selectExited.AddListener(XRGrabInteractable_SelectExited);

        gunHammerJoint.OnMinRotationReached += GunHammerJoint_OnMinRotationReached;

        DisableBulletsGrabbable(); // Should we wait one frame to disable grab interactable of bullets in the sockets interactors at start ? TO DO: Test
        bool onlyEmptyOnes = true;
        DisableGunBulletChambersSocketInteractors(onlyEmptyOnes);
    }


    private void GunHammerJoint_OnMinRotationReached(object obj, EventArgs args)
    {
        isHammerArmed = true;
    }


    private IEnumerator BarrelOutCoroutine()
    {
        float zAngleMinToLockBarrel = 0.05f;
        float zAngleToAutorizeBarrelLock = 35f;
        bool canBarrelBeLocked = false;

        isBarrelOut = true;

        barrelRigidbody.isKinematic = false;

        animator.enabled = false;

        audioSource.clip = barrelOutAudioClip;
        audioSource.Play();

        EnableBulletsGrabbable(); // bullets CAN be grabbed by player when the barrel is unlocked
        EnableGunBulletChambersSocketInteractors(); // bullet can be put in gun barrel chambers when the barrel is locked

        while (isBarrelOut)
        {
            yield return null;

            if (barrelRigidbody.transform.localEulerAngles.z > zAngleToAutorizeBarrelLock)
            {
                canBarrelBeLocked = true;
            }

            if (!canBarrelBeLocked) continue;

            if (barrelRigidbody.transform.localEulerAngles.z < zAngleMinToLockBarrel)
            {
                audioSource.clip = barrelOutAudioClip;
                audioSource.Play();

                barrelRigidbody.isKinematic = true;
                animator.enabled = true;

                isBarrelOut = false;

                DisableBulletsGrabbable(); // bullets CAN'T be grabbed by player when the barrel is locked
                bool onlyEmptyOnes = true;
                DisableGunBulletChambersSocketInteractors(onlyEmptyOnes); // bullets can't be put in gun barrel chambers when the barrel is locked, but socket interactors with bullets in it stay active
            }
        }
    }

    private void EnableBulletsGrabbable()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            if (gunBulletChamber.GunBullet != null)
            {
                gunBulletChamber.GunBullet.EnableCollider();
            }
        }
    }

    private void DisableBulletsGrabbable()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            if (gunBulletChamber.GunBullet != null)
            {
                gunBulletChamber.GunBullet.DisableCollider();
            }
        }
    }

    private void EnableGunBulletChambersSocketInteractors()
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            gunBulletChamber.EnableSocketInteractor();
        }
    }

    private void DisableGunBulletChambersSocketInteractors(bool onlyEmptyOnes)
    {
        foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
        {
            gunBulletChamber.DisableSocketInteractor(onlyEmptyOnes);
        }
    }


    private IEnumerator BulletsExpulsionCoroutine()
    {
        yield return new WaitForSeconds(1f);

        bool areBulletsOut = false;

        while (!areBulletsOut && isBarrelOut)
        {
            yield return null;

            Vector3 gunCurrentForward = transform.forward;
            float verticalDot = Vector3.Dot(gunCurrentForward, Vector3.up);

            float dotThreshold = 0.8f;
            if (verticalDot > dotThreshold) // the revolver is facing up
            {
                areBulletsOut = true;

                foreach (GunBulletChamber gunBulletChamber in gunBulletChambers)
                {
                    gunBulletChamber.GunBullet?.ExpulsionFromGunBarrel(gunCurrentForward);
                    //gunBulletChamber.GunBullet?.EnableCollider();
                }

                DisableGunBulletChambersSocketInteractors(false);   // disable all bullet chambers socket interactors, to allow bullets to fall
                Invoke(nameof(EnableGunBulletChambersSocketInteractors), 1f); // ...and re-enable it after a short time
            }
        }
    }

    private IEnumerator RotateBarrelAfterShootCoroutine()
    {
        Quaternion startRotation = barrelRotationTransform.localRotation;

        float rotationStep = 60f;
        float targetRotation = startRotation.eulerAngles.z + rotationStep;

        Quaternion endRotation = Quaternion.Euler(0, 0, targetRotation);

        float progress = 0f;
        float duration = 0.3f;

        while (progress < 1)
        {
            float slerpCurveValue = barrelRotationSpeedCurve.Evaluate(progress);
            barrelRotationTransform.localRotation = Quaternion.Slerp(startRotation, endRotation, slerpCurveValue);
            progress += Time.deltaTime / duration;
            yield return null;
        }

        barrelRotationTransform.localRotation = endRotation;
    }


    private void ActivateAction_Performed(InputAction.CallbackContext obj) // pressure on gun trigger with controller trigger
    {
        if (!isHammerArmed) return;

        float triggerValue = obj.ReadValue<float>();
        animator.SetFloat("Trigger_Pressure", triggerValue);
        TryShoot(triggerValue);
    }


    private void ActivateAction_Canceled(InputAction.CallbackContext obj) // reset gun trigger to idle position when controller trigger is not pressed anymore
    {
        shotDone = false;
        animator.SetFloat("Trigger_Pressure", 0f);
    }


    private void TryShoot(float triggerValue)
    {
        if (isBarrelOut) return;
        if (shotDone) return;
        if (shotDoneButNotFinished) return;    // there is a small cooldown after a shot (see ShotDelayCoroutine())


        float shotThreshold = 0.9f;
        if (triggerValue < shotThreshold) return;

        if (currentController == CurrentController.Left)
        {
            HapticsUtility.SendHapticImpulse(1, 0.1f, HapticsUtility.Controller.Left);
        }
        else if (currentController == CurrentController.Right)
        {
            HapticsUtility.SendHapticImpulse(1, 0.1f, HapticsUtility.Controller.Right);
        }

        Debug.Log("Shot is going to be fired. Bullet hole facing canon: " + bulletChamberFacingCanon);

        bool isValidBullet = gunBulletChambers[bulletChamberFacingCanon].GunBullet != null && !gunBulletChambers[bulletChamberFacingCanon].GunBullet.HasBeenShot;


        if (isValidBullet)
        {
            gunBulletChambers[bulletChamberFacingCanon].GunBullet.Shoot();
            float shotAnimSpeed = 2.5f;
            animator.SetFloat("ShotAnimSpeed", shotAnimSpeed);
            animator.SetTrigger("Shoot");
            float shotCooldown = 1 / shotAnimSpeed;
            StartCoroutine(ShotDelayCoroutine(shotCooldown));
        }


        if (bulletChamberFacingCanon < 5)
        {
            bulletChamberFacingCanon++;
        }
        else
        {
            bulletChamberFacingCanon = 0;
        }


        audioSource.clip = isValidBullet ? shotAudioClip : shotEmptyAudioClip;
        audioSource.Play();

        StartCoroutine(RotateBarrelAfterShootCoroutine());

        isHammerArmed = false;
        shotDone = true;
        OnShoot?.Invoke(this, EventArgs.Empty);
    }

    private IEnumerator ShotDelayCoroutine(float shotCooldown)
    {
        shotDoneButNotFinished = true;
        float time = 0f;
        while (time < shotCooldown)
        {
            yield return null;
            time += Time.deltaTime;
        }
        shotDoneButNotFinished = false;
    }


    private void PrimaryAction_Started(InputAction.CallbackContext obj) // ejecting barrel for reloading
    {
        if (isBarrelOut) return;
        if (shotDoneButNotFinished) return;

        StartCoroutine(BarrelOutCoroutine());
        StartCoroutine(BulletsExpulsionCoroutine());
    }

    private void XRGrabInteractable_SelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject.transform == leftInteractorTransform)
        {
            currentController = CurrentController.Left;

            leftActivateAction.action.performed += ActivateAction_Performed;
            leftActivateAction.action.canceled += ActivateAction_Canceled;

            leftPrimaryAction.action.started += PrimaryAction_Started;  // barrel ejection (reloading)
        }
        else
        {
            currentController = CurrentController.Right;

            rightActivateAction.action.performed += ActivateAction_Performed;
            rightActivateAction.action.canceled += ActivateAction_Canceled;

            rightPrimaryAction.action.started += PrimaryAction_Started;  // barrel ejection (reloading)
        }

        hammerCollider.enabled = true;

        OnGunGrabbed?.Invoke(this, EventArgs.Empty);
    }

    private void XRGrabInteractable_SelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject.transform == leftInteractorTransform)
        {
            leftActivateAction.action.performed -= ActivateAction_Performed;
            leftActivateAction.action.canceled -= ActivateAction_Canceled;

            leftPrimaryAction.action.started -= PrimaryAction_Started;
        }
        else
        {
            rightActivateAction.action.performed -= ActivateAction_Performed;
            rightActivateAction.action.canceled -= ActivateAction_Canceled;

            rightPrimaryAction.action.started -= PrimaryAction_Started;
        }

        currentController = CurrentController.None;

        hammerCollider.enabled = false;

        OnGunDropped?.Invoke(this, EventArgs.Empty);
    }
}
