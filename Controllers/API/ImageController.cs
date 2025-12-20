using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CongCuGoiYSanPham.Services;

namespace CongCuGoiYSanPham.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ICloudinaryService cloudinaryService, ILogger<ImageController> logger)
        {
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<ActionResult<object>> UploadImage(IFormFile file, [FromQuery] string folder = "products")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Kiểm tra định dạng file
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    return BadRequest("Only image files are allowed (JPEG, PNG, GIF, WebP)");

                // Kiểm tra kích thước file (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                    return BadRequest("File size must be less than 10MB");

                var publicId = await _cloudinaryService.UploadFileAsync(file, folder);
                var imageUrl = _cloudinaryService.GetFileUrl(publicId, true);

                return Ok(new
                {
                    success = true,
                    publicId = publicId,
                    imageUrl = imageUrl,
                    message = "Image uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, new { success = false, message = "Error uploading image", error = ex.Message });
            }
        }

        [HttpDelete("{publicId}")]
        [Authorize]
        public async Task<ActionResult<object>> DeleteImage(string publicId)
        {
            try
            {
                var result = await _cloudinaryService.DeleteFileAsync(publicId);
                
                return Ok(new
                {
                    success = result == "ok",
                    message = result == "ok" ? "Image deleted successfully" : "Failed to delete image",
                    result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image with publicId: {PublicId}", publicId);
                return StatusCode(500, new { success = false, message = "Error deleting image", error = ex.Message });
            }
        }

        [HttpGet("url/{publicId}")]
        public ActionResult<object> GetImageUrl(string publicId, [FromQuery] bool isImage = true)
        {
            try
            {
                var imageUrl = _cloudinaryService.GetFileUrl(publicId, isImage);
                
                return Ok(new
                {
                    success = true,
                    imageUrl = imageUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image URL for publicId: {PublicId}", publicId);
                return StatusCode(500, new { success = false, message = "Error getting image URL", error = ex.Message });
            }
        }
    }
}