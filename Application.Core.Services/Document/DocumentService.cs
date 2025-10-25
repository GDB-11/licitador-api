using Application.Core.DTOs.Document;
using Application.Core.Interfaces.Document;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Global.Helpers.Functional;
using Global.Objects.Document;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Company;

namespace Application.Core.Services.Document;

public sealed class DocumentService : IDocument
{
    private readonly IUserRepository _userRepository;

    public DocumentService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<Result<byte[], DocumentError>> GenerateAnnexesAsync(Guid userId, GenerateAnnexesRequest request) =>
        _userRepository.GetUserFirstCompanyAsync(userId)
            .MapErrorAsync(error => (DocumentError)new DocumentRepositoryError(error.Message, error.Exception))
            .BindAsync(company => company is not null
                ? _userRepository.GetCompanyDetailsAsync(company.CompanyId)
                    .MapErrorAsync(error => (DocumentError)new DocumentRepositoryError(error.Message, error.Exception))
                    .BindAsync(details => details?.Company is not null
                        ? GenerateDocumentAsync(request, details)
                        : Task.FromResult(Result<byte[], DocumentError>.Failure(new DocumentCompanyNotFoundError())))
                : Task.FromResult(Result<byte[], DocumentError>.Failure(new DocumentCompanyNotFoundError())));

    private static Task<Result<byte[], DocumentError>> GenerateDocumentAsync(
        GenerateAnnexesRequest request,
        CompanyDetails companyDetails)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
            {
                var mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
                var body = mainPart.Document.AppendChild(new Body());
                var header = mainPart.Document.AppendChild(new Header());

                // Add numbering definitions (for lists/numbering)
                AddNumberingDefinitions(wordDocument);

                AddHeader(header, request);

                // Anexo 1 - Individual Company Format
                AddAnexo1IndividualCompany(body, request, companyDetails);
                AddPageBreak(body);

                // Anexo 2
                AddAnexo2(body, request, companyDetails);
                AddPageBreak(body);

                // Anexo 3
                AddAnexo3(body, request, companyDetails);

                mainPart.Document.Save();
            }

