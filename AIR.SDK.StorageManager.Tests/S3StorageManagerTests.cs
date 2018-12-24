using System;
using AIR.API.Core.Storage;
using Amazon.S3;
using Amazon.S3.Model;
using AutoFixture.Xunit2;
using Castle.Core.Logging;
using NSubstitute;
using Xunit;

namespace AIR.SDK.StorageManager.Test
{
    public class S3StorageManagerTests
    {
	   private const string _bucketName = "estestsqs";
	   private const string _prefix = "_Tests_Received";

	   private readonly IAmazonS3 _amazonS3;
	   private readonly ILoggerFactory _loggerFactory;

	   public S3StorageManagerTests()
	   {
		  _amazonS3 = Substitute.For<IAmazonS3>();

		  _loggerFactory = Substitute.For<ILoggerFactory>();
	   }

	   [Theory]
	   [InlineData("")]
	   [InlineData("a_aa")]
	   [InlineData("aa")]
	   [InlineData("longvalueaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
	   [InlineData("aaa/")]
	   [InlineData("/aaa")]
	   [InlineData("a--a")]
	   [InlineData("a-.a")]
	   [InlineData("a.-a")]
	   internal void S3StorageManager_Ctor_CatchException(string bucketName)
	   {
		  Assert.Throws<ArgumentException>("bucketName", () => new S3StorageManager(bucketName, _prefix, _amazonS3));
	   }

	   [Fact, Trait("Category", "Unit")]
	   internal void S3StorageManager_StorageReferenceType()
	   {
		  var s3 = Substitute.ForPartsOf<S3StorageManager>(_bucketName, _prefix, _amazonS3);

		  Assert.True(s3.StorageReferenceType == typeof(S3StorageReference));
	   }

	   [Fact, Trait("Category", "Unit")]
	   internal void S3StorageManager_GenerateKey()
	   {
		  var s3 = Substitute.ForPartsOf<S3StorageManager>(_bucketName, _prefix, _amazonS3);

		  var key1 = s3.GenerateKey();
		  var key2 = s3.GenerateKey();

		  Assert.True(key1 != key2, "S3 key is not unique.");

		  Assert.StartsWith(_prefix + "/", key1);
	   }

	   [Fact, Trait("Category", "Unit")]
	   internal void S3StorageManager_Normalizekey()
	   {
		  var s3 = Substitute.ForPartsOf<S3StorageManager>(_bucketName, _prefix, _amazonS3);
		  string key = s3.Normalizekey("\\key");
		  Assert.True(!key.Contains("\\"));

		  key = s3.Normalizekey("/key");
		  Assert.True(!key.Contains("/"));

		  key = s3.Normalizekey("key//");
		  Assert.True(!key.Contains("//"));
	   }

	   [Theory, AutoData]
	   internal void DeleteFromStorageTest([Frozen] string bucketName)
	   {
		  IStorageReference reference = Substitute.For<IStorageReference>();
		  var amazonS3 = Substitute.For<IAmazonS3>();
		  var deleted = false;

		  amazonS3.When(x => x.DeleteObjectAsync(Arg.Any<DeleteObjectRequest>())).Do(x => { deleted = true; });

		  var storageManager = new S3StorageManager(bucketName, amazonS3);

		  storageManager.DeleteData(reference);

		  Assert.True(deleted);
	   }

	   [Theory, AutoData]
	   internal void PutDataToStorageTest([Frozen] string key, [Frozen] string data, [Frozen] string bucketName)
	   {
		  if (bucketName == null)
		  {
			 //throw new ArgumentNullException(nameof(bucketName));
		  }

		  var storeRef = Substitute.For<IStorageReference>();
		  storeRef.GetStorageKey().ReturnsForAnyArgs(key);

		  var amazon = Substitute.For<IAmazonS3>();

		  var storageManager = new S3StorageManager("bucket", amazon);

		  var reference = storageManager.PutData(data);
		  amazon.Received().PutObjectAsync(Arg.Is<PutObjectRequest>(r => r.ContentBody == data));
	   }
    }
}