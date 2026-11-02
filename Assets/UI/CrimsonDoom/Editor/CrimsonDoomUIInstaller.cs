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
            var attackRenderer = FindRenderer(scene, "Voss_Attack");
            var hurtRenderer = FindRenderer(scene, "Voss_Hurt");
            var characterAnimations = serialized.FindProperty("characterAnimations");
            characterAnimations.arraySize = 2;
            SetCharacterAnimation(characterAnimations.GetArrayElementAtIndex(0), "Voss", "Attack", 0, attackRenderer);
            SetCharacterAnimation(characterAnimations.GetArrayElementAtIndex(1), "Voss", "Hurt", 0, hurtRenderer);
            Set(serialized, "enemyRenderer", FindRenderer(scene, "Wrag_Idle"));
            Set(serialized, "cardTemplate", AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Folder + "Components/HandCard.uxml"));
            CardCatalogImporter.Rebuild();
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

        private static void SetCharacterAnimation(SerializedProperty binding, string characterName,
            string animationName, int characterSlot, SpriteRenderer renderer)
        {
            binding.FindPropertyRelative("characterName").stringValue = characterName;
            binding.FindPropertyRelative("animationName").stringValue = animationName;
            binding.FindPropertyRelative("characterSlot").enumValueIndex = characterSlot;
            binding.FindPropertyRelative("renderer").objectReferenceValue = renderer;
            binding.FindPropertyRelative("animator").objectReferenceValue =
                renderer != null ? renderer.GetComponent<Animator>() : null;
        }

        private static SpriteRenderer FindRenderer(Scene scene, string name) => scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name).Select(t => t.GetComponent<SpriteRenderer>()).FirstOrDefault();
    }
}
