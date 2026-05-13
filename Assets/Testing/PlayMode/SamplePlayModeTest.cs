using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Sample {
    [Category("Sample")]
    public class SamplePlayModeTest {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Load the board scene
            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);
            yield return null;
            
            // Setup GameManager to avoid sprite errors
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Mode = GameMode.Singleplayer;
                gm.PlayerCount = 1;
                gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            }
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator SamplePlayModeTestWithEnumeratorPasses()
        {
            GameObject player = GameObject.Find("Player");
            Assert.NotNull(player, "Player GameObject should exist in the scene");
            yield return null;
        }
    }
}