# Azure.Storage.Liberation #
A small .NET library containing some useful classes, extensions and examples for bulk extraction of data from Azure Table Storage.


## Usage / building ##
If you just want the helper classes and extension methods, you can use the NuGet package. You can [find the package here](https://nuget.org/packages/Azure.Storage.Liberation/).

If you want to build the library / examples yourself, you need Visual Studio 2012 and ruby / rake. Run setup-devenv.bat to install any required dependencies.

##What's in here?##

### CloudTable.FetchAll&lt;TTableEntity&gt;(int batchSize) ###

An extention method on CloudTable which asynchronously fetches all rows (in batches) from a table and publish as an IObservable&lt;TTableEntity&gt;.

### DynamicTableEntityConverter ###

A JSONConverter for Json.NET which projects the non-null properties of a DynamicTableEntity into a JSON object. 
This can easily be extended to customise the JSON, for example:
	
	public class IdGeneratingEntityConverter : DynamicTableEntityConverter
	{
	    private readonly string tableName;
	
	    public IdGeneratingEntityConverter(string tableName)
	    {
	        this.tableName = tableName;
	    }
	
	    protected override void WriteObjectProperties(JsonWriter writer, DynamicTableEntity entity)
	    {	
			// generate an id based on table, partition and row keys
			var idValue = string.Format("{0}-{1}-{2}", tableName, entity.PartitionKey, entity.RowKey);
			writer.WritePropertyName("_id");
			writer.WriteValue(idValue);
	
	        foreach (var property in entity.Properties)
	        {
	            WriteProperty(writer, property);
	        }
	    }
	}  


## Examples ##

### Export each table in the storage account to a JSON file ###

	var storageAccount = new CloudStorageAccount(new StorageCredentials(<accountName>, <accesskey>), true);
	var tableClient = storageAccount.CreateCloudTableClient();
	
	// fetch all tables associated with the storage account	
	var tables = tableClient.ListTables().ToArray();
	
	foreach (var table in tables)
	{
		// generate a json array from all rows and stream to a file
	    var jsonWriter = new JsonTextWriter(fileWriter);
        var jsonSerializer = JsonSerializer.Create(null);
        jsonSerializer.Converters.Add(new DynamicTableEntityConverter());

        jsonWriter.WriteStartArray();

        table.FetchAll<DynamicTableEntity>(100)
             .ForEach(entity => jsonSerializer.Serialize(jsonWriter, entity));

        jsonWriter.WriteEndArray();
        jsonWriter.Flush();
	}


## License ##
The MIT License (MIT)

Copyright (c) 2013 Cloudant

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


