using System.Drawing;
using UnityEngine;

public class ReverseWorld : MonoBehaviour
{
    private GrassBlock[] allBlocks;

    public Light _light;
    public Light _DarkLight;

    public Material DaySkybox;
    public Material NightSkybox;
  
    private UnityEngine.Color Nightcolor = new UnityEngine.Color(30f, 50f, 140f); 
    private UnityEngine.Color Daycolor = new UnityEngine.Color(255f, 255f, 255f);

    private float DayTemp = 5000;
    private float NightTemp = 20000;

    public bool reverse = false;


    void Start()
    {
        RenderSettings.skybox = DaySkybox;
        DynamicGI.UpdateEnvironment();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            reverse = !reverse;
            allBlocks = Object.FindObjectsByType<GrassBlock>(FindObjectsInactive.Include ,FindObjectsSortMode.None);

            foreach (var block in allBlocks)
            {
                block.ToggleDesign();
            }

            if(reverse == false)
            {
                ChangeSkybox(DaySkybox);
                ChangeLight(reverse);
            }
            else if (reverse == true)
            {
                ChangeSkybox(NightSkybox);
                ChangeLight(reverse);
            }
        }
    }

    public void ChangeSkybox(Material newSky)
    {
        RenderSettings.skybox = newSky;
        DynamicGI.UpdateEnvironment();
    }

    public void ChangeLight(bool reverse)
    {
        _light.enabled = !reverse;
        _DarkLight.enabled = reverse;
    }

}
