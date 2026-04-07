namespace GalleryBetak.Application.Interfaces;

/// <summary>
/// Service contract for handling image uploads to a cloud provider (e.g., Cloudinary).
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Uploads an image file to the cloud provider.
    /// </summary>
    /// <param name="fileStream">The file stream to upload.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="folder">Optional folder name in the cloud storage.</param>
    /// <returns>A tuple containing the generated image URL and the provider's public ID.</returns>
    Task<(string Url, string PublicId)> UploadImageAsync(Stream fileStream, string fileName, string folder = "products");

    /// <summary>
    /// Deletes an image from the cloud provider.
    /// </summary>
    /// <param name="publicId">The provider's public ID of the image.</param>
    /// <returns>True if deletion was successful.</returns>
    Task<bool> DeleteImageAsync(string publicId);
}

