using ClosedXML.Excel;

namespace FinNex.UI.Helpers;

// Sadə Excel (xlsx) export köməkçisi — PID/Məhkəmə grid səhifələri üçün.
// Başlıqlar + sətirlər (hər xana: string/decimal?/DateTime?/int/bool/null) → xlsx bayt massivi.
public static class ExcelExportHelper
{
    public static byte[] Yarat(string sheetAdi, string[] basliqlar, IEnumerable<object?[]> setirler)
    {
        using var wb = new XLWorkbook();
        // Sheet adı 31 simvoldan uzun ola bilməz (Excel limiti)
        var ad = sheetAdi.Length > 31 ? sheetAdi.Substring(0, 31) : sheetAdi;
        var ws = wb.Worksheets.Add(ad);

        // Başlıq sətri
        for (int c = 0; c < basliqlar.Length; c++)
            ws.Cell(1, c + 1).Value = basliqlar[c];
        var head = ws.Range(1, 1, 1, basliqlar.Length);
        head.Style.Font.Bold = true;
        head.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e2a3b");
        head.Style.Font.FontColor = XLColor.White;

        // Data sətirləri
        int row = 2;
        foreach (var setir in setirler)
        {
            for (int c = 0; c < setir.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                switch (setir[c])
                {
                    case null:
                        cell.Value = "";
                        break;
                    case string s:
                        cell.Value = s;
                        break;
                    case DateTime dt:
                        cell.Value = dt.ToString("dd.MM.yyyy");
                        break;
                    case decimal dec:
                        cell.Value = (double)dec;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case double dd:
                        cell.Value = dd;
                        cell.Style.NumberFormat.Format = "#,##0.00";
                        break;
                    case int ii:
                        cell.Value = (double)ii;
                        break;
                    case bool b:
                        cell.Value = b ? "Bəli" : "Xeyr";
                        break;
                    default:
                        cell.Value = setir[c]!.ToString() ?? "";
                        break;
                }
            }
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
