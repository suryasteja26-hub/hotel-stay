using HotelStay.Api.Domain;

namespace HotelStay.Api.Services;

/// <summary>
/// Distinguishes "the request is malformed/unknown" (maps to 400) from
/// "the document is valid but not accepted for this destination" (maps to 422).
/// </summary>
public enum DocumentValidationError
{
    None,
    UnknownDestination,
    DocumentMismatch
}

public readonly record struct DocumentValidationResult(
    DocumentValidationError Error,
    string? Message)
{
    public bool IsValid => Error == DocumentValidationError.None;

    public static DocumentValidationResult Success() =>
        new(DocumentValidationError.None, null);

    public static DocumentValidationResult UnknownDestination(string destination) =>
        new(DocumentValidationError.UnknownDestination,
            $"Unknown destination '{destination}'.");

    public static DocumentValidationResult Mismatch(string message) =>
        new(DocumentValidationError.DocumentMismatch, message);
}

public sealed class DocumentValidator
{
    private readonly DestinationRules _destinationRules;

    public DocumentValidator(DestinationRules destinationRules)
    {
        _destinationRules = destinationRules;
    }

    public DocumentValidationResult Validate(string destination, DocumentType documentType)
    {
        if (!_destinationRules.IsKnownDestination(destination))
        {
            return DocumentValidationResult.UnknownDestination(destination);
        }

        if (_destinationRules.IsInternational(destination) && documentType != DocumentType.Passport)
        {
            return DocumentValidationResult.Mismatch(
                "International destinations require a Passport.");
        }

        // Domestic destinations accept National ID or Passport.
        return DocumentValidationResult.Success();
    }
}
