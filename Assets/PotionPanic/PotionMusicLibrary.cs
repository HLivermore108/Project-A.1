using UnityEngine;

namespace PotionPanic
{
    // These references keep the chosen music in the build without duplicating the audio files.
    [CreateAssetMenu(menuName = "Potion Panic/Music library")]
    public sealed class PotionMusicLibrary : ScriptableObject
    {
        public AudioClip mainMenu;
        public AudioClip gameplay;
        public AudioClip results;
        public AudioClip orderComplete, newCustomer, brewReady, startBrewing, finishBrewing;
    }
}
