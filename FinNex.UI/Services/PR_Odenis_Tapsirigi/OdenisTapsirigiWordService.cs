using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Services.PR_Odenis_Tapsirigi;

public static class OdenisTapsirigiWordService
{
    private const string FontName = "Times New Roman";
    private const string FontSize = "22"; // 10pt in half-points
    private const string SmallFontSize = "26"; // 9pt

    public static byte[] Generate(OdenisTapsirigiWordDto dto)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Page settings (A4)
            var sectionProps = new SectionProperties(
                new PageSize { Width = 11906, Height = 16838, Orient = PageOrientationValues.Portrait },
                new PageMargin
                {
                    Top = 850,
                    Right = 850,
                    Bottom = 850,
                    Left = 1134,
                    Header = 720,
                    Footer = 720
                }
            );

            // Header: "A" formatli odenis tapshirigi No XXXXXX
            var nomreText = string.IsNullOrWhiteSpace(dto.Nomre) ? "______" : dto.Nomre;
            body.AppendChild(CreateParagraph(
                $"\"A\" formatli odenis tapshirigi  No  {nomreText}",
                bold: true, fontSize: "24", center: true));

            // Tarix
            var tarixText = string.IsNullOrWhiteSpace(dto.Tarix) ? DateTime.Now.ToString("dd MMMM yyyy") : dto.Tarix;
            body.AppendChild(CreateParagraph(tarixText + " ci il", center: true));
            body.AppendChild(CreateParagraph(""));

            // Main table with 4 columns: label | value | label | value
            var table = CreateMainTable(dto);
            body.AppendChild(table);

            // Spacer
            body.AppendChild(CreateParagraph(""));

            // Signature section
            AddSignatureSection(body);

