using System.Text.Json;
using System.Text.Json.Serialization;
using HotelStay.Api.Domain;

namespace HotelStay.Api.Storage;

public sealed class FileReservationStore : IReservationStore
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public FileReservationStore()
    {
        _basePath = Path.Combine(AppContext.BaseDirectory, "Data", "Reservations");
        Directory.CreateDirectory(_basePath);
        _options.Converters.Add(new JsonStringEnumConverter());
    }

    public void Save(Reservation reservation)
    {
        var path = Path.Combine(_basePath, SanitizeFileName(reservation.Reference) + ".json");
        var json = JsonSerializer.Serialize(reservation, _options);
        File.WriteAllText(path, json);
    }

    public Reservation? Get(string reference)
    {
        var path = Path.Combine(_basePath, SanitizeFileName(reference) + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Reservation>(json, _options);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
