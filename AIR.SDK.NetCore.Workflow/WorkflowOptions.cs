using System;
using System.Collections.Generic;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Represents set of parameters to initialize and execution workflow instance.
	/// </summary>
	public class WorkflowOptions : IWorkflowOptions
	{
		// These are amazon-related service fields.
		private int _taskStartToCloseTimeout = 150;
		private int _executionStartToCloseTimeout = 300;
		private int _workflowExecutionRetentionPeriodInDays = 1;

		private int _maxAttempts = 3;

		#region Properties
		
		/// <summary>
		/// A SWF domain. Each SWF entity runs in specific domain.
		/// </summary>
		public string Domain { get; set; }
		/// <summary>
		///  The name of the workflow type within the domain.
		///  The specified string must not start or end with whitespace. It must not contain
		///  a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		///  Also, it must not contain the literal string quotarnquot.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Workflow version. Use this for storing the old a new workflow implementations.
		/// </summary>
		public string Version { get; set; }
		/// <summary>
		/// Description of workflow.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// The task list for this workflow execution. <see cref="WorkflowBase.TaskList"/>
		/// </summary>
		public string TaskList { get; set; }
		/// <summary>
		/// This timeout specifies the maximum time that the corresponding decider can take to complete a decision task.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// A negative value (e.g. -1) can be used to specify unlimited duration.
		/// If this timeout is exceeded, the task is marked as timed out in the workflow execution history, 
		/// and Amazon SWF adds an event of type DecisionTaskTimedOut to the workflow history.
		/// </summary>
		public int TaskStartToCloseTimeout
		{
			get { return _taskStartToCloseTimeout; }
			set { _taskStartToCloseTimeout = value; }
		}
		/// <summary>
		/// This timeout specifies the maximum time that a workflow execution can take to complete.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// If this timeout is exceeded, Amazon SWF closes the workflow execution and adds an event of type WorkflowExecutionTimedOut 
		/// to the workflow execution history. In addition to the timeoutType, the event attributes specify the childPolicy 
		/// that is in effect for this workflow execution.
		/// </summary>
		public int ExecutionStartToCloseTimeout
		{
			get { return _executionStartToCloseTimeout; }
			set { _executionStartToCloseTimeout = value; }
		}
		/// <summary>
		/// The duration (in days) that records and histories of workflow executions on the domain should be kept by the service. 
		/// After the retention period, the workflow execution is not available in the results of visibility calls.
		/// If you pass the 0 (zero), then the workflow execution history will not be retained. As soon as the workflow execution completes, 
		/// the execution record and its history are deleted.
		/// The maximum workflow execution retention period is 90 days.
		/// </summary>
		public int WorkflowExecutionRetentionPeriodInDays
		{
			get { return _workflowExecutionRetentionPeriodInDays; }
			set { _workflowExecutionRetentionPeriodInDays = value; }
		}
		/// <summary>
		/// Used to retry running workflow. If exceeds, workflow is considered to be failed.
		/// </summary>
		public int MaxAttempts
		{
			get { return _maxAttempts; }
			set { _maxAttempts = value; }
		}
		#endregion

		/// <summary>
		/// The property used for implemented complex workflow logic such as conditionals.
		/// </summary>
		/// <remarks>
		/// <param name="stepKey">Unique key of current workflow step. <see cref="WorkflowBase.AttachStep(string, ISchedulable)"/></param>
		/// <param name="prevStepResult">A serialized current workflow output data.</param>
		/// <returns>Unique key of the next step to be scheduled.</returns>
		/// </remarks>
		/// <seealso cref="WorkflowBase.GetNextStep()"/>
		public Func<string, string, IResult<object>> NextStepHandler { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TInput">Type of entire input collection.</typeparam>
	/// <typeparam name="TOutput">Type of aggregate result object.</typeparam>
	public class WorkflowCollectionOptions<TInput, TOutput> : WorkflowOptions, IWorkflowCollectionOptions<TInput, TOutput>
		where TInput : class
		where TOutput : class
	{
		/// <summary>
		/// Returns collection of serialized objects which are used as input data for the first action in each parallel workflow.
		/// </summary>
		/// <param name="input">Input data to be processed.</param>
		/// <returns>Collection of serialized input objects.</returns>
		public Func<TInput, IEnumerable<ParallelCollectionItem<string>>> CollectionProcessor { get; set; }
		/// <summary>
		/// Reducer is a function for processing and aggregating the entire collection output.
		/// </summary>
		/// <param name="taskProcessorResults">it's a collection of serialized output objects computed by each workflow.</param>
		/// <returns>Object will be used as input data for next step in parent workflow.</returns>
		public Func<IEnumerable<string>, TOutput> Reducer { get; set; }
	}
}
