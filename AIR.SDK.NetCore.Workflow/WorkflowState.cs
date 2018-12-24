using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace AIR.SDK.Workflow
{
	[JsonObject]
	public class WorkflowState
	{
		/// <summary>
		/// Zero-based index of the currently executing step.
		/// </summary>
		[JsonProperty()]
		internal int CurrentStepNumber { get; set; }
		/// <summary>
		/// Number of actions scheduled at this step.
		/// </summary>
		[JsonProperty()]
		internal int NumberOfActions { get; set; }
		/// <summary>
		/// The results from the actions so far.
		/// Contains plls of "activity number" and "result/reference to result".
		/// </summary>
		[JsonProperty()]
		internal ConcurrentDictionary<int, string> Results { get; set; }

		public WorkflowState(int stepNum)
		{
			CurrentStepNumber = stepNum;
			NumberOfActions = 1;
			Results = new ConcurrentDictionary<int, string>();
		}

		[JsonConstructor]
		public WorkflowState()
		{
			Results = new ConcurrentDictionary<int,string>();
		}
	}

}