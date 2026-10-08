using System.Text.RegularExpressions;

namespace Share.Web.Ingest;

public record DataFile(int Number, string Name, string Path);

// The numbered files in the data directory, in number order.
public partial class DataFiles(string directory)
{
    public string Directory { get; } = directory;

    public IReadOnlyList<DataFile> All() =>
        !System.IO.Directory.Exists(Directory) ? [] :
        System.IO.Directory.GetFiles(Directory)
            .Select(p => (Path: p, Match: FileName().Match(System.IO.Path.GetFileName(p))))
            .Where(x => x.Match.Success)
            .Select(x => new DataFile(int.Parse(x.Match.Groups[1].Value), System.IO.Path.GetFileName(x.Path), x.Path))
            .OrderBy(f => f.Number)
            .ToList();

    [GeneratedRegex(@"^(\d{2})-[a-z0-9-]+\.(json|csv)$")]
    private static partial Regex FileName();
}
