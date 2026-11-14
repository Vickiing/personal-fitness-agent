using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using PersonalFitnessAgent.Core.Domain;

namespace PersonalFitnessAgent.Core.Application;

/// <summary>Reads user supplied A/B workout data from a bounded, local XLSX stream.</summary>
public static class WorkoutSheetXlsxReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const int MaximumWorkbookBytes = 10 * 1024 * 1024;
    private const long MaximumExpandedBytes = 25 * 1024 * 1024;
    private const int MaximumZipEntries = 100;
    private const int MaximumExercisesPerDay = 30;

    public static WorkoutSheet Read(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var buffer = new MemoryStream();
        var transfer = new byte[81920];
        int read;
        while ((read = source.Read(transfer, 0, transfer.Length)) > 0)
        {
            if (buffer.Length + read > MaximumWorkbookBytes)
                throw new InvalidDataException("A planilha excede o limite de tamanho suportado.");
            buffer.Write(transfer, 0, read);
        }
        buffer.Position = 0;

        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > MaximumZipEntries || archive.Entries.Sum(entry => entry.Length) > MaximumExpandedBytes)
            throw new InvalidDataException("A planilha excede os limites de conteúdo suportados.");

        var workbook = LoadXml(archive, "xl/workbook.xml");
        var relationshipDocument = LoadXml(archive, "xl/_rels/workbook.xml.rels");
        var relationshipMap = relationshipDocument.Root?.Elements()
            .Where(element => element.Name.LocalName == "Relationship")
            .ToDictionary(element => (string?)element.Attribute("Id") ?? "",
                element => (string?)element.Attribute("Target") ?? "") ?? [];
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPaths = workbook.Descendants(Main + "sheet").ToDictionary(
            element => (string?)element.Attribute("name") ?? "",
            element => ResolveWorkbookTarget(relationshipMap.GetValueOrDefault(
                (string?)element.Attribute(Relationships + "id") ?? "") ?? ""));

        var days = new[] { ReadDay(archive, sheetPaths, sharedStrings, "Treino A", "A"),
            ReadDay(archive, sheetPaths, sharedStrings, "Treino B", "B") };
        return new WorkoutSheet(Guid.NewGuid(), DateTimeOffset.UtcNow, days);
    }

    private static WorkoutSheetDay ReadDay(ZipArchive archive, IReadOnlyDictionary<string, string> sheetPaths,
        IReadOnlyList<string> sharedStrings, string sheetName, string key)
    {
        if (!sheetPaths.TryGetValue(sheetName, out var path))
            throw new InvalidDataException("A planilha precisa conter as abas Treino A e Treino B.");
        var document = LoadXml(archive, path);
        var rows = document.Descendants(Main + "row").Select(row =>
        {
            var rowNumber = int.TryParse((string?)row.Attribute("r"), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? number : 0;
            var cells = row.Elements(Main + "c").ToDictionary(
                cell => ColumnIndex((string?)cell.Attribute("r") ?? ""),
                cell => ReadCell(cell, sharedStrings));
            return (rowNumber, cells);
        }).ToDictionary(item => item.rowNumber, item => item.cells);

        var title = Cell(rows, 1, 1);
        var summary = Cell(rows, 4, 1);
        if (string.IsNullOrWhiteSpace(title)) title = sheetName;
        var exercises = new List<WorkoutSheetExercise>();
        foreach (var row in rows.Where(item => item.Key >= 8).OrderBy(item => item.Key))
        {
            var name = Value(row.Value, 2);
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (exercises.Count == MaximumExercisesPerDay)
                throw new InvalidDataException("A planilha contém exercícios demais em uma das abas.");
            var order = int.TryParse(Value(row.Value, 1), NumberStyles.None, CultureInfo.InvariantCulture, out var position)
                ? position : exercises.Count + 1;
            exercises.Add(new WorkoutSheetExercise(order, name, Value(row.Value, 3), Value(row.Value, 4),
                Value(row.Value, 5), Value(row.Value, 6), Value(row.Value, 7), Value(row.Value, 8)));
        }
        if (exercises.Count == 0)
            throw new InvalidDataException("A planilha precisa conter exercícios nas abas Treino A e Treino B.");

        var notes = new[] { Cell(rows, 17, 1), Cell(rows, 19, 1), Cell(rows, 21, 1) }
            .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return new WorkoutSheetDay(key, title, summary, exercises, notes);
    }

    private static XDocument LoadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException("A planilha está incompleta ou não é um arquivo XLSX válido.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 5_000_000
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        var document = LoadXml(archive, entry.FullName);
        return document.Root?.Elements(Main + "si")
            .Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value))).ToArray() ?? [];
    }

    private static string ReadCell(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(Main + "t").Select(text => text.Value)).Trim();
        var value = cell.Element(Main + "v")?.Value ?? "";
        if (type == "s" && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            return index >= 0 && index < sharedStrings.Count ? sharedStrings[index].Trim() : "";
        return value.Trim();
    }

    private static string ResolveWorkbookTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) throw new InvalidDataException("A planilha não contém um destino de aba válido.");
        var segments = new List<string>();
        var combined = target.StartsWith('/') || target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? target : $"xl/{target}";
        foreach (var segment in combined.Replace('\\', '/').Split('/'))
        {
            if (segment is "" or ".") continue;
            if (segment == "..")
            {
                if (segments.Count == 0) throw new InvalidDataException("O caminho de uma aba está fora do arquivo XLSX.");
                segments.RemoveAt(segments.Count - 1);
            }
            else segments.Add(segment);
        }
        return string.Join('/', segments);
    }

    private static int ColumnIndex(string cellReference)
    {
        var result = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsAsciiLetter(character)) break;
            result = checked(result * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        }
        return result;
    }

    private static string Cell(IReadOnlyDictionary<int, Dictionary<int, string>> rows, int row, int column) =>
        rows.TryGetValue(row, out var cells) ? Value(cells, column) : "";

    private static string Value(IReadOnlyDictionary<int, string> cells, int column) =>
        cells.TryGetValue(column, out var value) ? value : "";
}
