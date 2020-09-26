using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Settings : MonoBehaviour
{

    public Boolean useNew = false;
    public int method = 0;
    public int destroyMode = 0;
    public int itemCount = 0;
    public float ttl = 1;
    [Range(0f, 100000f)]
    public float spawnRate = 0;

    public UnityEngine.UI.Slider slider;
    public UnityEngine.UI.Text text;


    static Settings instance;





    // Start is called before the first frame update
    void Start()
    {

        Debug.Log("Calling Settings OnCreate");
        Settings.instance = this;

        
    }

    // Update is called once per frame
    void Update()
    {

        spawnRate = slider.value * slider.value;
        text.text = spawnRate.ToString();
        
    }


    public void TaskOnClick()
    {

        Debug.Log("Button Clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }


}
