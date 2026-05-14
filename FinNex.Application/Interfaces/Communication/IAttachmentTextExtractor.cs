namespace FinNex.Application.Interfaces.Communication;

public interface IAttachmentTextExtractor
{
    /// <summary>Returns extracted plain text (or null if unsupported / empty).</summary>
    string? Extract(string filePath, string contentType);
}
