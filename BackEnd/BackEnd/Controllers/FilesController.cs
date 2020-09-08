using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ILogger<FilesController> _logger;
        private readonly string _staticContentConnectionString; // Azure storage configuration.
        private readonly string _staticContentContainerName; // Azure storage container name.
        private readonly string _staticContentStorageFolder; // Azure storage folder name.
        private readonly string _staticContentAccountKey; // Azure storage account key.
        private readonly string _staticContentAzureUrl; // Azure storage endpoint.
        private readonly double _valetKeyDefaultLifeTime; // Static content hosting default Valet key duration.
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _staticContentContainer;

        public FilesController(IConfiguration configuration, ILogger<FilesController> logger)
        {
            _logger = logger;
            _staticContentConnectionString = configuration["ConnectionStrings:StaticContent.StorageConnectionString"];
            _staticContentContainerName = configuration["ConnectionStrings:StaticContent.StorageContainerName"];
            _staticContentStorageFolder = configuration["ConnectionStrings:StaticContent.StorageFolder"];
            _staticContentAccountKey = configuration["ConnectionStrings:StaticContent.AccountKey"];
            _staticContentAzureUrl = configuration["ConnectionStrings:StaticContent.AzureUrl"];
            _valetKeyDefaultLifeTime = double.Parse(configuration["ValetKey.DefaultLifeTime"]);
            _blobServiceClient = new BlobServiceClient(_staticContentConnectionString);
            _staticContentContainer = _blobServiceClient.GetBlobContainerClient(_staticContentContainerName);
        }

        [HttpGet("{username}")]
        // This method enables users to retrieve filename and static content hosting url of all their files.
        public IActionResult GetUserFilesInfo([FromRoute] string username)
        {
            double lifetime = _valetKeyDefaultLifeTime;
            string lifeTimeString = Request.Query["lifeTime"];
            if (lifeTimeString != string.Empty) { lifetime = double.Parse(lifeTimeString); }
            string userFolder = string.Format("{0}/{1}/", _staticContentStorageFolder, username);
            List<FileInfo> userFilesInfo = new List<FileInfo>();
            Pageable<BlobItem> blobs = _staticContentContainer.GetBlobs(prefix: userFolder);
            if (blobs.Count() > 0)
            {
                foreach (var blob in blobs)
                {
                    string fileName = blob.Name.Replace(userFolder, "");
                    userFilesInfo.Add(generateFileInfo(blob.Name, fileName, lifetime));
                }
                return Ok(userFilesInfo);
            }
            return NoContent();
        }

        [HttpDelete("{username}/{fileName}")]
        // This method enables users to remove files from their azure storage folder.
        public IActionResult DeleteUserFile([FromRoute] string username, string fileName)
        {
            string blobName = string.Format("{0}/{1}/{2}", _staticContentStorageFolder, username, fileName);
            var blob = _staticContentContainer.GetBlobClient(blobName);
            if (blob.DeleteIfExists()) { return Ok(fileName); }
            return NotFound(fileName);
        }

        [HttpPost("{username}")]
        // This method enables users to upload new files to their azure storage folder.
        public IActionResult PostUserFile([FromRoute] string username, [FromForm] IFormFile file)
        {
            var blobFile = _staticContentContainer.GetBlobClient(string.Format("{0}/{1}/{2}", _staticContentStorageFolder, username, file.FileName));
            bool overwrite = true;
            CancellationToken cancellationToken = default;
            blobFile.Upload(file.OpenReadStream(), overwrite, cancellationToken);
            return Ok(file.FileName);
        }

        // This method generates the Valet key used in the static content hosting url of a file.
        private FileInfo generateFileInfo(string blobName, string fileName, double lifeTime)
        {
            var storageSharedKeyCredential = new StorageSharedKeyCredential(_blobServiceClient.AccountName, _staticContentAccountKey);
            var blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _staticContentContainerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-lifeTime),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(lifeTime)
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sas = blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            string fileUrl = string.Format("{0}/{1}?{2}", _staticContentAzureUrl, blobName, sas);
            FileInfo file = new FileInfo
            {
                FileName = fileName,
                FileUrl = fileUrl
            };
            return file;
        }

        public class FileInfo
        {
            public string FileName { get; set; }
            public string FileUrl { get; set; }
        }
    }
}
