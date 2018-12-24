using System;
using System.Collections.Generic;
using Amazon.SimpleWorkflow;
using AIR.SDK.Workflow.Core;
using AIR.SDK.Workflow.Tests.Data;
using AIR.SDK.Workflow.Tests.TestUtils;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workflow
{
	public class ParallelWorkflowCollectionBaseTests
	{
		private const string _domain = "SomeActivity";
		private const string _workflowName = "WorkflowName";
		private const string _version = "1.0";
		private const string _tasklistName = "TaskList";

		private readonly IAmazonSimpleWorkflow _amazonSwf;

		public static IEnumerable<object[]> _Params
		{
			get
			{
				var data = new[]
				{
					new object[] 
					{
						new WorkflowCollectionOptions<TInput, string>
						{
							Domain = _domain,
							Name = _workflowName,
							Version = _version,
							TaskList = _tasklistName
						},
						/*new ActivityOptions
						{
							Name = _ActivityName,
							TaskList = _TaskList
							//ActivityAction = (x) => { return Substitute.For<IResult<string>>(); }
						}*/
					}
				};
				return data;
			}
		}

		public ParallelWorkflowCollectionBaseTests()
		{
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();
		}

		[Fact]
		internal void ParallelWorkflowCollectionBase_CtorTest()
		{
			var workflow = new AIR.SDK.Workflow.ParallelWorkflowCollectionBase<string, string>(_domain, _workflowName, _version, _tasklistName, _amazonSwf);

			Assert.True(workflow.Options.Domain == _domain, "ParallelWorkflowCollectionBase.Domain is not specified.");
			Assert.True(workflow.Options.Name == _workflowName, "ParallelWorkflowCollectionBase.Name is not specified.");
			Assert.True(workflow.Options.Version == _version, "ParallelWorkflowCollectionBase.Version is not specified.");
			Assert.True(workflow.TaskList.Name == _tasklistName, "ParallelWorkflowCollectionBase.TaskList.Name is not specified.");
		}

		[Fact]
		internal void ParallelWorkflowCollectionBase_CallCollectionProcessorTest()
		{
			var resultList = Substitute.For<IEnumerable<ParallelCollectionItem<string>>>();
			var wfOptions = Substitute.ForPartsOf<WorkflowCollectionOptions<TInput, string>>();
			wfOptions.Domain = _domain;
			wfOptions.Name = _workflowName;
			wfOptions.Version = _version;
			wfOptions.TaskList = _tasklistName;
			wfOptions.CollectionProcessor = (x) => { return resultList; };

			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, string>>(wfOptions, _amazonSwf);


			var input = new TInput { Value = "input string" };
			var serInput = Utils.SerializeToJSON(input);

			var result = ((IParallelCollection)workflow).Processor(serInput);

			wfOptions.Received().CollectionProcessor(Arg.Is<TInput>(x => x.Value == input.Value));
		}

		[Fact]
		internal void ParallelWorkflowCollectionBase_CallNullCollectionProcessorTest()
		{
			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, string>>(_domain, _workflowName, _version, _tasklistName, _amazonSwf);

			Assert.Throws<Exception>(() => ((IParallelCollection)workflow).Processor(null));
		}

		[Fact]
		
		internal void ParallelWorkflowCollectionBase_CallReducerTest()
		{
			var wfOptions = Substitute.ForPartsOf<WorkflowCollectionOptions<TInput, string>>();
			wfOptions.Domain = _domain;
			wfOptions.Name = _workflowName;
			wfOptions.Version = _version;
			wfOptions.TaskList = _tasklistName;
			wfOptions.Reducer = (x) => { return string.Empty; };

			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, string>>(wfOptions, _amazonSwf);

			var resultList = Substitute.For<IEnumerable<string>>();
			var result = ((IParallelCollection)workflow).Reducer(resultList);

			wfOptions.ReceivedWithAnyArgs().Reducer(resultList);
		}

		[Fact]
		internal void ParallelWorkflowCollectionBase_CallNullReducerTest()
		{
			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, TOutput>>(_domain, _workflowName, _version, _tasklistName, _amazonSwf);

			var result = ((IParallelCollection)workflow).Reducer(null);

			Assert.True(result == string.Empty);
		}

		[Fact]
		internal void ParallelActivityCollectionBase_SetInputTest()
		{
			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, TOutput>>(_domain, _workflowName, _version, _tasklistName, _amazonSwf);

			var exptected = new TInput { Value = "correct value" };

			workflow.Input = exptected;

			Assert.True(((IWorkflow)workflow).Input == Utils.SerializeToJSON(exptected));
		}

		[Fact]
		internal void ParallelActivityCollectionBase_GetInputTest()
		{
			var workflow = Substitute.ForPartsOf<ParallelWorkflowCollectionBase<TInput, TOutput>>(_domain, _workflowName, _version, _tasklistName, _amazonSwf);

			var exptected = new TInput { Value = "correct value" };

			((IWorkflow)workflow).Input = Utils.SerializeToJSON(exptected);

			var result = workflow.Input;

			AssertObjectEquals.PropertyValuesAreEqual(result, exptected);
		}
	}
}
