using ChatApp_Server.Settings;
using FluentResults;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Security.AccessControl;

namespace ChatApp_Server.Services
{
    public interface IFireBaseCloudService
    {
        Task<Result<string>> UploadFile(string name, IFormFile file);
    }
    public class FireBaseCloudService: IFireBaseCloudService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName ;
        private readonly string _baseUrl ;
       
        public FireBaseCloudService(StorageClient storageClient, AppSettings appSettings)
        {
            _storageClient = storageClient;
            _bucketName = appSettings.BucketName;
            _baseUrl = appSettings.GoogleApi;
        }

        public async Task<Result<string>> UploadFile(string name, IFormFile file)
        {
            try
            {
                var randomGuid = Guid.NewGuid();
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                var objectName = $"{randomGuid}-{name}";
                var blob = await _storageClient.UploadObjectAsync(_bucketName,
                   objectName , file.ContentType, stream);

                var publicUrl = @$"{_baseUrl}/{_bucketName}/{blob.Name}";
                return Result.Ok(publicUrl);
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
    }
}