            body.AppendChild(sectionProps);
        }

        return ms.ToArray();
    }

    private static Table CreateMainTable(OdenisTapsirigiWordDto dto)
    {
        var table = new Table();

        var tblProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 6, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 6, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProps);

        // 4 columns: label-left, value-left, label-right, value-right
        var tblGrid = new TableGrid(
            new GridColumn { Width = "1200" },
            new GridColumn { Width = "3800" },
            new GridColumn { Width = "1200" },
            new GridColumn { Width = "3800" }
        );
        table.AppendChild(tblGrid);

        // === ROW: A1 Emitent bank header | B1 Benefisiar bank header ===
        table.AppendChild(Create2ColHeaderRow("A1. Emitent(oduyun) bank", "B1. Benefisiar bank"));

        // Bank Adi
        table.AppendChild(Create4ColRow("Adi:", dto.OduyenBankAd, "Adi:", dto.AlanBankAd));
        // Kod
        table.AppendChild(Create4ColRow("Kod", dto.OduyenBankKod, "Kod", dto.AlanBankKod));
        // VOEN
        table.AppendChild(Create4ColRow("VOEN", dto.OduyenBankVoen, "VOEN", dto.AlanBankVoen));
        // M/hes
        table.AppendChild(Create4ColRow("M/hes", dto.OduyenBankMuxbirHesab, "M/hes", dto.AlanBankMuxbirHesab));
        // SWIFT
        table.AppendChild(Create4ColRow("SWIFT", dto.OduyenBankSwift, "SWIFT", dto.AlanBankSwift));

        // === ROW: A2 Oduyun mushteri header | B2 Alan mushteri header ===
        table.AppendChild(Create2ColHeaderRow("A2. Oduyun mushteri", "B2. Alan mushteri"));

        // Mushteri Adi
        table.AppendChild(Create4ColRow("Adi:", dto.OduyenMusteriAd, "Adi:", dto.AlanMusteriAd));
        // Hesab
        table.AppendChild(Create4ColRow("Hesab", dto.OduyenMusteriHesab, "Hesab", dto.AlanMusteriHesab));
        // VOEN
        table.AppendChild(Create4ColRow("VOEN", dto.OduyenMusteriVoen, "VOEN", dto.AlanMusteriVoen));

        // === C1. Valyuta novu === (full width)
        table.AppendChild(CreateFullWidthRow($"C1.  Valyuta novu    {dto.Valyuta}"));

        // === C2. Kocurulen mebleg ===
        table.AppendChild(CreateFullWidthRow($"C2.  Kocurulen mebleg"));
        table.AppendChild(CreateFullWidthRow($"     Mebleg reqemle: {dto.Mebleg}"));
        table.AppendChild(CreateFullWidthRow($"     Mebleg yazi ile:  {dto.MeblegYazi}"));

        // === D1. Odenishin teyinati ve esasi ===
        table.AppendChild(CreateFullWidthRow("D1. Odenishin teyinati ve esasi:"));
        table.AppendChild(CreateFullWidthRow(dto.Teyinat));

        // === D2. Odenishle elaqedar elave informasiya ===
        table.AppendChild(CreateFullWidthRow("D2. Odenishle elaqedar elave informasiya:"));
        if (!string.IsNullOrWhiteSpace(dto.ElaveInfo))
        {
            table.AppendChild(CreateFullWidthRow(dto.ElaveInfo));
        }
        else
        {
            table.AppendChild(CreateFullWidthRow(""));
        }

        // === D3 / D4 in same row ===
        table.AppendChild(CreateD3D4Row(dto.BudceTesnifatininKodu, dto.BudceSeviyyesininKodu));

        return table;
    }

    /// <summary>
    /// Creates a row with 2 merged header cells (left half = A, right half = B)
    /// </summary>
    private static TableRow Create2ColHeaderRow(string leftTitle, string rightTitle)
    {
        var row = new TableRow();

        // Left header (spans cols 1-2)
        var leftCell = new TableCell();
        leftCell.AppendChild(new TableCellProperties(
            new GridSpan { Val = 2 },
            new Shading { Color = "auto", Fill = "E0E0E0", Val = ShadingPatternValues.Clear }
        ));
        leftCell.AppendChild(CreateCellParagraph(leftTitle, bold: true));

        // Right header (spans cols 3-4)
        var rightCell = new TableCell();
        rightCell.AppendChild(new TableCellProperties(
            new GridSpan { Val = 2 },
            new Shading { Color = "auto", Fill = "E0E0E0", Val = ShadingPatternValues.Clear }
        ));
        rightCell.AppendChild(CreateCellParagraph(rightTitle, bold: true));

        row.AppendChild(leftCell);
        row.AppendChild(rightCell);
        return row;
    }

    /// <summary>
    /// Creates a 4 column data row
    /// </summary>
    private static TableRow Create4ColRow(string label1, string val1, string label2, string val2)
    {
        var row = new TableRow();

        row.AppendChild(CreateLabelCell(label1));
        row.AppendChild(CreateValueCell(val1));
        row.AppendChild(CreateLabelCell(label2));
        row.AppendChild(CreateValueCell(val2));

        return row;
    }

    /// <summary>
    /// Full width row spanning all 4 columns
    /// </summary>
    private static TableRow CreateFullWidthRow(string text)
    {
        var row = new TableRow();
        var cell = new TableCell();
        cell.AppendChild(new TableCellProperties(
            new GridSpan { Val = 4 }
        ));
        cell.AppendChild(CreateCellParagraph(text));
        row.AppendChild(cell);
        return row;
    }

    /// <summary>
    /// D3 and D4 in a split row
    /// </summary>
    private static TableRow CreateD3D4Row(string d3, string d4)
    {
        var row = new TableRow();

        // D3 spans left 3 cols
        var d3Cell = new TableCell();
        d3Cell.AppendChild(new TableCellProperties(
            new GridSpan { Val = 3 }
        ));
        d3Cell.AppendChild(CreateCellParagraph($"D3. Budce tesnifatinin kodu:   {d3}"));

        // D4 spans right 1 col
        var d4Cell = new TableCell();
        d4Cell.AppendChild(CreateCellParagraph($"D4. Budce seviyyesinin kodu:  {d4}"));

        row.AppendChild(d3Cell);
        row.AppendChild(d4Cell);
        return row;
    }

    private static TableCell CreateLabelCell(string text)
    {
        var cell = new TableCell();
        var cellProps = new TableCellProperties(
            new Shading { Color = "auto", Fill = "F5F5F5", Val = ShadingPatternValues.Clear },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        cell.AppendChild(cellProps);
        cell.AppendChild(CreateCellParagraph(text, bold: true));
        return cell;
    }

    private static TableCell CreateValueCell(string text)
    {
        var cell = new TableCell();
        var cellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        cell.AppendChild(cellProps);
        cell.AppendChild(CreateCellParagraph(string.IsNullOrWhiteSpace(text) ? "" : text));
        return cell;
    }

    private static Paragraph CreateCellParagraph(string text, bool bold = false)
    {
        var para = new Paragraph();
        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines { Before = "20", After = "20" }
        );
        para.AppendChild(paraProps);

        var run = new Run();
        var runProps = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = FontSize }
        );
        if (bold) runProps.AppendChild(new Bold());
        run.AppendChild(runProps);
        run.AppendChild(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);

        return para;
    }

    private static Paragraph CreateParagraph(string text, bool bold = false, string? fontSize = null, bool center = false)
    {
        fontSize ??= FontSize;

        var para = new Paragraph();
        var paraProps = new ParagraphProperties();

        if (center)
            paraProps.AppendChild(new Justification { Val = JustificationValues.Center });

        paraProps.AppendChild(new SpacingBetweenLines { Before = "40", After = "40" });
        para.AppendChild(paraProps);

        if (string.IsNullOrEmpty(text))
            return para;

        var run = new Run();
        var runProps = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = fontSize }
        );
        if (bold) runProps.AppendChild(new Bold());
        run.AppendChild(runProps);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);

        return para;
    }

    private static void AddSignatureSection(Body body)
    {
        // Mushterinin imzalari
        body.AppendChild(CreateParagraph(""));

        var sigTable = new Table();
        var sigTblProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        sigTable.AppendChild(sigTblProps);

        var sigGrid = new TableGrid(
            new GridColumn { Width = "5000" },
            new GridColumn { Width = "5000" }
        );
        sigTable.AppendChild(sigGrid);

        // Mushterinin imzalari header
        var headerRow = new TableRow();
        var headerCell = new TableCell();
        headerCell.AppendChild(new TableCellProperties(new GridSpan { Val = 2 }));
        headerCell.AppendChild(CreateCellParagraph("Mushterinin imzalari:", bold: true));
        headerRow.AppendChild(headerCell);
        sigTable.AppendChild(headerRow);

        // M.Y. | 1. / 2.
        var sigRow1 = new TableRow();
        var myCell1 = new TableCell();
        myCell1.AppendChild(CreateCellParagraph("M. Y."));
        var sigLines1 = new TableCell();
        sigLines1.AppendChild(CreateCellParagraph("1.\n2."));
        sigRow1.AppendChild(myCell1);
        sigRow1.AppendChild(sigLines1);
        sigTable.AppendChild(sigRow1);

        // Bankin imzalari header
        var bankHeaderRow = new TableRow();
        var bankHeaderCell = new TableCell();
        bankHeaderCell.AppendChild(new TableCellProperties(new GridSpan { Val = 2 }));
        bankHeaderCell.AppendChild(CreateCellParagraph("Bankin imzalari:", bold: true));
        bankHeaderRow.AppendChild(bankHeaderCell);
        sigTable.AppendChild(bankHeaderRow);

        // M.Y. | 1. / 2.
        var sigRow2 = new TableRow();
        var myCell2 = new TableCell();
        myCell2.AppendChild(CreateCellParagraph("M. Y."));
        var sigLines2 = new TableCell();
        sigLines2.AppendChild(CreateCellParagraph("1.\n2."));
        sigRow2.AppendChild(myCell2);
        sigRow2.AppendChild(sigLines2);
        sigTable.AppendChild(sigRow2);

        body.AppendChild(sigTable);

        // Icra qeydi
        body.AppendChild(CreateParagraph(""));
        body.AppendChild(CreateParagraph("Icra qeydi:", bold: true));
        body.AppendChild(CreateParagraph(
            "Vesaitin qeyd olunan rekvizitler uzre kocurulmesini oz imza ve muhurumuzle tesdiq edirik.",
            fontSize: SmallFontSize));
    }
}
