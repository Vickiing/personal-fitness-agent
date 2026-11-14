using System.IO.Compression;
using System.Text;
using PersonalFitnessAgent.Core.Application;

namespace PersonalFitnessAgent.Core.Tests.Application;

public sealed class WorkoutSheetXlsxReaderTests
{
    [Fact]
    public void ReadsBothWorkoutTabsAndPreservesExerciseDetails()
    {
        using var workbook = CreateWorkbook();

        var sheet = WorkoutSheetXlsxReader.Read(workbook);

        Assert.Equal(new[] { "A", "B" }, sheet.Days.Select(day => day.Key));
        Assert.Equal("Plan A", sheet.Days[0].Title);
        Assert.Equal("3 sessões semanais", sheet.Days[0].Summary);
        var exercise = Assert.Single(sheet.Days[0].Exercises);
        Assert.Equal("Remada sentada", exercise.Name);
        Assert.Equal("3", exercise.Sets);
        Assert.Equal("8–12", exercise.Repetitions);
        Assert.Equal("75–90 s", exercise.Rest);
        Assert.Equal("Peito apoiado; tronco estável.", exercise.Instructions);
        Assert.Equal("Principal", exercise.Priority);
        Assert.Equal("20 kg/lado", exercise.ReferenceLoad);
        Assert.Contains("Aquecimento leve.", sheet.Days[0].Notes);
    }

    [Fact]
    public void RejectsWorkbookMissingEitherWorkoutTab()
    {
        using var workbook = CreateWorkbook(includeB: false);

        Assert.Throws<InvalidDataException>(() => WorkoutSheetXlsxReader.Read(workbook));
    }

    private static MemoryStream CreateWorkbook(bool includeB = true)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets>
                    <sheet name="Treino A" sheetId="1" r:id="rId1"/>
                    <sheet name="Treino B" sheetId="2" r:id="rId2"/>
                  </sheets>
                </workbook>
                """);
            Add(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Target="worksheets/sheet2.xml"/>
                </Relationships>
                """);
            Add(archive, "xl/worksheets/sheet1.xml", Worksheet("Plan A", "3 sessões semanais"));
            if (includeB) Add(archive, "xl/worksheets/sheet2.xml", Worksheet("Plan B", "Resumo B"));
        }
        stream.Position = 0;
        return stream;
    }

    private static string Worksheet(string title, string summary)
    {
        static string Cell(string address, string value) =>
            $"<c r=\"{address}\" t=\"inlineStr\"><is><t>{System.Security.SecurityElement.Escape(value)}</t></is></c>";
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
                <row r="1">{Cell("A1", title)}</row>
                <row r="4">{Cell("A4", summary)}</row>
                <row r="8">{Cell("A8", "1")}{Cell("B8", "Remada sentada")}{Cell("C8", "3")}{Cell("D8", "8–12")}{Cell("E8", "75–90 s")}{Cell("F8", "Peito apoiado; tronco estável.")}{Cell("G8", "Principal")}{Cell("H8", "20 kg/lado")}</row>
                <row r="17">{Cell("A17", "Aquecimento leve.")}</row>
              </sheetData>
            </worksheet>
            """;
    }

    private static void Add(ZipArchive archive, string path, string contents)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(contents);
    }
}
