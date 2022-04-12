using UnityEngine;

public class ResourceLoadTest : MonoBehaviour
{
    public AudioSource audioSource;

    private AudioClip clip;
    
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AssetManager.Instance.LoadAssetAsync("Assets/GameData/Sounds/senlin.mp3",0,(o =>
            {
                clip = o as AudioClip;
                audioSource.clip = clip;
                audioSource.Play();
            }));
        }

        if (Input.GetMouseButtonDown(1))
        {
            AssetManager.Instance.UnLoadAsset(clip);
        }
    }
}
