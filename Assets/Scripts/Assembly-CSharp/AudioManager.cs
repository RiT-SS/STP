using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public int MaxAudioSources = 5;
	private List<AudioSource> audioSources = new List<AudioSource>();
	private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
	public static AudioManager Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		MaxAudioSources = Mathf.Max(1, MaxAudioSources);
		for (int i = 0; i < MaxAudioSources; i++)
		{
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.playOnAwake = false;
			audioSources.Add(audioSource);
		}
	}

	public void Play(string name)
	{
		// 数值越小越优先：死亡、动作、拾取。
		int priority = name == "Boss死了" ? 32 : name == "金币" ? 192 : 128;
		Play(name, priority);
	}

	public void Play(string name, int priority)
	{
		if (!clips.TryGetValue(name, out AudioClip audioClip))
		{
			audioClip = Resources.Load<AudioClip>(name);
			clips[name] = audioClip;
		}
		if (audioClip == null) return;
		priority = Mathf.Clamp(priority, 0, 256);
		AudioSource available = null;
		foreach (AudioSource source in audioSources)
		{
			if (!source.isPlaying)
			{
				available = source;
				break;
			}
			if (source.priority > priority && (available == null || source.priority > available.priority))
			{
				available = source;
			}
		}
		if (available == null) return;
		available.Stop();
		available.priority = priority;
		available.clip = audioClip;
		available.Play();
	}

	private void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}
}
