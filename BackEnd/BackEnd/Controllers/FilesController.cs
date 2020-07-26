using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private readonly string _staticContentConnectionString;
        private readonly string _staticContentContainerName;
        private readonly string _staticContentStorageFolder;
        private readonly string _staticContentAccountKey;
        private readonly string _staticContentAzureUrl;
        private readonly double _valetKeyDefaultLifeTime;
        private readonly string _filesUrl;
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
            _filesUrl = configuration["URLs:Files.Download"];
            _blobServiceClient = new BlobServiceClient(_staticContentConnectionString);
            _staticContentContainer = _blobServiceClient.GetBlobContainerClient(_staticContentContainerName);
        }

        [HttpGet("{username}")]
        public IEnumerable<FileInfo> GetUserFilesInfo([FromRoute] string username)
        {
            string userFolder = string.Format("{0}/{1}/", _staticContentStorageFolder, username);
            List<FileInfo> userFilesInfo = new List<FileInfo>();
            Pageable<BlobItem> blobs = _staticContentContainer.GetBlobs(prefix: userFolder);
            foreach (var blob in blobs)
            {
                string fileName = blob.Name.Replace(userFolder, "");
                userFilesInfo.Add(generateFileInfo(username, blob.Name, fileName, _valetKeyDefaultLifeTime));
            }
            return userFilesInfo;
        }

        [HttpGet("{username}/{fileName}/{valetKey}")]
        public IActionResult DownLoadFile([FromRoute] string username, string fileName, string valetKey)
        {
            var valetKeySas = Encoding.UTF8.GetString(Convert.FromBase64String(valetKey));
            string azureUrl = string.Format("{0}/{1}/{2}/{3}?{4}", _staticContentAzureUrl, _staticContentStorageFolder, username, fileName, valetKeySas);
            byte[] responseBytes = null;
            using (var client = new HttpClient())
            {
                using var message = client.GetByteArrayAsync(azureUrl);
                responseBytes = message.Result;
            }
            if (responseBytes != null) { return File(responseBytes, MimeMapping.GetMimeMapping(fileName)); }
            return NotFound(fileName);
        }

        [HttpGet("refreshValetKey/{username}/{fileName}")]
        public FileInfo RefreshUserFileValetKey([FromRoute] string username, string fileName)
        {
            double lifetime = _valetKeyDefaultLifeTime;
            string lifeTimeString = Request.Query["lifeTime"];
            if (lifeTimeString != string.Empty) { lifetime = double.Parse(lifeTimeString); }
            string blobName = string.Format("{0}/{1}/{2}", _staticContentStorageFolder, username, fileName);
            var blob = _staticContentContainer.GetBlobClient(blobName);
            if (blob.Exists()) { return generateFileInfo(username, blob.Name, fileName, lifetime); }
            Response.StatusCode = (int) HttpStatusCode.NotFound;
            return new FileInfo { FileName = fileName, FileUrl = "404 Not Found." };
        }

        [HttpDelete("{username}/{fileName}")]
        public IActionResult DeleteUserFile([FromRoute] string username, string fileName)
        {
            string blobName = string.Format("{0}/{1}/{2}", _staticContentStorageFolder, username, fileName);
            var blob = _staticContentContainer.GetBlobClient(blobName);
            if (blob.DeleteIfExists()) { return Ok(fileName); }
            return NotFound(fileName);
        }

        [HttpPost("{username}")]
        public IActionResult PostUserFile([FromRoute] string username, [FromForm] IFormFile file)
        {
            var blobFile = _staticContentContainer.GetBlobClient(string.Format("{0}/{1}/{2}", _staticContentStorageFolder, username, file.FileName));
            bool overwrite = true;
            CancellationToken cancellationToken = default;
            blobFile.Upload(file.OpenReadStream(), overwrite, cancellationToken);
            return Ok(file.FileName);
        }

        private FileInfo generateFileInfo(string username, string blobName, string fileName, double lifeTime)
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
            var valetKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(sas));
            string fileUrl = string.Format("{0}/{1}/{2}/{3}", _filesUrl, username, fileName, valetKey);
            FileInfo file = new FileInfo
            {
                FileName = blobName,
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
