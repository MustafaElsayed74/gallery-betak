using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ElMasria.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ElMasria.Infrastructure.Services;

/// <summary>
/// Cloudinary implementation of IPhotoService for cloud image storage.
/// </summary>
public sealed class PhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    /// <summary>Initializes PhotoService with Cloudinary configuration.</summary>
    public PhotoService(IConfiguration config)
    {
        var cloudinarySettings = config.GetSection("CloudinarySettings");
        var account = new Account(
            cloudinarySettings["CloudName"],
            cloudinarySettings["ApiKey"],
            cloudinarySettings["ApiSecret"]);

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    /// <inheritdoc/>
    public async Task<(string Url, string PublicId)> UploadImageAsync(Stream fileStream, string fileName, string folder = "products")
    {
        if (fileStream.Length == 0)
            throw new ArgumentException("File stream is empty", nameof(fileStream));

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
            Folder = $"ElMasria/{folder}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}
