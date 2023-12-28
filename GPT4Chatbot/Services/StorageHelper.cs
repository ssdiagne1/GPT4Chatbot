using Azure.Data.Tables;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace GPT4Chatbot.Services
{
    public class StorageHelper : IStorageHelper
    {
        private readonly IConfiguration _configuration;
        public StorageHelper(IConfiguration configuration) 
        { 
            _configuration = configuration;
        }

        // Create azure table storage service client
        private async Task<TableClient> GetTableClient(string tableName)
        {
            // New instance of the TableClient class
            TableServiceClient tableServiceClient = new TableServiceClient(_configuration.GetConnectionString("StorageAccount"));
            // New instance of TableClient class referencing the server-side table
            TableClient tableClient = tableServiceClient.GetTableClient(
                tableName: tableName
            );

            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        // insert entity into table
        public async Task InsertEntityAsync<T>(string tableName, T entity) where T : ITableEntity
        {
            try
            {
                TableClient tableClient = await GetTableClient(tableName);
                await tableClient.UpsertEntityAsync(entity);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while upsert entity -> " + ex.Message);
            }
        }

        // get entity from table
        public async Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                TableClient tableClient = await GetTableClient(tableName);
                return await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync("Error while getting entity-> " + ex.Message);
                return null;
            }
        }

        // delete entity from table
        public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
        {
            try
            {
                TableClient tableClient = await GetTableClient(tableName);
                await tableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while deleting entity -> " + ex.Message);
            }
        }
    }
}
