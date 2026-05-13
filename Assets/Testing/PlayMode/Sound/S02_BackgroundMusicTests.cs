using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sound 
{
    [Category("Sound")]
    public class S02_Background_Music
    {
        private const string START_SCENE = "MainMenu";
        private const float MAX_DEFAULT_VOLUME = 0.40f;
        private const float MIN_DEFAULT_VOLUME = 0.05f;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene(START_SCENE, LoadSceneMode.Single);
            yield return null;
        }

        private AudioSource FindMusicAudioSource()
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                // AudioManager creates two AudioSources - find the music one
                var sources = audioManager.GetComponents<AudioSource>();
                foreach (var src in sources)
                {
                    if (src != null && src.loop)
                    {
                        return src; // This is the music source
                    }
                }
            }

            // Fallback: search all AudioSources for a looping one
            foreach (var src in Object.FindObjectsOfType<AudioSource>())
            {
                if (src != null && src.clip != null && src.loop) 
                {
                    return src;
                }
            }

            return null;
        }

        [UnityTest]
        public IEnumerator MusicStartsPlayingWhenGameBegins()
        {
            var music = FindMusicAudioSource();
            Assert.IsNotNull(music, "No looping music AudioSource found.");
            Assert.IsTrue(music.isPlaying, "Music AudioSource exists but is not playing.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MusicContinuesAcrossScenes()
        {
            var music1 = FindMusicAudioSource();
            Assert.IsNotNull(music1, "Music AudioSource not found in MainMenu");
            Assert.IsTrue(music1.isPlaying, "Music not playing in MainMenu");
            
            var clip1 = music1.clip;

            SceneManager.LoadScene("CharacterSelect", LoadSceneMode.Single);
            yield return null;

            var music2 = FindMusicAudioSource();
            Assert.IsNotNull(music2, "Music AudioSource not found after scene change.");
            Assert.IsTrue(music2.isPlaying, "Music stopped after scene change.");
            Assert.AreSame(music1, music2, "Music AudioSource instance should persist (DontDestroyOnLoad)");
            Assert.AreEqual(clip1, music2.clip, "Music clip changed unexpectedly across scenes.");
        }

        [UnityTest]
        public IEnumerator MusicDefaultVolumeIsComfortable()
        {
            var music = FindMusicAudioSource();
            Assert.IsNotNull(music, "Music AudioSource not found");
            
            Assert.GreaterOrEqual(music.volume, MIN_DEFAULT_VOLUME, 
                $"Music volume too low: {music.volume}");
            Assert.LessOrEqual(music.volume, MAX_DEFAULT_VOLUME, 
                $"Music volume too loud: {music.volume}");
            
            yield return null;
        }

        [UnityTest]
        public IEnumerator MusicIsLooping()
        {
            var music = FindMusicAudioSource();
            Assert.IsNotNull(music, "Music AudioSource not found");
            Assert.IsTrue(music.loop, "Music AudioSource is not set to loop.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioManagerPersistsAcrossScenes()
        {
            var audioManager1 = AudioManager.Instance;
            Assert.IsNotNull(audioManager1, "AudioManager should exist in MainMenu");

            SceneManager.LoadScene("CharacterSelect", LoadSceneMode.Single);
            yield return null;

            var audioManager2 = AudioManager.Instance;
            Assert.IsNotNull(audioManager2, "AudioManager should exist after scene change");
            Assert.AreSame(audioManager1, audioManager2, "AudioManager should be the same instance (DontDestroyOnLoad)");
        }
    }
}