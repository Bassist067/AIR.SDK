using AIR.API.Core.Storage;

namespace AIR.SDK.StorageManager
{
    public class S3StorageReference : IStorageReference
    {
	   //public string BucketName {get; set; }
	   public string StorageKey { get; set; }

	   #region IStorageReference Members

	   public string GetStorageKey()
	   {
		  return StorageKey;
	   }

	   #endregion


	   public S3StorageReference(/*string bucketName,*/ string s3Key)
	   {
		  StorageKey = s3Key;
	   }
    }
}