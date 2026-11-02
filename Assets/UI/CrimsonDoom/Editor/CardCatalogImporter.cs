namespace CrimsonDoom.UI.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Game.ScriptableObjects.Card;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    /// <summary>Imports, moves, deletions, domain reloads and builds all reconcile the same index.</summary>
    [InitializeOnLoad]
    public sealed class CardCatalogImporter : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public const string ArtFolder = "Assets/Sprites/Cards";
        public const string DataFolder = "Assets/ScriptableObjects/Cards";
        private const string CatalogPath = "Assets/UI/CrimsonDoom/Resources/CrimsonDoomCardCatalog.asset";
        private static bool queued;
        public int callbackOrder => 0;
        static CardCatalogImporter() => QueueRebuild();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] oldPaths)
        {
            if (imported.Concat(deleted).Concat(moved).Concat(oldPaths).Any(IsCardPath)) QueueRebuild();
        }

        private static bool IsCardPath(string path) =>
            path == ArtFolder || path.StartsWith(ArtFolder + "/", StringComparison.Ordinal) ||
            path == DataFolder || path.StartsWith(DataFolder + "/", StringComparison.Ordinal);

        private static void QueueRebuild()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Rebuild(); };
        }

        public void OnPreprocessBuild(BuildReport report) => Rebuild();

        [MenuItem("Crimson Doom/UI Toolkit/Rebuild Card Catalog")]
        public static void Rebuild()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { QueueRebuild(); return; }
            var cards = AssetDatabase.FindAssets("t:CardTypeScriptableObject", new[] { DataFolder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<CardTypeScriptableObject>).Where(c => c != null).ToArray();
            var entries = new List<CardCatalog.Entry>();
            var dependencies = new List<string>();
            foreach (var card in cards)
                dependencies.Add(AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(card)).ToString());
            var seen = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder })
                .OrderBy(AssetDatabase.GUIDToAssetPath, StringComparer.Ordinal))
            {
                if (!seen.Add(guid)) continue;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                dependencies.Add(guid + AssetDatabase.GetAssetDependencyHash(path));
                var image = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (image == null) continue;
                string title = Path.GetFileNameWithoutExtension(path);
                if (title.StartsWith("Crimson_Doom_", StringComparison.OrdinalIgnoreCase)) title = title.Substring(13);
                title = title.Replace('_', ' ');
                string key = CardCatalog.Normalize(title);
                // Only explicit references or exact names bind behavior. Never infer effects from artwork.
                var data = cards.FirstOrDefault(c => c.fullCardTexture == image || c.metaData.cardTexture == image ||
                    CardCatalog.Normalize(c.metaData.name) == key || CardCatalog.Normalize(c.name) == key);
                entries.Add(new CardCatalog.Entry { id = guid, title = title, image = image, data = data });
            }
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            if (catalog == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/UI/CrimsonDoom/Resources"))
                    AssetDatabase.CreateFolder("Assets/UI/CrimsonDoom", "Resources");
                catalog = ScriptableObject.CreateInstance<CardCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var old = catalog.Entries;
            string hash = Hash128.Compute(string.Join("|", dependencies)).ToString();
            if (catalog.ContentHash == hash && old.Count == entries.Count && old.Select((e, i) => e.id == entries[i].id &&
                e.title == entries[i].title && e.image == entries[i].image && e.data == entries[i].data).All(equal => equal)) return;
            catalog.SetEntries(entries, hash);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
