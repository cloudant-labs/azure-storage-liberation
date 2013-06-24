using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Azure.Storage.Liberation;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using MyCouch;
using MyCouch.Commands;
using Newtonsoft.Json;

namespace Azure2CouchDB
{
    /// <summary>
    /// A simple example which streams entities from Azure Table Storage to CouchDB / Cloudant via a local JSON transformation.
    /// 
    /// Utilizes Azure.Storage.Liberation and the MyCouch client library.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var azureAccountName = args[0];
            var azureAccessKey = args[1];
            var couchDBUrl = args[2];

            var storageAccount = new CloudStorageAccount(new StorageCredentials(azureAccountName, azureAccessKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();

            // fetch all tables associated with the storage account 
            var tables = tableClient.ListTables().ToArray();
            
            foreach (var table in tables)
            {
                const int azureFetchBatchSize = 100;
                const int bulkInsertBatchSize = 1000;

                var jsonConverter = new DynamicTableEntityToCouchDBEntityConverter(table.Name);

                table.FetchAll<DynamicTableEntity>(azureFetchBatchSize)
                    .Select(entity => JsonConvert.SerializeObject(entity, jsonConverter))
                    .Buffer(bulkInsertBatchSize)
                    .Select(docs => PostToCouchDB(docs, couchDBUrl))
                    .ForEachAsync(LogResponse)
                    .Wait();
            }

        }

        private static void LogResponse(BulkResponse response)
        {
            if (response.IsSuccess)
            {
                // response.Status will be 201 if successful but this does not guarantee all documents were saved.
                // The response will contain an entry for each document posted with a success state for each.
                // See http://docs.cloudant.com/api/database.html#updating-documents-in-bulk for a detailed explanation.

                var groupedByHasError = response.Rows.GroupBy(row => String.IsNullOrEmpty(row.Error)).ToArray();
                var errored = groupedByHasError.SingleOrDefault(g => g.Key == false);
                var succeeded = groupedByHasError.SingleOrDefault(g => g.Key == true);

                if (succeeded != null && succeeded.Any()) Console.WriteLine("{0} inserted successfully.", succeeded.Count());
                if (errored != null && errored.Any())
                {
                    foreach (var row in errored)
                    {
                        // just print the errors. at this point you might choose to e.g. resolve conflicts
                        Console.WriteLine("{0} - {1} {2}", row.Id, row.Reason, row.Error);
                    }
                }
            }
            else
            {
                // couchdb returned an outright error
                Console.WriteLine(response.GenerateToStringDebugVersion());
            }
        }

        /// <summary>
        /// Generates a bulk document insertion query and posts to CouchDB.
        /// </summary>
        private static BulkResponse PostToCouchDB(IEnumerable<string> docsToInsert, string couchDbUrl)
        {
            using (var client = new Client(couchDbUrl))
            {
                var bulkCommand = new BulkCommand();
                foreach (var doc in docsToInsert)
                {
                    bulkCommand.Include(doc);
                }

                return client.Documents.Bulk(bulkCommand);
            }
        }
    }
}
