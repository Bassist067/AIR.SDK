using System;
using System.Collections.Generic;
using System.Linq;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// This class provides the parallel Activity functionality. Use it when you need to process a collection of generic business objects 
	/// </summary>
	/// <typeparam name="TInput">The collection comes as input</typeparam>
	/// <typeparam name="TOutput">The aggregated results for </typeparam>
	/// <typeparam name="TActivityInput">the individual collection object as input</typeparam>
	/// <typeparam name="TActivityOutput">result of the collection processor</typeparam>
	public class ParallelActivityCollectionBase<TInput, TOutput, TActivityInput, TActivityOutput> :
		ActivityBase<TActivityInput, TActivityOutput>, IParallelActivityCollection
		where TInput : class
		where TOutput : class
		where TActivityInput : class
		where TActivityOutput : class
	{
		public ParallelActivityCollectionBase(IActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput> collectionOptions)
			: base(collectionOptions)
		{
			if (collectionOptions == null)
				throw new ArgumentNullException(nameof(collectionOptions));

			Options = collectionOptions;
		}
		public ParallelActivityCollectionBase(string activityName, string taskList)
			: this(new ActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput> { Name = activityName, TaskList = taskList })
		{

		}

		public new IActivityCollectionOptions<TInput, TOutput, TActivityInput, TActivityOutput> Options { get; private set; }

		#region IParallelActivityCollection Members

		/// <summary>
		/// Type wrapper for the serialized input
		/// </summary>
		public new TInput Input
		{
			get { return Utils.DeserializeFromJSON<TInput>(((IActivity)this).Input); }
			set { ((IActivity)this).Input = Utils.SerializeToJSON(value); }
		}

		/// <summary>
		/// <see cref="IParallelCollection.Processor"/>
		/// </summary>
		/// <param name="input">serialized input</param>
		/// <returns>result for processing collection item</returns>
		IEnumerable<ICollectionItemInput> IParallelCollection.Processor(string input)
		{
			if (Options.CollectionProcessor == null)
				throw new Exception("Activity '" + Name + "': CollectionProcessor is undefined.");

			return Options.CollectionProcessor(Utils.DeserializeFromJSON<TInput>(input));
		}

		/// <summary>
		/// <see cref="IParallelCollection.Reducer"/>
		/// </summary>
		/// <param name="taskProcessorResults">Aggregates individual activity result</param>
		/// <returns>serialized parallel activity collection output</returns>
		string IParallelCollection.Reducer(IEnumerable<string> taskProcessorResults)
		{
			if (Options.Reducer == null)
				throw new Exception("Activity '"+Name+"': Reducer is undefined.");

			var result = Options.Reducer(taskProcessorResults.Select(Utils.DeserializeFromJSON<TActivityOutput>));
			return Utils.SerializeToJSON(result);
		}

		#endregion

		protected override void ValidateOptions()
		{
			base.ValidateOptions();

			string message = Options.Name + ". '{0}' is undefined.";

			if (Options.CollectionProcessor == null)
				throw new Exception(string.Format(message, "CollectionProcessor"));

			if (Options.Reducer == null)
				throw new Exception(string.Format(message, "Reducer"));
		}

		protected override void ValidateTypes()
		{
			base.ValidateTypes();

			Utils.ValidateType(typeof(TInput));
			Utils.ValidateType(typeof(TOutput));
		}

	}

	/// <summary>
	/// default constructor for serialized types
	/// </summary>
	public class ParallelActivityCollection : ParallelActivityCollectionBase< List<string>, string, string, string>
	{
		public ParallelActivityCollection(ActivityCollectionOptions<List<string>, string, string, string> collectionOptions)
			: base(collectionOptions)
		{
		}

		public ParallelActivityCollection(string name, string tasklist)
			: this(new ActivityCollectionOptions<List<string>, string, string, string> { Name = name, TaskList = tasklist })
		{
		}
	}
}