using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField] private float rotationSpeed = 3;

    //[SerializeField] private Vector3 cameraOffset;

    private Vector3 wantedForward;



    void Update()
    {
        FollowCameraRotation();
        MoveWithCamera();
    }


    private void FollowCameraRotation()
    {
        wantedForward = cam.transform.forward;
        wantedForward.y = 0;
        wantedForward = wantedForward.normalized;

        transform.forward = Vector3.Slerp(transform.forward, wantedForward, Time.deltaTime * rotationSpeed);
    }

    private void MoveWithCamera()
    {
        transform.position = cam.position;
    }
}
