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
        // Each scene owns its AudioManager. Scene buttons can therefore safely
        // keep direct Inspector references to the manager from the same scene.
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
