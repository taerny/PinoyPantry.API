namespace PinoyPantry.API.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteImageAsync(string blobUrl);
}
