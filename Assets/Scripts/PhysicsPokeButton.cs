using Unity.VisualScripting;
using UnityEngine;

public class PhysicsPokeButton : MonoBehaviour
{
    [SerializeField] private float yMoveLength = 0.15f;

    private float yBasePosition;
    private Rigidbody rb;

    private float minY;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        yBasePosition = transform.position.y;
        minY = yBasePosition - yMoveLength;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (transform.position.y < minY)
        {
            Debug.Log("Mimimum position reached. The button can't be pressed more.");
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }






}
