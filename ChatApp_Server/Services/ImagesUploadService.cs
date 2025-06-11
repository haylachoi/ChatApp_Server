using ChatApp_Server.Helper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FluentResults;

namespace ChatApp_Server.Services
{
    public interface IImagesUploadService
    {
        Task<Result<string>> UploadFile(string name, IFormFile file);
    }

    public class ImagesUploadService : IImagesUploadService
    {
        private readonly Cloudinary _cloudinary;

        public ImagesUploadService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<Result<string>> UploadFile(string name, IFormFile file)
              => await ExceptionHandler.HandleLazy<string>(async () =>
              {
                  if (file == null || file.Length == 0)
                      return Result.Fail("File không hợp lệ.");

                  await using var stream = file.OpenReadStream();

                  var randomGuid = Guid.NewGuid();
                  var objectName = $"{randomGuid}-{name}";
                  var uploadParams = new ImageUploadParams
                  {
                      File = new FileDescription(name, stream),
                      PublicId = objectName, // Tùy chọn: đặt tên ảnh
                      Folder = "chatapp", // Tùy chọn: đặt folder trong Cloudinary
                      UseFilename = true,
                      //UniqueFilename = true,
                      Overwrite = false,
                  };

                  var result = await _cloudinary.UploadAsync(uploadParams);

                  if (result.StatusCode == System.Net.HttpStatusCode.OK)
                      return Result.Ok(result.SecureUrl.ToString());

                  return Result.Fail("Upload thất bại: " + result.Error?.Message);
              });
    }
}
