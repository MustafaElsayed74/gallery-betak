using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using GalleryBetak.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GalleryBetak.Infrastructure.Services;

/// <summary>
/// Cloudinary implementation of IPhotoService for cloud image storage.
/// </summary>
public sealed class PhotoService : IPhotoService
{
    private readonly Cloudinary? _cloudinary;
    private readonly bool _isConfigured;

    /// <summary>Initializes PhotoService with Cloudinary configuration.</summary>
    public PhotoService(IConfiguration config)
    {
        var cloudinarySettings = config.GetSection("CloudinarySettings");
        var cloudName = cloudinarySettings["CloudName"];
        var apiKey = cloudinarySettings["ApiKey"];
        var apiSecret = cloudinarySettings["ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName)
            || string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret))
        {
            _isConfigured = false;
            return;
        }

        var account = new Account(
            cloudName,
            apiKey,
            apiSecret);

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
        _isConfigured = true;
    }

    /// <inheritdoc/>
    public async Task<(string Url, string PublicId)> UploadImageAsync(Stream fileStream, string fileName, string folder = "products")
    {
        if (!_isConfigured || _cloudinary is null)
            throw new InvalidOperationException("Cloudinary settings are not configured.");

        if (fileStream.Length == 0)
            throw new ArgumentException("File stream is empty", nameof(fileStream));

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Transformation = new Transformation().Quality("auto").FetchFormat("auto"),
            Folder = $"GalleryBetak/{folder}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

        return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (!_isConfigured || _cloudinary is null)
            throw new InvalidOperationException("Cloudinary settings are not configured.");

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }
}

