using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace LLamaBot
{
    public class LLamaEntity : TableEntity
    {
        public LLamaEntity()
        {
        }

        public long GroupId { get; set; }

        public int UserId { get; set; }

        public int Score { get; set; }
    }
}
