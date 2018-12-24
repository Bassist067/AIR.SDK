using System;
using System.Collections.Concurrent;
using System.Linq;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Context;
using AIR.SDK.Workflow.Core;
using AIR.SDK.Workflow.Tests.TestUtils;
using AutoFixture.Xunit2;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.WorkflowEventsProcessorTests
{
	public class MakeDecisionTests
	{
		private RespondDecisionTaskCompletedRequest _decisionCompletedRequest;
		private readonly DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
		private readonly IAmazonSimpleWorkflow _amazonSwf;
		private readonly WorkflowBase _workflowBase;
		private SDK.Workflow.WorkflowEventsProcessor _processor;

		public MakeDecisionTests()
		{
			_decisionTask = Substitute.For<DecisionTask>();
			_decisionTask.WorkflowType = new WorkflowType {Name = "TestWorkflow", Version = "TestVersion"};
			_decisionTask.WorkflowExecution = new WorkflowExecution {RunId = "TestRunId", WorkflowId = ""};

			var results = new ConcurrentDictionary<int, string>();
			results.AddOrUpdate(1, "TestResult", (key, value) => $"{key} - {value}");

			WorkflowStateSerializer.Serialize(new WorkflowState()
			{
				CurrentStepNumber = 1,
				NumberOfActions = 1,
				Results = results
			});


			_pollforDecisionTaskRequest = Substitute.For<PollForDecisionTaskRequest>();
			_pollforDecisionTaskRequest.Domain = "TestDomain";
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();
			_workflowBase = Substitute.For<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);

			var describeWorkflowExecutionRequest = Substitute.For<DescribeWorkflowExecutionRequest>();
			describeWorkflowExecutionRequest.Domain = _pollforDecisionTaskRequest.Domain;
			describeWorkflowExecutionRequest.Execution = _decisionTask.WorkflowExecution;
		}

		#region MakeDecisionTest

		[Theory, AutoData]
		public void MakeDecision_NullDeciderTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.WorkflowExecutionStarted };

			_decisionCompletedRequest = _processor.MakeDecision(null, decisionContext);

			var decision = _decisionCompletedRequest.Decisions.First();
			Assert.NotNull(decision);
			Assert.Equal(DecisionType.FailWorkflowExecution, decision.DecisionType);
			Assert.NotNull(decision.FailWorkflowExecutionDecisionAttributes);
			Assert.Equal("Decider not found.", decision.FailWorkflowExecutionDecisionAttributes.Reason);
		}

		[Theory, AutoData]
		public void MakeDecision_UnhandledDecisionTypeTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.LambdaFunctionTimedOut };

			Assert.Throws<InvalidOperationException>(() => _processor.MakeDecision(decider, decisionContext) );
		}

		[Theory, AutoData]
		public void MakeDecision_WorkflowExecutionStartedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.WorkflowExecutionStarted};

			decider.OnWorkflowExecutionStarted(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		//[Theory, AutoData]
		//public void MakeDecision_WorkflowExecutionContinuedAsNewTest([Frozen] IDecider decider, [Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		//{
		//    _processor = new WorkflowEventsProcessor(_decisionTask, _workflowBase,
		//	    _pollforDecisionTaskRequest, _amazonSwf);

		//    decider.OnWorkflowExecutionContinuedAsNew(_decisionContext).Returns(info => respondDecisionTask);
		//    _decisionContext.DecisionType = EventType.WorkflowExecutionContinuedAsNew;

		//    _decisionCompletedRequest = _processor.MakeDecision(decider, _decisionContext);
		//    AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		//}

		[Theory, AutoData]
		public void MakeDecisionActivityTaskCompletedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ActivityTaskCompleted };

			decider.OnActivityTaskCompleted(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ActivityTaskFailedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ActivityTaskFailed };

			decider.OnActivityTaskFailed(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ActivityTaskTimedOutTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ActivityTaskTimedOut };

			decider.OnActivityTaskTimedOut(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ScheduleActivityTaskFailedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ScheduleActivityTaskFailed };

			decider.OnScheduleActivityTaskFailed(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_StartChildWorkflowExecutionInitiatedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.StartChildWorkflowExecutionInitiated };

			decider.OnStartChildWorkflowExecutionInitiated(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ChildWorkflowExecutionStartedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ChildWorkflowExecutionStarted };

			decider.OnChildWorkflowExecutionStarted(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ChildWorkflowExecutionCompletedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ChildWorkflowExecutionCompleted };

			decider.OnChildWorkflowExecutionCompleted(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ChildWorkflowExecutionFailedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ChildWorkflowExecutionFailed };

			decider.OnChildWorkflowExecutionFailed(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ChildWorkflowExecutionTerminatedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ChildWorkflowExecutionTerminated };

			decider.OnChildWorkflowExecutionTerminated(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_ChildWorkflowExecutionTimedOutTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.ChildWorkflowExecutionTimedOut };

			decider.OnChildWorkflowExecutionTimedOut(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_StartChildWorkflowExecutionFailedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.StartChildWorkflowExecutionFailed };

			decider.OnStartChildWorkflowExecutionFailed(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_TimerStartedTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.TimerStarted };

			decider.OnTimerStarted(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_TimerFiredTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.StartChildWorkflowExecutionInitiated };

			decider.OnStartChildWorkflowExecutionInitiated(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		[Theory, AutoData]
		public void MakeDecision_TimerCanceledTest([Frozen] IDecider decider,
			[Frozen] RespondDecisionTaskCompletedRequest respondDecisionTask)
		{
			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			var decisionContext = new WorkflowDecisionContext() { DecisionType = EventType.TimerCanceled };

			decider.OnTimerCanceled(decisionContext).Returns(info => respondDecisionTask);

			_decisionCompletedRequest = _processor.MakeDecision(decider, decisionContext);
			AssertObjectEquals.PropertyValuesAreEqual(_decisionCompletedRequest.Decisions.First(), respondDecisionTask.Decisions.First());
		}

		#endregion
	}
}