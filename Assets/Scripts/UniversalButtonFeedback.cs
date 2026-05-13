using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

using UGUIButton = UnityEngine.UI.Button;
using UITKButton = UnityEngine.UIElements.Button;

public class UniversalButtonFeedback : MonoBehaviour
{
    private static UniversalButtonFeedback _instance;
    private StyleSheet _hoverSheet;

    private const float UITK_CLICK_LOCK_SECONDS = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject("[UniversalButtonFeedback]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<UniversalButtonFeedback>();
    }

    private void Awake()
    {
        _hoverSheet = Resources.Load<StyleSheet>("Buttons/UniversalHover");

        SceneManager.sceneLoaded += (_, __) => { StartCoroutine(ApplyNextFrames()); };
        StartCoroutine(ApplyNextFrames());
    }

    private IEnumerator ApplyNextFrames()
    {
        yield return null;
        yield return null;

        ApplyUIToolkitHoverAndClickBounce();
        ApplyUGUIHoverAndPressTint();
    }

    private void ApplyUIToolkitHoverAndClickBounce()
    {
        if (_hoverSheet == null) return;

        foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var root = doc.rootVisualElement;
            if (root == null) continue;

            if (!root.styleSheets.Contains(_hoverSheet))
                root.styleSheets.Add(_hoverSheet);

            foreach (var b in root.Query<UITKButton>().ToList())
            {
                if (b.ClassListContains("ui02-bound")) continue;
                b.AddToClassList("ui02-bound");

                b.RegisterCallback<ClickEvent>(_ =>
                {
                    if (b.ClassListContains("ui02-lock")) return;
                    StartCoroutine(ClickBounceAndLock(b));
                });
            }
        }
    }

    private IEnumerator ClickBounceAndLock(UITKButton b)
    {
        if (b == null) yield break;

        b.AddToClassList("ui02-lock");
        b.AddToClassList("ui02-clicked");

        yield return null;

        if (b != null) b.RemoveFromClassList("ui02-clicked");
        if (b != null) b.RemoveFromClassList("ui02-lock");
    }

    private void ApplyUGUIHoverAndPressTint()
    {
        foreach (var btn in FindObjectsByType<UGUIButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (btn == null) continue;
            if (btn.transition != Selectable.Transition.ColorTint) continue;

            var cb = btn.colors;
            var normal = cb.normalColor;

            cb.highlightedColor = Color.Lerp(normal, Color.white, 0.15f);
            cb.pressedColor = Color.Lerp(normal, Color.black, 0.20f);
            cb.fadeDuration = 0.15f;

            btn.colors = cb;
        }
    }
}