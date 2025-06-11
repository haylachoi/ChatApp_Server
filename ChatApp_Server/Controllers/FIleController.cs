using ChatApp_Server.Services;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;
using System.Xml.Linq;

namespace ChatApp_Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FIleController(IImagesUploadService imagesUploadService) : ControllerBase
    {
        [HttpPost()]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            //var result = await fireBaseCloudService.UploadFile(file.FileName, file);
            var result = await imagesUploadService.UploadFile(file.FileName, file);
            if (result.IsFailed)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An error occurred while processing the request."
                };
                return new ObjectResult(problemDetails)
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
            return Ok(result.Value);
        }
    }
}
