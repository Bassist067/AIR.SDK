using System;
using System.Collections.Generic;
using AIR.SDK.Workflow.Core;
using AIR.SDK.Workflow.Tests.Data;
using AIR.SDK.Workflow.Tests.TestUtils;
using AutoFixture.Xunit2;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Activity
{
	public class ActivityTest
	{
		//private readonly ActivityOptions _actOptions;

		private const string _Domain = "TestDomain";
		private const string _ActivityName = "SimpleActivity";
		private const string _TaskList = "TaskList";

		private readonly StepResult<string> _expectedResult;
		private readonly Func<string, IResult<string>> _activityAction;

		public static IEnumerable<object[]> _Params
		{
			get
			{
				var data = new[]
				{
					new object[] 
					{
						/*new WorkflowOptions
						{
							Domain = WorkflowCrashTests.Domain,
							Name = WorkflowCrashTests.WorkflowName,
							Version = WorkflowCrashTests.WorkflowVersion,
							TaskList = WorkflowCrashTests.TaskList
						},*/
						new ActivityOptions
						{
							Name = _ActivityName,
							TaskList = _TaskList
							//ActivityAction = (x) => { return Substitute.For<IResult<string>>(); }
						}
					}
				};
				return data;
			}
		}

		public ActivityTest()
		{
/*			_actOptions = new ActivityOptions
			{
				Name = _ActivityName,
				TaskList = _TaskList,
				
			};
*/
			_expectedResult = new StepResult<string>() { ReturnValue = "correct value", Success = true };

			_activityAction = (x) => { return _expectedResult; };
		}


		//[Theory]
		//[MemberData("_Params")]
		//internal void Activity_RegisterTest([Frozen] ActivityOptions actParam)
		//{
		//	var activityVersion = "1.0";

		//	actParam.Version = "1.1";

		//	var activity = Substitute.ForPartsOf<SDK.Workflow.Activity>(actParam);

		//	var client = Substitute.For<IAmazonSimpleWorkflow>();

		//	client.ListActivityTypes(Arg.Any<ListActivityTypesRequest>()).ReturnsForAnyArgs( info =>
		//		new ListActivityTypesResponse()
		//		{
		//			HttpStatusCode = HttpStatusCode.OK,
		//			ActivityTypeInfos = new ActivityTypeInfos {
		//				TypeInfos = new List<ActivityTypeInfo> {
		//					new ActivityTypeInfo { ActivityType = new ActivityType { Version = activityVersion}}
		//				},
		//			},
		//		});

			
		//	client.RegisterActivityType(Arg.Any<RegisterActivityTypeRequest>()).ReturnsForAnyArgs(info => new RegisterActivityTypeResponse{ HttpStatusCode = HttpStatusCode.OK });

		//	((IActivity)activity).Register(_Domain, client);

		//	client.ReceivedWithAnyArgs().RegisterActivityType(new RegisterActivityTypeRequest());
		//}

		[Theory]
		[MemberData(nameof(_Params))]
		internal void Activity_CallActivityActionTest([Frozen] ActivityOptions actParam)
		{
			actParam.ActivityAction = (x) => { return _expectedResult; };
			var activity = Substitute.ForPartsOf<SDK.Workflow.Activity>(actParam);
			

			var input = "input string";

			var result = ((IActivity)activity).TaskProcessor(input);
			var resultObj = Utils.DeserializeFromJSON<StepResult<string>>(result);

			Assert.True(resultObj.ReturnValue == _expectedResult.ReturnValue);
		}

		[Theory]
		[MemberData(nameof(_Params))]
		internal void Activity_CallNullActivityActionTest([Frozen] ActivityOptions actParam)
		{
			var activity = new AIR.SDK.Workflow.Activity(actParam);
			//var activity = Substitute.ForPartsOf<Activity>(actParam);

			Assert.Throws<Exception>(() => ((IActivity)activity).TaskProcessor(null));
		}

		[Fact]
		internal void Activity_GetTypedObjectTest()
		{
			var activity = Substitute.ForPartsOf<ActivityBase<string, TOutput>>("SimpleActivity", "taskList");// { Version = "1.0" };

			var exptected = new TOutput {Value = "correct value"};
			var objStr = Utils.SerializeToJSON(exptected);

			var result = activity.GetTypedObject(objStr);

			AssertObjectEquals.PropertyValuesAreEqual(result, exptected);
		}

		[Fact]
		internal void Activity_GetInputTest()
		{
			var activity = Substitute.ForPartsOf<ActivityBase<TOutput, TOutput>>("SimpleActivity", "taskList");// { Version = "1.0" };
			
			var exptected = new TOutput { Value = "correct value" };

			((IActivity)activity).Input = Utils.SerializeToJSON(exptected);

			var result = activity.Input;

			AssertObjectEquals.PropertyValuesAreEqual(result, exptected);
		}

		[Fact]
		internal void Activity_MaxAttemptsTest()
		{
			var activity = new AIR.SDK.Workflow.ActivityBase<TOutput, TOutput>("SimpleActivity", "taskList");// { Version = "1.0" };

			Assert.True(activity.MaxAttempts == 3, "Activity.MaxAttempts default value is incorrect.");

			var attExpected = 5;
			activity.MaxAttempts = attExpected;

			Assert.True(activity.MaxAttempts == attExpected, "Activity.MaxAttempts custom value is incorrect.");
		}

		[Fact]
		internal void Activity_HeartbeatTimeoutTest()
		{
			var activity = new AIR.SDK.Workflow.ActivityBase<TOutput, TOutput>("SimpleActivity", "taskList");// { Version = "1.0" };

			Assert.True(activity.Options.HeartbeatTimeout == -1, "Activity.HeartbeatTimeout default value is incorrect.");

			var attExpected = 5;
			activity.Options.HeartbeatTimeout = attExpected;

			Assert.True(activity.Options.HeartbeatTimeout == attExpected, "Activity.HeartbeatTimeout custom value is incorrect.");
		}

		[Fact]
		internal void Activity_ScheduleToStartTimeoutTest()
		{
			var activity = new AIR.SDK.Workflow.ActivityBase<TOutput, TOutput>("SimpleActivity", "taskList");// { Version = "1.0" };

			Assert.True(activity.Options.ScheduleToStartTimeout == 60, "Activity.ScheduleToStartTimeout default value is incorrect.");

			var attExpected = 5;
			activity.Options.ScheduleToStartTimeout = attExpected;

			Assert.True(activity.Options.ScheduleToStartTimeout == attExpected, "Activity.ScheduleToStartTimeout custom value is incorrect.");
		}

		[Fact]
		internal void Activity_ScheduleToCloseTimeoutTest()
		{
			var activity = new AIR.SDK.Workflow.ActivityBase<TOutput, TOutput>("SimpleActivity", "taskList");// { Version = "1.0" };

			Assert.True(activity.Options.ScheduleToCloseTimeout == 60, "Activity.ScheduleToCloseTimeout default value is incorrect.");

			var attExpected = 5;
			activity.Options.ScheduleToCloseTimeout = attExpected;

			Assert.True(activity.Options.ScheduleToCloseTimeout == attExpected, "Activity.ScheduleToCloseTimeout custom value is incorrect.");
		}

		[Fact]
		internal void Activity_StartToCloseTimeoutTest()
		{
			var activity = new AIR.SDK.Workflow.ActivityBase<TOutput, TOutput>("SimpleActivity", "taskList");// { Version = "1.0" };

			Assert.True(activity.Options.StartToCloseTimeout == 60, "Activity.StartToCloseTimeout default value is incorrect.");

			var attExpected = 5;
			activity.Options.StartToCloseTimeout = attExpected;

			Assert.True(activity.Options.StartToCloseTimeout == attExpected, "Activity.StartToCloseTimeout custom value is incorrect.");
		}

		[Fact]
		internal void Activity_CtorTest()
		{
			var name = "SomeActivity";
			var tasklistName = "TaskList";

			var activity = new AIR.SDK.Workflow.Activity(name, tasklistName);// { Version = "1.0" };

			Assert.True(activity.Name == name, "Activity.Name is not specified.");
			Assert.True(activity.TaskList.Name == tasklistName, "Activity.TaskList.Name is not specified.");
		}

		[Fact]
		internal void ActivityEventArgs_CtorTest()
		{
			var details = "details";
			var reason = "some reason";

			var evtArgs = new AIR.SDK.Workflow.ActivityEventArgs(details, reason);

			Assert.True(evtArgs.Details == details, "ActivityEventArgs.Details is undefined.");
			Assert.True(evtArgs.Reason == reason, "ActivityEventArgs.Reason is undefined.");
		}
		

		[Fact]
		internal void ParallelActivityCollection_CtorTest()
		{
			var name = "SomeActivity";
			var tasklistName = "TaskList";

			var activity = new AIR.SDK.Workflow.ParallelActivityCollection(name, tasklistName);// { Version = "1.0" };

			Assert.True(activity.Name == name, "ParallelActivityCollection.Name is not specified.");
			Assert.True(activity.TaskList.Name == tasklistName, "ParallelActivityCollection.TaskList.Name is not specified.");
		}


		[Fact]
		internal void ParallelActivityCollectionBase_IParallelCollection_ProcessorTest()
		{
			var options = Substitute.ForPartsOf<ActivityCollectionOptions<TInput, string, string, string>>();
			var resultList = Substitute.For<IEnumerable<ParallelCollectionItem<string>>>();
			options.CollectionProcessor = (x) => { return resultList; };

			var activity = Substitute.ForPartsOf<ParallelActivityCollectionBase<TInput, string, string, string>>(options);

			var input = new TInput {Value = "input string"};
			var serInput = Utils.SerializeToJSON(input);

			var result = ((IParallelCollection)activity).Processor(serInput);

			options.Received().CollectionProcessor(Arg.Is<TInput>(x => x.Value == input.Value));
		}

		[Fact]
		internal void ParallelActivityCollectionBase_NullCollectionProcessorTest()
		{
			var activity = new AIR.SDK.Workflow.ParallelActivityCollectionBase<TInput, string, string, string>("ActivityName", "TaskList");

			//var input = Utils.SerializeToJSON( new TInput { Value = "input string" });

			Assert.Throws<Exception>(() => ((IParallelCollection)activity).Processor(string.Empty));
		}

		[Fact]
		internal void ParallelActivityCollectionBase_IParallelCollection_ReducerTest()
		{
			var options = Substitute.ForPartsOf<ActivityCollectionOptions<TInput, string, string, string>>();
			options.Reducer = (x) => { return string.Empty; };

			var activity = Substitute.ForPartsOf<ParallelActivityCollectionBase<TInput, string, string, string>>(options);
			

			var resultList = Substitute.For<IEnumerable<string>>();
			var result = ((IParallelCollection)activity).Reducer(resultList);

			options.ReceivedWithAnyArgs().Reducer(resultList);
		}

		[Fact]
		internal void ParallelActivityCollectionBase_NullReducerTest()
		{
			var activity = Substitute.ForPartsOf<ParallelActivityCollectionBase<TInput, TOutput, string, string>>("ActivityName", "TaskList");

			Assert.Throws<Exception>(() => ((IParallelCollection)activity).Reducer(null));
		}

		[Fact]
		internal void ParallelActivityCollectionBase_SetInputTest()
		{
			var activity = Substitute.ForPartsOf<ParallelActivityCollectionBase<TInput, string, string, string>>("ActivityName", "TaskList");

			var exptected = new TInput { Value = "correct value" };

			activity.Input = exptected;

			Assert.True(((IActivity)activity).Input == Utils.SerializeToJSON(exptected));
		}

		[Fact]
		internal void ParallelActivityCollectionBase_GetInputTest()
		{
			var activity = Substitute.ForPartsOf<ParallelActivityCollectionBase<TInput, string, string, string>>("ActivityName", "TaskList");

			var exptected = new TInput { Value = "correct value" };

			((IActivity)activity).Input = Utils.SerializeToJSON(exptected);

			var result = activity.Input;

			AssertObjectEquals.PropertyValuesAreEqual(result, exptected);
		}

		[Fact]
		internal void ParallelCollectionItem_SetInputTest()
		{
			var item = Substitute.ForPartsOf<ParallelCollectionItem<TInput>>();

			var exptected = new TInput { Value = "correct value" };

			item.Input = exptected;

			Assert.True(((ICollectionItemInput)item).Input == Utils.SerializeToJSON(exptected));
		}

		[Fact]
		internal void ParallelCollectionItem_GetInputTest()
		{
			var item = Substitute.ForPartsOf<ParallelCollectionItem<TInput>>();

			var exptected = new TInput { Value = "correct value" };

			((ICollectionItemInput)item).Input = Utils.SerializeToJSON(exptected);

			var result = item.Input;

			AssertObjectEquals.PropertyValuesAreEqual(result, exptected);
		}
	}
}
