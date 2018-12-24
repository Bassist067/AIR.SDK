using System.Collections.Concurrent;
using System.Net;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Tests.TestUtils;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.WorkflowEventsProcessor
{
	/// <summary>
	/// 
	/// </summary>
	public class WorkflowEventsProcessorTests
	{
		private readonly string _executionContext;


		private WorkflowEventsIterator _workflowEventsIterator;

		private readonly DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
		private readonly IAmazonSimpleWorkflow _amazonSwf;
		private readonly WorkflowBase _workflowBase;
		private SDK.Workflow.WorkflowEventsProcessor _processor;

		public WorkflowEventsProcessorTests()
		{
			_decisionTask = Substitute.For<DecisionTask>();
			_decisionTask.WorkflowType = new WorkflowType {Name = "TestWorkflow", Version = "TestVersion"};
			_decisionTask.WorkflowExecution = new WorkflowExecution {RunId = "TestRunId", WorkflowId = ""};

			var results = new ConcurrentDictionary<int, string>();
			results.AddOrUpdate(1, "TestResult", UpdateValueFactory);

			_executionContext = WorkflowStateSerializer.Serialize(new WorkflowState()
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


			//_amazonSwf.DescribeWorkflowExecution(describeWorkflowExecutionRequest)
			//	.ReturnsForAnyArgs(
			//		info =>
			//			new DescribeWorkflowExecutionResponse()
			//			{
			//				HttpStatusCode = HttpStatusCode.OK,
			//				WorkflowExecutionDetail = new WorkflowExecutionDetail() {LatestExecutionContext = _executionContext}
			//			});


			SDK.Workflow.WorkflowEventsProcessor processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase,
				_pollforDecisionTaskRequest, _amazonSwf);

			processor.GetLastExecContext().ReturnsForAnyArgs(info => WorkflowStateSerializer.Deserialize(_executionContext));

			//_workflowEventsIterator = Substitute.For<WorkflowEventsIterator>(_decisionTask, _pollforDecisionTaskRequest,
			//	_amazonSwf);
		}

		private string UpdateValueFactory(int i, string s)
		{
			return $"{i} - {s}";
		}


		/// <summary>
		/// Testing <see cref="WorkflowEventsProcessor.GetLastExecContext"/>
		/// </summary>
		[Fact]
		public void WorkflowStateGetLastExecContextTest()
		{
			var historyEvent = Substitute.For<HistoryEvent>();
			historyEvent.EventId = 10;
			historyEvent.DecisionTaskCompletedEventAttributes = new DecisionTaskCompletedEventAttributes
			{
				ExecutionContext = _executionContext
			};
			historyEvent.EventType = EventType.DecisionTaskCompleted;
			_decisionTask.Events.Add(historyEvent);

			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase,
				_pollforDecisionTaskRequest, _amazonSwf);

			var describeWorkflowExecutionRequest = Substitute.For<DescribeWorkflowExecutionRequest>();
			describeWorkflowExecutionRequest.Domain = _pollforDecisionTaskRequest.Domain;
			describeWorkflowExecutionRequest.Execution = _decisionTask.WorkflowExecution;


			//_amazonSwf.DescribeWorkflowExecution(describeWorkflowExecutionRequest)
			//	.ReturnsForAnyArgs(
			//		info =>
			//			new DescribeWorkflowExecutionResponse()
			//			{
			//				HttpStatusCode = HttpStatusCode.OK,
			//				WorkflowExecutionDetail = new WorkflowExecutionDetail() {LatestExecutionContext = _executionContext}
			//			});


			var result = _processor.GetLastExecContext();

			AssertObjectEquals.PropertyValuesAreEqual(WorkflowStateSerializer.Deserialize(_executionContext), result);
		}

		//[Fact]
		//public void WorkflowExecutionStartedTest()
		//{
		//	_decisionTask.Events.Add(new HistoryEvent()
		//	{
		//		EventType = EventType.WorkflowExecutionStarted,
		//		WorkflowExecutionStartedEventAttributes = new WorkflowExecutionStartedEventAttributes
		//		{
		//			Input = "TestInput",
		//			ParentWorkflowExecution = null
		//		}
		//	});

		//	var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");

		//	var activityStep = Substitute.For<WorkflowStep>();
		//	activityStep.Action = activity;
		//	activityStep.StepKey = "activityActionName";
		//	activityStep.StepNumber = 0;
		//	_workflowBase.Steps.Add(activityStep);

		//	//var historyEvent = Substitute.For<HistoryEvent>();
		//	//historyEvent.EventId = 10;
		//	//historyEvent.DecisionTaskCompletedEventAttributes = new DecisionTaskCompletedEventAttributes
		//	//{
		//	//	ExecutionContext = _executionContext
		//	//};
		//	//historyEvent.EventType = EventType.DecisionTaskCompleted;
		//	//_decisionTask.Events.Add(historyEvent);
		//	_processor = new WorkflowEventsProcessor(_decisionTask, _workflowBase,
		//		_pollforDecisionTaskRequest, _amazonSwf);


		//	var result = _processor.Decide();

		//	var decision = result.Decisions.FirstOrDefault(d => d.DecisionType == DecisionType.ScheduleActivityTask);
		//	Assert.NotNull(decision);
		//}
	}
}