using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Multiplayer
{
    [Category("Multiplayer")]
    public class MP04_No_Repeat_Questions
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            SceneManager.LoadScene("Assets/Scenes/MainMenu.unity", LoadSceneMode.Single);

            yield return null;

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            gm.Mode = GameMode.Singleplayer;
            gm.PlayerCount = 1;
            gm.SelectedHeroes = new HeroType[] { HeroType.Scientist };
            gm.difficulty = Difficulty.Easy;

            yield return null;

            SceneManager.LoadScene("Assets/Scenes/BoardScene.unity", LoadSceneMode.Single);

            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EachCardShownOnceBeforeDeckEmpty()
        {
            BlueCardManager bcm = Object.FindFirstObjectByType<BlueCardManager>();

            int cardCount = bcm.deck.Count;

            var seenCards = new List<BlueCard>();

            for (int i = 0; i < cardCount; ++i)
            {
                var drawnCard = bcm.DrawCard();
                var foundCard = seenCards.Find(c =>
                {
                    return c.image == drawnCard.image && c.name == drawnCard.name;
                });

                Assert.IsNull(foundCard);

                seenCards.Add(drawnCard);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DeckRefilledWhenEmpty()
        {
            BlueCardManager bcm = Object.FindFirstObjectByType<BlueCardManager>();

            int cardCount = bcm.deck.Count;

            var seenCards = new List<BlueCard>();

            for (int i = 0; i < cardCount; ++i)
            {
                var drawnCard = bcm.DrawCard();
                var foundCard = seenCards.Find(c =>
                {
                    return c.image == drawnCard.image && c.name == drawnCard.name;
                });

                Assert.IsNull(foundCard);

                seenCards.Add(drawnCard);
            }

            var newCard = bcm.DrawCard();
            var fCard = seenCards.Find(c =>
            {
                return c.image == newCard.image && c.name == newCard.name;
            });

            Assert.IsNotNull(fCard);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CardsDrawnFairly()
        {
            BlueCardManager bcm = Object.FindFirstObjectByType<BlueCardManager>();

            int cardCount = bcm.deck.Count;
            int draws = cardCount * 50;

            var counts = new Dictionary<string, int>();

            for (int i = 0; i < draws; i++)
            {
                var card = bcm.DrawCard();
                string key = card.name + card.image.ToString();

                if (!counts.ContainsKey(key))
                {
                    counts[key] = 0;
                }

                counts[key]++;
            }

            float expected = (float)draws / cardCount;
            float tolerance = expected * 0.5f; // allow 50% variance

            foreach (var kvp in counts)
            {
                Assert.IsTrue(
                    Mathf.Abs(kvp.Value - expected) < tolerance,
                    $"Card {kvp.Key} drawn {kvp.Value} times, expected around {expected}"
                );
            }

            yield return null;
        }
    }
}
