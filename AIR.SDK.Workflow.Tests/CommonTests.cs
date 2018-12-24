using System;
using System.Text;
using Amazon.SimpleWorkflow;
using NSubstitute;
using Xunit;
using Newtonsoft.Json;
using System.IO;
using AIR.API.Core.Storage;
using AIR.SDK.StorageManager;
using AIR.SDK.Workflow;
using AIR.SDK.Workflow.Tests.Data;

namespace AIR.SDK.Workflow.Tests
{
	public class CommonTests
	{
		private readonly IAmazonSimpleWorkflow _amazonSwf;

		public CommonTests()
		{
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();
		}


		[Fact]
		internal void ValidateKeyTest()
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "name", "version", "taskList", _amazonSwf);
			var action = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var stepKey = "1";
			workflow.AttachStep(stepKey, action);

			Assert.Throws<ArgumentException>("stepKey", () => workflow.AttachStep(string.Empty, action));
			Assert.Throws<ArgumentException>("stepKey", () => workflow.AttachStep(stepKey, action));
		}


		[Fact]
		internal void Extensions_CutToLength_NegotiveZeroLength()
		{
			var str = "1234567890";
			var expected = "";

			Assert.Equal(expected, str.CutToLength(-1));
			Assert.Equal(expected, str.CutToLength(0));
		}
		[Fact]
		internal void Extensions_CutToLengthLessThan()
		{
			var str = "1234567890";
			var expected = "12345";

			Assert.Equal(expected, str.CutToLength(5));
		}
		[Fact]
		internal void Extensions_CutToLengthGreaterThan()
		{
			var str = "1234567890";
			var expected = "1234567890";

			Assert.Equal(expected, str.CutToLength(12));
		}
		[Fact]


		internal void ConcurrentDictionaryConverter_ReadJson_NullTokenTest()
		{
			var converter = Substitute.ForPartsOf<ConcurrentDictionaryConverter>();

			var rsonReader = Substitute.ForPartsOf<JsonReader>();
			rsonReader.TokenType.Returns(info => JsonToken.Null);

			var result = converter.ReadJson(rsonReader, typeof(string), null, null);

			Assert.Null(result);
		}
		[Fact]
		internal void ConcurrentDictionaryConverter_WriteJson()
		{
			var converter = Substitute.ForPartsOf<ConcurrentDictionaryConverter>();

			var jsonSerializer = new JsonSerializer();

			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);
			JsonWriter jsonWriter = new JsonTextWriter(sw);

			TInput data = new TInput { Value = "some data" };

			var expected = Utils.SerializeToJSON(data);

			converter.WriteJson(jsonWriter, data, jsonSerializer);

			//string strMyString = sw.ToString();

			Assert.Equal(expected, sw.ToString());
		}

		[Fact]
		internal void Utils_DeleteFromStoreTest()
		{
			var store = Substitute.For<IStorageManager>();
			var deleted = false;

			store.When(x=> x.DeleteData(Arg.Any<S3StorageReference>())).Do( x => {deleted = true;});

			Utils.DeleteFromStore("valid key", store);

			Assert.True(deleted);
		}

		[Fact]
		internal void Utils_PutDataToStoreTest()
		{
			var data = "some data";
			var key = "valid key";

			var storeRef = Substitute.For<IStorageReference>();
			storeRef.GetStorageKey().Returns(info=> key);

			var store = Substitute.For<IStorageManager>();
			store.PutData(Arg.Any<string>()).Returns(info => storeRef);

			var value = Utils.PutDataToStore(data, store);

			store.Received().PutData(data);
			Assert.Equal(key, value);
		}

	}
}
