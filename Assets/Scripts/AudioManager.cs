using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Takes in charge the global volume of the game
/// </summary>
[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour {
    static AudioManager manager; //singleton

    AudioSource source; //the music to play in the background (currently one per level, or almost)
    public Slider audioSlider; //slider for audio volume in the options menu
    public AudioClip[] clips; //clips to be triggered

    public float delay = 0f; //delay before starting music

    void Start() {
        manager = this;
        source = gameObject.GetComponent<AudioSource>();

        if (source == null) {
            Debug.LogError("No Audio Component found");
        }
        else if (source.clip == null) {
            Debug.LogError("No Audio clip found");
        }

        if(audioSlider != null) {
            audioSlider.gameObject.SetActive(true);

            //save audio level for the whole playthrough
            source.volume = PlayerPrefs.GetFloat("SliderVolumeLevel", source.volume);
            audioSlider.value = source.volume;
        }
        StartCoroutine(LaunchMusicAfterDelay());
    }

    public void UpdateVolume() {
        source.volume = audioSlider.value;
        PlayerPrefs.SetFloat("SliderVolumeLevel", source.volume);
    }


    /// <summary>
    /// Coroutine to Wait a certain amount of time before starting the music in a level
    /// </summary>
    IEnumerator LaunchMusicAfterDelay() {
        float tmpVol = source.volume;
        source.volume = 0;
        yield return new WaitForSeconds(delay);

        StartCoroutine(VolumeTransition(tmpVol));
    }

    public static void ReduceVolumeByHalf() {
        manager.source.volume /= 2;
    }

    /// <summary>
    /// Coroutine to transition music volume from 0 to volumeToReach or the invert
    /// </summary>
    /// <param name="volumeToReach"></param>
    public static IEnumerator VolumeTransition(float volumeToReach) {
        float step = .05f;

        if (volumeToReach > manager.source.volume) {
            while (manager.source.volume < volumeToReach && manager.source.volume < 1f) {
                manager.source.volume += .005f;
                yield return new WaitForSeconds(step);
            }
        }
        else {
            while (manager.source.volume > volumeToReach && manager.source.volume > 0f) {
                manager.source.volume -= .005f;
                yield return new WaitForSeconds(step);
            }

        }

    }

    /// <summary>
    /// Coroutine to make a smooth change between audio clips
    /// </summary>
    /// <param name="indexClip"></param>
    public static IEnumerator TriggerClipChange(int indexClip) {
        if(manager.clips.Length > 0) {
            manager.StartCoroutine(VolumeTransition(0f));
            yield return new WaitUntil(() => manager.source.volume <= 0f); //time to put previous clip volume to 0
            yield return new WaitForSeconds(.5f);

            AudioClip newClip = manager.clips[indexClip];
            manager.source.clip = newClip;
            manager.source.Play();
            manager.StartCoroutine(VolumeTransition(0.2f));
        }
    }

}
