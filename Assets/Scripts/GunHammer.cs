using System;
using UnityEngine;

public class GunHammer : MonoBehaviour
{
    public ArticulationBody rootArticulationBody;
    public ArticulationBody childArticulationBody;

    public Transform anchor;
    public Transform hammerMinRotation;

    private Quaternion minLocalRotation;
    private Quaternion maxLocalRotation;

    private bool isLockedMin;
    private bool isLockedMax;

    private float lowerLimit;
    private float upperLimit;

    public event EventHandler OnMinRotationReached;

    public Gun gun;


    private void Start()
    {
        maxLocalRotation = childArticulationBody.transform.localRotation;
        minLocalRotation = hammerMinRotation.transform.localRotation;

        upperLimit = maxLocalRotation.eulerAngles.x + 0.1f;

        lowerLimit = minLocalRotation.eulerAngles.x - 0.1f;
        if (lowerLimit > 180) lowerLimit -= 360;
        else if (lowerLimit < -180) lowerLimit += 360;

        gun.OnShoot += Gun_OnShoot;
    }


    private void Gun_OnShoot(object obj, EventArgs args)
    {
        isLockedMin = false;
    }


    private void FixedUpdate()
    {
        rootArticulationBody.TeleportRoot(anchor.position, anchor.rotation);

        float currentRotation = childArticulationBody.jointPosition[0] * Mathf.Rad2Deg;

        if (currentRotation < lowerLimit)
        {
            if (!isLockedMin)
            {
                isLockedMin = true;
            }
        }
        else if (currentRotation > upperLimit)
        {
            if (!isLockedMax)
            {
                isLockedMax = true;
            }
        }
    }

    private void Update()
    {
        if (isLockedMin)
        {
            childArticulationBody.transform.localRotation = minLocalRotation;
            childArticulationBody.velocity = Vector3.zero;
            childArticulationBody.angularVelocity = Vector3.zero;
            childArticulationBody.Sleep();
            rootArticulationBody.Sleep();

            OnMinRotationReached?.Invoke(this, EventArgs.Empty);

            isLockedMin = false;
        }
        else if (isLockedMax)
        {
            childArticulationBody.transform.localRotation = maxLocalRotation;
            childArticulationBody.velocity = Vector3.zero;
            childArticulationBody.angularVelocity = Vector3.zero;

            isLockedMax = false;
        }
    }
}
