// File: Service/IAzureStorageService.cs
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Azure.Data.Tables;

namespace ABCRetail.Service
{
    public interface IAzureStorageService
    {
        // Init (no-op for local)
        Task InitializeAsync();

        // -------- Blob --------
        Task<string> UploadBlobAsync(IFormFile file, string? blobName = null);
        Task<bool> DeleteBlobIfExistsAsync(string blobName);
        Task<(Stream Stream, string ContentType, string Name)> GetBlobAsync(string blobName);
        Task<(Stream Stream, string ContentType, string Name)> DownloadShareFileAsync(string blobName);
        Task<(Stream Stream, string ContentType, string Name)> DownloadShareFileAsync(string containerName, string blobName);
        Task<IReadOnlyList<string>> ListBlobUrlsAsync();

        // -------- Table --------
        Task AddEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task UpdateEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();

        Task DeleteByRowKeyAsync(string tableName, string rowKey);
        Task DeleteByRowKeyAsync(string tableName, string partitionKey, string rowKey);
        Task DeleteByRowKeyAsync<T>(string tableName, string rowKey) where T : class, ITableEntity, new();
        Task DeleteByRowKeyAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();

        Task<T?> GetByRowKeyAsync<T>(string tableName, string rowKey) where T : class, ITableEntity, new();
        Task<T?> GetByRowKeyAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();

        IAsyncEnumerable<T> QueryEntitiesAsync<T>(string tableName, string? filter = null) where T : class, ITableEntity, new();
        Task<List<T>> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();

        Task UpsertEntityAsync<T>(string tableName, T entity) where T : class, ITableEntity, new();
        Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();
    }
}
