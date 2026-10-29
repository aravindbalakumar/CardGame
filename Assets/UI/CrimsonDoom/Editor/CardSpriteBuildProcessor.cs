namespace CrimsonDoom.UI.Editor
{
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine.SceneManagement;

    // Embed the current folder contents into the build's scene copy, without saving source scenes.
    public sealed class CardSpriteBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var sprites = GameViewUIToolkit.DiscoverCardSprites();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var view in root.GetComponentsInChildren<GameViewUIToolkit>(true))
                {
                    var serialized = new SerializedObject(view);
                    var cards = serialized.FindProperty("folderCardSprites");
                    cards.arraySize = sprites.Length;
                    for (int i = 0; i < sprites.Length; i++)
                        cards.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
        }
    }
}
