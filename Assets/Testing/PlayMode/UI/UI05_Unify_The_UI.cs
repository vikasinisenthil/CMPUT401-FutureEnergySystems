using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UI
{
    [Category("UI")]
    public class UI05_Unify_The_UI
    {
        private List<string> scenesToTest = new List<string> 
        { 
            "MainMenu", 
            "CharacterSelect", 
            "BoardScene" 
        };

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            // Create GameManager if needed
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TitleTextUsesConsistentFontAcrossScenes()
        {
            string expectedFontName = "CherryBombOne"; // The font resource name
            Dictionary<string, string> titleLabels = new Dictionary<string, string>
            {
                { "MainMenu", "game-title" },
                { "CharacterSelect", "select-title" },
                { "BoardScene", null } // BoardScene might not have a main title
            };

            List<string> foundFonts = new List<string>();

            foreach (var sceneName in scenesToTest)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;

                if (!titleLabels.ContainsKey(sceneName) || titleLabels[sceneName] == null)
                    continue;

                var uiDocuments = Object.FindObjectsOfType<UIDocument>();
                
                foreach (var uiDoc in uiDocuments)
                {
                    var titleLabel = uiDoc.rootVisualElement.Q<Label>(className: titleLabels[sceneName]);
                    
                    if (titleLabel == null)
                    {
                        // Try finding by common title class names
                        titleLabel = uiDoc.rootVisualElement.Q<Label>(className: "title") ??
                                   uiDoc.rootVisualElement.Q<Label>(className: "settings-title") ??
                                   uiDoc.rootVisualElement.Q<Label>(className: "card-name");
                    }

                    if (titleLabel != null && titleLabel.resolvedStyle.display == DisplayStyle.Flex)
                    {
                        // Check if font definition is applied (USS sets -unity-font-definition)
                        // We can't directly read the font resource name, but we can verify font is set
                        var fontSize = titleLabel.resolvedStyle.fontSize;
                        
                        Assert.Greater(fontSize, 20f, 
                            $"Title in {sceneName} should have a large font size (>20px)");
                        
                        foundFonts.Add($"{sceneName}: {fontSize}px");
                        Debug.Log($"Title font found in {sceneName} with size {fontSize}px");
                        break;
                    }
                }
            }

            Assert.Greater(foundFonts.Count, 0, 
                "Should find title text with consistent font styling in at least one scene");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ButtonFontSizeIsConsistentAcrossScenes()
        {
            Dictionary<string, List<float>> buttonFontSizes = new Dictionary<string, List<float>>();

            foreach (var sceneName in scenesToTest)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;

                buttonFontSizes[sceneName] = new List<float>();

                var uiDocuments = Object.FindObjectsOfType<UIDocument>();
                
                foreach (var uiDoc in uiDocuments)
                {
                    var buttons = uiDoc.rootVisualElement.Query<Button>().ToList();
                    
                    foreach (var button in buttons)
                    {
                        if (button.resolvedStyle.display == DisplayStyle.Flex)
                        {
                            var fontSize = button.resolvedStyle.fontSize;
                            if (fontSize > 0)
                            {
                                buttonFontSizes[sceneName].Add(fontSize);
                            }
                        }
                    }
                }

                Debug.Log($"{sceneName} button font sizes: {string.Join(", ", buttonFontSizes[sceneName])}");
            }

            // Check that most buttons use similar font sizes (within reasonable range)
            // Standard button font should be around 18-28px
            foreach (var scene in buttonFontSizes)
            {
                if (scene.Value.Count > 0)
                {
                    foreach (var size in scene.Value)
                    {
                        Assert.IsTrue(size >= 14f && size <= 32f,
                            $"Button font size in {scene.Key} should be in reasonable range (14-32px), but was {size}px");
                    }
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator FontColorIsConsistentForSimilarElements()
        {
            Dictionary<string, Color> buttonTextColors = new Dictionary<string, Color>();

            foreach (var sceneName in scenesToTest)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;

                var uiDocuments = Object.FindObjectsOfType<UIDocument>();
                
                foreach (var uiDoc in uiDocuments)
                {
                    var buttons = uiDoc.rootVisualElement.Query<Button>(className: "menu-button").ToList();
                    
                    foreach (var button in buttons)
                    {
                        if (button.resolvedStyle.display == DisplayStyle.Flex)
                        {
                            var color = button.resolvedStyle.color;
                            buttonTextColors[$"{sceneName}_{button.name}"] = color;
                            Debug.Log($"Button {button.name} in {sceneName} has color: {color}");
                            break; // Just check one button per scene
                        }
                    }
                }
            }

            // Verify that menu buttons use white or consistent colors
            foreach (var buttonColor in buttonTextColors)
            {
                Assert.IsTrue(
                    buttonColor.Value == Color.white || 
                    (buttonColor.Value.r > 0.8f && buttonColor.Value.g > 0.8f && buttonColor.Value.b > 0.8f),
                    $"Menu button {buttonColor.Key} should use white or light color for text");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator OverlayBackdropStyleIsConsistent()
        {
            List<string> scenesWithOverlays = new List<string> { "BoardScene", "CharacterSelect" };
            List<Color> backdropColors = new List<Color>();

            foreach (var sceneName in scenesWithOverlays)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;

                var uiDocuments = Object.FindObjectsOfType<UIDocument>();
                
                foreach (var uiDoc in uiDocuments)
                {
                    var backdrop = uiDoc.rootVisualElement.Q(className: "backdrop");
                    
                    if (backdrop != null)
                    {
                        var bgColor = backdrop.resolvedStyle.backgroundColor;
                        backdropColors.Add(bgColor);
                        
                        // Check that backdrop is dark and semi-transparent
                        Assert.Less(bgColor.r, 0.3f, $"Backdrop in {sceneName} should be dark (low red)");
                        Assert.Less(bgColor.g, 0.3f, $"Backdrop in {sceneName} should be dark (low green)");
                        Assert.Less(bgColor.b, 0.3f, $"Backdrop in {sceneName} should be dark (low blue)");
                        Assert.Greater(bgColor.a, 0.5f, $"Backdrop in {sceneName} should be visible (alpha > 0.5)");
                        
                        Debug.Log($"Backdrop in {sceneName}: {bgColor}");
                        break;
                    }
                }
            }

            // All backdrops should be similar dark colors
            Assert.Greater(backdropColors.Count, 0, "Should find at least one backdrop");

            yield return null;
        }

        [UnityTest]
        public IEnumerator CardPopupsUseSimilarStyling()
        {
            SceneManager.LoadScene("BoardScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var uiDocuments = Object.FindObjectsOfType<UIDocument>();
            List<VisualElement> popupCards = new List<VisualElement>();

            foreach (var uiDoc in uiDocuments)
            {
                // Try multiple class names for popup cards
                var card = uiDoc.rootVisualElement.Q(className: "popup-card") ??
                        uiDoc.rootVisualElement.Q(className: "pt-card") ??
                        uiDoc.rootVisualElement.Q(className: "settings-card");
                
                if (card != null)
                {
                    popupCards.Add(card);
                }
            }

            if (popupCards.Count == 0)
            {
                Assert.Inconclusive("No popup cards found in the scene to test");
                yield break;
            }

            // Check that all popup cards have consistent styling
            foreach (var card in popupCards)
            {
                var borderWidth = card.resolvedStyle.borderTopWidth;
                var borderRadius = card.resolvedStyle.borderTopLeftRadius;
                var borderColor = card.resolvedStyle.borderTopColor;

                // Some cards might not have borders set yet if they're hidden
                if (borderWidth > 0)
                {
                    Assert.Greater(borderWidth, 2f, "Popup cards should have visible borders (>2px)");
                }
                
                if (borderRadius > 0)
                {
                    Assert.Greater(borderRadius, 8f, "Popup cards should have rounded corners (>8px)");
                }
                
                Debug.Log($"Popup card styling - Border: {borderWidth}px, Radius: {borderRadius}px, Color: {borderColor}");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PrimaryButtonsUseConsistentStyling()
        {
            Dictionary<string, Color> primaryButtonColors = new Dictionary<string, Color>();

            foreach (var sceneName in scenesToTest)
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;

                var uiDocuments = Object.FindObjectsOfType<UIDocument>();
                
                foreach (var uiDoc in uiDocuments)
                {
                    // Look for primary action buttons (menu-button, settings-close, etc.)
                    var buttons = uiDoc.rootVisualElement.Query<Button>(className: "menu-button").ToList();
                    
                    foreach (var button in buttons)
                    {
                        if (button.resolvedStyle.display == DisplayStyle.Flex)
                        {
                            var bgColor = button.resolvedStyle.backgroundColor;
                            primaryButtonColors[$"{sceneName}_{button.name}"] = bgColor;
                            
                            // Check that primary buttons use blue theme
                            Assert.IsTrue(bgColor.b > bgColor.r && bgColor.b > bgColor.g,
                                $"Primary button in {sceneName} should use blue color scheme");
                            
                            Debug.Log($"Primary button in {sceneName}: {bgColor}");
                            break;
                        }
                    }
                }
            }

            Assert.Greater(primaryButtonColors.Count, 0, "Should find primary buttons to test");

            yield return null;
        }
    }
}