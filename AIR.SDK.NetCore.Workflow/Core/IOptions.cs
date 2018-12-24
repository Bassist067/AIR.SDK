using System;
using System.Collections.Generic;

namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// 
	/// </summary>
	public interface IOptions
	{
		/// <summary>
		///  The name of the workflow type within the domain.
		///  The specified string must not start or end with whitespace. It must not contain
		///  a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		///  Also, it must not contain the literal string quotarnquot.
		/// </summary>
		string Name { get; set; }
		/// <summary>
		/// The task list for a schedulable action execution. 
		/// <see cref="ActivityBase.TaskList"/>. <see cref="WorkflowBase.TaskList"/>.
		/// </summary>
		string TaskList { get; set; }
		/// <summary>
		/// Schedulable action version. Use this for storing the old a new action implementations.
		/// </summary>
		string Version { get; set; }
		/// <summary>
		/// A textual description of an action type.
		/// </summary>
		string Description { get; set; }
		/// <summary>
		/// Used to retry running schedulable action (<see cref="IWorkflow"/> or <see cref="IAcrivity"/>). 
		/// If exceeds, activity is considered to be failed.
		/// </summary>
		int MaxAttempts { get; set; }

	}

	/// <summary>
	/// 
	/// </summary>
	public interface IActivityOptions : IOptions
	{
		/// <summary>
		/// Heartbeat timeout is used for checking running activity is still alive and not hung up
		/// </summary>
		int HeartbeatTimeout { get; set; }

		/// <summary>
		/// Schedule to start is used for checking if delay between activity was scheduled and actual starting is ok
		/// </summary>
		int ScheduleToStartTimeout { get; set; }

		/// <summary>
		/// Schedule to start is used for checking if delay between activity was started and closed is ok
		/// </summary>
		int StartToCloseTimeout { get; set; }

		/// <summary>
		/// Schedule to start is used for checking if delay between activity was scheduled and closed is ok
		/// </summary>
		int ScheduleToCloseTimeout { get; set; }

		/// <summary>
		/// Use this for delaying an activity in specific time
		/// </summary>
		int DelayTimeoutInSeconds { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TInput"></typeparam>
	/// <typeparam name="TOutput"></typeparam>
	public interface IActivityOptions<TInput, TOutput> : IActivityOptions
	{
		/// <summary>
		/// This method is what actually does the work of the task.
		/// </summary>
		Func<TInput, IResult<TOutput>> ActivityAction { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TInput"></typeparam>
	/// <typeparam name="TOutput"></typeparam>
	/// <typeparam name="TActivityInput"></typeparam>
	/// <typeparam name="TActivityOutput"></typeparam>
	public interface IActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput> : IActivityOptions<TActivityInput, TActivityOutput>
		where TInput : class
		where TOutput : class
		where TActivityInput : class
		where TActivityOutput : class
	{
		/// <summary>
		/// Used to prepare item collection for processing. <see cref="IParallelCollection.Processor"/>.
		/// </summary>
		/// <returns></returns>
		Func<TInput, IEnumerable<ParallelCollectionItem<TActivityInput>>> CollectionProcessor { get; set; }

		/// <summary>
		/// Used for getting collection processing result. <see cref="IParallelCollection.Reducer"/>.
		/// </summary>
		/// <returns>serialized parallel activity collection output</returns>
		Func<IEnumerable<TActivityOutput>, TOutput> Reducer { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public interface IWorkflowOptions : IOptions
	{
		/// <summary>
		/// A SWF domain. Each SWF entity runs in specific domain.
		/// </summary>
		string Domain { get; set; }

		/// <summary>
		/// TaskStartToCloseTimeout is a default value for all child activities. Set this to ensure all activities having the same TaskStartToCloseTimeout value.
		/// </summary>
		int TaskStartToCloseTimeout { get; set; }

		/// <summary>
		/// ExecutionStartToCloseTimeout is a default value for all child activities. Set this to ensure all activities having the same ExecutionStartToCloseTimeout value.
		/// </summary>
		int ExecutionStartToCloseTimeout { get; set; }

		/// <summary>
		/// A duration (in days) for which the record (including the history) of workflow executions in this domain should be kept by the service. 
		/// After the retention period, the workflow execution will not be available in the results of visibility calls. 
		/// If you pass the value NONE then there is no expiration for workflow execution history (effectively an infinite retention period). 
		/// </summary>
		int WorkflowExecutionRetentionPeriodInDays { get; set; }

		/// <summary>
		/// A main method used for implemented complex workflow logic such as conditionals.
		/// </summary>
		/// <returns>Unique key of next step to be scheduled.</returns>
		//IResult<object> GetNextStep(string stepKey, string prevStepResult);
		Func<string, string, IResult<object>> NextStepHandler { get; set; }
	}


	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TInput"></typeparam>
	/// <typeparam name="TOutput"></typeparam>
	/// <typeparam name="TActivityInput"></typeparam>
	/// <typeparam name="TActivityOutput"></typeparam>
	public interface IWorkflowCollectionOptions<TInput, TOutput> : IWorkflowOptions
		where TInput : class
		where TOutput : class
	{
		/// <summary>
		/// Returns collection of serialized objects which are used as input data for the first action in each parallel workflow.
		/// </summary>
		/// <param name="input">Input data to be processed.</param>
		/// <returns>Collection of serialized input objects.</returns>
		Func<TInput, IEnumerable<ParallelCollectionItem<string>>> CollectionProcessor { get; set; }
		/// <summary>
		/// Reducer is a function for processing and aggregating the entire collection output.
		/// </summary>
		/// <param name="taskProcessorResults">it's a collection of serialized output objects computed by each workflow.</param>
		/// <returns>Object will be used as input data for next step in parent workflow.</returns>
		Func<IEnumerable<string>, TOutput> Reducer { get; set; }
	}
}
