using System.Collections.Generic;
using UnityEngine;

public class SetColorFromList : MonoBehaviour
{
    public List<Color> colors;

    public void SetColor(int i)
    {
        transform.GetComponent<Renderer>().material.color = colors[i];
    }
}
