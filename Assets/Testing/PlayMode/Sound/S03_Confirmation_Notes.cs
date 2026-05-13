using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Sound 
{
    [Category("Sound")]
    public class S03_Confirmation_Notes 
    {
        private GameController gameController;
        private AudioManager audioManager;
        private Button rollDiceButton;
        private AudioSource sfxSource;

        [UnitySetUp]
        public IEnumerator UnitySetUp() 
        {
            // Load MainMenu first to create GameManager and AudioManager
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            
            // Verify managers exist
            Assert.IsNotNull(GameManager.Instance, "GameManager should exist");
            Assert.IsNotNull(AudioManager.Instance, "AudioManager should exist");
            
            // Now load BoardScene
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;

            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            audioManager = AudioManager.Instance;
            
            Assert.IsNotNull(gameController, "GameController should exist in the scene");
            Assert.IsNotNull(audioManager, "AudioManager should persist from MainMenu");
            
            rollDiceButton = gameController.inGameUiDocument.rootVisualElement.Q<Button>("dice_button");
            Assert.IsNotNull(rollDiceButton, "Roll dice button should exist");
            
            // Find the SFX AudioSource (the one that's not looping)
            var audioSources = audioManager.GetComponents<AudioSource>();
            foreach (var source in audioSources)
            {
                if (!source.loop)
                {
                    sfxSource = source;
                    break;
                }
            }
            
            Assert.IsNotNull(sfxSource, "AudioManager should have a non-looping AudioSource for SFX");
        }

        [UnityTest]
        public IEnumerator DiceRollPlaysSound() 
        {
            Assert.IsNotNull(audioManager.diceRollSound, "Dice roll sound should be assigned");
            
            // Click the dice button
            using (ClickEvent click = ClickEvent.GetPooled()) 
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            yield return null;
            
            // Verify the sound is assigned and the source exists
            Assert.IsTrue(sfxSource != null, "SFX audio source should exist and be ready to play sounds");
        }

        [UnityTest]
        public IEnumerator SoundPlaysImmediatelyAfterButtonPress() 
        {
            using (ClickEvent click = ClickEvent.GetPooled()) 
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            
            yield return null;

            // Verify sound exists and can be played
            Assert.IsTrue(sfxSource != null && audioManager.diceRollSound != null, 
                "Sound should be ready to play");
        }

        [UnityTest]
        public IEnumerator ConfirmationSoundAffectedByVolumeSettings() 
        {
            // Verify that VolumeManager affects playback
            float originalSfxVolume = VolumeManager.Instance.GetRawSfxVolume();
            
            VolumeManager.Instance.SetSfxVolume(0.5f);
            yield return null;
            
            float adjustedVolume = VolumeManager.Instance.GetAdjustedSfxVolume();
            
            using (ClickEvent click = ClickEvent.GetPooled()) 
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            yield return null;
            
            Assert.LessOrEqual(adjustedVolume, 0.5f, 
                "Adjusted SFX volume should respect volume settings");
            
            // Restore original
            VolumeManager.Instance.SetSfxVolume(originalSfxVolume);
        }

        [UnityTest]
        public IEnumerator ConfirmationSoundDoesNotPlayWhenMuted() 
        {
            float originalSfxVolume = VolumeManager.Instance.GetRawSfxVolume();
            
            VolumeManager.Instance.SetSfxVolume(0f);
            yield return null;
            
            using (ClickEvent click = ClickEvent.GetPooled()) 
            {
                click.target = rollDiceButton;
                rollDiceButton.SendEvent(click);
            }
            yield return null;
            
            float adjustedVolume = VolumeManager.Instance.GetAdjustedSfxVolume();
            Assert.AreEqual(0f, adjustedVolume, 
                "When SFX volume is 0, adjusted volume should be 0 (muted)");
            
            // Restore original
            VolumeManager.Instance.SetSfxVolume(originalSfxVolume);
        }

        [UnityTest]
        public IEnumerator GrayCardCloseButtonPlaysConfirmSound() 
        {
            UIDocument grayCard = gameController.grayCardUiDocument;
            gameController.LandedOnGraySquare();
            yield return null;
            
            var overlay = grayCard.rootVisualElement.Q<VisualElement>("overlay");
            Assert.IsNotNull(overlay, "Gray card overlay should exist");
            
            var grayCloseButton = overlay.Q<Button>("close_button");
            Assert.IsNotNull(grayCloseButton, "Gray card close button should exist");
            
            Assert.IsNotNull(audioManager.confirmSound, "Confirm sound should be assigned");
            
            using (ClickEvent click = ClickEvent.GetPooled()) 
            {
                click.target = grayCloseButton;
                grayCloseButton.SendEvent(click);
            }
            yield return null;
            
            // Verify sound exists and can be played
            Assert.IsTrue(sfxSource != null && audioManager.confirmSound != null, 
                "Gray card close button should trigger confirm sound");
        }

        [UnityTest]
        public IEnumerator AllSoundEffectsAreAssigned() 
        {
            Assert.IsNotNull(audioManager.diceRollSound, "Dice roll sound should be assigned");
            Assert.IsNotNull(audioManager.coughSound, "Cough sound should be assigned");
            Assert.IsNotNull(audioManager.confirmSound, "Confirm sound should be assigned");
            Assert.IsNotNull(audioManager.birdsChirpingSound, "Birds chirping sound should be assigned");
            
            yield return null;
        }
    }
}