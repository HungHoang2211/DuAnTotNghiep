using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SimpleSurvival.Items.EditorTools
{
    public static class ItemIdValidator
    {
        private const string ValidateMenuPath = "Tools/Simple Survival/Validate Item IDs";
        private const string FillMenuPath = "Tools/Simple Survival/Fill Missing Item IDs From Name";
        private const string ItemIdProperty = "itemId";
        private const string SlugFallback = "item";

        private static readonly Regex SnakeCasePattern = new Regex("^[a-z][a-z0-9_]*$");
        private static readonly Regex NonSlugCharacters = new Regex("[^a-z0-9]+");

        [MenuItem(ValidateMenuPath)]
        public static void Validate()
        {
            ItemData[] items = ItemAssetFinder.FindAll();
            if (items.Length == 0)
            {
                Debug.Log("No ItemData assets found.");
                return;
            }

            int issues = ReportEmptyIds(items)
                + ReportDuplicateIds(items)
                + ReportMalformedIds(items);

            if (issues == 0)
            {
                Debug.Log($"Item ID validation passed. {items.Length} items checked.");
                return;
            }

            Debug.LogWarning($"Item ID validation finished with {issues} issue(s).");
        }

        [MenuItem(FillMenuPath)]
        public static void FillMissingIds()
        {
            ItemData[] items = ItemAssetFinder.FindAll();
            HashSet<string> takenIds = CollectExistingIds(items);

            int filled = 0;
            foreach (ItemData item in items)
            {
                if (HasId(item))
                    continue;

                string suggestion = MakeUniqueSlug(item.ItemName, takenIds);
                AssignId(item, suggestion);
                takenIds.Add(suggestion);
                filled++;
            }

            if (filled > 0)
                AssetDatabase.SaveAssets();

            Debug.Log($"Filled {filled} missing itemId(s). Review each and add the proper prefix before shipping.");
        }

        private static int ReportEmptyIds(IEnumerable<ItemData> items)
        {
            int count = 0;
            foreach (ItemData item in items)
            {
                if (HasId(item))
                    continue;

                Debug.LogError($"Empty itemId on '{item.ItemName}'.", item);
                count++;
            }
            return count;
        }

        private static int ReportDuplicateIds(IEnumerable<ItemData> items)
        {
            int count = 0;
            foreach (IGrouping<string, ItemData> group in GroupByExistingId(items))
            {
                if (group.Count() <= 1)
                    continue;

                foreach (ItemData item in group)
                {
                    Debug.LogError($"Duplicate itemId '{group.Key}' on '{item.ItemName}'.", item);
                    count++;
                }
            }
            return count;
        }

        private static int ReportMalformedIds(IEnumerable<ItemData> items)
        {
            int count = 0;
            foreach (ItemData item in items)
            {
                if (!HasId(item))
                    continue;
                if (SnakeCasePattern.IsMatch(item.ItemId))
                    continue;

                Debug.LogWarning($"itemId '{item.ItemId}' on '{item.ItemName}' is not lower_snake_case.", item);
                count++;
            }
            return count;
        }

        private static HashSet<string> CollectExistingIds(IEnumerable<ItemData> items)
        {
            return new HashSet<string>(items.Where(HasId).Select(item => item.ItemId));
        }

        private static IEnumerable<IGrouping<string, ItemData>> GroupByExistingId(IEnumerable<ItemData> items)
        {
            return items.Where(HasId).GroupBy(item => item.ItemId);
        }

        private static bool HasId(ItemData item)
        {
            return !string.IsNullOrWhiteSpace(item.ItemId);
        }

        private static void AssignId(ItemData item, string id)
        {
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty(ItemIdProperty).stringValue = id;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }

        private static string MakeUniqueSlug(string source, HashSet<string> takenIds)
        {
            string baseSlug = ToSlug(source);
            if (!takenIds.Contains(baseSlug))
                return baseSlug;

            int suffix = 2;
            while (takenIds.Contains($"{baseSlug}_{suffix}"))
                suffix++;

            return $"{baseSlug}_{suffix}";
        }

        private static string ToSlug(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return SlugFallback;

            string lowered = source.Trim().ToLowerInvariant();
            string collapsed = NonSlugCharacters.Replace(lowered, "_").Trim('_');

            if (collapsed.Length == 0)
                return SlugFallback;
            if (char.IsLetter(collapsed[0]))
                return collapsed;

            return $"{SlugFallback}_{collapsed}";
        }
    }
}