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

                // Add numbering definitions (for lists/numbering)
                AddNumberingDefinitions(wordDocument);

                // Add header and footer
                AddHeaderAndFooter(mainPart, request);

                // Anexo 1
                AddAnexo1(body, request, companyDetails);
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

    private static void AddAnexo1(Body body, GenerateAnnexesRequest request, CompanyDetails companyDetails)
    {
        AddHeading(body, "ANEXO 1 - DECLARACIÓN JURADA");

        // Add a table with company information
        var table = CreateTable(new[]
        {
            ("Campo", "Valor"),
            ("Número de Licitación", request.LicitacionNumber),
            ("Entidad", request.EntityName),
            ("Ciudad", request.City)
        }, hasHeaderRow: true);
        body.AppendChild(table);

        AddParagraph(body, string.Empty);
        AddHeading(body, "DATOS DE LA EMPRESA:", fontSize: "24");

        // Company details table
        var companyTable = CreateTable(new[]
        {
            ("RUC", companyDetails.Company.Ruc),
            ("Razón Social", companyDetails.Company.RazonSocial),
            ("Domicilio Legal", companyDetails.Company.DomicilioLegal),
            ("Teléfono", companyDetails.Company.Telefono ?? "N/A"),
            ("Email", companyDetails.Company.Email),
            ("Es MYPE", companyDetails.Company.IsMype ? "Sí" : "No")
        });
        body.AppendChild(companyTable);

        AddParagraph(body, string.Empty);

        if (companyDetails.LegalRepresentative is not null)
        {
            AddHeading(body, "REPRESENTANTE LEGAL:", fontSize: "24");
            var legalRepTable = CreateTable(new[]
            {
                ("Nombre", companyDetails.LegalRepresentative.FullName),
                ("Tipo de Documento", companyDetails.LegalRepresentative.DocumentType),
                ("Número de Documento", companyDetails.LegalRepresentative.DocumentNumber)
            });
            body.AppendChild(legalRepTable);
            AddParagraph(body, string.Empty);
        }

        // Numbered list
        AddNumberedList(body, new[]
        {
            $"Objeto de la Contratación: {request.PurchaseObject}",
            request.AutorizaNotificacionesEmail && !string.IsNullOrWhiteSpace(request.EmailNotificaciones)
                ? $"Autoriza Notificaciones: Sí - Email: {request.EmailNotificaciones}"
                : "Autoriza Notificaciones: No"
        });
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

    /// <summary>
    /// Adds header and footer to the document
    /// </summary>
    private static void AddHeaderAndFooter(MainDocumentPart mainPart, GenerateAnnexesRequest request)
    {
        // Add header
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var header = new Header();
        var headerParagraph = new Paragraph(
            new Run(new DocumentFormat.OpenXml.Wordprocessing.Text($"Licitación: {request.LicitacionNumber} - {request.EntityName}"))
        );
        header.AppendChild(headerParagraph);
        headerPart.Header = header;

        // Add footer with page number
        var footerPart = mainPart.AddNewPart<FooterPart>();
        var footer = new Footer();
        var footerParagraph = new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Página ")),
            new Run(
                new FieldChar { FieldCharType = FieldCharValues.Begin }),
            new Run(new FieldCode(" PAGE ")),
            new Run(new FieldChar { FieldCharType = FieldCharValues.End })
        );
        footer.AppendChild(footerParagraph);
        footerPart.Footer = footer;

        // Link header and footer to document
        var sectionProperties = mainPart.Document.Body!.AppendChild(new SectionProperties());
        sectionProperties.AppendChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(headerPart) });
        sectionProperties.AppendChild(new FooterReference { Type = HeaderFooterValues.Default, Id = mainPart.GetIdOfPart(footerPart) });
    }

    #endregion
}