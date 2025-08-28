// File: Service/AzureStorageService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetail.Service
{
    public class AzureStorageService : IAzureStorageService
    {
        // Azure mode
        private readonly bool _useAzure;
        private readonly BlobContainerClient? _blobContainer;
        private readonly TableServiceClient? _tableService;

        // Local fallback
        private readonly string _localUploadsPath = "";
        private const string _localRequestPrefix = "/uploads";

        public AzureStorageService(IConfiguration config, IWebHostEnvironment env)
        {
            var connStr = config["AzureStorage:ConnectionString"]
                          ?? config.GetConnectionString("AzureStorage");
            var containerName = config["AzureStorage:BlobContainerName"];
            if (string.IsNullOrWhiteSpace(containerName)) containerName = "product-images";

            if (!string.IsNullOrWhiteSpace(connStr))
            {
                _useAzure = true;
                _blobContainer = new BlobContainerClient(connStr, containerName);
                _blobContainer.CreateIfNotExists(PublicAccessType.Blob);
                _tableService = new TableServiceClient(connStr);
            }
            else
            {
                _useAzure = false;
                var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                _localUploadsPath = Path.Combine(webRoot, "uploads");
                Directory.CreateDirectory(_localUploadsPath);
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;

        // ================== Blob ==================
        public async Task<string> UploadBlobAsync(IFormFile file, string? blobName = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided.", nameof(file));

            var ext = Path.GetExtension(file.FileName);
            var name = string.IsNullOrWhiteSpace(blobName) ? $"{Guid.NewGuid():N}{ext}" : blobName;

            if (_useAzure)
            {
                var client = _blobContainer!.GetBlobClient(name);
                await using var stream = file.OpenReadStream();
                await client.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType ?? "application/octet-stream" }
                }).ConfigureAwait(false);
                return client.Uri.ToString();
            }
            else
            {
                var fullPath = Path.Combine(_localUploadsPath, name);
                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await file.CopyToAsync(fs).ConfigureAwait(false);
                return $"{_localRequestPrefix}/{name}".Replace("\\", "/");
            }
        }

        public async Task<bool> DeleteBlobIfExistsAsync(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName)) return false;

            if (_useAzure)
            {
                var client = _blobContainer!.GetBlobClient(blobName);
                var result = await client.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots).ConfigureAwait(false);
                return result.Value;
            }
            else
            {
                var name = Path.GetFileName(blobName);
                var fullPath = Path.Combine(_localUploadsPath, name);
                if (File.Exists(fullPath)) { File.Delete(fullPath); return true; }
                return false;
            }
        }

        public async Task<(Stream Stream, string ContentType, string Name)> GetBlobAsync(string blobName)
        {
            if (_useAzure)
            {
                var client = _blobContainer!.GetBlobClient(blobName);
                var download = await client.DownloadContentAsync().ConfigureAwait(false);
                var ms = new MemoryStream(download.Value.Content.ToArray(), writable: false);
                var ct = download.Value.Details.ContentType ?? "application/octet-stream";
                return (ms, ct, client.Name);
            }
            else
            {
                var name = Path.GetFileName(blobName);
                var fullPath = Path.Combine(_localUploadsPath, name);
                if (!File.Exists(fullPath)) throw new FileNotFoundException(fullPath);
                var bytes = await File.ReadAllBytesAsync(fullPath).ConfigureAwait(false);
                return (new MemoryStream(bytes, writable: false), "application/octet-stream", name);
            }
        }

        public Task<(Stream Stream, string ContentType, string Name)> DownloadShareFileAsync(string blobName)
            => GetBlobAsync(blobName);

        public Task<(Stream Stream, string ContentType, string Name)> DownloadShareFileAsync(string containerName, string blobName)
            => GetBlobAsync(blobName);

        public async Task<IReadOnlyList<string>> ListBlobUrlsAsync()
        {
            var list = new List<string>();

            if (_useAzure)
            {
                await foreach (var item in _blobContainer!.GetBlobsAsync())
                    list.Add(_blobContainer.GetBlobClient(item.Name).Uri.ToString());
            }
            else
            {
                if (Directory.Exists(_localUploadsPath))
                {
                    foreach (var path in Directory.GetFiles(_localUploadsPath))
                        list.Add($"{_localRequestPrefix}/{Path.GetFileName(path)}".Replace("\\", "/"));
                }
            }

            return list;
        }

        // ================== Table ==================
        private TableClient RequireTable(string tableName)
        {
            if (_tableService == null)
                throw new NotSupportedException("Azure Table Storage is not configured.");
            var table = _tableService.GetTableClient(tableName);
            table.CreateIfNotExists();
            return table;
        }

        public async Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
            => await RequireTable(tableName).AddEntityAsync(entity).ConfigureAwait(false);

        public async Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
            => await RequireTable(tableName).UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace).ConfigureAwait(false);

        public async Task DeleteByRowKeyAsync(string tableName, string rowKey)
        {
            var table = RequireTable(tableName);
            await foreach (var e in table.QueryAsync<TableEntity>(filter: $"RowKey eq '{rowKey.Replace("'", "''")}'"))
            { await table.DeleteEntityAsync(e.PartitionKey, e.RowKey).ConfigureAwait(false); return; }
        }

        public async Task DeleteByRowKeyAsync(string tableName, string partitionKey, string rowKey)
            => await RequireTable(tableName).DeleteEntityAsync(partitionKey, rowKey).ConfigureAwait(false);

        public Task DeleteByRowKeyAsync<T>(string tableName, string rowKey) where T : class, ITableEntity, new()
            => DeleteByRowKeyAsync(tableName, rowKey);

        public Task DeleteByRowKeyAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
            => DeleteByRowKeyAsync(tableName, partitionKey, rowKey);

        public async Task<T?> GetByRowKeyAsync<T>(string tableName, string rowKey)
            where T : class, ITableEntity, new()
        {
            var table = RequireTable(tableName);
            await foreach (var e in table.QueryAsync<T>(filter: $"RowKey eq '{rowKey.Replace("'", "''")}'"))
                return e;
            return null;
        }

        public async Task<T?> GetByRowKeyAsync<T>(string tableName, string partitionKey, string rowKey)
            where T : class, ITableEntity, new()
        {
            try { var r = await RequireTable(tableName).GetEntityAsync<T>(partitionKey, rowKey).ConfigureAwait(false); return r.Value; }
            catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
        }

        // Interface EXACT signature (fixes CS0535)
        public async IAsyncEnumerable<T> QueryEntitiesAsync<T>(string tableName, string? filter = null)
            where T : class, ITableEntity, new()
        {
            if (_tableService is null)
                yield break;

            var table = _tableService.GetTableClient(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var query = table.QueryAsync<T>(filter: filter, maxPerPage: 1000);
            await foreach (var e in query)
                yield return e;
        }

        // Optional overload with cancellation (keep if you want)
        public async IAsyncEnumerable<T> QueryEntitiesAsync<T>(string tableName, string? filter,
            [EnumeratorCancellation] CancellationToken cancellationToken)
            where T : class, ITableEntity, new()
        {
            if (_tableService is null)
                yield break;

            var table = _tableService.GetTableClient(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var query = table.QueryAsync<T>(filter: filter, maxPerPage: 1000, select: null, cancellationToken: cancellationToken);
            await foreach (var e in query.WithCancellation(cancellationToken))
                yield return e;
        }

        public async Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            var list = new List<T>();
            await foreach (var e in QueryEntitiesAsync<T>(tableName)) list.Add(e);
            return list;
        }

        public async Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new()
            => await RequireTable(tableName).UpsertEntityAsync(entity, TableUpdateMode.Replace).ConfigureAwait(false);

        public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try { var r = await RequireTable(tableName).GetEntityAsync<T>(partitionKey, rowKey).ConfigureAwait(false); return r.Value; }
            catch (RequestFailedException ex) when (ex.Status == 404) { return null; }
        }
    }
}
