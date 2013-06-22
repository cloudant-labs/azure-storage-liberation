using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Azure.Storage.Liberation
{
    public static class CloudTableExtensions
    {
        /// <summary>
        /// Asynchronously fetch all rows from the table in batches and expose as an observable.
        /// </summary>
        /// <remarks>Azure (sensibly) prevents bulk retrieval of all rows from a table in a single request.
        /// This method fetches rows in batches and exposes the results as an observable collection.
        ///  </remarks>
        /// <typeparam name="TTableEntity">The type of row to fetch</typeparam>
        /// <param name="sourceTable">The table to retrieve from</param>
        /// <param name="batchSize">Number of rows to fetch in each request. Note that Azure places limits on this.</param>
        /// <returns>An observable collection to which rows are published as they are returned from the server.</returns>
        public static IObservable<TTableEntity> FetchAll<TTableEntity>(this CloudTable sourceTable, int batchSize) where TTableEntity : ITableEntity, new()
        {
            if (sourceTable == null) throw new ArgumentNullException("sourceTable");

            TableContinuationToken token = null;
            var reqOptions = new TableRequestOptions { };
            var ctx = new OperationContext { ClientRequestID = "" };

            var result = new Subject<TableQuerySegment<TTableEntity>>();

            // fetch on a background thread
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        var query = (new TableQuery<TTableEntity>()).Take(batchSize);
                        var evt = new ManualResetEvent(false);
                        var response = sourceTable.ExecuteQuerySegmented(query, token, reqOptions, ctx);
                        token = response.ContinuationToken;

                        result.OnNext(response);

                        evt.Set();
                        evt.WaitOne();

                        if (token == null)
                        {
                            result.OnCompleted();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.OnError(ex);
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

            // split the batches into individual entities for subscribers
            return result.SelectMany(r => r);
        }
    }
}