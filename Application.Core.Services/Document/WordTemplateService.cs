using Application.Core.Interfaces.Document;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Application.Core.Services.Document;

public class WordTemplateService// : IWordTemplate
{
    public byte[] FillTemplate(byte[] templateBytes, Dictionary<string, string> replacements)
    {
        using var memoryStream = new MemoryStream();
        memoryStream.Write(templateBytes, 0, templateBytes.Length);
        
        using (var wordDoc = WordprocessingDocument.Open(memoryStream, true))
        {
            var body = wordDoc.MainDocumentPart!.Document.Body!;
            
            // Reemplazar en párrafos
            ReplaceInParagraphs(body.Descendants<Paragraph>(), replacements);
            
            // Reemplazar en tablas
            ReplaceInTables(body.Descendants<Table>(), replacements);
            
            // Reemplazar en headers
            foreach (var headerPart in wordDoc.MainDocumentPart.HeaderParts)
            {
                ReplaceInParagraphs(headerPart.Header.Descendants<Paragraph>(), replacements);
            }
            
            // Reemplazar en footers
            foreach (var footerPart in wordDoc.MainDocumentPart.FooterParts)
            {
                ReplaceInParagraphs(footerPart.Footer.Descendants<Paragraph>(), replacements);
            }
            
            wordDoc.MainDocumentPart.Document.Save();
        }
        
        return memoryStream.ToArray();
    }
    
    private void ReplaceInParagraphs(IEnumerable<Paragraph> paragraphs, Dictionary<string, string> replacements)
    {
        foreach (var paragraph in paragraphs)
        {
            var text = GetFullText(paragraph);
            
            foreach (var replacement in replacements)
            {
                if (text.Contains(replacement.Key))
                {
                    ReplaceTextInParagraph(paragraph, replacement.Key, replacement.Value);
                    text = GetFullText(paragraph); // Actualizar texto
                }
            }
        }
    }
    
    private void ReplaceInTables(IEnumerable<Table> tables, Dictionary<string, string> replacements)
    {
        foreach (var table in tables)
        {
            foreach (var cell in table.Descendants<TableCell>())
            {
                ReplaceInParagraphs(cell.Descendants<Paragraph>(), replacements);
            }
        }
    }
    
    private string GetFullText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
    }
    
    private void ReplaceTextInParagraph(Paragraph paragraph, string placeholder, string value)
    {
        var fullText = GetFullText(paragraph);
        
        if (!fullText.Contains(placeholder))
            return;
        
        // Preservar el formato del primer Run
        var firstRun = paragraph.Descendants<Run>().FirstOrDefault();
        var runProperties = firstRun?.RunProperties?.CloneNode(true) as RunProperties;
        
        // Remover todos los Runs
        paragraph.RemoveAllChildren<Run>();
        
        // Crear el texto reemplazado
        var newText = fullText.Replace(placeholder, value);
        
        // Crear nuevo Run con el texto reemplazado
        var newRun = new Run();
        if (runProperties != null)
        {
            newRun.RunProperties = runProperties;
        }
        newRun.AppendChild(new Text(newText));
        
        paragraph.AppendChild(newRun);
    }
}