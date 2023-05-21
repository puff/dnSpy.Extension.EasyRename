using System.Collections.Generic;
using dnSpy.Contracts.Extension;

namespace EasyRename;

[ExportExtension]
internal sealed class Extension : IExtension
{
    public IEnumerable<string> MergedResourceDictionaries
    {
        // We don't have any extra resource dictionaries
        get { yield break; }
    }

    public ExtensionInfo ExtensionInfo => new()
    {
        ShortDescription = "A simple extension for easily renaming members"
    };

    public void OnEvent(ExtensionEvent @event, object? obj)
    {
        // We don't care about any events
    }
}