using Amazon.SimpleWorkflow.Model;

namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	///  This interface is for registering activities - steps in the workflow
	/// </summary>
	public interface IActivity : IRegistrable/*, ICloneable*/
	{
		IActivityOptions Options { get; }

		/// <summary>
		/// An auto-generated Unique ID
		/// </summary>
		string ActivityId { get; set; }

		/// <summary>
		/// TaskList is a grouping key for Amazon  You can set activity workers polling for specific task lists
		/// </summary>
		TaskList TaskList { get; }

		/// <summary>
		/// Input is a serialized input object. Everything coming to SWF is a string
		/// </summary>
		string Input { get; set; }

		/// <summary>
		/// Task processor is a main activity method. It actually does all the useful work in activity. 
		/// </summary>
		/// <param name="input">A serialized input object. It's an initial data</param>
		/// <returns>string output is a serialized output object as a result of the activity</returns>
		string TaskProcessor(string input);

		void LockVersion(string version);

		/// <summary>
		/// Deserializes and packages result.
		/// </summary>
		/// <param name="result">Serialized object.</param>
		/// <returns>Deserialized object.</returns>
		object GetTypedObject(string result);

		/*
		/// <summary>
		/// Gets activity execution result
		/// </summary>
		/// <param name="result">A serialized generic business input object <see cref="IResult<T>"/>.</param>
		/// <returns>A serialized generic business output object.</returns>
		IResult<string> GetResult(string result);
		*/
	}
}