using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicPlaylist : MonoBehaviour
{
    [SerializeField] private AudioClip[] tracks;
    private AudioSource source;
    private float normalVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        normalVolume = source.volume;
    }

    public void SetDucked(bool ducked) => source.volume = normalVolume * (ducked ? 0.2f : 1f);

    private IEnumerator Start()
    {
        if (tracks == null || tracks.Length == 0) yield break;

        source.loop = false;

        for (int index = 0; ; index = (index + 1) % tracks.Length)
        {
            source.clip = tracks[index];
            source.Play();
            yield return new WaitWhile(() => source.isPlaying);
        }
    }
}
