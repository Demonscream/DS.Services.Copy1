namespace DS.Services.Extensions;

public static class ListExtensions
{
    public static void AddRangeWithoutEmptyAndDuplicates(this List<string> list, List<string> collection)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(collection);

        list.AddRange(collection);
        list.DeduplicateAndRemoveEmpty();
        
    }

    public static void DeduplicateAndRemoveEmpty(this List<string> list)
    {
        ArgumentNullException.ThrowIfNull(list);

        // Remove empty/null/whitespace entries
        list.RemoveAll(string.IsNullOrWhiteSpace);

        // De-duplicate (keeping first occurrence)
        var uniqueItems = new HashSet<string>();
        list.RemoveAll(x => !uniqueItems.Add(x));
    }
}