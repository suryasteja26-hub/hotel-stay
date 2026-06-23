using HotelStay.Api.Domain;
using HotelStay.Api.Services;

namespace HotelStay.Tests;

public class DocumentValidatorTests
{
    private static DocumentValidator CreateValidator() =>
        new(new DestinationRules());

    [Theory]
    [InlineData("London")]
    [InlineData("Manchester")]
    public void Domestic_destination_accepts_NationalId(string destination)
    {
        var result = CreateValidator().Validate(destination, DocumentType.NationalId);

        Assert.True(result.IsValid);
        Assert.Equal(DocumentValidationError.None, result.Error);
    }

    [Theory]
    [InlineData("Paris")]
    [InlineData("New York")]
    [InlineData("Tokyo")]
    public void International_destination_rejects_NationalId(string destination)
    {
        var result = CreateValidator().Validate(destination, DocumentType.NationalId);

        Assert.False(result.IsValid);
        Assert.Equal(DocumentValidationError.DocumentMismatch, result.Error);
    }

    [Theory]
    [InlineData("Paris")]
    [InlineData("New York")]
    [InlineData("Tokyo")]
    public void International_destination_accepts_Passport(string destination)
    {
        var result = CreateValidator().Validate(destination, DocumentType.Passport);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Unknown_destination_returns_clear_validation_error()
    {
        var result = CreateValidator().Validate("Atlantis", DocumentType.Passport);

        Assert.False(result.IsValid);
        Assert.Equal(DocumentValidationError.UnknownDestination, result.Error);
        Assert.Contains("Atlantis", result.Message);
    }
}
