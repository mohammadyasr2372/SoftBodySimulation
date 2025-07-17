using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PersistentMusic : MonoBehaviour
{
    private static PersistentMusic instance;
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            AudioSource source = GetComponent<AudioSource>();
            if (!source.isPlaying)
                source.Play();
        }
        else
        {
            Destroy(gameObject);
        }
    }

}
