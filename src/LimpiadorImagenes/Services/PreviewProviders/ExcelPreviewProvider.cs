using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class ExcelPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Excel;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (item.Extension is ".xls")
                return new PreviewResult
                {
                    TextContent = $"[ XLS ]\n\n{item.FileName}\n{item.FormattedSize}\n\n" +
                                  "Formato binario antiguo (.xls) — sin vista previa disponible."
                };

            try
            {
                using var doc = SpreadsheetDocument.Open(item.FullPath, false);
                var workbookPart = doc.WorkbookPart;
                if (workbookPart == null)
                    return new PreviewResult { TextContent = "[Archivo Excel vacío]" };

                var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault();
                if (sheet?.Id?.Value == null)
                    return new PreviewResult { TextContent = "[Sin hojas en el libro]" };

                var wsPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
                var rows = wsPart.Worksheet.Descendants<Row>().Take(50).ToList();

                if (rows.Count == 0)
                    return new PreviewResult { TextContent = "[Hoja vacía]" };

                var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
                var sb = new StringBuilder();

                var sheetName = sheet.Name?.Value ?? "Hoja 1";
                sb.AppendLine($"═══  {sheetName}  ═══\n");

                foreach (var row in rows)
                {
                    ct.ThrowIfCancellationRequested();
                    var cells = row.Elements<Cell>().ToList();
                    var values = cells.Select(c => GetCellValue(c, sharedStrings));
                    sb.AppendLine(string.Join("\t│\t", values));
                }

                return new PreviewResult { TextContent = sb.ToString() };
            }
            catch (Exception ex)
            {
                return new PreviewResult { TextContent = $"[Error al leer Excel: {ex.Message}]" };
            }
        }, ct);
    }

    private static string GetCellValue(Cell cell, SharedStringTable? sharedStrings)
    {
        var value = cell.CellValue?.InnerText ?? "";
        if (cell.DataType?.Value == CellValues.SharedString
            && sharedStrings != null
            && int.TryParse(value, out int idx))
        {
            return sharedStrings.ElementAt(idx).InnerText;
        }
        return value;
    }
}
