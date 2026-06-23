namespace HotelStay.Api.Services;

/// <summary>
/// Classifies destinations as domestic or international. Matching is
/// case-insensitive. Adding a city is a data change here, not a flow change.
/// </summary>
public sealed class DestinationRules
{
    private static readonly HashSet<string> Domestic =
        new(StringComparer.OrdinalIgnoreCase) { "London", "Manchester" };

    private static readonly HashSet<string> International =
        new(StringComparer.OrdinalIgnoreCase) { "Paris", "New York", "Tokyo" };

    public bool IsKnownDestination(string destination) =>
        Domestic.Contains(destination) || International.Contains(destination);

    public bool IsInternational(string destination) =>
        International.Contains(destination);

    public bool IsDomestic(string destination) =>
        Domestic.Contains(destination);
}
