using System;
using System.Threading;
using System.Threading.Tasks;

namespace AIR.API.Core.Storage
{
    /// <summary>
    /// This interface provides methods to access the store service like AWS S3 or some other
    /// </summary>
    public interface IStorageManager
    {
        /// <summary>
        /// Type of the store service being used (S3, DynamoDB etc.)
        /// </summary>
        Type StorageReferenceType { get; }

        /// <summary>
        /// Puts data to store
        /// </summary>
        /// <param name="content">Serialized object to be stored.</param>
        /// <param name="storageKey">Unique key of data (Optional). Considered as file name.</param>
        /// <returns>The reference specific to storage used</returns>
        IStorageReference PutData(string content, string storageKey = null);

        /// <summary>
        /// Gets data from store by reference
        /// </summary>
        /// <param name="storageReference">The generic object key</param>
        /// <returns>A serialized object</returns>
        string GetData(IStorageReference storageReference);

        /// <summary>
        /// Gets data from store by key
        /// </summary>
        /// <param name="storageKey">Unique key of data.</param>
        /// <returns>A serialized object</returns>
        string GetData(string storageKey);

        /// <summary>
        /// Removes data from store by reference
        /// </summary>
        /// <param name="storageReference">The generic object key</param>
        void DeleteData(IStorageReference storageReference);

        /// <summary>
        /// Removes data from store by key
        /// </summary>
        /// <param name="storageKey">Unique key of data.</param>
        void DeleteData(string storageKey);


        /// <summary>
        /// Asynchonously puts data to store.
        /// </summary>
        /// <param name="content">Serialized object to be stored.</param>
        /// <param name="storageKey">Unique key of data (Optional). Considered as file name.</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        /// <returns>a Task with the generic object key result</returns>
        Task<IStorageReference> PutDataAsync(string content, string storageKey = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchonously gets data from store.
        /// </summary>
        /// <param name="storageReference">the generic object key</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        /// <returns>A serialized object</returns>
        Task<string> GetDataAsync(IStorageReference storageReference, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchonously gets data from store.
        /// </summary>
        /// <param name="storageKey">The unique key of data.</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        /// <returns>A serialized object</returns>
        Task<string> GetDataAsync(string storageKey, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes data from store by reference
        /// </summary>
        /// <param name="storageReference">The generic object key</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        Task DeleteDataAsync(IStorageReference storageReference, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Removes data from store by key
        /// </summary>
        /// <param name="storageKey">The unique key of data.</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        Task DeleteDataAsync(string storageKey, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="prefix"></param>
        void SetBucket(string bucketName, string prefix = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        void SetPrefix(string prefix);
    }
}