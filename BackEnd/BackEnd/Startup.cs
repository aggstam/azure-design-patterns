using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Core.Extensions;
using Azure.Storage.Blobs.Models;
using System.Threading;
using System.IO;
using BackEnd.Models;
using Microsoft.Extensions.Options;
using BackEnd.Authentication;

namespace BackEnd
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string staticContentConnectionString = Configuration["ConnectionStrings:StaticContent.StorageConnectionString"];
            string staticContentContainerName = Configuration["ConnectionStrings:StaticContent.StorageContainerName"];

            services.Configure<UsersDatabaseSettings>(Configuration.GetSection(nameof(UsersDatabaseSettings)));
            services.AddSingleton<IUsersDatabaseSettings>(sp => sp.GetRequiredService<IOptions<UsersDatabaseSettings>>().Value);
            services.AddScoped<IUserService, UserService>();
            services.AddControllers();
            services.AddAzureClients(builder =>
            {
                builder.AddBlobServiceClient(staticContentConnectionString, preferMsi: true);
                builder.AddQueueServiceClient(staticContentConnectionString, preferMsi: true);
            });

            DeployStaticContent(staticContentConnectionString, staticContentContainerName);
        }

        // This methid is used to initialize the Azure storage container with private access permissions on the blobs.
        private void DeployStaticContent(string staticContentConnectionString, string staticContentContainerName)
        {

            var blobServiceClient = new BlobServiceClient(staticContentConnectionString);
            var staticContentContainer = blobServiceClient.GetBlobContainerClient(staticContentContainerName);
            staticContentContainer.DeleteIfExists();
            staticContentContainer.CreateIfNotExists(PublicAccessType.None);

            // Uploads dummy files used for testing.
            var userFolders = Directory.GetDirectories(Configuration["ConnectionStrings:StaticContent.StorageFolder"]);
            foreach (var userFolder in userFolders)
            {
                var userImages = Directory.GetFiles(userFolder);
                foreach (var imageFile in userImages)
                {
                    var fileName = Path.GetFileName(imageFile);
                    if (fileName == null) { return; }
                    var blobFile = staticContentContainer.GetBlobClient(string.Format("{0}/{1}", userFolder, fileName));
                    bool overwrite = true;
                    CancellationToken cancellationToken = default;
                    blobFile.Upload(imageFile, overwrite, cancellationToken);
                }
            }            
        }

        // This method gets called by the runtime to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
    internal static class StartupExtensions
    {
        public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri)) { return builder.AddBlobServiceClient(serviceUri); }
            return builder.AddBlobServiceClient(serviceUriOrConnectionString);
        }
        public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi)
        {
            if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri serviceUri)) { return builder.AddQueueServiceClient(serviceUri); }
            return builder.AddQueueServiceClient(serviceUriOrConnectionString);
        }
    }
}
