namespace WechatBotWeb.TableData
{
    using Microsoft.WindowsAzure.Storage.Table;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class TableEntityBag<T> where T : class, ITableEntity
    {
        public TableEntityBag(int httpStatusCode, T entity)
        {
            HttpStatusCode = httpStatusCode;
            Entity = entity;
        }

        public int HttpStatusCode { get; set; }
        public T Entity { get; set; }
    }

    public static class AzureTableUtility
    {
        public static async Task<int> InsertAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.Insert(entity));
            return result.HttpStatusCode;
        }

        public static async Task<int> InsertOrMergeAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.InsertOrMerge(entity));
            return result.HttpStatusCode;
        }

        public static async Task<int> InsertOrReplaceAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            return result.HttpStatusCode;
        }

        public static async Task<int> DeleteAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.Delete(entity));
            return result.HttpStatusCode;
        }

        public static async Task<int> MergeAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.Merge(entity));
            return result.HttpStatusCode;
        }

        public static async Task<int> ReplaceAsync<T>(this CloudTable table, T entity) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.Replace(entity));
            return result.HttpStatusCode;
        }

        public static async Task<TableEntityBag<T>> RetrieveAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var result = await table.ExecuteAsync(TableOperation.Retrieve<T>(partitionKey, rowKey));
            return new TableEntityBag<T>(result.HttpStatusCode, result.Result as T);
        }
    }
}
