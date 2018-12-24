namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// Suspendable activity allows to suspend workflow before specific amount of time passed or task processor reported workflow can continue.
	/// </summary>
	public interface ISuspendable
	{
		/// <summary>
		/// Delay time. When exceeded, activity is considered failed.
		/// </summary>
		int WaitingTimeInSeconds { get; set; }
	}
}