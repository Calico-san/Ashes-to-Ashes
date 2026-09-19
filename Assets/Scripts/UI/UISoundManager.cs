using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    [Header("Audio Clips")]
    public AudioClip buttonClickClip;
    public AudioClip panelOpenClip;
    public AudioClip buttonSound;
    public AudioClip buttonHoverSound;

    private AudioSource audioSource;

    private void Awake()
    {
        // Simple Singleton pattern so you can call this from anywhere
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    
    public void PlayButtonClick()
    {
        PlaySound(buttonClickClip);
    }

    
    public void PlayPanelOpen()
    {
        PlaySound(panelOpenClip);
    }

    
    public void PlayButtonSound()
    {
        PlaySound(buttonSound);
    }

    public void PlayHoverSound()
    {
        PlaySound(buttonHoverSound);
    }

    // Utility method using PlayOneShot to allow overlapping sounds
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}