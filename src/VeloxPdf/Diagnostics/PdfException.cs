namespace VeloxPdf.Diagnostics;

public enum PdfErrorCode
{
    InvalidFont,
    CompressionFailed,
    InvalidImage,
    LayoutError,
    TemplateNotFound,
    WriteError,
    ParseError,
    InvalidInput,
    OutOfMemory
}

public sealed class PdfException : Exception
{
    public PdfErrorCode ErrorCode { get; }
    public int? ObjectNumber { get; }

    public PdfException(PdfErrorCode code, string message)
        : base(message) { ErrorCode = code; }

    public PdfException(PdfErrorCode code, string message, Exception inner)
        : base(message, inner) { ErrorCode = code; }

    public PdfException(PdfErrorCode code, string message, int objectNumber)
        : base(message) { ErrorCode = code; ObjectNumber = objectNumber; }
}
