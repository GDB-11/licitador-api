namespace Application.Core.Interfaces.Document;

public interface IWordTemplate
{
    byte[] FillTemplate(byte[] templateBytes, Dictionary<string, string> replacements);
}