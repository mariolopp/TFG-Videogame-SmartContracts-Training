using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class SoundEntry
    {
        public string id;           // ej: "coin_land", "button_click", "trader_arrive"
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitch = 1f;
        public bool randomizePitch = false;
        [Range(0f, 0.3f)] public float pitchVariance = 0.05f;
    }

    [Header("Efectos de sonido")]
    public SoundEntry[] sfx;

    [Header("Música por escena/contexto")]
    public SoundEntry[] music;
}