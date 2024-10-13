using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RadialSelection : MonoBehaviour
{
    [SerializeField][Range(2, 10)] private int radialPartsNumber = 4;
    [SerializeField] private float angleBetweenParts = 10f;
    [SerializeField] private GameObject radialPartPrefab;
    [SerializeField] private Transform radialPartsCanvas;
    [SerializeField] private Transform handTransform;
    [SerializeField] private InputActionReference radialMenuInputAction;
    public UnityEvent<int> OnPartSelected;


    private List<GameObject> spawndParts = new List<GameObject>();

    private int currentSelectedRadialPart = -1;

    private bool hasInput;



    private void Start()
    {
        SpawnRadialParts();

        radialMenuInputAction.action.started += radialMenuInputAction_started;
        radialMenuInputAction.action.canceled += radialMenuInputAction_canceled;
        radialMenuInputAction.action.Enable();
    }

    private void OnDisable()
    {
        radialMenuInputAction.action.Disable();
        radialMenuInputAction.action.started -= radialMenuInputAction_started;
        radialMenuInputAction.action.canceled -= radialMenuInputAction_canceled;
    }


    private void Update()
    {
        if (hasInput)
        {
            GetSelectedRadialPart();
        }
    }

    public void HideAndTriggerSelected()
    {
        radialPartsCanvas.gameObject.SetActive(false);
        OnPartSelected?.Invoke(currentSelectedRadialPart);
    }


    private void radialMenuInputAction_started(InputAction.CallbackContext obj)
    {
        SpawnRadialParts();
        hasInput = true;
    }


    private void radialMenuInputAction_canceled(InputAction.CallbackContext obj)
    {
        HideAndTriggerSelected();
        hasInput = false;
    }


    public void GetSelectedRadialPart()
    {
        Vector3 centerToHand = handTransform.position - radialPartsCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartsCanvas.forward);

        float angle = Vector3.SignedAngle(radialPartsCanvas.up, centerToHandProjected, -radialPartsCanvas.forward);

        if (angle < 0)
        {
            angle += 360;
        }

        currentSelectedRadialPart = (int)angle * radialPartsNumber / 360;

        for (int i = 0; i < spawndParts.Count; i++)
        {
            if (i == currentSelectedRadialPart)
            {
                spawndParts[i].GetComponent<Image>().color = Color.yellow;
                spawndParts[i].transform.localScale = 1.1f * Vector3.one;
            }
            else
            {
                spawndParts[i].GetComponent<Image>().color = Color.white;
                spawndParts[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void SpawnRadialParts()
    {
        radialPartsCanvas.gameObject.SetActive(true);
        radialPartsCanvas.position = handTransform.position;
        radialPartsCanvas.rotation = handTransform.rotation;

        foreach (var item in spawndParts)
        {
            Destroy(item);
        }
        spawndParts.Clear();


        for (int i = 0; i < radialPartsNumber; i++)
        {
            float angle = -i * 360 / radialPartsNumber - angleBetweenParts * 0.5f;
            Vector3 radialPartEulerAngle = new Vector3(0, 0, angle);
            GameObject radialPart = Instantiate(radialPartPrefab, radialPartsCanvas);
            radialPart.transform.position = radialPartsCanvas.position;
            radialPart.transform.localEulerAngles = radialPartEulerAngle;

            Image radialPartImage = radialPart.GetComponent<Image>();
            radialPartImage.fillAmount = (1 / (float)radialPartsNumber) - (angleBetweenParts / 360);

            spawndParts.Add(radialPart);
        }
    }
}
