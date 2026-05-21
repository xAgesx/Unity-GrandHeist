using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour {
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource constant;
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource footstepSource;
    public AudioSource uiSource;
    public AudioSource ambientSource;

    [Header("Constant Test Sound")]
    public AudioClip constantClip;

    [Header("Music")]
    public AudioClip musicMenu;
    public AudioClip[] musicGameplayPlaylist;
    public AudioClip musicGameOver;

    [Header("SFX")]
    public AudioClip sfxFootstepCrouch;
    public AudioClip sfxFootstepWalk;
    public AudioClip sfxFootstepRun;
    public AudioClip sfxDoorOpen;
    public AudioClip sfxDoorLocked;
    public AudioClip sfxKeycardPickup;
    public AudioClip sfxLaserAlert;
    public AudioClip sfxLaserCatch;
    public AudioClip sfxGuardAlert;
    public AudioClip sfxGunshot;
    public AudioClip sfxAlarm;

    [Header("UI")]
    public AudioClip uiClick;
    public AudioClip uiHover;
    public AudioClip uiBack;
    public AudioClip uiConfirm;

    [Header("Ambient")]
    public AudioClip ambientHum;
    public AudioClip ambientVent;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float constantVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float uiVolume = 1f;
    [Range(0f, 1f)] public float ambientVolume = 1f;

    AudioClip previousConstantClip;
    List<int> playlistQueue = new List<int>();
    int playlistIndex;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Update() {
        if (constantClip != null && constantClip != previousConstantClip) {
            previousConstantClip = constantClip;
            constant.clip = constantClip;
            constant.volume = constantVolume * masterVolume;
            if (!constant.isPlaying) constant.Play();
        }

        if (playlistQueue.Count > 0 && !musicSource.isPlaying) {
            playlistIndex++;
            PlayNextInPlaylist();
        }
    }

    void Start() {
        if (constant == null) constant = gameObject.AddComponent<AudioSource>();
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        if (footstepSource == null) footstepSource = gameObject.AddComponent<AudioSource>();
        if (uiSource == null) uiSource = gameObject.AddComponent<AudioSource>();
        if (ambientSource == null) ambientSource = gameObject.AddComponent<AudioSource>();

        constant.loop = true;
        musicSource.loop = true;
        ambientSource.loop = true;

        if (constantClip != null) {
            constant.clip = constantClip;
            constant.volume = constantVolume * masterVolume;
            constant.Play();
        }

        PlayMusicPlaylist();
    }

    public void PlayMusic(AudioClip clip, bool loop = true) {
        playlistQueue.Clear();
        if (clip == null) return;
        musicSource.loop = loop;
        musicSource.clip = clip;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    public void PlayMusicPlaylist() {
        if (musicGameplayPlaylist == null || musicGameplayPlaylist.Length == 0) return;
        playlistQueue.Clear();
        for (int i = 0; i < musicGameplayPlaylist.Length; i++)
            playlistQueue.Add(i);
        playlistQueue.Shuffle();
        playlistIndex = 0;
        PlayNextInPlaylist();
    }

    void PlayNextInPlaylist() {
        if (playlistQueue.Count == 0) return;
        if (playlistIndex >= playlistQueue.Count) {
            playlistQueue.Shuffle();
            playlistIndex = 0;
        }
        int idx = playlistQueue[playlistIndex];
        musicSource.loop = false;
        musicSource.clip = musicGameplayPlaylist[idx];
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip, float volumeOverride = -1f) {
        if (clip == null) return;
        float vol = volumeOverride >= 0f ? volumeOverride : sfxVolume;
        sfxSource.PlayOneShot(clip, vol * masterVolume);
    }

    public void PlayUI(AudioClip clip) {
        if (clip == null) return;
        uiSource.PlayOneShot(clip, uiVolume * masterVolume);
    }

    public void PlayAmbient(AudioClip clip, bool loop = true) {
        if (clip == null) return;
        ambientSource.loop = loop;
        ambientSource.clip = clip;
        ambientSource.volume = ambientVolume * masterVolume;
        ambientSource.Play();
    }

    public void PlayFootstep(PlayerController.MoveState state) {
        AudioClip clip = null;
        float pitch = 1f;
        switch (state) {
            case PlayerController.MoveState.Crouch: clip = sfxFootstepCrouch; break;
            case PlayerController.MoveState.Walk:   clip = sfxFootstepWalk;   pitch = 2.1f; break;
            case PlayerController.MoveState.Run:    clip = sfxFootstepRun;    pitch = 2.1f; break;
        }
        if (clip == null) return;
        footstepSource.pitch = pitch;
        footstepSource.PlayOneShot(clip, 0.4f * masterVolume);
        footstepSource.pitch = 1f;
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeOverride = -1f) {
        if (clip == null) return;
        float vol = volumeOverride >= 0f ? volumeOverride : sfxVolume;
        AudioSource.PlayClipAtPoint(clip, position, vol * masterVolume);
    }

    public void SwapConstantClip(AudioClip newClip) {
        if (newClip == null) return;
        constant.clip = newClip;
        if (!constant.isPlaying) constant.Play();
    }

    public void StopMusic() {
        musicSource.Stop();
    }

    public void StopAmbient() {
        ambientSource.Stop();
    }

    public void StopAll() {
        musicSource.Stop();
        sfxSource.Stop();
        uiSource.Stop();
        ambientSource.Stop();
    }

    public void SetMasterVolume(float volume) {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume) {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume * masterVolume;
    }

    public void SetSFXVolume(float volume) {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetUIVolume(float volume) {
        uiVolume = Mathf.Clamp01(volume);
    }

    public void SetAmbientVolume(float volume) {
        ambientVolume = Mathf.Clamp01(volume);
        ambientSource.volume = ambientVolume * masterVolume;
    }

    void ApplyVolumes() {
        constant.volume = constantVolume * masterVolume;
        musicSource.volume = musicVolume * masterVolume;
        ambientSource.volume = ambientVolume * masterVolume;
    }
}

public static class ListExtensions {
    public static void Shuffle<T>(this IList<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
