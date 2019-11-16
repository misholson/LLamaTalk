using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace LLamaBot
{
    public class LLamaTable
    {
        CloudStorageAccount Account = null;
        CloudTableClient TableClient = null;
        CloudTable Table = null;

        public static DateTime Today
        {
            get
            {
                return DateTime.UtcNow.AddHours(-6).Date;
            }
        }

        private string GetDefaultPartitionKey(long groupId)
        {
            return $"{Today.ToString("yyyyMMdd")}_{groupId}";
        }

        public LLamaTable()
        {
            Account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureStorageConnectionString"));
            TableClient = Account.CreateCloudTableClient();
            Table = TableClient.GetTableReference("scores");
        }

        public async Task AddScore(long groupId, int userId, int score)
        {
            try
            {
                var entity = new LLamaEntity()
                {
                    PartitionKey = GetDefaultPartitionKey(groupId),
                    RowKey = userId.ToString(),
                    GroupId = groupId,
                    UserId = userId,
                    Score = score
                };

                var op = TableOperation.InsertOrReplace(entity);
                await Table.ExecuteAsync(op);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<LLamaEntity>> GetStatus(long groupId)
        {
            string filter = TableQuery.GenerateFilterCondition(nameof(LLamaEntity.PartitionKey), QueryComparisons.Equal, GetDefaultPartitionKey(groupId));
            var query = new TableQuery<LLamaEntity>().Where(filter);

            List<LLamaEntity> allResults = new List<LLamaEntity>();

            TableContinuationToken tct = null;
            do
            {
                var results = await Table.ExecuteQuerySegmentedAsync(query, tct);

                if (results != null)
                {
                    allResults.AddRange(results.Results);
                    tct = results.ContinuationToken;
                }
            } while (tct != null);

            return allResults;
        }
    }
}
