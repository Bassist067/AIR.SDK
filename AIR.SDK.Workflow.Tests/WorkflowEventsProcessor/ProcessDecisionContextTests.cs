using System.Collections.Concurrent;
using System.Net;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Context;
using AIR.SDK.Workflow.Tests.TestUtils;
using AutoFixture.Xunit2;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.WorkflowEventsProcessor
{
	public class ProcessDecisionContextTests
	{
	    private WorkflowDecisionContext _decisionContext;

	    private readonly string _executionContext;


	    private WorkflowEventsIterator _workflowEventsIterator;

	    private readonly DecisionTask _decisionTask;
	    private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
	    private readonly IAmazonSimpleWorkflow _amazonSwf;
	    private readonly WorkflowBase _workflowBase;
	    private SDK.Workflow.WorkflowEventsProcessor _processor;


	    public ProcessDecisionContextTests()
	    {
			_decisionTask = Substitute.For<DecisionTask>();
			_decisionTask.WorkflowType = new WorkflowType {Name = "TestWorkflow", Version = "TestVersion"};
			_decisionTask.WorkflowExecution = new WorkflowExecution {RunId = "TestRunId", WorkflowId = ""};

			var results = new ConcurrentDictionary<int, string>();
			results.AddOrUpdate(1, "TestResult", (key, value) => $"{key} - {value}");

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


			//_processor = Substitute.For<WorkflowEventsProcessor>(_decisionTask, _workflowBase,
			//	_pollforDecisionTaskRequest, _amazonSwf);
		}

		#region ProcessDecisionContextTest

		//TODO cover ParentWorkflowTest
		[Theory, AutoData]
		public void ProcessDecisionContextWorkflowExecutionStartedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.WorkflowExecutionStarted;
			historyEvent.WorkflowExecutionStartedEventAttributes = new WorkflowExecutionStartedEventAttributes
			{
				Input = "TestInput",
				ParentWorkflowExecution = null
			};

			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Input, historyEvent.WorkflowExecutionStartedEventAttributes.Input);
			Assert.Equal(_decisionContext.StartingInput, historyEvent.WorkflowExecutionStartedEventAttributes.Input);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextWorkflowExecutionContinuedAsNewTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.WorkflowExecutionContinuedAsNew;
			historyEvent.WorkflowExecutionContinuedAsNewEventAttributes = new WorkflowExecutionContinuedAsNewEventAttributes
			{
				Input = "TestInput",
			};

			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Input, historyEvent.WorkflowExecutionContinuedAsNewEventAttributes.Input);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextWorkflowExecutionCancelRequestedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.WorkflowExecutionCancelRequested;
			historyEvent.WorkflowExecutionCancelRequestedEventAttributes = new WorkflowExecutionCancelRequestedEventAttributes
			{
				Cause = "TestCause",
			};

			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Cause, historyEvent.WorkflowExecutionCancelRequestedEventAttributes.Cause);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextDecisionTaskCompletedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.DecisionTaskCompleted;
			historyEvent.DecisionTaskCompletedEventAttributes = new DecisionTaskCompletedEventAttributes
			{
				ScheduledEventId = 10,
				StartedEventId = 10,
				ExecutionContext = _executionContext
			};

			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			AssertObjectEquals.PropertyValuesAreEqual(_decisionContext.ExecutionContext, WorkflowStateSerializer.Deserialize(_executionContext));

			Assert.Equal(_decisionContext.StartedEventId,
				historyEvent.DecisionTaskCompletedEventAttributes.StartedEventId);
			Assert.Equal(_decisionContext.ScheduledEventId,
				historyEvent.DecisionTaskCompletedEventAttributes.ScheduledEventId);
		}

		//TODO cover timer removal test
		[Theory, AutoData]
		public void ProcessDecisionContextActivityTaskScheduledTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ActivityTaskScheduled;
			historyEvent.ActivityTaskScheduledEventAttributes = new ActivityTaskScheduledEventAttributes
			{
				ActivityType = new ActivityType {Name = "TestActivity", Version = "1.0"},
				ActivityId = "TestActivityID",
				Control = "ImportantData"
			};


			_processor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.ActivityId, historyEvent.ActivityTaskScheduledEventAttributes.ActivityId);
			Assert.Equal(_decisionContext.ActivityVersion,
				historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Version);
			Assert.Equal(_decisionContext.ActivityName,
				historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Name);
			Assert.Equal(_decisionContext.Control, historyEvent.ActivityTaskScheduledEventAttributes.Control);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextActivityTaskCompletedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ActivityTaskCompleted;
			historyEvent.ActivityTaskCompletedEventAttributes = new ActivityTaskCompletedEventAttributes
			{
				ScheduledEventId = 1,
				StartedEventId = 0,
				Result = "Success!"
			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.ActivityTaskScheduledEventAttributes = new ActivityTaskScheduledEventAttributes
			{
				ActivityType = new ActivityType {Name = "TestActivity", Version = "1.0"},
				ActivityId = "TestActivityID",
				Control = "ImportantData"
			};


			//var activity = Substitute.For<ActivityBase<string, string>>("name", "taskList");

			//var activityStep = Substitute.For<WorkflowStep>();
			//activityStep.Action = activity;
			//activityStep.StepKey = "activityActionName";
			//activityStep.StepNumber = 0;
			//_workflowBase.Steps.Add(activityStep);


			//var activity1 = Substitute.For<ActivityBase<string, string>>("name1", "taskList");
			//var activityStep1 = Substitute.For<WorkflowStep>();
			//activityStep.Action = activity1;
			//activityStep.StepKey = "activityActionName";
			//activityStep.StepNumber = 1;
			//_workflowBase.Steps.Add(activityStep1);


			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.ActivityId,
				newShcheduledEvent.ActivityTaskScheduledEventAttributes.ActivityId);
			Assert.Equal(_decisionContext.Control, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Input);

			Assert.Equal(_decisionContext.ScheduledEventId,
				historyEvent.ActivityTaskCompletedEventAttributes.ScheduledEventId);
			Assert.Equal(_decisionContext.StartedEventId,
				historyEvent.ActivityTaskCompletedEventAttributes.StartedEventId);
			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Result, historyEvent.ActivityTaskCompletedEventAttributes.Result);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextActivityTaskFailedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ActivityTaskFailed;
			historyEvent.ActivityTaskFailedEventAttributes = new ActivityTaskFailedEventAttributes
			{
				ScheduledEventId = 1,
				StartedEventId = 0,
				Reason = "Because it should fail",
				Details = "Who does really care"
			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.ActivityTaskScheduledEventAttributes = new ActivityTaskScheduledEventAttributes
			{
				ActivityType = new ActivityType {Name = "TestActivity", Version = "1.0"},
				ActivityId = "TestActivityID",
				Control = "ImportantData"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.ActivityId,
				newShcheduledEvent.ActivityTaskScheduledEventAttributes.ActivityId);
			Assert.Equal(_decisionContext.Control, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Input);

			Assert.Equal(_decisionContext.ScheduledEventId,
				historyEvent.ActivityTaskFailedEventAttributes.ScheduledEventId);
			Assert.Equal(_decisionContext.StartedEventId,
				historyEvent.ActivityTaskFailedEventAttributes.StartedEventId);
			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Reason, historyEvent.ActivityTaskFailedEventAttributes.Reason);
			Assert.Equal(_decisionContext.Details, historyEvent.ActivityTaskFailedEventAttributes.Details);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextActivityTaskTimedOutTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ActivityTaskTimedOut;
			historyEvent.ActivityTaskTimedOutEventAttributes = new ActivityTaskTimedOutEventAttributes
			{
				ScheduledEventId = 1,
				StartedEventId = 0,
				TimeoutType = ActivityTaskTimeoutType.HEARTBEAT,
				Details = "Who does really care"
			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.ActivityTaskScheduledEventAttributes = new ActivityTaskScheduledEventAttributes
			{
				ActivityType = new ActivityType {Name = "TestActivity", Version = "1.0"},
				ActivityId = "TestActivityID",
				Control = "ImportantData"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.ActivityId,
				newShcheduledEvent.ActivityTaskScheduledEventAttributes.ActivityId);
			Assert.Equal(_decisionContext.Control, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.ActivityTaskScheduledEventAttributes.Input);

			Assert.Equal(_decisionContext.ScheduledEventId,
				historyEvent.ActivityTaskTimedOutEventAttributes.ScheduledEventId);
			Assert.Equal(_decisionContext.StartedEventId,
				historyEvent.ActivityTaskTimedOutEventAttributes.StartedEventId);
			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.TimeoutType, historyEvent.ActivityTaskTimedOutEventAttributes.TimeoutType);
			Assert.Equal(_decisionContext.Details, historyEvent.ActivityTaskTimedOutEventAttributes.Details);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextScheduleActivityTaskFailedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ScheduleActivityTaskFailed;
			historyEvent.ScheduleActivityTaskFailedEventAttributes = new ScheduleActivityTaskFailedEventAttributes
			{
				ActivityType = new ActivityType {Name = "TestActivity", Version = "1.0"},
				ActivityId = "TestActivityID",
				DecisionTaskCompletedEventId = 1,
				Cause = "Nothing to schedule"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.ActivityId,
				historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityId);
			Assert.Equal(_decisionContext.ActivityVersion,
				historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version);
			Assert.Equal(_decisionContext.ActivityName,
				historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name);
			Assert.Equal(_decisionContext.DecisionTaskCompletedEventId,
				historyEvent.ScheduleActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId);
			Assert.Equal(_decisionContext.Cause,
				historyEvent.ScheduleActivityTaskFailedEventAttributes.Cause);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextStartChildWorkflowExecutionInitiatedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.StartChildWorkflowExecutionInitiated;
			historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes = new StartChildWorkflowExecutionInitiatedEventAttributes
			{
				Input = "TestInput",
				Control = "AdditionalData",
				WorkflowType = new WorkflowType
				{
					Name = "WorkflowName",
					Version = "Workflow Version"
				}
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.Control,
				historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control);
			Assert.Equal(_decisionContext.Input,
				historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Input);
			Assert.Equal(_decisionContext.ChildWorkflowName,
				historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion,
				historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextChildWorkflowExecutionStartedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ChildWorkflowExecutionStarted;
			historyEvent.ChildWorkflowExecutionStartedEventAttributes = new ChildWorkflowExecutionStartedEventAttributes
			{
				WorkflowType = new WorkflowType
				{
					Name = "WorkflowName",
					Version = "Workflow Version"
				},
				WorkflowExecution = new WorkflowExecution
				{
					RunId = "TestRunID",
					WorkflowId = "TestWorkflowID"
				}
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.ChildWorkflowName,
				historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion,
				historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.WorkflowExecRunId,
				historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.RunId);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextChildWorkflowExecutionCompletedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ChildWorkflowExecutionCompleted;
			historyEvent.ChildWorkflowExecutionCompletedEventAttributes = new ChildWorkflowExecutionCompletedEventAttributes
			{
			    WorkflowType = new WorkflowType
			    {
				   Name = "WorkflowName",
				   Version = "Workflow Version"
			    },
			    Result = "Success!",

			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes = new StartChildWorkflowExecutionInitiatedEventAttributes
			{
			    Control = "ImportantData",
			};


			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.ChildWorkflowName,historyEvent.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion, historyEvent.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.Result, historyEvent.ChildWorkflowExecutionCompletedEventAttributes.Result);

			Assert.Equal(_decisionContext.Control,
				newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextChildWorkflowExecutionFailedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ChildWorkflowExecutionFailed;
			historyEvent.ChildWorkflowExecutionFailedEventAttributes = new ChildWorkflowExecutionFailedEventAttributes
			{
			    WorkflowType = new WorkflowType
			    {
				   Name = "WorkflowName",
				   Version = "Workflow Version"
			    },

			    Details = "It all should fail some time",
			    Reason = "Nobody cares",

			    WorkflowExecution = new WorkflowExecution
			    {
				    RunId = "FailedRunID"
			    }
			};


			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes = new StartChildWorkflowExecutionInitiatedEventAttributes
			{
			    Control = "ImportantData",
			    Input = "FailedInput"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.ChildWorkflowName, historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion, historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.Reason, historyEvent.ChildWorkflowExecutionFailedEventAttributes.Reason);
			Assert.Equal(_decisionContext.Details, historyEvent.ChildWorkflowExecutionFailedEventAttributes.Details);
			Assert.Equal(_decisionContext.WorkflowExecRunId, historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowExecution.RunId);

			Assert.Equal(_decisionContext.Control, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Input);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextChildWorkflowExecutionTerminatedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ChildWorkflowExecutionTerminated;
			historyEvent.ChildWorkflowExecutionTerminatedEventAttributes = new ChildWorkflowExecutionTerminatedEventAttributes
			{
			    WorkflowType = new WorkflowType
			    {
				   Name = "WorkflowName",
				   Version = "Workflow Version"
			    },


			    WorkflowExecution = new WorkflowExecution
			    {
				   RunId = "FailedRunID"
			    }

			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes = new StartChildWorkflowExecutionInitiatedEventAttributes
			{
			    Control = "ImportantData",
			    Input = "FailedInput"
			};


			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);
			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);


			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.ChildWorkflowName, historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion, historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.WorkflowExecRunId, historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowExecution.RunId);

			Assert.Equal(_decisionContext.Control, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Input);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextChildWorkflowExecutionTimedOutTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.ChildWorkflowExecutionTimedOut;
			historyEvent.ChildWorkflowExecutionTimedOutEventAttributes = new ChildWorkflowExecutionTimedOutEventAttributes
			{
			    WorkflowType = new WorkflowType
			    {
				   Name = "WorkflowName",
				   Version = "Workflow Version"
			    },

			    TimeoutType = WorkflowExecutionTimeoutType.START_TO_CLOSE,

			    WorkflowExecution = new WorkflowExecution
			    {
				   RunId = "FailedRunID"
			    }
			    
			};

			var newShcheduledEvent = Substitute.For<HistoryEvent>();
			newShcheduledEvent.EventType = EventType.ActivityTaskScheduled;
			newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes = new StartChildWorkflowExecutionInitiatedEventAttributes
			{
			    Control = "ImportantData",
			    Input = "FailedInput"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);
			_processor.FindEventTypeById(1).ReturnsForAnyArgs(info => newShcheduledEvent);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.ChildWorkflowName, historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion, historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.WorkflowExecRunId, historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowExecution.RunId);
			Assert.Equal(_decisionContext.TimeoutType, historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.TimeoutType);

			Assert.Equal(_decisionContext.Control, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control);
			Assert.Equal(_decisionContext.Input, newShcheduledEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Input);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextMarkerRecordedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.MarkerRecorded;
			historyEvent.MarkerRecordedEventAttributes = new MarkerRecordedEventAttributes
			{ 
			    MarkerName = "TestMarker"
			    ,Details = "MarkerDetails"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.Markers["TestMarker"], historyEvent.MarkerRecordedEventAttributes.Details);
		}

		[Theory, AutoData]
		public void ProcessDecisionContextStartChildWorkflowExecutionFailedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.StartChildWorkflowExecutionFailed;
			historyEvent.StartChildWorkflowExecutionFailedEventAttributes = new StartChildWorkflowExecutionFailedEventAttributes
			{
			    WorkflowType = new WorkflowType
			    {
				    Name = "WorkflowName",
				    Version = "1.0"
			    },
			    Cause = "Nobody knows",
			    Control = "Important Data"
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);

			Assert.Equal(_decisionContext.ChildWorkflowName, historyEvent.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name);
			Assert.Equal(_decisionContext.ChildWorkflowVersion, historyEvent.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version);
			Assert.Equal(_decisionContext.Cause, historyEvent.StartChildWorkflowExecutionFailedEventAttributes.Cause);
			Assert.Equal(_decisionContext.Control, historyEvent.StartChildWorkflowExecutionFailedEventAttributes.Control);

		}

		[Theory, AutoData]
		public void ProcessDecisionContextTimerStartedTest([Frozen] HistoryEvent historyEvent)
		{
			historyEvent.EventType = EventType.TimerStarted;
			historyEvent.TimerStartedEventAttributes = new TimerStartedEventAttributes
			{
			    TimerId = "TestID",
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_processor.ProcessDecisionContext(historyEvent, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent.EventType);
			Assert.Equal(_decisionContext.TimerId, historyEvent.TimerStartedEventAttributes.TimerId);
			Assert.Equal(_decisionContext.Timers["TestID"], historyEvent.TimerStartedEventAttributes);
		    
		}

	    //TODO Cover timer not present case
		[Theory, AutoData]
		public void ProcessDecisionContextTimerFiredTest([Frozen] HistoryEvent historyEvent)
		{
		    historyEvent.EventType = EventType.TimerStarted;
		    historyEvent.TimerStartedEventAttributes = new TimerStartedEventAttributes
		    {
			   TimerId = "TestID",
		    };

			var historyEvent2 = Substitute.For<HistoryEvent>();
			historyEvent2.EventType = EventType.TimerFired;
			historyEvent2.TimerFiredEventAttributes = new TimerFiredEventAttributes
			{
			    TimerId = "TestID",
			};

			_processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
			_decisionContext.Timers["TestID"] = historyEvent.TimerStartedEventAttributes;

			_processor.ProcessDecisionContext(historyEvent2, _decisionContext);

			Assert.Equal(_decisionContext.DecisionType, historyEvent2.EventType);
			Assert.Equal(_decisionContext.DecisionType, historyEvent2.EventType);
			Assert.Equal(_decisionContext.TimerId, historyEvent2.TimerFiredEventAttributes.TimerId);
			Assert.Equal(_decisionContext.FiredTimers["TestID"], historyEvent2.TimerFiredEventAttributes);
		}

		//TODO Cover timer not present case
		[Theory, AutoData]
		public void ProcessDecisionContextTimerCanceledTest([Frozen] HistoryEvent historyEvent)
		{
		    historyEvent.EventType = EventType.TimerStarted;
		    historyEvent.TimerStartedEventAttributes = new TimerStartedEventAttributes()
		    {
			   TimerId = "TestID",
		    };

		    var historyEvent2 = Substitute.For<HistoryEvent>();
		    historyEvent2.EventType = EventType.TimerCanceled;
		    historyEvent2.TimerCanceledEventAttributes = new TimerCanceledEventAttributes()
		    {
			   TimerId = "TestID",
		    };

		    _processor = Substitute.For<SDK.Workflow.WorkflowEventsProcessor>(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf);

			_decisionContext = Substitute.For<WorkflowDecisionContext>();
		    _decisionContext.Timers["TestID"] = historyEvent.TimerStartedEventAttributes;

			_processor.ProcessDecisionContext(historyEvent2, _decisionContext);

		    Assert.Equal(_decisionContext.DecisionType, historyEvent2.EventType);
		    Assert.Equal(_decisionContext.DecisionType, historyEvent2.EventType);
		    Assert.Equal(_decisionContext.TimerId, historyEvent2.TimerCanceledEventAttributes.TimerId);
		    Assert.Equal(_decisionContext.CanceledTimers["TestID"], historyEvent2.TimerCanceledEventAttributes);
		}

		#endregion


		[Fact]
		internal void ResultObjectTestObject()
		{
			var obj = new StepResult();
			var s_obj = Utils.SerializeToJSON(obj);

			var stepResult = new StepResult<StepResult>(obj, true, "testKey");

			var context = new WorkflowDecisionContext();
			context.ResultRef = Utils.SerializeToJSON(stepResult);

			Assert.Equal(s_obj, context.ResultData);
		}

		[Fact]
		internal void ResultObjectTestString()
		{
			var obj = string.Empty;
			var s_obj = Utils.SerializeToJSON(obj);

			var stepResult = new StepResult<string>(obj, true, "testKey");

			var context = new WorkflowDecisionContext();
			context.ResultRef = Utils.SerializeToJSON(stepResult);

			Assert.Equal(s_obj, context.ResultData);
		}

	}
}