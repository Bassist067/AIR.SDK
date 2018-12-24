using Amazon.SimpleWorkflow;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workers
{
	public class WorkflowWorkerTests
	{
		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void ProcessActivityTask_Test()
		{
			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			//var workflow = Substitute.For<IWorkflow>();
			//workflow.When(x => x.GetActivity(Arg.Any<string>())).DoNotCallBase(); //Make sure the ReadFile call won't call real implementation
			//workflow.GetActivity(Arg.Any<string>()).Returns(activity); // This won't run the real ReadFile now
			//workflow.TaskList.Returns(new TaskList { Name = "Fake_TaskList" });

			//var workflow = new WorkflowBase("","","","", swf);

			var worker = Substitute.For<WorkflowWorker>(null, swf);
			//var result = (string)worker.Protected("TestInternalMethod");
			Assert.NotNull(worker);
		}
		
	}
}
