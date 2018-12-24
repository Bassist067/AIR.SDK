using Newtonsoft.Json;

namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// IResult interface is a mandatory type for activity output.
	/// </summary>
	/// <typeparam name="T">A generic output business object</typeparam>
	public interface IResult<T>
	{
		/// <summary>
		/// Success defines if activity has been run successfully.
		/// </summary>
		[JsonProperty]
		bool Success { get; set; }

		/// <summary>
		/// Gets or sets user specified key corresponding to an activity.
		/// Required for <see cref="IWorkflow.GetNextStep(string,string)"/>.
		/// </summary>
		[JsonProperty]
		string StepKey { get; set; }

		/// <summary>
		/// Return value is a generic business object of activity output type.
		/// </summary>
		[JsonProperty]
		T ReturnValue { get; set; }
	}
}