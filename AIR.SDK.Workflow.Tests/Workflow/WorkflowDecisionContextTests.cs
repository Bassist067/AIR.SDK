using AIR.API.Core;
using AIR.API.Core.Storage;
using AIR.SDK.StorageManager;
using AIR.SDK.Workflow.Context;
using AIR.SDK.Workflow.Tests.TestUtils;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workflow
{


	public class WorkflowDecisionContextTests
	{
		[Fact]
		public void ResultObject_WithoutStoreTest()
		{
			var expectedValue = "Expected Value";

			var context = Substitute.For<WorkflowDecisionContext>();

			var result = new StepResult<string>(expectedValue, true);
			var resultSerd = Utils.SerializeToJSON(result);

			context.ResultRef = resultSerd;

			AssertObjectEquals.PropertyValuesAreEqual(context.ResultObject, result);

			Assert.Equal(expectedValue, context.ResultData);
		}

		[Fact]
		public void ResultObject_WithStoreTest()
		{
			var expectedValue = "Expected Value";
			var storageKey = "validStoreKey";

			var result = new StepResult<string>(expectedValue, true);
			var resultSerd = Utils.SerializeToJSON(result);

			var storeManager = Substitute.For<IStorageManager>();
			storeManager.GetData(Arg.Any<S3StorageReference>()).Returns(info => resultSerd);

			var context = Substitute.For<WorkflowDecisionContext>(storeManager);

			context.ResultRef = storageKey;

			AssertObjectEquals.PropertyValuesAreEqual(context.ResultObject, result);
			Assert.Equal(expectedValue, context.ResultData);

			// Checking if the GetSerializedObjectFromStore method was called with exact parameter.
			storeManager.Received().GetData(Arg.Is<S3StorageReference>(x => x.StorageKey == storageKey));
		}
	}
}
