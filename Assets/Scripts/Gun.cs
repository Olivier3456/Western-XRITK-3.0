using System;
using System.Collections;
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

    [SerializeField] private Rigidbody[] bulletsRigidbodies;

    [SerializeField] private GunHammerJoint gunHammerJoint;

    [SerializeField] private Transform barrelRotationTransform;
    [SerializeField] private AnimationCurve barrelRotationSpeedCurve;

    public event EventHandler OnShoot;





    private enum CurrentController { None, Left, Right }
    private CurrentController currentController;

    private bool shotDone;
    private bool shotDoneButNotFinished;
    private bool isBarrelOut;

    private const int MAX_SHOTS = 6;
    private int shotsLeft = MAX_SHOTS;

    private bool isHammerArmed;


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

    private IEnumerator BulletsExpulsionCoroutine()
    {
        yield return new WaitForSeconds(1f);

        bool areBulletsOut = false;

        while (!areBulletsOut && isBarrelOut)
        {
            yield return null;

            Vector3 bulletsCurrentForward = bulletsRigidbodies[0].transform.forward;
            float verticalDot = Vector3.Dot(bulletsCurrentForward, Vector3.down);

            float dotThreshold = 0.8f;
            if (verticalDot > dotThreshold) // the revolver is facing up
            {
                areBulletsOut = true;

                foreach (Rigidbody rb in bulletsRigidbodies)
                {
                    rb.isKinematic = false;
                    rb.gameObject.transform.parent = null;

                    float expulsionForce = 1f;
                    rb.AddForce(bulletsCurrentForward * expulsionForce, ForceMode.Impulse);
                }
            }
        }
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
        if (shotDoneButNotFinished) return;    // there is a small cooldown after a shot


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

        shotsLeft = Math.Max(--shotsLeft, 0);

        audioSource.clip = shotsLeft > 0 ? shotAudioClip : shotEmptyAudioClip;
        audioSource.Play();

        StartCoroutine(RotateBarrelAfterShootCoroutine());

        if (shotsLeft > 0)
        {           
            float shotAnimSpeed = 2.5f;
            animator.SetFloat("ShotAnimSpeed", shotAnimSpeed);
            animator.SetTrigger("Shoot");
            float shotCooldown = 1 / shotAnimSpeed;
            StartCoroutine(ShotDelayCoroutine(shotCooldown));
        }

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

        isBarrelOut = true;

        barrelRigidbody.isKinematic = false;
        animator.enabled = false;

        audioSource.clip = barrelOutAudioClip;
        audioSource.Play();

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
    }
}
