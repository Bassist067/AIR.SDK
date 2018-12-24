using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Represents suspendable activity.
	/// Workflow will be suspended until the <see cref="SuspendableActivity{TInput,TOutput}.TaskProcessor"/> method returns true.
	/// </summary>
	/// <typeparam name="TInput">Generic Input</typeparam>
	/// <typeparam name="TOutput"></typeparam>
	//[Serializable]
	public class SuspendableActivity<TInput, TOutput> : ActivityBase<TInput, TOutput>, ISuspendable
		where TInput : class
		where TOutput : class
	{
		#region ISuspendable Members

		/// <summary>
		/// Interval during which the activity is working on. When exceeded, the activity is considered as failed.
		/// </summary>
		public int WaitingTimeInSeconds { get; set; }

		#endregion

		public SuspendableActivity(IActivityOptions<TInput, TOutput> activityOptions)
			: base(activityOptions)
		{
		}

		public SuspendableActivity(string name, string tasklist)
			: this(new ActivityOptions<TInput, TOutput> { Name = name, TaskList = tasklist })
		{
		}
	}
}
