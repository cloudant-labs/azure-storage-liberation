using System;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Azure.Storage.Liberation
{
    /// <summary>
    /// A JSON.NET converter for DynamicTableEntity. 
    /// </summary>
    /// <remarks> 
    /// Properties are generatied for PartitionKey, RowKey, Timestamp and all custom properties in the row. Null properies are ignored (since rows are schema-free).
    /// </remarks>
    public class DynamicTableEntityConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WriteObjectProperties(writer, (DynamicTableEntity)value);

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(DynamicTableEntity).IsAssignableFrom(objectType);
        }

        protected virtual void WriteObjectProperties(JsonWriter writer, DynamicTableEntity entity)
        {
            writer.WritePropertyName("PartitionKey");
            writer.WriteValue(entity.PartitionKey);

            writer.WritePropertyName("RowKey");
            writer.WriteValue(entity.RowKey);

            writer.WritePropertyName("Timestamp");
            writer.WriteValue(entity.Timestamp);

            foreach (var property in entity.Properties)
            {
                WriteProperty(writer, property);
            }
        }

        protected virtual void WriteProperty(JsonWriter writer, KeyValuePair<string, EntityProperty> property)
        {
            switch (property.Value.PropertyType)
            {
                case EdmType.String:
                    WritePropertyNameValue(writer, property.Key, property.Value.StringValue);
                    break;
                case EdmType.Boolean:
                    WritePropertyNameValue(writer, property.Key, property.Value.BooleanValue);
                    break;
                case EdmType.DateTime:
                    WritePropertyNameValue(writer, property.Key, property.Value.DateTimeOffsetValue);
                    break;
                case EdmType.Double:
                    WritePropertyNameValue(writer, property.Key, property.Value.DoubleValue);
                    break;
                case EdmType.Guid:
                    WritePropertyNameValue(writer, property.Key, property.Value.GuidValue);
                    break;
                case EdmType.Int32:
                    WritePropertyNameValue(writer, property.Key, property.Value.Int32Value);
                    break;
                case EdmType.Int64:
                    WritePropertyNameValue(writer, property.Key, property.Value.Int64Value);
                    break;
                case EdmType.Binary:
                    WritePropertyNameValue(writer, property.Key, property.Value.BinaryValue);
                    break;
            }
        }

        private void WritePropertyNameValue<TValue>(JsonWriter writer, string key, TValue value)
            where TValue : class
        {
            // exclude null properties since objects may have different schemas
            if (value != null)
            {
                writer.WritePropertyName(key);
                writer.WriteValue(value);
            }
        }

        // forced into duplication by the type system...
        private void WritePropertyNameValue<TValue>(JsonWriter writer, string key, TValue? value)
            where TValue : struct
        {
            // exclude null properties since objects may have different schemas
            if (value != null)
            {
                writer.WritePropertyName(key);
                writer.WriteValue(value);
            }
        }
    }
}