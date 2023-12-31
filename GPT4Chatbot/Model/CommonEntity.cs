﻿using Azure;
using Azure.Data.Tables;
using System;

namespace GPT4Chatbot.Model
{
    public class CommonEntity : ITableEntity
    {
        public string RowKey { get; set; } = default!;

        public string PartitionKey { get; set; } = default!;

        public ETag ETag { get; set; } = default!;

        public DateTimeOffset? Timestamp { get; set; } = default!;
    }
}