            return Task.FromResult(Result<byte[], DocumentError>.Success(memoryStream.ToArray()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<byte[], DocumentError>.Failure(
                new DocumentGenerationError($"Failed to generate DOCX file: {ex.Message}", ex)));
        }
    }

    private static void AddHeader(Header header, GenerateAnnexesRequest request)
    {
        // Entity name in italic
        AddItalicHeader(header, request.EntityName.ToUpper());
        AddItalicHeader(header, $"LICITACIÓN PÚBLICA N°{request.LicitacionNumber}");
    }

    private static void AddAnexo1IndividualCompany(Body body, GenerateAnnexesRequest request, CompanyDetails companyDetails)
    {
        // Title - ANEXO N° 1
        AddCenteredBoldParagraph(body, "ANEXO N° 1", "32");
        AddParagraph(body, string.Empty);

        // Subtitle
        AddCenteredBoldParagraph(body, "DECLARACIÓN JURADA DE DATOS DEL POSTOR", "24");
        AddParagraph(body, string.Empty);

        // Señores section
        AddParagraph(body, "Señores");
        AddBoldParagraph(body, "EVALUADORES");
        AddBoldParagraph(body, $"LICITACIÓN PÚBLICA DE OBRAS N° {request.LicitacionNumber}");
        AddParagraph(body, "Presente.-");
        AddParagraph(body, string.Empty);

        // Main declaration text
        var declarationText = $"El que se suscribe, {companyDetails.LegalRepresentative?.FullName} postor y/o representante Legal de " +
                            $"{companyDetails.Company.RazonSocial}, identificado con {companyDetails.LegalRepresentative?.DocumentType} N° " +
                            $"{companyDetails.LegalRepresentative?.DocumentNumber}, con poder inscrito en la localidad de " +
                            $"{request.City} en la Ficha N° 58009 Asiento N° 5800965 " +
                            $"DECLARO BAJO JURAMENTO que la siguiente información se sujeta a la verdad.";
        AddJustifiedParagraph(body, declarationText);
        AddParagraph(body, string.Empty);

        // Data table
        var dataTable = CreateDataTable(companyDetails);
        body.AppendChild(dataTable);
        AddParagraph(body, string.Empty);

        // Email authorization section
        AddBoldParagraph(body, "Autorización de notificación por correo electrónico:");
        AddParagraph(body, string.Empty);
        AddParagraph(body, "Autorizo que se notifiquen al correo electrónico indicado las siguientes actuaciones:");
        AddParagraph(body, string.Empty);

        // Numbered list of authorizations
        AddNumberedList(body, new[]
        {
            "Solicitud de la descripción a detalle de todos los elementos constitutivos de la oferta.",
            "Solicitud de negociación regulado en el numeral 167.4 del artículo 167 del Reglamento de la Ley N° 32069, Ley General de Contrataciones Públicas, aprobado por Decreto Supremo N° 009-2025-EF.",
            "Solicitud de subsanación de los requisitos para perfeccionar el contrato.",
            "Solicitud para presentar los documentos para perfeccionar el contrato, según orden de prelación, de conformidad con lo previsto en el artículo 91 del Reglamento de la Ley N° 32069, Ley General de Contrataciones Públicas, aprobado por Decreto Supremo N° 009-2025-EF.",
            "Respuesta a la solicitud de acceso al expediente de contratación."
        });

        AddParagraph(body, string.Empty);
        AddJustifiedParagraph(body, "Asimismo, me comprometo a remitir la confirmación de recepción del correo electrónico, en el plazo máximo de dos días hábiles de recibida la comunicación.");
        AddParagraph(body, string.Empty);

        // City and date
        AddBoldParagraph(body, "[CONSIGNAR CIUDAD Y FECHA]");
        AddParagraph(body, string.Empty);
        AddParagraph(body, string.Empty);
        AddParagraph(body, string.Empty);

        // Signature line
        AddCenteredParagraph(body, "____________________________________");
        AddCenteredParagraph(body, "Firma, nombres y apellidos del postor o");
        AddCenteredParagraph(body, "representante legal, según corresponda");
        AddParagraph(body, string.Empty);

        // Footer note
        var footerNote = "⁽¹⁾ Esta información será verificada por la entidad contratante en la página web del Ministerio de Trabajo y Promoción del Empleo " +
                        "(www.trabajo.gob.pe) o en la que haga sus veces. En caso de no estar inscrito o no contar con la [CÓDIGO DE CUENTA], de ser el caso, y si " +
                        "se tendrá en consideración, en caso el consorcio ganador de la buena pro solicite la retención del siete por ciento (7%) del " +
                        "monto del contrato, en calidad de garantía de fiel cumplimiento, según lo señalado en el artículo 114, del Reglamento.";
        AddSmallParagraph(body, footerNote);
    }

    private static Table CreateDataTable(CompanyDetails companyDetails)
    {
        var table = new Table();

        // Table properties with borders
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tableProperties);

        // Row 1: Nombre, Denominación o Razón Social
        var row1 = new TableRow();
        row1.AppendChild(CreateMergedCell($"Nombre, Denominación o Razón Social: {companyDetails.Company.RazonSocial}", 3));
        table.AppendChild(row1);

        // Row 2: Domicilio Legal
        var row2 = new TableRow();
        row2.AppendChild(CreateMergedCell($"Domicilio Legal: {companyDetails.Company.DomicilioLegal}", 3));
        table.AppendChild(row2);

        // Row 3: RUC and Teléfono(s)
        var row3 = new TableRow();
        row3.AppendChild(CreateTableCell($"RUC: {companyDetails.Company.Ruc}"));
        row3.AppendChild(CreateMergedCell("Teléfono(s):", 2));
        row3.AppendChild(CreateMergedCell(companyDetails.Company.Telefono ?? "", 3));
        row3.AppendChild(CreateMergedCell(string.Empty, 4));
        table.AppendChild(row3);

        // Row 4: MYPE
        var row4 = new TableRow();
        row4.AppendChild(CreateTableCell("MYPE⁽¹⁾"));
        row4.AppendChild(CreateTableCell(companyDetails.Company.IsMype ? "SÍ ( )" : "SÍ (   )"));
        row4.AppendChild(CreateTableCell(companyDetails.Company.IsMype ? "NO (   )" : "NO ( )"));
        table.AppendChild(row4);

        // Row 5: Correo electrónico
        var row5 = new TableRow();
        row5.AppendChild(CreateMergedCell("Correo electrónico:", 3));
        table.AppendChild(row5);

        var row5Value = new TableRow();
        row5Value.AppendChild(CreateMergedCell(companyDetails.Company.Email, 3));
        table.AppendChild(row5Value);

        return table;
    }

    private static void AddAnexo2(Body body, GenerateAnnexesRequest request, CompanyDetails companyDetails)
    {
        AddHeading(body, "ANEXO 2 - INFORMACIÓN COMPLEMENTARIA");

        var infoTable = CreateTable(new[]
        {
            ("Campo", "Información"),
            ("Número de Licitación", request.LicitacionNumber),
            ("Entidad", request.EntityName),
            ("Ciudad", request.City),
            ("Objeto", request.PurchaseObject)
        }, hasHeaderRow: true);
        body.AppendChild(infoTable);

        AddParagraph(body, string.Empty);
        AddHeading(body, "DATOS BANCARIOS:", fontSize: "24");

        if (companyDetails.BankAccount is not null)
        {
            var bankTable = CreateTable(new[]
            {
                ("Banco", companyDetails.BankAccount.BankName),
                ("Número de Cuenta", companyDetails.BankAccount.AccountNumber),
                ("CCI", companyDetails.BankAccount.CciCode)
            });
            body.AppendChild(bankTable);
        }
        else
        {
            AddParagraph(body, "No hay datos bancarios registrados");
        }

        AddParagraph(body, string.Empty);

        if (companyDetails.Company.FechaConstitucion.HasValue)
        {
            AddParagraph(body, $"Fecha de Constitución: {companyDetails.Company.FechaConstitucion.Value:dd/MM/yyyy}");
        }
    }

    private static void AddAnexo3(Body body, GenerateAnnexesRequest request, CompanyDetails companyDetails)
    {
        AddHeading(body, "ANEXO 3 - DECLARACIÓN DE COMPROMISO");

        var headerTable = CreateTable(new[]
        {
            ("Licitación", request.LicitacionNumber),
            ("Entidad", request.EntityName),
            ("Ciudad", request.City)
        });
        body.AppendChild(headerTable);

        AddParagraph(body, string.Empty);
        AddParagraph(body, $"Empresa: {companyDetails.Company.RazonSocial} (RUC: {companyDetails.Company.Ruc})");

        if (companyDetails.LegalRepresentative is not null)
        {
            AddParagraph(body, $"Representado por: {companyDetails.LegalRepresentative.FullName}");
            AddParagraph(body, $"{companyDetails.LegalRepresentative.DocumentType}: {companyDetails.LegalRepresentative.DocumentNumber}");
        }

        AddParagraph(body, string.Empty);
        AddParagraph(body, "Declara bajo juramento que:");

        // Add numbered list
        AddNumberedList(body, new[]
        {
            "Cumple con todos los requisitos establecidos en las bases.",
            "No tiene impedimento legal para contratar con el Estado.",
            "La información proporcionada es veraz y completa."
        });

        AddParagraph(body, string.Empty);
        AddParagraph(body, $"Fecha: {DateTime.UtcNow:dd/MM/yyyy}");
        AddParagraph(body, string.Empty);
        AddParagraph(body, "_________________________");
        AddParagraph(body, "Firma del Representante Legal");
    }

    #region Helper Methods

    private static void AddHeading(Body body, string text, string fontSize = "28")
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize { Val = fontSize });
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddBoldParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Bold());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddItalicHeader(Header header, string text)
    {
        var paragraph = header.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Italic());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddItalicParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Italic());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddCenteredParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddCenteredBoldParagraph(Body body, string text, string fontSize = "24")
    {
        var paragraph = body.AppendChild(new Paragraph());
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize { Val = fontSize });
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddJustifiedParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Both });
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddSmallParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new FontSize { Val = "16" });
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
    }

    private static void AddPageBreak(Body body)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Break { Type = BreakValues.Page });
    }

    /// <summary>
    /// Creates a table with the specified data
    /// </summary>
    /// <param name="data">Array of tuples (label, value)</param>
    /// <param name="hasHeaderRow">Whether the first row should be styled as a header</param>
    private static Table CreateTable((string Label, string Value)[] data, bool hasHeaderRow = false)
    {
        var table = new Table();

        // Table properties with borders
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct } // 100% width
        );
        table.AppendChild(tableProperties);

        for (int i = 0; i < data.Length; i++)
        {
            var (label, value) = data[i];
            bool isHeader = hasHeaderRow && i == 0;

            var row = new TableRow();

            // Label cell
            var labelCell = CreateTableCell(label, isHeader);
            row.AppendChild(labelCell);

            // Value cell
            var valueCell = CreateTableCell(value, isHeader);
            row.AppendChild(valueCell);

            table.AppendChild(row);
        }

        return table;
    }

    /// <summary>
    /// Creates a table cell with optional header styling
    /// </summary>
    private static TableCell CreateTableCell(string text, bool isHeader = false)
    {
        var cell = new TableCell();

        var paragraph = new Paragraph();
        var run = new Run();

        if (isHeader)
        {
            var runProperties = new RunProperties(
                new Bold(),
                new FontSize { Val = "22" }
            );
            run.AppendChild(runProperties);

            // Add gray shading for header
            var cellProperties = new TableCellProperties(
                new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = "D9D9D9"
                }
            );
            cell.AppendChild(cellProperties);
        }

        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
        paragraph.AppendChild(run);
        cell.AppendChild(paragraph);

        return cell;
    }

    /// <summary>
    /// Creates a merged table cell spanning multiple columns
    /// </summary>
    private static TableCell CreateMergedCell(string text, int colspan)
    {
        var cell = new TableCell();

        var cellProperties = new TableCellProperties();
        if (colspan > 1)
        {
            cellProperties.AppendChild(new GridSpan { Val = colspan });
        }
        cell.AppendChild(cellProperties);

        var paragraph = new Paragraph();
        var run = new Run();
        run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
        paragraph.AppendChild(run);
        cell.AppendChild(paragraph);

        return cell;
    }

    /// <summary>
    /// Adds a numbered list to the document
    /// </summary>
    private static void AddNumberedList(Body body, string[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = 0 },
                    new NumberingId { Val = 1 }
                )
            );
            paragraph.AppendChild(paragraphProperties);

            var run = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(items[i]));
            paragraph.AppendChild(run);

            body.AppendChild(paragraph);
        }
    }

    /// <summary>
    /// Adds numbering definitions to the document
    /// </summary>
    private static void AddNumberingDefinitions(WordprocessingDocument document)
    {
        var numberingPart = document.MainDocumentPart!.AddNewPart<NumberingDefinitionsPart>();
        var numbering = new Numbering();

        // Abstract numbering definition
        var abstractNum = new AbstractNum { AbstractNumberId = 1 };
        abstractNum.AppendChild(new MultiLevelType { Val = MultiLevelValues.SingleLevel });

        var level = new Level { LevelIndex = 0 };
        level.AppendChild(new StartNumberingValue { Val = 1 });
        level.AppendChild(new NumberingFormat { Val = NumberFormatValues.Decimal });
        level.AppendChild(new LevelText { Val = "%1." });
        level.AppendChild(new LevelJustification { Val = LevelJustificationValues.Left });

        abstractNum.AppendChild(level);
        numbering.AppendChild(abstractNum);

        // Numbering instance
        var numberingInstance = new NumberingInstance { NumberID = 1 };
        numberingInstance.AppendChild(new AbstractNumId { Val = 1 });
        numbering.AppendChild(numberingInstance);

        numberingPart.Numbering = numbering;
        numberingPart.Numbering.Save();
    }

    #endregion
}