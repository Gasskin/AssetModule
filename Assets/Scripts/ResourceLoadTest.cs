using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    private AudioClip go;


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            go = AssetManager.Instance.LoadAsset<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
            var audio = GetComponent<AudioSource>();
            audio.clip = go;
            audio.Play();
        }
        
        if (Input.GetMouseButtonDown(1))
        {
            AssetManager.Instance.UnLoadAsset(go);
        }
    }
}
