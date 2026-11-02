namespace CrimsonDoom.UI
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Game.ScriptableObjects.Card;
    using UnityEngine;

    /// <summary>Build-safe index generated from the card folders, never from a scene's references.</summary>
    public sealed class CardCatalog : ScriptableObject
    {
        public const string ResourceName = "CrimsonDoomCardCatalog";
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public string title;
            public Texture2D image;
            public CardTypeScriptableObject data;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private int revision;
        [SerializeField] private string contentHash;
        public IReadOnlyList<Entry> Entries => entries;
        public int Revision => revision;
        public string ContentHash => contentHash;
        public static CardCatalog Load() => Resources.Load<CardCatalog>(ResourceName);

        public Texture2D FindImage(CardTypeScriptableObject data)
        {
            if (data == null) return null;
            if (data.fullCardTexture != null) return data.fullCardTexture;
            string key = Normalize(data.metaData.name);
            foreach (var entry in entries)
                if (entry.data == data || Normalize(entry.title) == key || entry.image == data.metaData.cardTexture)
                    return entry.image;
            return null;
        }

        public static string Normalize(string value)
        {
            var result = new StringBuilder();
            foreach (char c in value ?? "")
                if (char.IsLetterOrDigit(c)) result.Append(char.ToLowerInvariant(c));
            return result.ToString();
        }

#if UNITY_EDITOR
        public void SetEntries(List<Entry> value, string hash)
        {
            entries = value;
            contentHash = hash;
            revision++;
        }
#endif
    }
}
