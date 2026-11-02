using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CrimsonDoom.UI;
using CrimsonDoom.UI.Editor;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Temporary editor driver; requests/results stay in Temp. Removed after verification.
[InitializeOnLoad]
internal static class CardUIVerificationDriver
{
    static CardUIVerificationDriver() { EditorApplication.update += Tick; }
    static void Tick()
    {
        const string request = "Temp/card-ui-verify.request";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var action = File.ReadAllText(request).Trim(); File.Delete(request);
        try
        {
            if (action == "play") EditorApplication.isPlaying = true;
            if (action == "stop") EditorApplication.isPlaying = false;
            if (action == "refresh") { AssetDatabase.Refresh(); CardCatalogImporter.Rebuild(); }
            if (action == "validate") Validate();
            if (action == "layout")
            {
                var doc = UnityEngine.Object.FindAnyObjectByType<GameViewUIToolkit>().GetComponent<UIDocument>();
                var scroll = doc.rootVisualElement.Q<ScrollView>("hand-scroll");
                var cards = doc.rootVisualElement.Query<Button>(className: "hand-card").ToList();
                Check(cards.All(b => Mathf.Abs(b.resolvedStyle.width - cards[0].resolvedStyle.width) < 0.1f), "Unequal widths");
                Check(scroll.horizontalScroller.highValue > 0, "Missing horizontal overflow");
                Debug.Log("CARD_UI_LAYOUT_PASS width=" + cards[0].resolvedStyle.width + " overflow=" + scroll.horizontalScroller.highValue);
            }
            if (action == "small")
            {
                var gameRoot = UnityEngine.Object.FindAnyObjectByType<GameViewUIToolkit>().GetComponent<UIDocument>().rootVisualElement.Q("game-root");
                gameRoot.style.width = 950; gameRoot.style.height = 700;
            }
            if (action == "normal")
            {
                var gameRoot = UnityEngine.Object.FindAnyObjectByType<GameViewUIToolkit>().GetComponent<UIDocument>().rootVisualElement.Q("game-root");
                gameRoot.style.width = StyleKeyword.Null; gameRoot.style.height = StyleKeyword.Null;
            }
            if (action == "effect")
            {
                var testPlayer = UnityEngine.Object.FindAnyObjectByType<UserPlayer>();
                var ui = UnityEngine.Object.FindAnyObjectByType<GameViewUIToolkit>();
                var select = typeof(GameViewUIToolkit).GetMethod("SelectCard", BindingFlags.Instance | BindingFlags.NonPublic);
                int index = testPlayer.currentHand.FindIndex(c => c.cardAction.actionType == Game.DataStructures.CardActionType.Heal);
                Check(index >= 0, "No heal to test");
                testPlayer.AddHealth(-15);
                int before = testPlayer.Health;
                select.Invoke(ui, new object[] { index });
                typeof(GameViewUIToolkit).GetMethod("TargetSelf", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
                Check(testPlayer.Health == Mathf.Min(testPlayer.MaxHealth, before + testPlayer.currentHand[index].cardAction.value), "Healing through Toolkit target failed");
                Debug.Log("CARD_UI_EFFECT_PASS: selected heal and targeted self through original gameplay path");
            }
            if (action == "live")
            {
                var player = UnityEngine.Object.FindFirstObjectByType<UserPlayer>(FindObjectsInactive.Include);
                for (var t = player.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
                typeof(UserPlayer).GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
                UnityEngine.Object.FindFirstObjectByType<GameViewUIToolkit>().RefreshHand();
            }
            if (action == "catalog")
                typeof(GameViewUIToolkit).GetMethod("ToggleCatalog", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(UnityEngine.Object.FindFirstObjectByType<GameViewUIToolkit>(), null);
            if (action == "screenshot") ScreenCapture.CaptureScreenshot("Temp/card-ui-play.png");
            var view = UnityEngine.Object.FindFirstObjectByType<GameViewUIToolkit>();
            var root = view != null ? view.GetComponent<UIDocument>().rootVisualElement : null;
            var handler = UnityEngine.Object.FindFirstObjectByType<GameHandler>(FindObjectsInactive.Include);
            var catalog = CardCatalog.Load();
            File.WriteAllText("Temp/card-ui-verify.result", "OK " + action + " play=" + EditorApplication.isPlaying +
                " scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path +
                " catalog=" + (catalog != null ? catalog.Entries.Count : -1) +
                " buttons=" + (root != null ? root.Query<Button>(className: "hand-card").ToList().Count : -1) +
                " handler=" + (handler != null ? handler.isActiveAndEnabled.ToString() : "null") +
                " players=" + string.Join(",", UnityEngine.Object.FindObjectsByType<UserPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(p => p.name + ":" + p.isActiveAndEnabled + ":" + p.currentHand.Count)));
        }
        catch (Exception e) { File.WriteAllText("Temp/card-ui-verify.result", e.ToString()); Debug.LogException(e); }
    }
    static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
    static void Validate()
    {
        Check(Application.isPlaying, "Play Mode required");
        var catalog = CardCatalog.Load();
        Check(catalog != null && catalog.Entries.Count > 0, "Missing catalog");
        Check(catalog.Entries.All(e => e.image != null), "Missing images");
        Check(catalog.Entries.Select(e => e.id).Distinct().Count() == catalog.Entries.Count, "Duplicate catalog IDs");
        var view = UnityEngine.Object.FindFirstObjectByType<GameViewUIToolkit>();
        Check(view != null, "Missing view");
        view.RefreshHand();
        var root = view.GetComponent<UIDocument>().rootVisualElement;
        var buttons = root.Query<Button>(className: "hand-card").ToList();
        var player = UnityEngine.Object.FindFirstObjectByType<UserPlayer>();
        bool live = player != null && !string.IsNullOrEmpty(player.playerID);
        Check(buttons.Count == (live ? player.currentHand.Count : catalog.Entries.Count), "Card count mismatch");
        Check(buttons.All(b => b.Q<Image>("card-frame").image != null), "Missing rendered face");
        var method = typeof(GameViewUIToolkit).GetMethod(live ? "SelectCard" : "InspectPreview", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(view, new object[] { buttons.Count - 1 });
        Check(buttons.Last().ClassListContains("selected"), "Individual selection failed");
        if (live)
        {
            var handler = UnityEngine.Object.FindFirstObjectByType<GameHandler>();
            var serialized = new SerializedObject(handler);
            var parent = (Transform)serialized.FindProperty("cardParent").objectReferenceValue;
            handler.UpdateCardDeck(); handler.UpdateCardDeck();
            Check(parent.GetComponentsInChildren<Card>().Length == player.currentHand.Count(c => c != null), "Duplicate backing cards after refresh");
            var original = player.currentHand.ToList();
            try
            {
                for (int i = 0; i < 40; i++) player.currentHand.Add(original[i % original.Count]);
                view.RefreshHand();
                Check(root.Query<Button>(className: "hand-card").ToList().Count == original.Count + 40, "Growing hand failed");
                method.Invoke(view, new object[] { original.Count + 39 });
                Check(handler.SelectedCard != null && handler.SelectedCard.cardData == player.currentHand.Last(), "Repeated card selection failed");
            }
            finally { player.currentHand.Clear(); player.currentHand.AddRange(original); view.RefreshHand(); }
        }
        Debug.Log("CARD_UI_VERIFY_PASS: rendering, count, unique catalog, individual selection" + (live ? ", deck idempotence, 40-card growth, repeated-card selection" : " (preview)"));
    }
}
