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

    [SerializeField] private Collider hammerCollider;

    [SerializeField] private GunBarrel gunBarrel;

    public event EventHandler OnShoot;

    public event EventHandler<CurrentController> OnGunGrabbed;
    public event EventHandler<CurrentController> OnGunDropped;



    public enum CurrentController { None, Left, Right }
    private CurrentController currentController;

    private bool shotDone;
    private bool shotDoneButNotFinished;

    public bool IsBarrelOut { get => isBarrelOut; }
    private bool isBarrelOut;


    private bool isHammerArmed;

    private int bulletChamberFacingCanon = 0;
    public int BulletChamberFacingCanon => bulletChamberFacingCanon;


    private void Start()
    {
        xRGrabInteractable.selectEntered.AddListener(XRGrabInteractable_SelectEntered);
        xRGrabInteractable.selectExited.AddListener(XRGrabInteractable_SelectExited);

        gunHammerJoint.OnMinRotationReached += GunHammerJoint_OnMinRotationReached;
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

                gunBarrel.DisableBarrelBulletDetectionCollider(); // bullets can NOT be added to gun barrel chambers when the barrel is locked
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

        //Debug.Log("Shot is going to be fired. Bullet hole facing canon: " + bulletChamberFacingCanon);

        bool isValidBullet = gunBarrel.GunBulletChambers[bulletChamberFacingCanon].GunBullet != null && !gunBarrel.GunBulletChambers[bulletChamberFacingCanon].GunBullet.HasBeenShot;


        if (isValidBullet)
        {
            gunBarrel.GunBulletChambers[bulletChamberFacingCanon].GunBullet.Shoot();
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


#region Inputs
    private void PrimaryAction_Started(InputAction.CallbackContext obj) // ejecting barrel for reloading
    {
        if (isBarrelOut) return;
        if (shotDoneButNotFinished) return;

        StartCoroutine(BarrelOutCoroutine());
        StartCoroutine(gunBarrel.BulletsExpulsionCoroutine());
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

        OnGunGrabbed?.Invoke(this, currentController);
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

        OnGunDropped?.Invoke(this, currentController);
        
        currentController = CurrentController.None;

        hammerCollider.enabled = false;
    }
    #endregion
}
