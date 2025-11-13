using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class ReverseWorld : MonoBehaviour
{
    private GrassBlock[] allBlocks;

    [Header("Timer Settings")]
    public float minTime = 90f; // 1.30 minutes
    public float maxTime = 135f; // 2.15 minutes
    private float currentTime;
    private float targetTime;
    public GameObject transitionPanel;

    public Light _light;
    public Light _DarkLight;

    public Material DaySkybox;
    public Material NightSkybox;

    private UnityEngine.Color Nightcolor = new UnityEngine.Color(30f, 50f, 140f);
    private UnityEngine.Color Daycolor = new UnityEngine.Color(255f, 255f, 255f);

    private float DayTemp = 5000;
    private float NightTemp = 20000;

    public bool reverse = false;

    // AJOUTS SONORES
    [Header("Sound Settings")]
    public AudioSource transitionSound;
    public AudioSource finalTransitionSound;
    public AudioSource dayMusic;       
    public AudioSource nightMusic;      

    void Start()
    {
        RenderSettings.skybox = DaySkybox;
        DynamicGI.UpdateEnvironment();

        if (dayMusic != null) dayMusic.Play();

        ResetTimer();
    }

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime >= targetTime)
        {
            reverse = !reverse;
            allBlocks = Object.FindObjectsByType<GrassBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            StartCoroutine(TransitionCoroutine());

            ResetTimer();
        }
    }

    void ResetTimer()
    {
        currentTime = 0f;
        targetTime = Random.Range(minTime, maxTime);
        Debug.Log("Nouveau timer : " + targetTime + " secondes");
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

    public IEnumerator TransitionCoroutine()
    {
        if (reverse == false)
        {
            if (nightMusic != null && nightMusic.isPlaying)
                yield return StartCoroutine(StopMusic(2.0f, nightMusic));
        }
        else
        {
            if (dayMusic != null && dayMusic.isPlaying)
                yield return StartCoroutine(StopMusic(2.0f,dayMusic));
        }

        if (transitionSound != null)
            transitionSound.Play();
        StartCoroutine(StopAfterDelay(1f, transitionSound));

        transitionPanel.SetActive(true);
        yield return new WaitForSeconds(0.4f);
        transitionPanel.SetActive(false);
        yield return new WaitForSeconds(0.2f);

        transitionPanel.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        transitionPanel.SetActive(false);
        yield return new WaitForSeconds(0.05f);

        transitionPanel.SetActive(true);
        yield return new WaitForSeconds(1f);
        transitionPanel.SetActive(false);

        if (finalTransitionSound != null)
            finalTransitionSound.Play();

        foreach (var block in allBlocks)
        {
            block.ToggleDesign();
        }

        ChangeLight(reverse);

        if (reverse == false)
        {
            ChangeSkybox(DaySkybox);
            StartMusic(dayMusic);
        }
        else if (reverse == true)
        {
            ChangeSkybox(NightSkybox);
            StartMusic(nightMusic);
        }
    }

    void StartMusic(AudioSource musique)
    {
        if (musique != null && !musique.isPlaying) musique.Play();  
    }

    IEnumerator StopMusic(float duration, AudioSource audio)
    {
        float startVolume = audio.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audio.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        audio.Stop();
        audio.volume = startVolume; 
    }


    IEnumerator StopAfterDelay(float delay, AudioSource audio)
    {
        yield return new WaitForSeconds(delay);
        audio.Stop();
    }

}