using UnityEngine;
using UnityEngine.UI;

public class ClearButton : MonoBehaviour
{
    public CircuitGridController controller;

    public void OnClick()
    {
        if (controller != null)
        {
            controller.ClearAllCircuit();
        }
    }
}