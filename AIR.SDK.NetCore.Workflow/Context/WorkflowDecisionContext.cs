using System.Collections.Generic;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.API.Core.Storage;

namespace AIR.SDK.Workflow.Context
{
	/// <summary>
	/// Information required by workflow object to make a decision.
	/// </summary>
	public class WorkflowDecisionContext
	{
		private readonly IStorageManager _storageClient;


		public WorkflowDecisionContext(IStorageManager storageClient)
			: this()
		{
			_storageClient = storageClient;
		}

		public WorkflowDecisionContext()
		{
			Markers = new Dictionary<string, string>();
			Timers = new Dictionary<string, TimerStartedEventAttributes>();
			FiredTimers = new Dictionary<string, TimerFiredEventAttributes>();
			CanceledTimers = new Dictionary<string, TimerCanceledEventAttributes>();

			Inputs = new List<string>();//new Dictionary<int, Dictionary<int, string>>();
			Results = new Dictionary<int, Dictionary<int, string>>();
		}


	    /// <summary>
	    /// Amazon decision type event constants. Should really be enum
	    /// </summary>
		internal EventType DecisionType { get; set; }
	    /// <summary>
	    /// 
	    /// </summary>
		internal string StartingInput { get; set; }
		//internal string ExecutionContext { get; set; }
		internal WorkflowState ExecutionContext { get; set; }
		internal WorkflowState LastExecutionContext { get; set; }

		internal string WorkflowName { get; set; }
		internal string WorkflowVersion { get; set; }
		internal string WorkflowId { get; set; }

		internal string WorkflowParentId { get; set; }

		internal string WorkflowExecRunId { get; set; }

		internal string ActivityId { get; set; }
		internal string ActivityName { get; set; }
		internal string ActivityVersion { get; set; }
		internal string ChildWorkflowName { get; set; }
		internal string ChildWorkflowVersion { get; set; }
		internal string TimerId { get; set; }

		internal long ScheduledEventId { get; set; }
		internal long StartedEventId { get; set; }
		internal long DecisionTaskCompletedEventId { get; set; }

		internal Dictionary<string, string> Markers { get; set; }
		internal Dictionary<string, TimerStartedEventAttributes> Timers { get; set; }
		internal Dictionary<string, TimerFiredEventAttributes> FiredTimers { get; set; }
		internal Dictionary<string, TimerCanceledEventAttributes> CanceledTimers { get; set; }

		/// <summary>
		/// Contains inputs (references) of schedulable actions (activity & workflow).
		/// The key of the dictionary is StepNumber. The key of value dictionary is ActionNumber.
		/// </summary>
		internal List<string> Inputs { get; set; }
		/// <summary>
		/// Contains results (references) of schedulable actions (activity & workflow).
		/// The key of the dictionary is StepNumber. The key of value dictionary is ActionNumber.
		/// </summary>
		internal Dictionary<int, Dictionary<int, string>> Results { get; set; }


		/// <summary>
		/// Gets serialized input object.
		/// </summary>
		internal string Input { get { return Utils.GetDataFromStore(InputRef, _storageClient);} }

		/// <summary>
		/// Reference to serialized input object in <see cref="ll.API.Core.IStorageManager"/>.
		/// If IStorageManager is not defined the reference will contain serialized object as is.
		/// </summary>
		internal string InputRef { get; set; }

		/// <summary>
		/// Gets or sets serialized StepResult<string> object.
		/// </summary>
		internal string Result { get { return Utils.GetDataFromStore(ResultRef, _storageClient);}}

		/// <summary>
		/// Reference to serialized result object in <see cref="ll.API.Core.IStorageManager"/>.
		/// If IStorageManager is not defined the reference will contain serialized object as is.
		/// </summary>
		internal string ResultRef { get; set; }

		/// <summary>
		/// Gets serialized object from StepResult<string>.ReturnValue
		/// </summary>
		internal string ResultData { get { return ResultObject.ReturnValue; } }

		private StepResult<string> _resultObject;
		internal StepResult<string> ResultObject
		{
			get 
			{
				if (_resultObject == null)
				{
					_resultObject = new StepResult<string>("");

					StepResult<object> resultObj = Utils.DeserializeFromJSON<StepResult<object>>(Result);
					if (resultObj != null)
						_resultObject = new StepResult<string>(Utils.SerializeToJSON(resultObj.ReturnValue), resultObj.Success, resultObj.StepKey);
				}
				return _resultObject; 
			}
		}

		internal string Cause { get; set; }
		internal string Details { get; set; }
		internal string Reason { get; set; }
		internal string Control { get; set; }
		internal string TimeoutType { get; set; }

#if DEBUG
		internal string dd()
		{
			return
				$"DecisionType: {DecisionType} /nWorkflowName: {WorkflowName}/nWorkflowId: {WorkflowId}/nActivityName: {ActivityName}/nActivityId: {ActivityId}/nInputRef: {InputRef}/nInput: {Input}/nControl: {Control}.";
		}
#endif
	}
}