using System;
using System.Collections.Generic;
using Amazon.SimpleWorkflow;
using AIR.API.Core.Storage;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	public class ParallelWorkflowCollectionBase<TInput, TOutput> : WorkflowBase, IParallelWorkflowCollection
		where TInput : class
		where TOutput : class
	{
		public ParallelWorkflowCollectionBase(IWorkflowCollectionOptions<TInput, TOutput> options, IAmazonSimpleWorkflow swfClient)
			: this(options, swfClient, null)
		{
		}

		public ParallelWorkflowCollectionBase( string domain, string workflowName, string version, string tasklistName, IAmazonSimpleWorkflow swfClient)
			: this(domain, workflowName, version, tasklistName, swfClient, null)
		{
		}

		public ParallelWorkflowCollectionBase(string domain, string workflowName, string version, string tasklistName, IAmazonSimpleWorkflow swfClient, IStorageManager storageClient)
			: this(new WorkflowCollectionOptions<TInput, TOutput> { Domain = domain, Name = workflowName, Version = version, TaskList = tasklistName }, swfClient, storageClient)
		{
		}

		public ParallelWorkflowCollectionBase(IWorkflowCollectionOptions<TInput, TOutput> collectionOptions, IAmazonSimpleWorkflow swfClient, IStorageManager storeClient)
			: base(collectionOptions, swfClient, storeClient)
		{
			if (collectionOptions == null)
				throw new ArgumentNullException(nameof(collectionOptions));

			Options = collectionOptions;
		}

		public new IWorkflowCollectionOptions<TInput, TOutput> Options { get; private set; }

		public new TInput Input
		{
			get { return Utils.DeserializeFromJSON<TInput>(((IWorkflow)this).Input); }
			set { ((IWorkflow)this).Input = Utils.SerializeToJSON<TInput>(value); }
		}


		#region IParallelCollection Members

		/// <summary>
		/// Returns collection of serialized objects which are used as input data for the first action in each parallel workflow.
		/// </summary>
		/// <param name="input">Input data to be processed.</param>
		/// <returns>Collection of serialized input objects.</returns>
		IEnumerable<ICollectionItemInput> IParallelCollection.Processor(string input)
		{
			if (Options.CollectionProcessor == null)
				throw new Exception("Workflow '" + Name + "': CollectionProcessor is undefined.");

			return Options.CollectionProcessor(Utils.DeserializeFromJSON<TInput>(input));
		}

		/// <summary>
		/// Reducer is a function for processing and aggregating the entire collection output.
		/// </summary>
		/// <param name="taskProcessorResults">it's a collection of serialized output objects computed by each workflow.</param>
		/// <returns>Serialized object will be used as input data for next step in parent workflow.</returns>
		string IParallelCollection.Reducer(IEnumerable<string> taskProcessorResults)
		{
			// TODO: If Reducer is undefined its best to just hold a warning message, but dont throw exception.
			if (Options.Reducer == null)
				return string.Empty;//throw new Exception("Workflow '" + Name + "': Reducer is undefined.");
			
			return Utils.SerializeToJSON(Options.Reducer(taskProcessorResults));
		}

		#endregion

		protected override void ValidateTypes()
		{
			base.ValidateTypes();

			Utils.ValidateType(typeof(TInput));
			Utils.ValidateType(typeof(TOutput));
		}

		protected override void ValidateOptions()
		{
			base.ValidateOptions();

			//if (Options == null)
			//	throw new Exception(string.Format(message, this.GetType().Name, "Options"));

			string message = Options.Name + ". '{0}' is undefined.";

			//throw new NotImplementedException();
			if (Options.CollectionProcessor == null)
				throw new Exception(string.Format(message, "CollectionProcessor"));
		}

	}
}
