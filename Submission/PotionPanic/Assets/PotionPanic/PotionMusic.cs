using UnityEngine;

namespace PotionPanic
{
    public sealed class PotionMusic : MonoBehaviour
    {
        public enum Cue { MainMenu, Gameplay, Results }
        PotionMusicLibrary library;
        public AudioSource Source { get; private set; }

        void Awake()
        {
            library = Resources.Load<PotionMusicLibrary>("PotionMusic");
            // Keep music on its own source so a button's sound doesn't interrupt the song.
            Source = gameObject.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = true;
            Source.spatialBlend = 0;
            Source.volume = .4f;
        }

        public void Play(Cue cue)
        {
            if (library == null) return;
            AudioClip track = cue == Cue.MainMenu ? library.mainMenu : cue == Cue.Gameplay ? library.gameplay : library.results;
            // Rebuilding a menu or switching stations shouldn't restart the same song.
            if (Source.clip == track) return;
            Source.Stop();
            Source.clip = track;
            if (track != null) Source.Play();
        }
    }
}
