using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FinNex.UI.ViewModels.PR_Odenis_Tapsirigi;

namespace FinNex.UI.Services.PR_Odenis_Tapsirigi;

public static class OdenisTapsirigiWordService
{
    private const string FontName = "Times New Roman";
    private const string FontSize = "24"; // 12pt in half-points

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
                    Top = 1134,    // ~2cm
                    Right = 850,   // ~1.5cm
                    Bottom = 1134, // ~2cm
                    Left = 1418,   // ~2.5cm
                    Header = 720,
                    Footer = 720
                }
            );

            // Title
            body.AppendChild(CreateParagraph("ÖDƏNİŞ TAPŞIRIĞI", bold: true, fontSize: "32", center: true));
            body.AppendChild(CreateParagraph($"A-format · Daxili bank köçürməsi", fontSize: "20", center: true));
            body.AppendChild(CreateParagraph(""));
            body.AppendChild(CreateParagraph($"Tarix: {DateTime.Now:dd.MM.yyyy}", center: true));
            body.AppendChild(CreateParagraph(""));

            // Main table
            var table = CreatePaymentTable(dto);
            body.AppendChild(table);

            // Spacer
            body.AppendChild(CreateParagraph(""));
            body.AppendChild(CreateParagraph(""));

            // Signatures
            body.AppendChild(CreateSignatureSection());

            body.AppendChild(sectionProps);
        }

        return ms.ToArray();
    }

    private static Table CreatePaymentTable(OdenisTapsirigiWordDto dto)
    {
        var table = new Table();

        // Table properties
        var tblProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 6, Color = "333333" },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "333333" },
                new LeftBorder { Val = BorderValues.Single, Size = 6, Color = "333333" },
                new RightBorder { Val = BorderValues.Single, Size = 6, Color = "333333" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProps);

        // Column widths
        var tblGrid = new TableGrid(
            new GridColumn { Width = "3200" },
            new GridColumn { Width = "6800" }
        );
        table.AppendChild(tblGrid);

        // === ÖDƏYƏN BANK (A1) ===
        table.AppendChild(CreateSectionHeaderRow("ÖDƏYƏN BANK (A1)"));
        table.AppendChild(CreateDataRow("Bank adı", dto.OduyenBankAd));
        table.AppendChild(CreateDataRow("Kod", dto.OduyenBankKod));
        table.AppendChild(CreateDataRow("VÖEN", dto.OduyenBankVoen));
        table.AppendChild(CreateDataRow("Müxbir hesab", dto.OduyenBankMuxbirHesab));
        table.AppendChild(CreateDataRow("SWIFT / BIC", dto.OduyenBankSwift));

        // === ÖDƏYƏN MÜŞTƏRİ (A2) ===
        table.AppendChild(CreateSectionHeaderRow("ÖDƏYƏN MÜŞTƏRİ (A2)"));
        table.AppendChild(CreateDataRow("Müştəri adı", dto.OduyenMusteriAd));
        table.AppendChild(CreateDataRow("Hesab №", dto.OduyenMusteriHesab));
        table.AppendChild(CreateDataRow("VÖEN", dto.OduyenMusteriVoen));

        // === ALAN BANK (B1) ===
        table.AppendChild(CreateSectionHeaderRow("ALAN BANK (B1)"));
        table.AppendChild(CreateDataRow("Bank adı", dto.AlanBankAd));
        table.AppendChild(CreateDataRow("Kod", dto.AlanBankKod));
        table.AppendChild(CreateDataRow("VÖEN", dto.AlanBankVoen));
        table.AppendChild(CreateDataRow("Müxbir hesab", dto.AlanBankMuxbirHesab));
        table.AppendChild(CreateDataRow("SWIFT / BIC", dto.AlanBankSwift));

        // === ALAN MÜŞTƏRİ (B2) ===
        table.AppendChild(CreateSectionHeaderRow("ALAN MÜŞTƏRİ (B2)"));
        table.AppendChild(CreateDataRow("Müştəri adı", dto.AlanMusteriAd));
        table.AppendChild(CreateDataRow("Hesab №", dto.AlanMusteriHesab));
        table.AppendChild(CreateDataRow("VÖEN", dto.AlanMusteriVoen));

        // === MƏBLƏĞ ===
        table.AppendChild(CreateSectionHeaderRow("MƏBLƏĞ"));
        table.AppendChild(CreateDataRow("Valyuta", dto.Valyuta));
        table.AppendChild(CreateDataRow("Məbləğ", dto.Mebleg));
        table.AppendChild(CreateDataRow("Yazı ilə", dto.MeblegYazi));

        // === ÖDƏNİŞ TƏYİNATI ===
        table.AppendChild(CreateSectionHeaderRow("ÖDƏNİŞ TƏYİNATI"));
        table.AppendChild(CreateDataRow("Təyinat", dto.Teyinat));
        if (!string.IsNullOrWhiteSpace(dto.ElaveInfo))
        {
            table.AppendChild(CreateDataRow("Əlavə info", dto.ElaveInfo));
        }

        return table;
    }

    private static TableRow CreateSectionHeaderRow(string title)
    {
        var row = new TableRow();

        var cell = new TableCell();

        // Merge across 2 columns
        var cellProps = new TableCellProperties(
            new GridSpan { Val = 2 },
            new Shading
            {
                Color = "auto",
                Fill = "1a3a5c",
                Val = ShadingPatternValues.Clear
            },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        cell.AppendChild(cellProps);

        var para = new Paragraph();
        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines { Before = "60", After = "60" }
        );
        para.AppendChild(paraProps);

        var run = new Run();
        var runProps = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = FontSize },
            new Bold(),
            new Color { Val = "FFFFFF" }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(title));
        para.AppendChild(run);

        cell.AppendChild(para);
        row.AppendChild(cell);

        return row;
    }

    private static TableRow CreateDataRow(string label, string value)
    {
        var row = new TableRow();

        // Label cell
        var labelCell = new TableCell();
        var labelCellProps = new TableCellProperties(
            new Shading
            {
                Color = "auto",
                Fill = "F0F4F8",
                Val = ShadingPatternValues.Clear
            },
            new TableCellWidth { Width = "3200", Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        labelCell.AppendChild(labelCellProps);
        labelCell.AppendChild(CreateCellParagraph(label, bold: true));

        // Value cell
        var valueCell = new TableCell();
        var valueCellProps = new TableCellProperties(
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
        );
        valueCell.AppendChild(valueCellProps);
        valueCell.AppendChild(CreateCellParagraph(string.IsNullOrWhiteSpace(value) ? "—" : value));

        row.AppendChild(labelCell);
        row.AppendChild(valueCell);

        return row;
    }

    private static Paragraph CreateCellParagraph(string text, bool bold = false)
    {
        var para = new Paragraph();
        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines { Before = "40", After = "40" }
        );
        para.AppendChild(paraProps);

        var run = new Run();
        var runProps = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = "22" } // 11pt
        );
        if (bold) runProps.AppendChild(new Bold());
        run.AppendChild(runProps);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);

        return para;
    }

    private static Paragraph CreateParagraph(string text, bool bold = false, string fontSize = null!, bool center = false)
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

    private static Paragraph CreateSignatureSection()
    {
        var table = new Table();
        // We'll just use a simple paragraph-based layout for signatures

        var para = new Paragraph();
        var paraProps = new ParagraphProperties(
            new SpacingBetweenLines { Before = "200", After = "0" }
        );
        para.AppendChild(paraProps);

        // Return a container with signature lines
        var body = new Body();

        var lines = new[]
        {
            "Rəhbər:  _______________________                    Baş mühasib:  _______________________",
            "",
            "",
            "                                                         M.Y."
        };

        // We'll return just the first line as a paragraph; the caller can add more
        // Actually, let's create a single paragraph with the first signature line
        var run = new Run();
        var runProps = new RunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName },
            new FontSize { Val = FontSize }
        );
        run.AppendChild(runProps);
        run.AppendChild(new Text(lines[0]) { Space = SpaceProcessingModeValues.Preserve });
        para.AppendChild(run);

        return para;
    }
}
