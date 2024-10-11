using System;
using UnityEngine;

public class GunHammerJoint : MonoBehaviour
{
    private bool isLockedMin;

    private float maxAngle = 0f;
    private float minAngle = -60f;

    public event EventHandler OnMinRotationReached;

    public Gun gun;

    private Vector3 minLocalRotation;  // the local rotation when the hammer is fully turned, and locked in ready-to-fire position


    private void Start()
    {
        gun.OnShoot += Gun_OnShoot;
        minLocalRotation = new Vector3(transform.localEulerAngles.x + minAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }


    private void Gun_OnShoot(object obj, EventArgs args)
    {
        isLockedMin = false;
    }


    private void Update()
    {
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (isLockedMin)
        {
            transform.localEulerAngles = minLocalRotation;
            return;
        }

        Vector3 localEulerAnglesConstrained = transform.localEulerAngles;
        localEulerAnglesConstrained.y = 0f;
        localEulerAnglesConstrained.z = 0f;

        if (localEulerAnglesConstrained.x > 180) localEulerAnglesConstrained.x -= 360;
        else if (localEulerAnglesConstrained.x < -180) localEulerAnglesConstrained.x += 360;

        if (localEulerAnglesConstrained.x > maxAngle)
        {
            localEulerAnglesConstrained.x = maxAngle;
        }
        else if (localEulerAnglesConstrained.x < minAngle)
        {
            isLockedMin = true;
            OnMinRotationReached?.Invoke(this, EventArgs.Empty);
            localEulerAnglesConstrained.x = minAngle;
        }

        transform.localEulerAngles = localEulerAnglesConstrained;
    }
}
