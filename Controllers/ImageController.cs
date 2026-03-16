using Microsoft.AspNetCore.Mvc;
using PinoyPantry.API.Services;

namespace PinoyPantry.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IBlobStorageService _blobService;
    private readonly IProductService _productService;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ImageController(IBlobStorageService blobService, IProductService productService)
    {
        _blobService = blobService;
        _productService = productService;
    }

    /// <summary>
    /// Upload an image and optionally attach it to a product.
    /// POST /api/image/upload?productId=2
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] int? productId)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { message = "File size exceeds 5 MB limit." });

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPEG, PNG, WebP, and GIF images are allowed." });

        using var stream = file.OpenReadStream();
        var imageUrl = await _blobService.UploadImageAsync(stream, file.FileName, file.ContentType);

        if (productId.HasValue)
        {
            var product = await _productService.GetByIdAsync(productId.Value);
            if (product == null)
                return NotFound(new { message = $"Product {productId} not found." });

            await _productService.UpdateImageUrlAsync(productId.Value, imageUrl);
        }

        return Ok(new { imageUrl, message = "Image uploaded successfully." });
    }
}
