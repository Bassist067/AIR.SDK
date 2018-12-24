using System.Collections.Generic;

namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// ICollectionItemInput is a type for implementing parallel action input.
	/// </summary>
	public interface ICollectionItemInput
	{
		/// <summary>
		/// Use this to implement delay for each parallel action.
		/// </summary>
		int DelayTimeoutInSeconds { get; set; }

		/// <summary>
		/// Input is a storage for action serialized data.
		/// </summary>
		string Input { get; set; }
	}

	/// <summary>
	/// This is an interface for implementing parallel actions. 
	/// </summary>
	public interface IParallelCollection
	{
		/// <summary>
		/// Processes input of collection and represents data for each parallel action.
		/// </summary>
		/// <param name="input">Serialized object which used to determine data for each parallel action.</param>
		/// <returns>a collection of input objects to process</returns>
		IEnumerable<ICollectionItemInput> Processor(string input);

		/// <summary>
		/// Reducer is a function for processing and aggregating the entire collection output.
		/// </summary>
		/// <param name="taskProcessorResults">it's a Collection of output objects computed by each action.</param>
		/// <returns>Aggregated collection processing result</returns>
		string Reducer(IEnumerable<string> taskProcessorResults);
	}

	/// <summary>
	/// This is an interface for implementing parallel activities. 
	/// Use this for running the same activities based on collection input. 
	/// I.e. - Process 10 loans with the same task processor with a delay available to be specified for each activity.
	/// </summary>
	public interface IParallelActivityCollection: IParallelCollection, IActivity
	{
	}

	/// <summary>
	/// This is an interface for implementing parallel activities. 
	/// Use this for running the same activities based on collection input. 
	/// I.e. - Process 10 loans with the same task processor with a delay available to be specified for each activity.
	/// </summary>
	public interface IParallelWorkflowCollection : IParallelCollection, IWorkflow
	{
	} 
}