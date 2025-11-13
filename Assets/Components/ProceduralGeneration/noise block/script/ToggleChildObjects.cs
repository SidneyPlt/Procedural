using UnityEngine;

public class GrassBlock : MonoBehaviour
{
    [SerializeField] private GameObject design1;
    [SerializeField] private GameObject design2;

    private bool usingFirstDesign = true;

    public bool GetGrassShape() => usingFirstDesign;

    public void ToggleDesign()
    {
        usingFirstDesign = !usingFirstDesign;
        design1.SetActive(usingFirstDesign);
        design2.SetActive(!usingFirstDesign);
    }
}
