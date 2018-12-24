using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Context;
using AIR.SDK.Workflow.Core;
using System.Text;
using AIR.API.Core.Storage;



namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Processes SWF events to extract the context upon which the current decision needs to be made
	/// and calls on the appropriate workflow decision object to make a decision.
	/// </summary>
	internal class WorkflowEventsProcessor
	{
		private readonly DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _request;
		private readonly WorkflowDecisionContext _decisionContext;
		private readonly IAmazonSimpleWorkflow _swfClient;
		private readonly IStorageManager _storageClient;

		//This is a part of something complex and dependent - and we really need to cover this
		internal readonly WorkflowEventsIterator Events;
		private readonly WorkflowBase _workflow;

		/// <summary>
		/// Constructor for the workflow event processor. 
		/// </summary>
		/// <param name="decisionTask">Decision task passed in from SWF as decision task response.</param>
		/// <param name="workflow">IEnumerable set of string for workflow name and Type for workflow class.</param>
		/// <param name="request">The request used to retrieve <paramref name="decisionTask"/>, which will be used to retrieve subsequent history event pages.</param>
		/// <param name="swfClient">An SWF client.</param>
		/// <param name="storageManager">IStorageManager</param>
		public WorkflowEventsProcessor(DecisionTask decisionTask, WorkflowBase workflow, PollForDecisionTaskRequest request,
			IAmazonSimpleWorkflow swfClient, IStorageManager storageManager) : this(decisionTask, workflow, request, swfClient)
		{
			_storageClient = storageManager;
			_decisionContext = new WorkflowDecisionContext(_storageClient);
		}

		/// <summary>
		/// Constructor for the workflow event processor. 
		/// </summary>
		/// <param name="decisionTask">Decision task passed in from SWF as decision task response.</param>
		/// <param name="workflow">IEnumerable set of string for workflow name and Type for workflow class.</param>
		/// <param name="request">The request used to retrieve <paramref name="decisionTask"/>, which will be used to retrieve subsequent history event pages.</param>
		/// <param name="swfClient">An SWF client.</param>
		public WorkflowEventsProcessor(DecisionTask decisionTask, WorkflowBase workflow, PollForDecisionTaskRequest request,
			IAmazonSimpleWorkflow swfClient)
		{
			if (decisionTask == null)
			{
				throw new ArgumentNullException(nameof(decisionTask));
			}
			if (workflow == null)
			{
				throw new ArgumentNullException(nameof(workflow));
			}
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}
			if (swfClient == null)
			{
				throw new ArgumentNullException(nameof(swfClient));
			}

			// Store the decision task and allocate a new decision context and event dictionary which
			// we will use as we walk through the chain of events
			_decisionTask = decisionTask;
			_request = request;
			_decisionContext = new WorkflowDecisionContext();
			_swfClient = swfClient;

			_workflow = workflow;

			// Set up our events data structure.
			Events = new WorkflowEventsIterator(ref decisionTask, _request, _swfClient);
		}

		/// <summary>
		/// Walks through the relevant part of the event history chain and populates the decision context. Then calls
		/// on the appropriate workflow decision object to make a decision.
		/// </summary>
		/// <returns>Decision on how to proceed.</returns>
		internal RespondDecisionTaskCompletedRequest Decide()
		{
			// Step 1: Walk through the relevant part of the event history chain and populate the decision context

			// Retrieve and store the workflow information
			_decisionContext.WorkflowName = _decisionTask.WorkflowType.Name;
			_decisionContext.WorkflowVersion = _decisionTask.WorkflowType.Version;
			_decisionContext.WorkflowId = _decisionTask.WorkflowExecution.WorkflowId;

			// TODO: Warning. Need to check if it's the same as historyEvent.DecisionTaskCompletedEventAttributes.ExecutionContext
			// Move call to an appropriate event
			_decisionContext.LastExecutionContext = GetLastExecContext();

			// Walk through the chain of events based on event ID to identify what we need to decide on
			Debug.WriteLine(">>> Workflow: " + _decisionContext.WorkflowName);

			Logger.Debug("Workflow: {0}, PreviousStartedEventId: {1}, StartedEventId: {2}",
				_decisionContext.WorkflowName,
				_decisionTask.PreviousStartedEventId,
				_decisionTask.StartedEventId
				);

			var logData = new StringBuilder();

			foreach (var historyEvent in Events)
			{
				logData.AppendLine($"Event Type: [{historyEvent.EventId}] [{historyEvent.EventType}]");

				ProcessDecisionContext(historyEvent, _decisionContext);
			}

			Logger.Info(logData.ToString());


			Logger.Info("CONTEXT Event Type: {0}, Timers: {1}, Fired: {2}, Canceled: {3}",
				_decisionContext.DecisionType,
				_decisionContext.Timers.Count,
				_decisionContext.FiredTimers.Count,
				_decisionContext.CanceledTimers.Count
				);

			// Step 2: decide on what to do based on the processed events

			// Getting the correct instance of the decider.
			var decider = _workflow.GetDecider(_decisionContext.WorkflowId);
			//(IDecider) Activator.CreateInstance(_workflows[_decisionContext.WorkflowName]);

			var decisionCompletedRequest = MakeDecision(decider, _decisionContext);

			logData.Clear();
			bool needToClearStore = false;
			foreach (var d in decisionCompletedRequest.Decisions)
			{
				logData.AppendLine($"Decision Type: {d.DecisionType}");

				needToClearStore = (d.DecisionType == DecisionType.CompleteWorkflowExecution ||
					d.DecisionType == DecisionType.CancelWorkflowExecution ||
					d.DecisionType == DecisionType.FailWorkflowExecution);
			}

			if (needToClearStore)
				ClearStoreData(_decisionContext);

			Logger.Warn(logData.ToString());

			// Assign the task token and return.
			decisionCompletedRequest.TaskToken = _decisionTask.TaskToken;
			decisionCompletedRequest.ExecutionContext = WorkflowStateSerializer.Serialize(_decisionContext.ExecutionContext);

			return decisionCompletedRequest;
		}

	    /// <summary>
		/// Creates an Amazon RespondDecisionTaskCompletedRequest to notify SWF that decison was made based on decision  
	    /// </summary>
	    /// <param name="decider"></param>
	    /// <returns></returns>
		internal RespondDecisionTaskCompletedRequest MakeDecision(IDecider decider, WorkflowDecisionContext decisionContext)
		{
			if (decider == null)
			{
				var attributes = new FailWorkflowExecutionDecisionAttributes
				{
					Details = "WorkflowId: " + decisionContext.WorkflowId,
					Reason = "Decider not found."
				};

				var decisionRequest = new RespondDecisionTaskCompletedRequest
				{
					Decisions = new List<Decision>
					{
						new Decision
						{
							DecisionType = DecisionType.FailWorkflowExecution,
							FailWorkflowExecutionDecisionAttributes = attributes
						}
					}
				};
				return decisionRequest;
			}

			RespondDecisionTaskCompletedRequest decisionCompletedRequest;
			// Match the context and call the right method to make a decision
			if (decisionContext.DecisionType == EventType.WorkflowExecutionStarted)
			{
				decisionCompletedRequest = decider.OnWorkflowExecutionStarted(decisionContext);
			}
			else if (decisionContext.DecisionType == DecisionType.ContinueAsNewWorkflowExecution)
			{
				decisionCompletedRequest = decider.OnWorkflowExecutionContinuedAsNew(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.WorkflowExecutionCancelRequested)
			{
				decisionCompletedRequest = decider.OnWorkflowExecutionCancelRequested(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ActivityTaskCompleted)
			{
				decisionCompletedRequest = decider.OnActivityTaskCompleted(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ActivityTaskFailed)
			{
				decisionCompletedRequest = decider.OnActivityTaskFailed(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ActivityTaskTimedOut)
			{
				decisionCompletedRequest = decider.OnActivityTaskTimedOut(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ScheduleActivityTaskFailed)
			{
				decisionCompletedRequest = decider.OnScheduleActivityTaskFailed(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ChildWorkflowExecutionStarted)
			{
				decisionCompletedRequest = decider.OnChildWorkflowExecutionStarted(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ChildWorkflowExecutionCompleted)
			{
				decisionCompletedRequest = decider.OnChildWorkflowExecutionCompleted(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ChildWorkflowExecutionFailed)
			{
				decisionCompletedRequest = decider.OnChildWorkflowExecutionFailed(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ChildWorkflowExecutionTerminated)
			{
				decisionCompletedRequest = decider.OnChildWorkflowExecutionTerminated(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.ChildWorkflowExecutionTimedOut)
			{
				decisionCompletedRequest = decider.OnChildWorkflowExecutionTimedOut(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.StartChildWorkflowExecutionFailed)
			{
				decisionCompletedRequest = decider.OnStartChildWorkflowExecutionFailed(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.TimerStarted)
			{
				decisionCompletedRequest = decider.OnTimerStarted(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.TimerFired)
			{
				decisionCompletedRequest = decider.OnTimerFired(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.TimerCanceled)
			{
				decisionCompletedRequest = decider.OnTimerCanceled(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.StartChildWorkflowExecutionInitiated)
			{
				decisionCompletedRequest = decider.OnStartChildWorkflowExecutionInitiated(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.WorkflowExecutionSignaled)
			{
				decisionCompletedRequest = decider.OnWorkflowExecutionSignaled(decisionContext);
			}
			else if (decisionContext.DecisionType == EventType.WorkflowExecutionCompleted)
			{
				decisionCompletedRequest = decider.OnWorkflowExecutionCompleted(decisionContext);
			}
			else
			{
				throw new InvalidOperationException("Unhandled event type.");
			}
			return decisionCompletedRequest;
		}

		/// <summary>
		/// Fills the decision context (a god object contains all the particular data based on event type).
		/// </summary>
		/// <param name="historyEvent">Amazon history event. It's a core Amazon SWF object storing all the serialized workflow events. </param>
		internal void ProcessDecisionContext(HistoryEvent historyEvent, WorkflowDecisionContext decisionContext)
		{
			//impl
			if (historyEvent.EventType == EventType.WorkflowExecutionStarted)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.InputRef = historyEvent.WorkflowExecutionStartedEventAttributes.Input;
				decisionContext.StartingInput = historyEvent.WorkflowExecutionStartedEventAttributes.Input;
				if (historyEvent.WorkflowExecutionStartedEventAttributes.ParentWorkflowExecution != null)
					decisionContext.WorkflowParentId =
						historyEvent.WorkflowExecutionStartedEventAttributes.ParentWorkflowExecution.WorkflowId;

				AddInput(decisionContext.InputRef);
			}
			//impl
			else if (historyEvent.EventType == EventType.WorkflowExecutionContinuedAsNew)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.InputRef = historyEvent.WorkflowExecutionContinuedAsNewEventAttributes.Input;
			}
			else if (historyEvent.EventType == EventType.WorkflowExecutionCancelRequested)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.Cause = historyEvent.WorkflowExecutionCancelRequestedEventAttributes.Cause;
			}
			else if (historyEvent.EventType == EventType.DecisionTaskCompleted)
			{
				// If a decision task completed event was encountered, use it to save 
				// some of the key information as the execution context is not available as part of
				// the rest of the ActivityTask* event attributes.
				// NB: We don't act on this event.
				decisionContext.ScheduledEventId = historyEvent.DecisionTaskCompletedEventAttributes.ScheduledEventId;
				decisionContext.StartedEventId = historyEvent.DecisionTaskCompletedEventAttributes.StartedEventId;
				decisionContext.ExecutionContext =
					WorkflowStateSerializer.Deserialize(historyEvent.DecisionTaskCompletedEventAttributes.ExecutionContext);
			}
			else if (historyEvent.EventType == EventType.ActivityTaskScheduled)
			{
				// If an activity task scheduled event was encountered, use it to save 
				// some of the key information as the activity information is not available as part of
				// the rest of the ActivityTask* event attributes. We don't act on this event.
				decisionContext.ActivityId = historyEvent.ActivityTaskScheduledEventAttributes.ActivityId;
				decisionContext.ActivityName = historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Name;
				decisionContext.ActivityVersion = historyEvent.ActivityTaskScheduledEventAttributes.ActivityType.Version;
				decisionContext.Control = historyEvent.ActivityTaskScheduledEventAttributes.Control;
				decisionContext.InputRef = historyEvent.ActivityTaskScheduledEventAttributes.Input;

				// Remove related timer if applicable.
				var state = SchedulableStateSerializer.Deserialize(decisionContext.Control);
				foreach (var timer in decisionContext.Timers)
				{
					var timerState = SchedulableStateSerializer.Deserialize(timer.Value.Control);
					if (timerState != null && timerState.Equals(state))
					{
						if (decisionContext.FiredTimers.ContainsKey(timer.Key))
							decisionContext.FiredTimers.Remove(timer.Key);

						decisionContext.Timers.Remove(timer.Key);
						break;
					}
				}
			}
			//impl
			else if (historyEvent.EventType == EventType.ActivityTaskCompleted)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ScheduledEventId = historyEvent.ActivityTaskCompletedEventAttributes.ScheduledEventId;
				decisionContext.StartedEventId = historyEvent.ActivityTaskCompletedEventAttributes.StartedEventId;
				decisionContext.ResultRef = historyEvent.ActivityTaskCompletedEventAttributes.Result;

				var evt = FindEventTypeById(decisionContext.ScheduledEventId);
				decisionContext.ActivityId = evt.ActivityTaskScheduledEventAttributes.ActivityId;
				decisionContext.Control = evt.ActivityTaskScheduledEventAttributes.Control;
				decisionContext.InputRef = evt.ActivityTaskScheduledEventAttributes.Input;

				AddInput(decisionContext.InputRef);
				AddResult(decisionContext.Control, decisionContext.ResultRef);
			}
			//impl
			else if (historyEvent.EventType == EventType.ActivityTaskFailed)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ScheduledEventId = historyEvent.ActivityTaskFailedEventAttributes.ScheduledEventId;
				decisionContext.StartedEventId = historyEvent.ActivityTaskFailedEventAttributes.StartedEventId;
				decisionContext.Details = historyEvent.ActivityTaskFailedEventAttributes.Details;
				decisionContext.Reason = historyEvent.ActivityTaskFailedEventAttributes.Reason;

				var evt = FindEventTypeById(decisionContext.ScheduledEventId);
				decisionContext.ActivityId = evt.ActivityTaskScheduledEventAttributes.ActivityId;
				decisionContext.Control = evt.ActivityTaskScheduledEventAttributes.Control;
				decisionContext.InputRef = evt.ActivityTaskScheduledEventAttributes.Input;
			}
			//impl
			else if (historyEvent.EventType == EventType.ActivityTaskTimedOut)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ScheduledEventId = historyEvent.ActivityTaskTimedOutEventAttributes.ScheduledEventId;
				decisionContext.StartedEventId = historyEvent.ActivityTaskTimedOutEventAttributes.StartedEventId;
				decisionContext.Details = historyEvent.ActivityTaskTimedOutEventAttributes.Details;
				decisionContext.TimeoutType = historyEvent.ActivityTaskTimedOutEventAttributes.TimeoutType;

				var evt = FindEventTypeById(decisionContext.ScheduledEventId);
				decisionContext.ActivityId = evt.ActivityTaskScheduledEventAttributes.ActivityId;
				decisionContext.Control = evt.ActivityTaskScheduledEventAttributes.Control;
				decisionContext.InputRef = evt.ActivityTaskScheduledEventAttributes.Input;
			}
			else if (historyEvent.EventType == EventType.ScheduleActivityTaskFailed)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ActivityId = historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityId;
				decisionContext.DecisionTaskCompletedEventId = historyEvent.ScheduleActivityTaskFailedEventAttributes.DecisionTaskCompletedEventId;
				decisionContext.ActivityName = historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityType.Name;
				decisionContext.ActivityVersion = historyEvent.ScheduleActivityTaskFailedEventAttributes.ActivityType.Version;
				decisionContext.Cause = historyEvent.ScheduleActivityTaskFailedEventAttributes.Cause;
			}
			else if (historyEvent.EventType == EventType.StartChildWorkflowExecutionInitiated)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.InputRef = historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Input;
				decisionContext.Control = historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.Control;

				//decisionContext.WorkflowId = historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowId;
				decisionContext.ChildWorkflowName =
					historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion =
					historyEvent.StartChildWorkflowExecutionInitiatedEventAttributes.WorkflowType.Version;

				AddInput(decisionContext.InputRef);
			}
			else if (historyEvent.EventType == EventType.ChildWorkflowExecutionStarted)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName = historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion = historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowType.Version;
				//decisionContext.WorkflowId = historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.WorkflowId;
				decisionContext.WorkflowExecRunId = historyEvent.ChildWorkflowExecutionStartedEventAttributes.WorkflowExecution.RunId;
			}
			//impl
			else if (historyEvent.EventType == EventType.ChildWorkflowExecutionCompleted)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName = historyEvent.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion = historyEvent.ChildWorkflowExecutionCompletedEventAttributes.WorkflowType.Version;
				decisionContext.ResultRef = historyEvent.ChildWorkflowExecutionCompletedEventAttributes.Result;

				var evt = FindEventTypeById(historyEvent.ChildWorkflowExecutionCompletedEventAttributes.InitiatedEventId);
				decisionContext.Control = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Control;

				AddInput(evt.StartChildWorkflowExecutionInitiatedEventAttributes.Input);
				AddResult(decisionContext.Control, decisionContext.ResultRef);
			}
			//impl
			else if (historyEvent.EventType == EventType.ChildWorkflowExecutionFailed)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName = historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion =
					historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version;
				decisionContext.Details = historyEvent.ChildWorkflowExecutionFailedEventAttributes.Details;
				decisionContext.Reason = historyEvent.ChildWorkflowExecutionFailedEventAttributes.Reason;
				decisionContext.WorkflowExecRunId =
					historyEvent.ChildWorkflowExecutionFailedEventAttributes.WorkflowExecution.RunId;

				var evt = FindEventTypeById(historyEvent.ChildWorkflowExecutionFailedEventAttributes.InitiatedEventId);
				decisionContext.Control = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Control;
				decisionContext.InputRef = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Input;
			}
			else if (historyEvent.EventType == EventType.ChildWorkflowExecutionTerminated)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName = historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion =
					historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowType.Version;
				decisionContext.Details = "";
				decisionContext.WorkflowExecRunId =
					historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.WorkflowExecution.RunId;

				var evt = FindEventTypeById(historyEvent.ChildWorkflowExecutionTerminatedEventAttributes.InitiatedEventId);
				decisionContext.Control = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Control;
				decisionContext.InputRef = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Input;
			}
			//impl
			else if (historyEvent.EventType == EventType.ChildWorkflowExecutionTimedOut)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName = historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion =
					historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowType.Version;
				decisionContext.TimeoutType = historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.TimeoutType;
				decisionContext.Details = "";
				decisionContext.WorkflowExecRunId =
					historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.WorkflowExecution.RunId;

				var evt = FindEventTypeById(historyEvent.ChildWorkflowExecutionTimedOutEventAttributes.InitiatedEventId);
				decisionContext.Control = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Control;
				decisionContext.InputRef = evt.StartChildWorkflowExecutionInitiatedEventAttributes.Input;
			}
			else if (historyEvent.EventType == EventType.MarkerRecorded)
			{
				// We don't act on markers but save the marker information in the decision context so that
				// the workflow has all the information it needs to make the decision. NOTE: values of markers
				// with the same names are overwritten.
				var markerName = historyEvent.MarkerRecordedEventAttributes.MarkerName;
				decisionContext.Markers[markerName] = Utils.GetDataFromStore(historyEvent.MarkerRecordedEventAttributes.Details, _storageClient);
			}
			else if (historyEvent.EventType == EventType.StartChildWorkflowExecutionFailed)
			{
				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.ChildWorkflowName =
					historyEvent.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Name;
				decisionContext.ChildWorkflowVersion =
					historyEvent.StartChildWorkflowExecutionFailedEventAttributes.WorkflowType.Version;
				decisionContext.Cause = historyEvent.StartChildWorkflowExecutionFailedEventAttributes.Cause;
				decisionContext.Control = historyEvent.StartChildWorkflowExecutionFailedEventAttributes.Control;
			}
			else if (historyEvent.EventType == EventType.TimerStarted)
			{
				var timer = historyEvent.TimerStartedEventAttributes;

				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.TimerId = timer.TimerId;
				decisionContext.Timers[timer.TimerId] = timer;
			}
			else if (historyEvent.EventType == EventType.TimerFired)
			{
				var firedTimer = historyEvent.TimerFiredEventAttributes;

				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.TimerId = firedTimer.TimerId;

				if (decisionContext.Timers.ContainsKey(firedTimer.TimerId))
				{
					decisionContext.FiredTimers[firedTimer.TimerId] = firedTimer;
					//decisionContext.Control = decisionContext.Timers[firedTimer.TimerId].Control;
					//decisionContext.Timers.Remove(firedTimer.TimerId);
				}
			}
			else if (historyEvent.EventType == EventType.TimerCanceled)
			{
				var canceledTimer = historyEvent.TimerCanceledEventAttributes;

				decisionContext.DecisionType = historyEvent.EventType;
				decisionContext.TimerId = canceledTimer.TimerId;

				if (decisionContext.Timers.ContainsKey(canceledTimer.TimerId))
				{
					decisionContext.CanceledTimers[canceledTimer.TimerId] = canceledTimer;
					//decisionContext.Control = decisionContext.Timers[canceledTimer.TimerId].Control;
					//decisionContext.Timers.Remove(canceledTimer.TimerId);
				}
			}
			else if (historyEvent.EventType == EventType.StartChildWorkflowExecutionInitiated)
			{

			}
		}

		/// <summary>
		/// Adds new input to workflow state - Don't override this
		/// </summary>
		/// <param name="control"></param>
		/// <param name="result"></param>
		internal virtual void AddInput(string input)
		{
			if (!_decisionContext.Inputs.Contains(input))
				_decisionContext.Inputs.Add(input);
		}

		/// <summary>
		/// Adds new result to workflow state - Don't override this
		/// </summary>
		/// <param name="control"></param>
		/// <param name="result"></param>
		internal virtual void AddResult(string control, string result)
		{
			var state = SchedulableStateSerializer.Deserialize(control);

			if (state == null)
				state = new SchedulableState();

			if (!_decisionContext.Results.ContainsKey(state.StepNumber))
				_decisionContext.Results.Add(state.StepNumber, new Dictionary<int, string>());

			var res = _decisionContext.Results[state.StepNumber];
			res[state.ActionNumber] = result;
		}


	    /// <summary>
		/// Gets last execution context. Don't override this
	    /// </summary>
	    /// <returns></returns>
		internal virtual WorkflowState GetLastExecContext()
		{
			var req = new DescribeWorkflowExecutionRequest
			{
				Domain = _request.Domain,
				Execution = _decisionTask.WorkflowExecution
			};

			var res = _swfClient.DescribeWorkflowExecutionAsync(req).Result;

			return WorkflowStateSerializer.Deserialize(res.WorkflowExecutionDetail.LatestExecutionContext);
		}

		internal virtual HistoryEvent FindEventTypeById(long eventId)
		{
			return Events.FirstOrDefault(e => e.EventId == eventId);
		}

		private void ClearStoreData(WorkflowDecisionContext decisionContext)
		{
			try
			{
				Logger.Info("");

				foreach (string input in decisionContext.Inputs)
				{
					Utils.DeleteFromStore(input, _storageClient);
				}
				foreach (Dictionary<int, string> item in decisionContext.Results.Values)
				{
					foreach (var res in item.Values)
					{
						Utils.DeleteFromStore(res, _storageClient);
					}
				}

				Utils.DeleteFromStore(decisionContext.ResultRef, _storageClient);
				Utils.DeleteFromStore(decisionContext.InputRef, _storageClient);
			}
			catch (Exception ex) 
			{
				Logger.Fatal(ex.Message);
			}
		}

	}
}