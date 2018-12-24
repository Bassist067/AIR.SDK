using System;
using System.Collections.Generic;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Implements options for <see cref="ActivityBase"/>.
	/// </summary>
	/// <typeparam name="TInput"></typeparam>
	/// <typeparam name="TOutput"></typeparam>
	public class ActivityOptions<TInput, TOutput> : IActivityOptions<TInput, TOutput>
		where TInput : class
		where TOutput : class
	{
		// These are amazon-related service fields.
		private int _scheduleToCloseTimeout = 60;
		private int _scheduleToStartTimeout = 60;
		private int _startToCloseTimeout = 60;
		private int _heartbeatTimeout = -1; // "NONE"

		private int _maxAttempts = 3;

		#region Properties

		/// <summary>
		/// The name of the activity type within the domain.
		/// The specified string must not start or end with whitespace. It must not contain a : 
		/// (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		/// Also, it must not contain the literal string quotarnquot.
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// TaskList is a grouping key for Amazon. You can set activity workers polling for specific task lists
		/// <see cref="ActivityBase.TaskList"/>.
		/// </summary>
		public string TaskList { get; set; }
		/// <summary>
		/// Activity version. Use this for storing the old a new activity implementations.
		/// </summary>
		public string Version { get; set; }
		/// <summary>
		/// A textual description of the activity type.
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Used to retry running activity. If exceeds, activity is considered to be failed.
		/// </summary>
		public int MaxAttempts
		{
			get { return _maxAttempts; }
			set { _maxAttempts = value; }
		}
		/// <summary>
		/// Specifies the maximum duration a worker may take to process this activity task.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// A negative value (e.g. -1) can be used to specify unlimited duration.
		/// </summary>
		public int StartToCloseTimeout
		{
			get { return _startToCloseTimeout; }
			set { _startToCloseTimeout = value; }
		}
		/// <summary>
		/// Specifies the maximum time before which a worker processing a task of this type must report progress 
		/// by calling RecordActivityTaskHeartbeat. If the timeout is exceeded, the activity task is automatically timed out.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// A negative value (e.g. -1) can be used to specify unlimited duration.
		/// </summary>
		public int HeartbeatTimeout
		{
			get { return _heartbeatTimeout; }
			set { _heartbeatTimeout = value; }
		}
		/// <summary>
		/// Specifies the maximum duration the activity task can wait to be assigned to a worker.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// A negative value (e.g. -1) can be used to specify unlimited duration.
		/// </summary>
		public int ScheduleToStartTimeout
		{
			get { return _scheduleToStartTimeout; }
			set { _scheduleToStartTimeout = value; }
		}
		/// <summary>
		/// Specifies how long the task can take from the time it is scheduled to the time it is complete. 
		/// As a best practice, this value should not be greater than the sum of the task schedule-to-start 
		/// timeout and the task start-to-close timeout.
		/// The duration is specified in seconds; an integer greater than or equal to 0. 
		/// A negative value (e.g. -1) can be used to specify unlimited duration.
		/// </summary>
		public int ScheduleToCloseTimeout
		{
			get { return _scheduleToCloseTimeout; }
			set { _scheduleToCloseTimeout = value; }
		}
		/// <summary>
		/// The duration to wait before firing the Activity. 
		/// Optional. The duration is specified in seconds; an integer greater than or equal to 0.
		/// If value is 0 or negative the Activity will be scheduled immediately.
		/// </summary>
		public int DelayTimeoutInSeconds { get; set; }

		#endregion

		/// <summary>
		/// This method is what actually does the work of the task. <seealso cref="ActivityBase.ActivityAction"/>.
		/// </summary>
		public Func<TInput, IResult<TOutput>> ActivityAction { get; set; }
		}


	/// <summary>
	/// Shortcut for <see cref="ActivityOptions<TInput, TOutput>"/> where both types are <see cref="string"/>.
	/// </summary>
	public class ActivityOptions : ActivityOptions<string, string>
	{
	}


	public class ActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput> : 
		ActivityOptions<TActivityInput, TActivityOutput>, 
		IActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput>
		where TInput : class
		where TOutput : class
		where TActivityInput : class
		where TActivityOutput : class
	{
		/// <summary>
		/// Used to prepare item collection for processing. <see cref="IParallelCollection.Processor"/>.
		/// </summary>
		/// <param name="input">Input collection object</param>
		/// <returns></returns>
		public Func<TInput, IEnumerable<ParallelCollectionItem<TActivityInput>>> CollectionProcessor { get; set; }
		/// <summary>
		/// Used for getting collection processing result. <see cref="IParallelCollection.Reducer"/>.
		/// </summary>
		/// <param name="taskProcessorResults">Aggregates individual activity result</param>
		/// <returns>serialized parallel activity collection output</returns>
		public Func<IEnumerable<TActivityOutput>, TOutput> Reducer { get; set; }
	}
}
