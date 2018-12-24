using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Context;
using AIR.SDK.Workflow.Core;
using AutoFixture.Xunit2;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workflow
{
	public class WorkflowTests
	{
		private readonly DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
		private readonly IAmazonSimpleWorkflow _amazonSwf;
		private readonly WorkflowBase _workflow;
		//private WorkflowEventsProcessor _processor;
		private readonly string _executionContext;

		private const string _defaultWorkflowName = "workflowName";


		public static IEnumerable<object[]> TestData
		{
			get
			{
				var data = new[]
				{
					new[] {new[] {"0"}},
				};
				return data;
			}
		}

		public static IEnumerable<object[]> TestDataMultiple
		{
			get
			{
				var data = new[]
				{
					new[] {new[] {"0"}},
					new[] {new[] {"0", "1"}},
					//new[] {new[] {""}}
				};
				return data;
			}
		}

		public WorkflowTests()
		{
			_decisionTask = Substitute.For<DecisionTask>();
			_decisionTask.WorkflowType = new WorkflowType {Name = "TestWorkflow", Version = "TestVersion"};
			_decisionTask.WorkflowExecution = new WorkflowExecution {RunId = "TestRunId", WorkflowId = ""};

			var results = new ConcurrentDictionary<int, string>();
			results.AddOrUpdate(1, "TestResult", (index, value) => $"{value} - {index}");


			_executionContext = WorkflowStateSerializer.Serialize(new WorkflowState()
			{
				CurrentStepNumber = 1,
				NumberOfActions = 1,
				Results = results
			});


			_pollforDecisionTaskRequest = Substitute.For<PollForDecisionTaskRequest>();
			_pollforDecisionTaskRequest.Domain = "TestDomain";
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();
			_workflow = Substitute.For<WorkflowBase>("domain", _defaultWorkflowName, "version", "taskList", _amazonSwf);
		}

		[Theory]
		[InlineData("")]
		// So just parse value ActivityId (e.g. Name._ParentIndex._StepNum._ActionNum._Attempts) to get Step and Activity numbers.
		public void GetDeciderOrdinaryWorkflowTest(string workflowId)
		{
			var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var activityStep = Substitute.For<IWorkflowStep>();
			activityStep.Action = activity;
			activityStep.StepKey = "activityActionName";
			activityStep.StepNumber = 0;

			var workflow = Substitute.For<WorkflowBase>("domain", "GetDeciderOrdinaryWorkflowTest", "version", "taskList", _amazonSwf);
			workflow.Steps.Add(activityStep);

			var result = workflow.GetDecider(workflowId);
			Assert.True(result.GetType().BaseType == typeof (WorkflowBase)); //if we really returned a workwlow type
		}

		// So just parse value ActivityId (e.g. Name._ParentIndex._StepNum._ActionNum._Attempts) to get Step and Activity numbers.
		[Theory]
		[InlineData("0")]
		public void GetDeciderChildWorkflowTest(string workflowId)
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", _defaultWorkflowName, "version", "taskList", _amazonSwf);

			var childWorkflow = Substitute.For<WorkflowBase>("domain", "childWorkflowName", "version", "taskList", _amazonSwf);

			//var childWorkflowAction = Substitute.For<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			//var childWorkflowStep = Substitute.For<WorkflowStep>();
			//childWorkflowStep.Action = childWorkflowAction;
			//childWorkflowStep.StepKey = "childWorkflowActionName";
			//childWorkflowStep.StepNumber = 0;

			//var workflowStep = Substitute.For<WorkflowStep>();
			//workflowStep.Action = workflowAction;
			//workflowStep.StepKey = "workflowActionName";
			//workflowStep.StepNumber = 1;

			workflow.AttachStep(childWorkflow);

			var result = workflow.GetDecider(workflowId);
			Assert.True(result.GetType().BaseType == typeof (WorkflowBase)); //if we really returned a workwlow type
		}

		[Theory, AutoData]
		internal void GetActionIdTest([Frozen] string name, [Frozen] SchedulableState state)
		{
			state.StepNumber = 1;
			state.StepKey = "testActivity";
			state.ActionNumber = 1;
			state.AttemptNumber = 1;
			state.MaxAttempts = 1;
			state.TotalActions = 1;
			state.DelayTimeoutInSeconds = 0;

			string activityID = Utils.CreateActionId(name, "", state.StepNumber, state.ActionNumber, state.AttemptNumber);
			string expected =
				$"{String.Empty}__{name.TrimForID()}._{state.StepNumber}._{state.ActionNumber}._{state.AttemptNumber}";

			Assert.Equal(activityID, expected);
		}

		/// <summary>
		/// Covers ordinary workflow (no child)
		/// </summary>
		/// <param name="path">Array of index (stringified integer)</param>
		[Theory, MemberData(nameof(TestDataMultiple))]
		//[Theory, AutoData]
		public void FindWorkflowGeneralTest(string[] path)
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", _defaultWorkflowName, "version", "taskList", _amazonSwf);

			var workflowAction = Substitute.For<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var activityStep = Substitute.For<WorkflowStep>();
			activityStep.Action = activity;
			activityStep.StepKey = "activityActionName";
			activityStep.StepNumber = 0;
			workflow.Steps.Add(activityStep);

			var workflowStep = Substitute.For<WorkflowStep>();
			workflowStep.Action = workflowAction;
			workflowStep.StepKey = "workflowActionName";
			workflowStep.StepNumber = 1;
			workflow.Steps.Add(workflowStep);

			var result = workflow.FindWorkflow(path);

			//Check if we got object of WorkflowBase type. BaseType is because of Substitute wrap
			Assert.True(result.GetType().BaseType == typeof (WorkflowBase));
			//AssertObjectEquals.PropertyValuesAreEqual(result, workflowAction); //property check crashes when comparing task lists (type mismatch). TODO investigate
		}

		/// <summary>
		/// Covers workflow starting with child workflow
		/// </summary>
		/// <param name="path">Array of index (stringified integer)</param>
		[Theory, MemberData(nameof(TestData))]
		internal void FindWorkflowStartingWithChildTest(string[] path)
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", _defaultWorkflowName, "version", "taskList", _amazonSwf);

			var workflowAction = Substitute.For<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var workflowStep = Substitute.For<WorkflowStep>();
			workflowStep.Action = workflowAction;
			workflowStep.StepKey = "workflowActionName";
			workflowStep.StepNumber = 0;
			workflow.Steps.Add(workflowStep);

			var result = workflow.FindWorkflow(path);

			//Check if we got object of WorkflowBase type. BaseType is because of Substitute wrap
			Assert.True(result.GetType().BaseType == typeof (WorkflowBase));

			//AssertObjectEquals.PropertyValuesAreEqual(result, workflowAction); //property check crashes when comparing task lists (type mismatch). TODO investigate
		}

		[Theory]
		[InlineData("", _defaultWorkflowName)]
		[InlineData("0", "ChildWorkflowName1")]
		[InlineData("0.1", "ChildWorkflowName2")]
		internal void FindWorkflowTest(string path, string expectedWorkflowName)
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", _defaultWorkflowName, "version", "taskList", _amazonSwf);

			var action1 = Substitute.ForPartsOf<WorkflowBase>("domain", "ChildWorkflowName1", "version", "taskList", _amazonSwf);
			var action2 = Substitute.ForPartsOf<WorkflowBase>("domain", "ChildWorkflowName2", "version", "taskList", _amazonSwf);

			action1.AttachStep(Substitute.For<ActivityBase<string, string>>("name", "taskList"));
			action1.AttachStep(action2);

			workflow.AttachStep(action1);

			// workflow --> childeWorkflow1 --> childWorkflow2

			action1.WorkflowId = Utils.CreateWorkflowId(action1.Name, action1.TreePath, 0, 0, 0); // "0__ChildWorkflowName1._0._0._0-23ede5a7-0325-4477-a4e8-d131a6ab54c5"
			action2.WorkflowId = Utils.CreateWorkflowId(action2.Name, action2.TreePath, 1, 0, 0); // "0.1__ChildWorkflowName2._1._0._0-f8455f4b-04dc-4fed-8eb5-8bdfcb391980"

			workflow.WorkflowId = Utils.CreateWorkflowId(workflow.Name, workflow.TreePath,0, 0, 1);

			var treeKeys = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			var result = workflow.FindWorkflow(treeKeys);
			Assert.NotNull(result);
			Assert.Equal(result.TreePath, path);
			Assert.Equal(result.Name, expectedWorkflowName);

			//AssertObjectEquals.PropertyValuesAreEqual(result, workflowAction); //property check crashes when comparing task lists (type mismatch). TODO investigate
		}


		//TODO: Cover null cases
		[Theory, AutoData]
		internal void ProcessActionCollectionTest(WorkflowState state, SchedulableState schedulableState)
		{
			state.CurrentStepNumber = 0;
			state.Results.AddOrUpdate(1, "TestResult", (i, s) => $"{s}{i}");

			schedulableState.StepNumber = 0;
			schedulableState.StepKey = "testActivity";
			schedulableState.ActionNumber = 1;
			schedulableState.AttemptNumber = 1;
			schedulableState.MaxAttempts = 1;
			schedulableState.TotalActions = 1;
			schedulableState.DelayTimeoutInSeconds = 0;

			var action = Substitute.For<ParallelActivityCollectionBase<string, string, string, string>>("name", "taskList");

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityActionName";
			step.StepNumber = 0;
			//_workflow.Steps.Add(activityStep);

			//_workflow.GetStep(Arg.Any<int>()).Returns(info => activityStep);

			//_processor = new WorkflowEventsProcessor(_decisionTask, _workflow, _pollforDecisionTaskRequest, _amazonSwf);
			//_workflow.GetStep(1).ReturnsForAnyArgs(info => activityStep);

			var context = Substitute.For<WorkflowDecisionContext>();

			context.Control = SchedulableStateSerializer.Serialize(schedulableState);

			Dictionary<int, string> results = state.Results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


			context.Results[schedulableState.StepNumber] = results;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.WhenForAnyArgs(x => x.GetStep(Arg.Any<int>())).DoNotCallBase();
			workflow.GetStep(Arg.Any<int>()).Returns(info => step);

			workflow.WhenForAnyArgs(x => x.CallReducer(action, results)).DoNotCallBase();
			workflow.CallReducer(action, results).Returns(info => String.Empty);

			workflow.WhenForAnyArgs(x => x.ResolveStepNumber(step, String.Empty, 0)).DoNotCallBase();
			workflow.ResolveStepNumber(step, String.Empty, 0)
				.ReturnsForAnyArgs(info => new StepResult
				{ Input = String.Empty, StepNumber = 0 });

			workflow.WhenForAnyArgs(x => x.ScheduleStep(0, String.Empty, 3)).DoNotCallBase();
			workflow.ScheduleStep(0, String.Empty, 3)
				.ReturnsForAnyArgs(info => new StepDecision());

			workflow.ProcessActionCollection(context, state, schedulableState, action, step);

			//check if these methods were called
			workflow.Received().CallReducer(action, results);
			workflow.Received().ResolveStepNumber(step, String.Empty, 1);
			//1 is expected from schedulableState.StepNumber = 0 + 1
			workflow.Received().ScheduleStep(0, String.Empty, 0); // 0 is expected from schedulableState.AttemptNumber = 1 - 1;
		}

		[Theory, AutoData]
		internal void ProcessISuspendableSuccessTest(WorkflowState wfState, SchedulableState schedulableState)
		{
			var action = Substitute.For<SuspendableActivity<string, string>>("name", "taskList");

			var activityStep = Substitute.For<WorkflowStep>();
			activityStep.Action = action;
			activityStep.StepKey = "activityActionName";
			activityStep.StepNumber = 0;

			var stepResult = new StepResult<string>(string.Empty, true, activityStep.StepKey);
			var result = Utils.SerializeToJSON(stepResult);

			var context = Substitute.For<WorkflowDecisionContext>();
			context.Control = SchedulableStateSerializer.Serialize(schedulableState);
			context.ResultRef = result;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.WhenForAnyArgs(x => x.ResolveStepNumber(activityStep, string.Empty, 0)).DoNotCallBase();
			workflow.ResolveStepNumber(activityStep, string.Empty, 0)
				.ReturnsForAnyArgs(info => new StepResult
				{ Input = string.Empty, StepNumber = activityStep.StepNumber + 1 });

			workflow.WhenForAnyArgs(x => x.ScheduleStep(0, string.Empty, 3)).DoNotCallBase();
			workflow.ScheduleStep(0, string.Empty, 3)
				.ReturnsForAnyArgs(info => new StepDecision());

			workflow.ProcessISuspendable(context, wfState, activityStep, schedulableState);

			//check if these methods were called
			workflow.Received().ResolveStepNumber(activityStep, string.Empty, activityStep.StepNumber + 1);
			workflow.Received().ScheduleStep(activityStep.StepNumber + 1, string.Empty, 0);
		}

		[Theory, AutoData]
		internal void ProcessISuspendableSuspendTest(WorkflowState wfState, SchedulableState schedulableState)
		{
			schedulableState.AttemptNumber = 3;

			var action = Substitute.For<SuspendableActivity<string, string>>("name", "taskList");
			action.Input = string.Empty;
			action.Options.DelayTimeoutInSeconds = 30;

			var activityStep = Substitute.For<WorkflowStep>();
			activityStep.Action = action;
			activityStep.StepKey = "activityActionName";
			activityStep.StepNumber = 0;

			var stepResult = new StepResult<string>(string.Empty, false, activityStep.StepKey);
			var result = Utils.SerializeToJSON(stepResult);

			var context = Substitute.For<WorkflowDecisionContext>();
			context.Control = SchedulableStateSerializer.Serialize(schedulableState);
			context.ResultRef = result;
			context.InputRef = stepResult.ReturnValue;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.WhenForAnyArgs(x => x.ScheduleActivity(schedulableState, string.Empty, action)).DoNotCallBase();
			workflow.ScheduleActivity(schedulableState, string.Empty, action)
				.ReturnsForAnyArgs(info => new List<Decision>());

			workflow.ProcessISuspendable(context, wfState, activityStep, schedulableState);

			Assert.True(schedulableState.DelayTimeoutInSeconds == action.Options.DelayTimeoutInSeconds, "SchedulableState.DelayTimeoutInSeconds has not been updated.");
			Assert.True(schedulableState.AttemptNumber == 0, "SchedulableState.AttemptNumber has not been reset.");

			//check if these methods were called
			workflow.Received().ScheduleActivity(schedulableState, stepResult.ReturnValue, action);
		}

		[Theory, AutoData]
		internal void ProcessIActivitySuccessTest(WorkflowState wfState, SchedulableState schedulableState)
		{
			var action = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var activityStep = Substitute.For<WorkflowStep>();
			activityStep.Action = action;
			activityStep.StepKey = "activityActionName";
			activityStep.StepNumber = 0;

			var stepResult = new StepResult<string>(string.Empty, true, activityStep.StepKey);
			var result = Utils.SerializeToJSON(stepResult);

			var context = Substitute.For<WorkflowDecisionContext>();
			context.Control = SchedulableStateSerializer.Serialize(schedulableState);
			context.ResultRef = result;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.WhenForAnyArgs(x => x.ResolveStepNumber(activityStep, string.Empty, 0)).DoNotCallBase();
			workflow.ResolveStepNumber(activityStep, string.Empty, 0)
				.ReturnsForAnyArgs(info => new StepResult
				{ Input = string.Empty, StepNumber = activityStep.StepNumber + 1 });

			workflow.WhenForAnyArgs(x => x.ScheduleStep(0, string.Empty, 3)).DoNotCallBase();
			workflow.ScheduleStep(0, string.Empty, 3)
				.ReturnsForAnyArgs(info => new StepDecision());

			workflow.ProcessIActivity(context, activityStep, schedulableState);

			//check if these methods were called
			workflow.Received().ResolveStepNumber(activityStep, string.Empty, activityStep.StepNumber + 1);
			workflow.Received().ScheduleStep(activityStep.StepNumber + 1, string.Empty, 0);
		}

		[Theory, AutoData]
		internal void ProcessIActivityNotSuccessTest(WorkflowState wfState, SchedulableState schedulableState)
		{
			var action = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityActionName";
			step.StepNumber = 0;

			var stepResult = new StepResult<string>(string.Empty, false, step.StepKey);

			var context = Substitute.For<WorkflowDecisionContext>();
			context.Control = SchedulableStateSerializer.Serialize(schedulableState);
			context.ResultRef = Utils.SerializeToJSON(stepResult);

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.WhenForAnyArgs(x => x.FailWorkflow(string.Empty, string.Empty)).DoNotCallBase();
			workflow.FailWorkflow(string.Empty, string.Empty).ReturnsForAnyArgs(info => new Decision());

			workflow.ProcessIActivity(context, step, schedulableState);

			//check if these methods were called
			workflow.ReceivedWithAnyArgs().FailWorkflow(string.Empty, string.Empty);
		}

		[Theory, AutoData]
		internal void NextStepNullScheduleState(WorkflowDecisionContext decisionContext, WorkflowState wfState)
		{
			_workflow.NextStep(decisionContext, wfState);

			//check if these methods were called
			_workflow.ReceivedWithAnyArgs().FailWorkflow(string.Empty, string.Empty);
		}

		[Theory, AutoData]
		internal void NextStepNullStep(WorkflowDecisionContext decisionContext, WorkflowState wfState)
		{
			decisionContext.Control = SchedulableStateSerializer.Serialize(new SchedulableState());
			decisionContext.ResultRef = Utils.SerializeToJSON(new StepResult<string>());

			_workflow.GetStep(Arg.Any<int>()).Returns(info => null);

			_workflow.CompleteWorkflow(Arg.Any<string>())
				.Returns(info => new Decision());


			_workflow.NextStep(decisionContext, wfState);

			//check if these methods were called
			_workflow.ReceivedWithAnyArgs().CompleteWorkflow(string.Empty);
		}

		[Theory, AutoData]
		internal void NextStepParallelCollection(WorkflowDecisionContext decisionContext, WorkflowState wfState)
		{
			var state = new SchedulableState();
			decisionContext.Control = SchedulableStateSerializer.Serialize(state);
			decisionContext.ResultRef = Utils.SerializeToJSON(new StepResult<string>());

			var action = Substitute.For<ParallelActivityCollectionBase<string, string, string, string>>("name", "taskList");

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activity_ParallelCollection";
			step.StepNumber = 0;

			var workflow = Substitute.For<WorkflowBase>("domain", "NextStepParallelCollection", "version", "taskList", _amazonSwf);
			workflow.Steps.Add(step);

			workflow.GetStep(Arg.Any<int>()).Returns(info => step);

			workflow.ProcessActionCollection(decisionContext, wfState, state, action, step)
				.Returns(info => new StepDecision());

			workflow.ProcessIActivity(decisionContext, step, state)
				.Returns(info => new StepDecision());

			workflow.NextStep(decisionContext, wfState);

			//check if these methods were called
			workflow.Received().ProcessActionCollection(decisionContext, wfState, state, action, step);
			workflow.DidNotReceiveWithAnyArgs().ProcessIActivity(null, null, null);
		}

		[Theory, AutoData]
		internal void NextStepSuspendable(WorkflowDecisionContext decisionContext, WorkflowState wfState)
		{
			var state = new SchedulableState();
			decisionContext.Control = SchedulableStateSerializer.Serialize(state);
			decisionContext.ResultRef = Utils.SerializeToJSON(new StepResult<string>());

			var action = Substitute.For<SuspendableActivity<string, string>>("name", "taskList");

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityActionName";
			step.StepNumber = 0;

			_workflow.GetStep(Arg.Any<int>()).Returns(info => step);

			_workflow.ProcessISuspendable(decisionContext, wfState, step, state)
				.Returns(info => new StepDecision());

			_workflow.ProcessIActivity(decisionContext, step, state)
				.Returns(info => new StepDecision());

			_workflow.NextStep(decisionContext, wfState);

			//check if these methods were called
			_workflow.Received().ProcessISuspendable(decisionContext, wfState, step, state);
			_workflow.DidNotReceiveWithAnyArgs().ProcessIActivity(null, null, null);
		}

		[Theory, AutoData]
		internal void NextStepActivity(WorkflowDecisionContext decisionContext, WorkflowState wfState)
		{
			var state = new SchedulableState();
			decisionContext.Control = SchedulableStateSerializer.Serialize(state);
			decisionContext.ResultRef = Utils.SerializeToJSON(new StepResult<string>());

			var action = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityActionName";
			step.StepNumber = 0;

			_workflow.GetStep(Arg.Any<int>()).Returns(info => step);

			_workflow.ProcessIActivity(decisionContext, step, state)
				.Returns(info => new StepDecision());

			_workflow.NextStep(decisionContext, wfState);

			//check if these methods were called
			_workflow.Received().ProcessIActivity(decisionContext, step, state);
		}

		[Theory, AutoData]
		internal void ResolveStepNumberFoundStepTest([Frozen] WorkflowStep currentStep, [Frozen] string input)
		{
			var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");
			currentStep.Action = activity;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			//workflow.WhenForAnyArgs(x => x.GetNextStep(String.Empty, String.Empty)).DoNotCallBase();
			workflow.GetNextStep(String.Empty, String.Empty)
				.ReturnsForAnyArgs(info => new StepResult<object> { ReturnValue = input, Success = true, StepKey = currentStep.StepKey });

			workflow.Steps.Add(currentStep);

			var result = workflow.ResolveStepNumber(currentStep, input, 0);

			Assert.Equal(result.Input, input);
			Assert.Equal(result.StepNumber, currentStep.StepNumber);
		}

		[Theory, AutoData]
		internal void ResolveStepNumberNullStepTest([Frozen] WorkflowStep currentStep, [Frozen] string input)
		{
			int defaultStepNumber = 0;

			var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");
			currentStep.Action = activity;

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			//workflow.WhenForAnyArgs(x => x.GetNextStep(String.Empty, String.Empty)).DoNotCallBase();
			workflow.GetNextStep(String.Empty, String.Empty)
				.ReturnsForAnyArgs(info => new StepResult<object> { ReturnValue = input, Success = true, StepKey = currentStep.StepKey });

			var result = workflow.ResolveStepNumber(currentStep, input, defaultStepNumber);

			Assert.Equal(result.Input, input);
			Assert.Equal(result.StepNumber, defaultStepNumber);
		}


		[Fact]
		internal void ScheduleStep_NullStepTest()
		{
			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.GetStep(Arg.Any<int>())
				.Returns(info => null);

			workflow.CompleteWorkflow(Arg.Any<string>())
				.Returns(info => new Decision());

			workflow.ScheduleStep(0, string.Empty, 0);

			//check if these methods were called
			workflow.Received().GetStep(0);
			workflow.Received().CompleteWorkflow(string.Empty);
		}

		[Fact]
		internal void ScheduleStep_ParallelActivityCollectionTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var list = new List<ParallelCollectionItem<string>>();
			list.Add(new ParallelCollectionItem<string> {Input = input});

			var action = Substitute.For<IParallelActivityCollection>();
			action.Processor(input).Returns(info => list);
			action.Clone().Returns(info => (IActivity)action);

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityCollectionName";
			step.StepNumber = 0;

			workflow.GetStep(Arg.Any<int>())
				.Returns(info => step);

			workflow.When(x => x.ScheduleActivity(Arg.Any<SchedulableState>(), Arg.Any<string>(), (IActivity)action)).DoNotCallBase();
			workflow.ScheduleActivity(Arg.Any<SchedulableState>(), Arg.Any<string>(), (IActivity)action)
				.ReturnsForAnyArgs(info => new List<Decision>() { new Decision() { DecisionType = DecisionType.ScheduleActivityTask } });

			var stepDecision = workflow.ScheduleStep(0, input, 0);

			Assert.True(stepDecision.Decisions.Count == list.Count);

			//check if these methods were called
			workflow.Received().GetStep(step.StepNumber);
			action.Received().Processor(input);
			workflow.ReceivedWithAnyArgs().ScheduleActivity(null, input, action);
		}

		[Fact]
		internal void ScheduleStep_ParallelWorkflowCollectionTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var list = new List<ParallelCollectionItem<string>>();
			list.Add(new ParallelCollectionItem<string> { Input = input });

			var action = Substitute.For<IParallelWorkflowCollection>();
			action.Processor(input).Returns(info => list);
			action.Clone().Returns(info => (IWorkflow)action);

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "workflowCollectionName";
			step.StepNumber = 0;

			workflow.GetStep(Arg.Any<int>())
				.Returns(info => step);

			workflow.When(x => x.ScheduleChildWorkflow(Arg.Any<SchedulableState>(), Arg.Any<string>(), (IWorkflow)action)).DoNotCallBase();
			workflow.ScheduleChildWorkflow(Arg.Any<SchedulableState>(), Arg.Any<string>(), (IWorkflow)action)
				.ReturnsForAnyArgs(info => new Decision() { DecisionType = DecisionType.StartChildWorkflowExecution });

			var stepDecision = workflow.ScheduleStep(0, input, 0);

			Assert.True(stepDecision.Decisions.Count == list.Count);

			//check if these methods were called
			workflow.Received().GetStep(step.StepNumber);
			action.Received().Processor(input);
			workflow.ReceivedWithAnyArgs().ScheduleChildWorkflow(null, input, action);
		}

		[Fact]
		internal void ScheduleStep_ActivityTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IActivity>();

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "activityName";
			step.StepNumber = 0;

			workflow.GetStep(Arg.Any<int>())
				.Returns(info => step);

			workflow.When(x => x.ScheduleActivity(0, 1, step.StepNumber, step.StepKey, 0, input, action)).DoNotCallBase();
			workflow.ScheduleActivity(0, 1, step.StepNumber, step.StepKey, 0, input, action)
				.ReturnsForAnyArgs(info => new List<Decision>() { new Decision() { DecisionType = DecisionType.ScheduleActivityTask } });

			var stepDecision = workflow.ScheduleStep(0, input, 0);

			//check if these methods were called
			workflow.Received().GetStep(step.StepNumber);
			workflow.Received().ScheduleActivity(0, 1, step.StepNumber, step.StepKey, 0, input, action);
		}

		[Fact]
		internal void ScheduleStep_WorkflowTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IWorkflow>();

			var step = Substitute.For<WorkflowStep>();
			step.Action = action;
			step.StepKey = "workflowCollectionName";
			step.StepNumber = 0;

			workflow.GetStep(Arg.Any<int>())
				.Returns(info => step);

			workflow.When(x => x.ScheduleChildWorkflow(0, 1, step.StepNumber, step.StepKey, 0, input, action)).DoNotCallBase();
			workflow.ScheduleChildWorkflow(0, 1, step.StepNumber, step.StepKey, 0, input, action)
				.ReturnsForAnyArgs(info => new Decision() { DecisionType = DecisionType.StartChildWorkflowExecution });

			var stepDecision = workflow.ScheduleStep(0, input, 0);

			//check if these methods were called
			workflow.Received().GetStep(step.StepNumber);
			workflow.ReceivedWithAnyArgs().ScheduleChildWorkflow(0, 1, step.StepNumber, step.StepKey, 0, input, action);
		}


		[Fact]
		internal void ScheduleActivity_ActivityTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IActivity>();
			action.Name.Returns("TestActivity");
			action.MaxAttempts = 3;
			action.Options.DelayTimeoutInSeconds = 0;

			var state = new SchedulableState
			{
				StepNumber = 0,
				StepKey = "activityKey",
				ActionNumber = 0,
				AttemptNumber = 1,
				MaxAttempts = action.MaxAttempts,
				TotalActions = 1,
				DelayTimeoutInSeconds = action.Options.DelayTimeoutInSeconds
			};
			
			//workflow.When(x => x.ScheduleActivity(0, 1, step.StepNumber, step.StepKey, 0, input, action)).DoNotCallBase();
			//workflow.ScheduleActivity(0, 1, step.StepNumber, step.StepKey, 0, input, action)
			//	.ReturnsForAnyArgs(info => new List<Decision>() { new Decision() { DecisionType = DecisionType.ScheduleActivityTask } });

			var decisions = workflow.ScheduleActivity(state, input, action);

			Assert.True(decisions.Count == 1, "The only one decision is applicable for a simple non-delayed activity.");
			Assert.True(decisions[0].DecisionType == DecisionType.ScheduleActivityTask,
				$"Incorrect decision type [{decisions[0].DecisionType}].");
		}

		[Fact]
		internal void ScheduleActivity_ActivityTimerTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IActivity>();
			action.Name.Returns("TestActivity");
			action.MaxAttempts = 3;
			action.Options.DelayTimeoutInSeconds = 100;

			var state = new SchedulableState
			{
				StepNumber = 0,
				StepKey = "activityKey",
				ActionNumber = 0,
				AttemptNumber = 1,
				MaxAttempts = action.MaxAttempts,
				TotalActions = 1,
				DelayTimeoutInSeconds = action.Options.DelayTimeoutInSeconds
			};

			var decisions = workflow.ScheduleActivity(state, input, action);

			Assert.True(decisions.Count == 2, "Incorrect decisions count.");
			Assert.True(decisions[0].DecisionType == DecisionType.StartTimer,
				$"The first decision [{decisions[0].DecisionType}] is incorrect.");
			Assert.True(decisions[1].DecisionType == DecisionType.RecordMarker,
				$"The second decision [{decisions[1].DecisionType}] is incorrect.");
		}

		[Fact]
		internal void ScheduleActivity_SuspendableTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IActivity, ISuspendable>();
			action.Name.Returns("TestActivity");
			action.MaxAttempts = 3;
			((ISuspendable)action).WaitingTimeInSeconds = (100);

			var state = new SchedulableState
			{
				StepNumber = 0,
				StepKey = "activityKey",
				ActionNumber = 0,
				AttemptNumber = 1,
				MaxAttempts = action.MaxAttempts,
				TotalActions = 1,
				DelayTimeoutInSeconds = action.Options.DelayTimeoutInSeconds,
				StartedDate = DateTime.UtcNow
			};

			var decisions = workflow.ScheduleActivity(state, input, action);

			Assert.True(state.StartedDate != DateTime.MinValue);

			Assert.True(decisions.Count == 1, "The only one decision is applicable for a simple non-delayed activity.");
			Assert.True(decisions[0].DecisionType == DecisionType.ScheduleActivityTask,
				$"Incorrect decision type [{decisions[0].DecisionType}].");
		}

		[Fact]
		internal void ScheduleActivity_SuspendableExceededTimeTest()
		{
			string input = "input data";

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var action = Substitute.For<IActivity, ISuspendable>();
			action.Name.Returns("TestActivity");
			action.MaxAttempts = 3;
			((ISuspendable)action).WaitingTimeInSeconds = (100);

			var state = new SchedulableState
			{
				StepNumber = 0,
				StepKey = "activityKey",
				ActionNumber = 0,
				AttemptNumber = 1,
				MaxAttempts = action.MaxAttempts,
				TotalActions = 1,
				DelayTimeoutInSeconds = action.Options.DelayTimeoutInSeconds,
				StartedDate = DateTime.UtcNow.AddSeconds(-((ISuspendable)action).WaitingTimeInSeconds -1) // Was started 100 seconds ago.
			};

			var decisions = workflow.ScheduleActivity(state, input, action);

			workflow.Received().FailWorkflow(action.ActivityId, "Suspendable activity exceeded waiting time.");
		}


		[Fact]
		internal void RetryScheduled_NullStateTest()
		{
			WorkflowState wfState = new WorkflowState(0);

			WorkflowDecisionContext context = Substitute.For<WorkflowDecisionContext>();

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			workflow.RetryScheduled(context, wfState);

			workflow.Received().FailWorkflow(context.Details, "State is undefined.");
		}

		[Fact]
		internal void RetryScheduled_GoodAttemptsTest()
		{
			WorkflowState wfState = new WorkflowState(0);

			SchedulableState state = new SchedulableState()
			{
				StepNumber = 0,
				StepKey = "testActivity",
				ActionNumber = 1,
				AttemptNumber = 1,
				MaxAttempts = 1,
				TotalActions = 1,
				DelayTimeoutInSeconds = 0,
			};

			var context = Substitute.For<WorkflowDecisionContext>();
			context.Control = SchedulableStateSerializer.Serialize(state);

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			/*workflow.RetryScheduled(context, wfState);

			workflow.Received().FailWorkflow(context.Details, "State is undefined.");
			 */
		}


		[Fact]
		internal void CallReducerTest()
		{
			Dictionary<int, string> results = new Dictionary<int, string>();
			var step1 = new StepResult<string> { ReturnValue = "{\"Amount\":10000.0}", StepKey = "1", Success = true };
			var step2 = new StepResult<string> { ReturnValue = "{\"Amount\":20000.0}", StepKey = "2", Success = true };
			results[0] = Utils.SerializeToJSON(step1);
			results[1] = Utils.SerializeToJSON(step2);

			var exprectedList = string.Join(",", new List<string> {step1.ReturnValue, step2.ReturnValue});

			var workflow = Substitute.ForPartsOf<WorkflowBase>("domain", "name", "version", "taskList", _amazonSwf);

			var parallelCollection = Substitute.For<IParallelCollection>();
			parallelCollection.Reducer(Arg.Any<IEnumerable<string>>())
				.Returns(info => 
				{
					var list = info.Arg<List<string>>();
					return string.Join(",", list);
				});

			var result = workflow.CallReducer(parallelCollection, results);

			Assert.Equal(exprectedList, result);
		}


		[Fact]
		internal void WorkflowEventArgs_CtorTest()
		{
			object data = "some object";

			var evtArgs = new AIR.SDK.Workflow.WorkflowEventArgs(data);

			Assert.True(evtArgs.Data.Equals(data), "WorkflowEventArgs.Data is undefined.");
		}

		[Fact]
		internal void WorkflowEventArgs_Ctor2Test()
		{
			var details = "details";
			var reason = "some reason";

			var evtArgs = new AIR.SDK.Workflow.WorkflowEventArgs(details, reason);

			Assert.True(evtArgs.Details == details, "WorkflowEventArgs.Details is undefined.");
			Assert.True(evtArgs.Reason == reason, "WorkflowEventArgs.Reason is undefined.");
		}

		[Fact]
		internal void Workflow_CloneTest()
		{
			var workflow = new WorkflowBase("domain", "workflowName", "version", "taskList", _amazonSwf);

			var wfClone = workflow.Clone();

			Assert.False(Object.ReferenceEquals(workflow, wfClone));
		}
	}
}