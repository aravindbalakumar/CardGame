namespace CrimsonDoom.UI.Editor
{
    using System.Linq;
    using Game;
    using Game.ActorAndPlayers;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;

    /// <summary>Explicit, repeatable installation. Never runs automatically on scene load.</summary>
    public static class CrimsonDoomUIInstaller
    {
        private const string Folder = "Assets/UI/CrimsonDoom/";
        private const string ObjectName = "[UI Toolkit] Crimson Doom Game UI";

        [MenuItem("Crimson Doom/UI Toolkit/Install in GameView")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/GameView.unity")
                throw new System.InvalidOperationException("Open GameView.unity before installing its UI.");
            if (scene.GetRootGameObjects().Any(g => g.name == ObjectName)) return;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Folder + "GameViewPanelSettings.asset");
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1672, 941);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = 0.5f;
                panel.sortingOrder = 100;
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Folder + "RuntimeTheme.tss");
                AssetDatabase.CreateAsset(panel, Folder + "GameViewPanelSettings.asset");
            }
            var root = new GameObject(ObjectName);
            root.SetActive(false);
            Undo.RegisterCreatedObjectUndo(root, "Install Crimson Doom UI Toolkit");
            var document = root.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "GameView.uxml");
            document.sortingOrder = 100;
            var controller = root.AddComponent<GameViewUIToolkit>();
            var serialized = new SerializedObject(controller);
            var handler = Object.FindFirstObjectByType<GameHandler>();
            if (handler != null)
            {
                var game = new SerializedObject(handler);
                Set(serialized, "gameHandler", handler);
                Set(serialized, "localPlayer", game.FindProperty("currentLocalPlayer").objectReferenceValue);
                Set(serialized, "legacyCardParent", game.FindProperty("cardParent").objectReferenceValue);
                var opponents = game.FindProperty("opponents");
                if (opponents.arraySize > 0) Set(serialized, "opponent", opponents.GetArrayElementAtIndex(0).objectReferenceValue);
            }
            Set(serialized, "heroRenderer", FindRenderer(scene, "Voss_Idle"));
            Set(serialized, "enemyRenderer", FindRenderer(scene, "Wrag_Idle"));
            Set(serialized, "cardTemplate", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "Components/HandCard.uxml"));
            var enemyFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Warg/Warg_IDLE.png").OfType<Sprite>()
                .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
            var animation = serialized.FindProperty("enemyFrames");
            animation.arraySize = enemyFrames.Length;
            for (int i = 0; i < enemyFrames.Length; i++) animation.GetArrayElementAtIndex(i).objectReferenceValue = enemyFrames[i];
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/Cardswithoutdescription.png").OfType<Sprite>().ToArray();
            var names = new[] { "strike", "Resuurection", "Sword slash", "Holy light", "Blessing" };
            var cards = serialized.FindProperty("previewCardSprites");
            cards.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                cards.GetArrayElementAtIndex(i).objectReferenceValue = sprites.Single(s => s.name == names[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssetIfDirty(panel);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
        }

        private static void Set(SerializedObject target, string property, Object value) =>
            target.FindProperty(property).objectReferenceValue = value;

        private static SpriteRenderer FindRenderer(Scene scene, string name) => scene.GetRootGameObjects()
            .Where(g => g.name == name).Select(g => g.GetComponent<SpriteRenderer>()).FirstOrDefault();
    }
}
