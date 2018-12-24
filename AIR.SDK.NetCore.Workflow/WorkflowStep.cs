using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Represents metadata of a workflow step.
	/// </summary>
	////[Serializable]
	public class WorkflowStep : IWorkflowStep
	{
		/// <summary>
		/// System generated zero-based identifier.
		/// </summary>
		public int StepNumber { get; set; }

		/// <summary>
		/// User specified unique identifier.
		/// </summary>
		public string StepKey { get; set; }

		/// <summary>
		/// System generated key: [Level.Position].
		/// </summary>
		public string TreeKey { get; set; }

		/// <summary>
		/// Reference to an action object: some implementation of <see cref="IActivity"/> or <see cref="IWorkflow"/>.
		/// </summary>
		public ISchedulable Action { get; set; }
	}
}
