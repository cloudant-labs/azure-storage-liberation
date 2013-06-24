using Azure.Storage.Liberation;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Azure2CouchDB
{
    class DynamicTableEntityToCouchDBEntityConverter : DynamicTableEntityConverter
    {
        private readonly string tableName;

        public DynamicTableEntityToCouchDBEntityConverter(string tableName)
        {
            this.tableName = tableName;
        }

        protected override void WriteObjectProperties(JsonWriter writer, DynamicTableEntity entity)
        {
            GenerateUniqueId(writer, entity);

            WriteAzureMetaData(writer, entity);

            foreach (var property in entity.Properties)
            {
                WriteProperty(writer, property);
            }
        }

        private void GenerateUniqueId(JsonWriter writer, DynamicTableEntity entity)
        {
            var idValue = string.Format("{0}-{1}-{2}", tableName, entity.PartitionKey, entity.RowKey);
            writer.WritePropertyName("_id");
            writer.WriteValue(idValue);
        }

        private void WriteAzureMetaData(JsonWriter writer, DynamicTableEntity entity)
        {
            writer.WritePropertyName("AzureMetaData");
            writer.WriteStartObject();

            writer.WritePropertyName("Table");
            writer.WriteValue(tableName);

            writer.WritePropertyName("PartitionKey");
            writer.WriteValue(entity.PartitionKey);

            writer.WritePropertyName("RowKey");
            writer.WriteValue(entity.RowKey);

            writer.WritePropertyName("Timestamp");
            writer.WriteValue(entity.Timestamp);

            writer.WritePropertyName("ETag");
            writer.WriteValue(entity.ETag);

            writer.WriteEndObject();
        }
    }
}