using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunAnchor : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float rotationSpeed = 1f;

    [SerializeField] private InputActionReference[] playerDisplacementsInputActions;

    private bool isGunGrabbed;
    private bool isGunAnchored;

    private void Gun_OnGunGrabbed(object obj, EventArgs args)
    {
        isGunGrabbed = true;
        isGunAnchored = false;
    }

    private void Gun_OnGunDropped(object obj, EventArgs args)
    {
        isGunGrabbed = false;
        isGunAnchored = false;
    }


    private void OnPlayerStartMoving(InputAction.CallbackContext obj)
    {
        if (!isGunGrabbed)
        {
            isGunAnchored = true;
        }
    }


    private void Start()
    {
        gun.OnGunGrabbed += Gun_OnGunGrabbed;
        gun.OnGunDropped += Gun_OnGunDropped;

        foreach (InputActionReference item in playerDisplacementsInputActions)
        {
            item.action.started += OnPlayerStartMoving;
        }
    }


    private void Update()
    {
        if (isGunGrabbed)
        {
            return;
        }


        if (isGunAnchored)
        {
            gun.transform.position = transform.position;
            gun.transform.rotation = transform.rotation;
            return;
        }


        gun.transform.position = Vector3.Lerp(gun.transform.position, transform.position, Time.deltaTime * moveSpeed);
        gun.transform.rotation = Quaternion.Slerp(gun.transform.rotation, transform.rotation, Time.deltaTime * rotationSpeed);

        float distanceToAnchor = Vector3.Distance(gun.transform.position, transform.position);
        float dotToAnchorForward = Vector3.Dot(gun.transform.eulerAngles.normalized, transform.rotation.eulerAngles.normalized);

        if (distanceToAnchor < 0.001f && dotToAnchorForward > 0.99f)
        {
            isGunAnchored = true;
        }

        //Debug.Log("Gun is moving and rotating to anchor. distanceToAnchor = " + distanceToAnchor + " and dotToAnchorForward = " + dotToAnchorForward);        
    }
}
