using AIR.SDK.Workflow.Core;
using Newtonsoft.Json;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Default implementation of IResult<T> interface.
	/// </summary>
	/// <typeparam name="T">Serializable type</typeparam>
	//[Serializable]
	public class StepResult<T> : IResult<T>
	{
		#region IResult Members

		// There should be something like ReturnCode enum.
		/// <summary>
		/// A step success - defines whether workflow is allowd to continue
		/// </summary>
		[JsonProperty]
		public bool Success { get; set; }

		/// <summary>
		/// Current step name
		/// </summary>
		[JsonProperty]
		public string StepKey { get; set; }

		/// <summary>
		/// An activity result object
		/// </summary>
		[JsonProperty]
		public T ReturnValue { get; set; }

		#endregion

		public StepResult(T returnValue, bool success)
		{
			ReturnValue = returnValue;
			Success = success;
		}

		public StepResult(T returnValue)
		{
			ReturnValue = returnValue;
			Success = true;
		}

		public StepResult(T returnValue, bool success, string stepKey)
		{
			ReturnValue = returnValue;
			Success = success;
			StepKey = stepKey;
		}

		public StepResult() { }
	}

	/// <summary>
	/// This class is being used for calculating whether workflow can be continued
	/// </summary>
	//[Serializable]
	internal class StepResult
	{
		[JsonProperty]
		internal int StepNumber { get; set; }

		[JsonProperty]
		internal string Input { get; set; }

		public StepResult() { }
	}
}