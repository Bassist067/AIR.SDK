using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AIR.API.Core.Storage;

[assembly: InternalsVisibleTo("AIR.SDK.StorageManager.Tests")]
namespace AIR.SDK.StorageManager
{
    public class S3StorageManager : IStorageManager
    {
        private string _bucketName;
        private string _prefix;

        private string _base64Key;

        private readonly IAmazonS3 _client;

        public Type StorageReferenceType => typeof(S3StorageReference);

        private bool UseEncryption => !string.IsNullOrEmpty(_base64Key);


        /// <summary>
        /// Constructs AmazonS3Client for AmazonS3 scalable storage in the cloud.
        /// </summary>
        /// <param name="bucketName">Must conform with DNS requirements <see cref="http://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/M_Amazon_S3_AmazonS3_PutBucket.htm"/></param>
        /// <param name="prefix">A "folder" to use within the bucket</param>
        /// <param name="client">Amazon S3 interface</param>
        public S3StorageManager(string bucketName, string prefix, IAmazonS3 client)
        {
            SetBucket(bucketName);

            _prefix = prefix;

            _client = client;

            //Aes aesEncryption = Aes.Create();
            //aesEncryption.KeySize = 256;
            //aesEncryption.GenerateKey();
            //_base64Key = Convert.ToBase64String(aesEncryption.Key);

            _base64Key = "Td2m7DRYn/OcFILR+GcMz3tLK/rDXrr9Dly5N7jy5Gw=";
        }

        public S3StorageManager(string bucketName, IAmazonS3 client) : this(bucketName, String.Empty, client)
        { }

        internal bool IsBucketExists(string bucketName)
        {
            bool rez = _client.DoesS3BucketExistAsync(bucketName).Result;
            return rez;
        }

        private void EnsureBucketExists()
        {
            var exists = _client.DoesS3BucketExistAsync(_bucketName).Result;
            if (!exists)
                _client.EnsureBucketExistsAsync(_bucketName).Wait();
        }

        private async Task<bool> EnsureBucketExistsAsync()
        {
            var exists = await _client.DoesS3BucketExistAsync(_bucketName);
            if (!exists)
                await _client.EnsureBucketExistsAsync(_bucketName);

            return true;
        }

        #region IStorageManager Members

        /// <summary>
        /// Uploads a serialized object to storage (AWS S3) 
        /// </summary>
        /// <param name="content">A serialized string to upload</param>
        /// <param name="storageKey">Unique key of data (Optional).</param>
        /// <returns><see cref="IStorageReference"/>- A general link to object in any storage service</returns>
        public IStorageReference PutData(string content, string storageKey = null)
        {
            var storageRef = new S3StorageReference( /*_bucketName,*/ GenerateKey(storageKey));
            try
            {
                EnsureBucketExists();

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = storageRef.StorageKey,
                    ContentBody = content
                };

                if (UseEncryption)
                {
                    //putRequest.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
                    //putRequest.ServerSideEncryptionCustomerProvidedKey = _base64Key;
                    putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
                }

                _client.PutObjectAsync(putRequest).Wait();
            }
            catch (AmazonServiceException e)
            {
                throw new StorageManagerException("Failed to store the message content in an S3 object.", e);
            }
            catch (AmazonClientException e)
            {
                throw new StorageManagerException("Failed to store the message content in an S3 object.", e);
            }

            return storageRef;
        }

        /// <summary>
        /// Uploads serialized data asynchonously to AWS S3
        /// </summary>
        /// <param name="content">Serialized data object</param>
        /// <param name="storageKey">Unique key of data (Optional)</param>
        /// <param name="cancellationToken">A cancellation token to control task execution</param>
        /// <returns>Task returning <see cref="IStorageReference"/> result</returns>
        public async Task<IStorageReference> PutDataAsync(string content, string storageKey = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var storageRef = new S3StorageReference( /*_bucketName,*/ GenerateKey(storageKey));
            try
            {
                await EnsureBucketExistsAsync();

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = storageRef.StorageKey,
                    ContentBody = content
                };

                if (UseEncryption)
                {
                    //putRequest.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
                    //putRequest.ServerSideEncryptionCustomerProvidedKey = _base64Key;
                    putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
                }

                await _client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);

