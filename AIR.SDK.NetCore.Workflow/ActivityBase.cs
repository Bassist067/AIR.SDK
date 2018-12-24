using System;
using System.Linq;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Core;

//using System.Runtime.Serialization;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// This is a base class for creating activities. 
	/// Activity is a workflow step meant to take some input data and process it to output data.
	/// </summary>
	/// <typeparam name="TInput">Generic business input object. It's an input data.</typeparam>
	/// <typeparam name="TOutput">Generic business output object. It's an output data (after processing in task processor).</typeparam>
	////[Serializable]
	public class ActivityBase<TInput, TOutput> : IActivity
		where TInput : class
		where TOutput : class
	{
		// Used to prevent updating Version of activity.
		private bool _committed = false;

		#region ISchedulable Members

		/// <summary>
		/// The name of the activity type within the domain.
		/// The specified string must not start or end with whitespace. It must not contain a : 
		/// (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		/// Also, it must not contain the literal string quotarnquot.
		/// </summary>
		public string Name { get { return Options.Name; } }
		/// <summary>
		/// A textual description of the activity type.
		/// </summary>
		public string Description { get { return Options.Description; } set { Options.Description = value; } }
		/// <summary>
		/// Used to retry running activity. If exceeds, activity is considered to be failed.
		/// </summary>
		public int MaxAttempts { get { return Options.MaxAttempts; } set { Options.MaxAttempts = value; } }
		/// <summary>
		/// The duration to wait before firing the Activity. 
		/// Optional. The duration is specified in seconds; an integer greater than or equal to 0.
		/// If value is 0 or negative the Activity will be scheduled immediately.
		/// </summary>
		public int DelayTimeoutInSeconds { get { return Options.DelayTimeoutInSeconds; } set { Options.DelayTimeoutInSeconds = value; } }

		#endregion

		#region IActivity Members

		public IActivityOptions<TInput, TOutput> Options { get; set; }

		IActivityOptions IActivity.Options
		{
			get { return (IActivityOptions) this.Options; }
		}

		string IActivity.ActivityId { get; set; }
		/// <summary>
		/// TaskList is a grouping key for Amazon  You can set activity workers polling for specific task lists
		/// </summary>
		public TaskList TaskList { get; private set; }
		/// <summary>
		/// Serialized input object. Everything coming to SWF is a string.
		/// </summary>
		string IActivity.Input { get; set; }
		/// <summary>
		/// Input object. The data passed to the <see cref="TaskProcessor(TInput)"/> method.
		/// </summary>
		public TInput Input
		{
			get { return Utils.DeserializeFromJSON<TInput>(((IActivity) this).Input); }
			set { ((IActivity) this).Input = Utils.SerializeToJSON(value); }
		}
		/// <summary>
		/// This method is what actually does the work of the task.
		/// </summary>
		string IActivity.TaskProcessor(string input)
		{
			if (Options == null)
				throw new Exception("Options is undefined");

			if (Options.ActivityAction == null)
				throw new Exception("ActivityAction is undefined");

			return Utils.SerializeToJSON(Options.ActivityAction(Utils.DeserializeFromJSON<TInput>(input)));
		}
		/// <summary>
		/// Shortcut for <see cref="Options.ActivityAction"/>.
		/// This method is what actually does the work of the task.
		/// </summary>
		//public Func<TInput, IResult<TOutput>> ActivityAction { get {return Options.ActivityAction; } set {Options.ActivityAction = value; } }

		/// <summary>
		/// Deserializes and packages result.
		/// </summary>
		/// <param name="result">Serialized object.</param>
		/// <returns>Deserialized and packaged TOutput object.</returns>
		public object GetTypedObject(string result)
		{
			return Utils.DeserializeFromJSON<TOutput>(result);
		}

		#endregion



		/// <summary>
		/// Initializes a new instance of the <see cref="ll.SDK.Workflow.ActivityBase"/> class.
		/// </summary>
		/// <param name="name">Name of the activity type within the domain. <see cref="IActivity.Name"/></param>
		/// <param name="taskList">The task list for this workflow execution. <see cref="IActivity.TaskList"/></param>
		public ActivityBase(IActivityOptions<TInput, TOutput> activityOptions)
		{
			if (activityOptions == null)
				throw new ArgumentNullException(nameof(activityOptions));

			ValidateTypes();

			//TODO: this should happen before starting workflow.
			//ValidateOptions(activityOptions);

			Options = activityOptions;
			TaskList = new TaskList { Name = activityOptions.TaskList };
		}

		public ActivityBase(string name, string tasklist)
			: this( new ActivityOptions<TInput, TOutput> { Name = name, TaskList = tasklist})
		{
		}

		public ActivityBase(string name, string tasklist, Func<TInput, IResult<TOutput>> activityAction)
			: this(new ActivityOptions<TInput, TOutput> { Name = name, TaskList = tasklist, ActivityAction = activityAction })
		{
		}

		/// <summary>
		/// Registers activity type for a specific version.
		/// </summary>
		/// <param name="domainName">Domain where the activity type should be registered.</param>
		/// <param name="client">Interface for accessing Amazon Simple Workflow Service.</param>
		void IRegistrable.Register(string domainName, IAmazonSimpleWorkflow client)
		{
			var listActivityRequest = new ListActivityTypesRequest
			{
				Domain = domainName,
				Name = Options.Name,
				RegistrationStatus = RegistrationStatus.REGISTERED
			};


			if (
				client.ListActivityTypesAsync(listActivityRequest).Result
					.ActivityTypeInfos.TypeInfos.FirstOrDefault(x => x.ActivityType.Version == Options.Version) == null)
			{
				Logger.Trace("New Activity Type.");

				RegisterActivityTypeRequest request = new RegisterActivityTypeRequest
				{
					Name = Options.Name,
					Domain = domainName,
					Description = Options.Description,
					Version = Options.Version,
					DefaultTaskList = TaskList, //Worker poll based on this
					DefaultTaskScheduleToCloseTimeout = Options.ScheduleToCloseTimeout.ToStringOrNone(),
					DefaultTaskScheduleToStartTimeout = Options.ScheduleToStartTimeout.ToStringOrNone(),
					DefaultTaskStartToCloseTimeout = Options.StartToCloseTimeout.ToStringOrNone(),
					DefaultTaskHeartbeatTimeout = Options.HeartbeatTimeout.ToStringOrNone()
				};

				client.RegisterActivityTypeAsync(request).Wait();
			}

			Logger.Trace("Activity. Name: {0}, Version: {1}", Options.Name, Options.Version);
		}

		void IRegistrable.Validate() 
		{
			ValidateOptions();
		}

		/// <summary>
		/// Sets and locks activity version.
		/// Version becomes read only after the activity was added to a workflow.
		/// </summary>
		void IActivity.LockVersion(string version)
		{
			if (!_committed)
			{
				Options.Version = version;
				_committed = true;
			}
		}

		protected virtual void ValidateOptions()
		{
			//if (Options == null)
			//	throw new ArgumentNullException("Options");

			string message = Options.Name + ". '{0}' is undefined.";

			//throw new NotImplementedException();
			if (string.IsNullOrEmpty(Options.Name))
				throw new Exception(string.Format(message, "Name"));

			if (string.IsNullOrEmpty(Options.TaskList))
				throw new Exception(string.Format(message, "TaskList"));

			if (Options.ActivityAction == null)
				throw new Exception(string.Format(message, "ActivityAction"));
		}

		protected virtual void ValidateTypes()
		{
			Utils.ValidateType(typeof(TInput));
			Utils.ValidateType(typeof(TOutput));
		}

		#region ICloneable Members

		public object Clone()
		{
			var clone = (ActivityBase<TInput, TOutput>) this.MemberwiseClone();
			HandleCloned(clone);
			return clone;
		}

		protected virtual void HandleCloned(ActivityBase<TInput, TOutput> clone)
		{
			//Nothing particular in the base class, but maybe usefull for childs.
			//Not abstract so childs may not implement this if they don't need to.
		}

		#endregion
	}

	/// <summary>
	/// Represents shortcut for activity <see cref="ActivityBase{String,String}"/>. 
	/// </summary>
	////[Serializable]
	public class Activity : ActivityBase<string, string>
	{
		public Activity(IActivityOptions<string, string> activityOptions)
			: base(activityOptions)
		{
		}
		public Activity(string name, string tasklist)
			: this(new ActivityOptions<string, string> { Name = name, TaskList = tasklist })
		{
		}

		public Activity(string name, string tasklist, Func<string, IResult<string>> activityAction)
			: this(new ActivityOptions<string, string> { Name = name, TaskList = tasklist, ActivityAction = activityAction })
		{
		}
	}

}