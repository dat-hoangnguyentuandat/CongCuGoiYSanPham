using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace CongCuGoiYSanPham.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder = "products");
        string GetFileUrl(string publicId, bool isImage = false);
        Task<string> DeleteFileAsync(string publicId);
        Task<byte[]> DownloadFileAsync(string publicId);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly string _cloudName;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            _cloudName = configuration["Cloudinary:CloudName"];
            var account = new Account(
                _cloudName,
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder = "products")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file được chọn");

            using var stream = file.OpenReadStream();

            // Kiểm tra nếu là hình ảnh
            bool isImage = file.ContentType.StartsWith("image/");

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    UseFilename = true,
                    UniqueFilename = true,
                    Folder = folder,
                    Transformation = new Transformation()
                        .Quality("auto")
                        .FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return uploadResult.PublicId;
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    UseFilename = true,
                    UniqueFilename = true,
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams, "raw");
                return uploadResult.PublicId;
            }
        }

        public string GetFileUrl(string publicId, bool isImage = false)
        {
            if (isImage)
            {
                // Format cho hình ảnh: https://res.cloudinary.com/cloud_name/image/upload/public_id
                return $"https://res.cloudinary.com/{_cloudName}/image/upload/{publicId}";
            }
            else
            {
                // Format cho file tài liệu: https://res.cloudinary.com/cloud_name/raw/upload/public_id
                return $"https://res.cloudinary.com/{_cloudName}/raw/upload/{publicId}";
            }
        }

        public async Task<byte[]> DownloadFileAsync(string publicId)
        {
            try
            {
                var fileUrl = GetFileUrl(publicId, false);
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(fileUrl);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tải file với publicId: {publicId}");
                throw;
            }
        }

        public async Task<string> DeleteFileAsync(string publicId)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu xóa file với publicId: {publicId}");
                
                // Nếu publicId là URL, chuyển đổi thành publicId
                if (publicId.StartsWith("http"))
                {
                    var uri = new Uri(publicId);
                    var pathSegments = uri.AbsolutePath.Split('/');
                    var uploadIndex = Array.IndexOf(pathSegments, "upload");
                    if (uploadIndex != -1 && uploadIndex + 1 < pathSegments.Length)
                    {
                        publicId = string.Join("/", pathSegments.Skip(uploadIndex + 1));
                        _logger.LogInformation($"Đã chuyển đổi URL thành publicId: {publicId}");
                    }
                }

                // Thử xóa với ResourceType.Image trước
                try
                {
                    var deleteParams = new DeletionParams(publicId)
                    {
                        ResourceType = ResourceType.Image
                    };
                    var result = await _cloudinary.DestroyAsync(deleteParams);
                    _logger.LogInformation($"Kết quả xóa file với ResourceType.Image: {result.Result}");
                    if (result.Result == "ok")
                    {
                        return result.Result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Không thể xóa file với ResourceType.Image: {ex.Message}");
                }

                // Nếu không xóa được với Image hoặc kết quả không phải "ok", thử với Raw
                var rawDeleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Raw
                };
                var rawResult = await _cloudinary.DestroyAsync(rawDeleteParams);
                _logger.LogInformation($"Kết quả xóa file với ResourceType.Raw: {rawResult.Result}");
                return rawResult.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa file với publicId: {publicId}");
                throw;
            }
        }
    }
} 