                return storageRef;
            }
            catch (AmazonServiceException e)
            {
                throw new StorageManagerException("Failed to store the content in an S3 object.", e);
            }
            catch (AmazonClientException e)
            {
                throw new StorageManagerException("Failed to store the message content.", e);
            }
        }

        /// <summary>
        /// Retrieves serialized data from data storage (AWS S3)
        /// </summary>
        /// <param name="storageReference"><see cref="IStorageReference"/>- A general link to object in any storage service</param>
        /// <returns>A serialized object</returns>
        public string GetData(IStorageReference storageReference)
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = GenerateKey(storageReference.GetStorageKey())
            };

            //if (UseEncryption)
            //{
            //    getObjectRequest.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            //    getObjectRequest.ServerSideEncryptionCustomerProvidedKey = _base64Key;
            //}

            try
            {
                EnsureBucketExists();

                using (var getObjectResponse = _client.GetObjectAsync(getObjectRequest).Result)
                {
                    var streamReader = new StreamReader(getObjectResponse.ResponseStream);
                    var text = streamReader.ReadToEnd();
                    return text;
                }
            }
            catch (AmazonServiceException e)
            {
                throw new StorageManagerException("Failed to get the S3 object which contains the message payload.", e);
            }
            catch (AmazonClientException e)
            {
                throw new StorageManagerException("Failed to get the S3 object which contains the message payload.", e);
            }
        }

        /// <summary>
        /// Gets data from storage by key
        /// </summary>
        /// <param name="storageKey">Unique key of data.</param>
        /// <returns>Serialized object</returns>
        public string GetData(string storageKey)
        {
            return GetData(new S3StorageReference(storageKey));
        }

        /// <summary>
        /// Retrieves data from storage asynchronously 
        /// </summary>
        /// <param name="storageReference"><see cref="IStorageReference"/></param>
        /// <param name="cancellationToken">a cancellation token to control task execution</param>
        /// <returns>Serialized object</returns>
        public async Task<string> GetDataAsync(IStorageReference storageReference, CancellationToken cancellationToken = default(CancellationToken))
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = GenerateKey(storageReference.GetStorageKey())
            };

            //if (UseEncryption)
            //{
            //    getObjectRequest.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            //    getObjectRequest.ServerSideEncryptionCustomerProvidedKey = _base64Key;
            //}

            try
            {
                using (var getObjectResponse = await _client.GetObjectAsync(getObjectRequest, cancellationToken).ConfigureAwait(false))
                {
                    var streamReader = new StreamReader(getObjectResponse.ResponseStream);
                    var text = streamReader.ReadToEnd();
                    return text;
                }
            }
            catch (AmazonServiceException e)
            {
                throw new StorageManagerException("Failed to get the S3 object.", e);
            }
            catch (AmazonClientException e)
            {
                throw new StorageManagerException("Failed to get the S3 object.", e);
            }
        }

        /// <summary>
        /// Retrieves data from storage asynchronously 
        /// </summary>
        /// <param name="storageKey">Unique key of data.</param>
        /// <param name="cancellationToken">a cancellation token to control task execution</param>
        /// <returns>Serialized object</returns>
		public async Task<string> GetDataAsync(string storageKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetDataAsync(new S3StorageReference(storageKey), cancellationToken);
        }

        /// <summary>
        /// Removes object from storage by storage reference
        /// </summary>
        /// <param name="storageReference"><see cref="IStorageReference"/>- A general link to object in any storage service</param>
		public void DeleteData(IStorageReference storageReference)
        {
            var deleteObjectRequest = new DeleteObjectRequest { BucketName = _bucketName, Key = GenerateKey(storageReference.GetStorageKey()) };
            try
            {
                _client.DeleteObjectAsync(deleteObjectRequest).Wait();
            }
            catch (AmazonServiceException e)
            {
                throw new StorageManagerException("Failed to delete the S3 object.", e);
            }
            catch (AmazonClientException e)
            {
                throw new StorageManagerException("Failed to delete the S3 object.", e);
            }
        }

        /// <summary>
        /// Removes data from storage by key
        /// </summary>
        /// <param name="storageKey">Unique key of data.</param>
        public void DeleteData(string storageKey)
        {
            DeleteData(new S3StorageReference(storageKey));
        }

        /// <summary>
        /// Removes data asynchronously from storage
        /// </summary>
        /// <param name="storageReference"><see cref="IStorageReference"/></param>
        /// <param name="cancellationToken">a cancellation token to control task execution</param>
        /// <returns>Task with no type</returns>
        public async Task DeleteDataAsync(IStorageReference storageReference, CancellationToken cancellationToken = default(CancellationToken))
        {
            var deleteObjectRequest = new DeleteObjectRequest { BucketName = _bucketName, Key = GenerateKey(storageReference.GetStorageKey()) };
            try
            {
                await _client.DeleteObjectAsync(deleteObjectRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException e)
            {
                throw new AmazonClientException("Failed to delete the S3 object.", e);
            }
            catch (AmazonClientException e)
            {
                throw new AmazonClientException("Failed to delete the S3 object.", e);
            }
        }

        /// <summary>
        /// Removes data from storage by key
        /// </summary>
        /// <param name="storageKey">The unique key of data.</param>
        /// <param name="cancellationToken">a cancellation token to control the method execution</param>
        public async Task DeleteDataAsync(string storageKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteDataAsync(new S3StorageReference(storageKey), cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="prefix"></param>
        public void SetBucket(string bucketName, string prefix = null)
        {
            //  To conform with DNS requirements, the following constraints apply:
            //  Bucket names should not contain underscores (_)
            //  Bucket names should be between 3 and 63 characters long
            //  Bucket names should not end with a dash
            //  Bucket names cannot contain two, adjacent periods
            //  Bucket names cannot contain dashes next to periods(e.g., "my-.bucket.com" and "my.-bucket" are invalid)
            //  Bucket names cannot contain uppercase characters

            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentException("S3StorageManager: bucketName cannot be empty.", nameof(bucketName));

            if (!IsValidName(bucketName))
                throw new ArgumentException("S3StorageManager: bucketName does not conform to specification.", nameof(bucketName));

            _bucketName = bucketName;

            if (!string.IsNullOrEmpty(prefix))
            {
                if (!IsValidName(prefix))
                    throw new ArgumentException("S3StorageManager: prefix does not conform to specification.", nameof(prefix));
            }

            if (prefix != null)
                _prefix = prefix;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix"></param>
        public void SetPrefix(string prefix)
        {
            if (!string.IsNullOrEmpty(prefix))
                // TODO: Vlidate prefix.
                if (!IsValidName(prefix))
                    throw new ArgumentException("S3StorageManager: prefix does not conform to specification.", nameof(prefix));

            _prefix = prefix;
        }
        #endregion


        internal virtual string Normalizekey(string key)
        {
            string objectkey = key.Replace("\\", "/").Replace("//", "/");
            if (objectkey.StartsWith("/"))
                objectkey = objectkey.Substring(1);

            return objectkey;
        }

        /// <summary>
        /// Just uses a guid as unique key ID
        /// </summary>
        /// <returns>Generated key</returns>
        internal virtual string GenerateKey(string storageKey = null)
        {
            var key = string.IsNullOrEmpty(storageKey) ? Guid.NewGuid().ToString("N") : storageKey;

            if (!string.IsNullOrEmpty(_prefix) && !key.StartsWith(_prefix))
                return Normalizekey($"{_prefix}/{key}");

            return key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool IsValidName(string name)
        {
            return !(name.Contains("_")
                   || name.Length < 3
                   || name.Length > 63
                   || name.EndsWith("/")
                   || name.StartsWith("/")
                   || name.Contains("--")
                   || name.Contains("-.")
                   || name.Contains(".-"));
        }
    }
}