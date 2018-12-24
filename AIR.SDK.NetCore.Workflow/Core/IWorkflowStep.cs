namespace AIR.SDK.Workflow.Core
{
	interface IWorkflowStep
	{
		/// <summary>
		/// System generated zero-based identifier.
		/// </summary>
		int StepNumber { get; set; }

		/// <summary>
		/// User specified unique identifier.
		/// </summary>
		string StepKey { get; set; }

		/// <summary>
		/// System generated key: [Level.Position].
		/// </summary>
		string TreeKey { get; set; }

		/// <summary>
		/// Reference to an action object: some implementation of <see cref="IActivity"/> or <see cref="IWorkflow"/>.
		/// </summary>
		ISchedulable Action { get; set; }
	}
}
