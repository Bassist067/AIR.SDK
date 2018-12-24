namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// ISchedulable interface should be implemented for each workflow step can be scheduled. 
	/// "Scheduled" means "made a run activity request to "
	/// </summary>
	public interface ISchedulable
	{
		/// <summary>
		///  The name of the action (Activity or child Workflow) type within the domain.
		///  The specified string must not start or end with whitespace. It must not contain
		///  a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		///  Also, it must not contain the literal string quotarnquot.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Description of schedulabe entry. <see cref="IActivity"/> and <see cref="IWorkflow"/>.
		/// </summary>
		string Description { get; set; }

		/// <summary>
		/// Number of attempts for retry running activity. If exceeds, activity is considered to be failed.
		/// </summary>
		int MaxAttempts { get; set; }
	}
}