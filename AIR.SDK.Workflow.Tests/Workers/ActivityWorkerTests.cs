using System;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Core;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workers
{
	public class ActivityWorkerTests
	{
		public ActivityWorkerTests()
		{
			//Assert.IsFalse(String.IsNullOrEmpty(ConfigurationManager.AppSettings["AWSRegion"]));
		}

		/// <summary>
		/// 
		/// </summary>
		[Fact]
		public void ProcessActivityTask_WithWorkflow()
		{
			string returnValue = "activity result";

			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			var activity = Substitute.For<IActivity>();
			activity.Name.Returns("testActivity");
			activity.TaskProcessor(Arg.Any<string>()).Returns(v => Utils.SerializeToJSON(new StepResult<string>(returnValue, true)));

			var workflow = Substitute.For<IWorkflow>();
			//workflow.When(x => x.GetActivity(Arg.Any<string>())).DoNotCallBase(); //Make sure the ReadFile call won't call real implementation
			workflow.GetActivity(Arg.Any<string>()).Returns(activity); // This won't run the real ReadFile now
			workflow.TaskList.Returns(new TaskList {Name = "Fake_TaskList"});

			var worker = Substitute.ForPartsOf<ActivityWorker>(workflow, swf);

			var task = Substitute.For<ActivityTask>();
			task.ActivityId.Returns(activity.ActivityId);

			var result = (string)worker.Protected("ProcessActivityTask", task, workflow);
			var resultObj = Utils.DeserializeFromJSON<StepResult<string>>(result);

			Assert.True(resultObj.Success);
			Assert.Equal(returnValue, resultObj.ReturnValue);
		}

		/// <summary>
		/// Testing call of internal <see cref="ActivityWorker.TaskProcessor(string)"/> when the workflow is not specified.
		/// </summary>
		[Fact]
		public void ProcessActivityTask_WithoutWorkflow()
		{
			string returnValue = "task processor result";

			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			var worker = Substitute.For<ActivityWorker>("","", swf);
			worker.TaskProcessor = (x) => {return Utils.SerializeToJSON(new StepResult<string>(returnValue, true));};

			var task = Substitute.For<ActivityTask>(); 

			var result = (string)worker.Protected("ProcessActivityTask", task, null);
			var resultObj = Utils.DeserializeFromJSON<StepResult<string>>(result);

			Assert.True(resultObj.Success);
			Assert.Equal(returnValue, resultObj.ReturnValue);
		}

		/// <summary>
		/// When actvity is not found the <see cref="ActivityWorker.ProcessActivityTask()"/> should return unsuccessful StepResult.
		/// </summary>
		[Fact]
		public void ProcessActivityTask_NullActivity()
		{
			var activityID = "Fake_Activity_ID";
			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			var workflow = Substitute.For<IWorkflow>();
			workflow.GetActivity(Arg.Any<string>()).Returns((IActivity)null);
			workflow.TaskList.Returns(new TaskList { Name = "Fake_TaskList" });

			var worker = Substitute.ForPartsOf<ActivityWorker>(workflow, swf);

			var task = Substitute.For<ActivityTask>();
			task.ActivityId = activityID;

			var result = (string)worker.Protected("ProcessActivityTask", task, workflow);
			var resultObj = Utils.DeserializeFromJSON<StepResult<string>>(result);

			Assert.False(resultObj.Success);
			Assert.Equal($"Activity '{activityID}' not found.", resultObj.ReturnValue, true);
		}

		/// <summary>
		/// Testing call of <see cref="ActivityWorker.ProcessActivityTask()"/> with caught exception.
		/// </summary>
		[Fact]
		public void ProcessActivityTask_CatchException()
		{
			string message = "Exception Message";

			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			var worker = Substitute.For<ActivityWorker>("", "", swf);
			worker.TaskProcessor = (x) => { throw new Exception(message); };

			var task = Substitute.For<ActivityTask>();

			var result = (string)worker.Protected("ProcessActivityTask", task, null);
			var resultObj = Utils.DeserializeFromJSON<StepResult<string>>(result);

			Assert.False(resultObj.Success);
			Assert.Equal(message, resultObj.ReturnValue, true);
		}

		/// <summary>
		/// Testing call of <see cref="ActivityWorker.TaskProcessor()"/> with caught exception.
		/// </summary>
		[Fact(Skip="TaskProcessor is field now.")]
		public void TaskProcessor_CatchException()
		{
			var swf = Substitute.For<IAmazonSimpleWorkflow>();

			var worker = Substitute.ForPartsOf<ActivityWorker>("", "", swf);

			Assert.Throws<NotImplementedException>(() => {var result = worker.TaskProcessor(string.Empty);});
		}

	}
}
