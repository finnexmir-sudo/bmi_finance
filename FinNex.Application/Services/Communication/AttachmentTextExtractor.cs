using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FinNex.Application.Interfaces.Communication;
using UglyToad.PdfPig;

namespace FinNex.Application.Services.Communication;

public class AttachmentTextExtractor : IAttachmentTextExtractor
{
    public string? Extract(string filePath, string contentType)
    {
        try
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".pdf" || contentType.Contains("pdf"))
                return ExtractPdf(filePath);

            if (ext == ".docx" || contentType.Contains("wordprocessingml") || contentType.Contains("msword"))
                return ExtractDocx(filePath);

            if (ext == ".xlsx" || contentType.Contains("spreadsheetml") || contentType.Contains("excel"))
                return ExtractXlsx(filePath);

            if (ext is ".txt" || contentType.StartsWith("text/plain"))
                return File.ReadAllText(filePath);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractPdf(string path)
    {
        using var doc = PdfDocument.Open(path);
        var sb = new System.Text.StringBuilder();
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.Length > 0 ? sb.ToString().Trim() : null;
    }

    private static string? ExtractDocx(string path)
    {
        using var docx = WordprocessingDocument.Open(path, false);
        var body = docx.MainDocumentPart?.Document?.Body;
        return body?.InnerText?.Trim();
    }

    private static string? ExtractXlsx(string path)
    {
        using var spreadsheet = SpreadsheetDocument.Open(path, false);
        var workbookPart = spreadsheet.WorkbookPart;
        if (workbookPart == null) return null;

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
        var sb = new System.Text.StringBuilder();

        foreach (var sheet in workbookPart.WorksheetParts)
        {
            foreach (var row in sheet.Worksheet.Descendants<Row>())
            {
                var cells = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    var val = cell.CellValue?.Text ?? "";
                    if (cell.DataType?.Value == CellValues.SharedString && sharedStrings != null)
                        val = sharedStrings.ElementAt(int.Parse(val)).InnerText;
                    cells.Add(val);
                }
                sb.AppendLine(string.Join("\t", cells));
            }
        }
        return sb.Length > 0 ? sb.ToString().Trim() : null;
    }
}
