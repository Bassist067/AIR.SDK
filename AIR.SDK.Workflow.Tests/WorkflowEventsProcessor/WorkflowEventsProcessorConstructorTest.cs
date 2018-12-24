using System;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.WorkflowEventsProcessor
{
	/// <summary>
	/// Here we're testing all the constructor custom logic
	/// </summary>
	public class WorkflowEventsProcessorConstructorTest
	{
		private readonly DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
		private readonly IAmazonSimpleWorkflow _amazonSwf;
		private readonly WorkflowBase _workflowBase;

		/// <summary>
		/// Initialize constructor parameters
		/// </summary>
		
		public WorkflowEventsProcessorConstructorTest()
		{
			_decisionTask = Substitute.For<DecisionTask>();
			_pollforDecisionTaskRequest = Substitute.For<PollForDecisionTaskRequest>();
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();
			_workflowBase = Substitute.For<WorkflowBase>("domain", "workflowName", "version", "taskList", _amazonSwf);
		}

		/// <summary>
		/// A test case for new WorkflowEventsProcessor(null, any, any, swf)
		/// </summary>
		[Fact]
		public void WorkflowEventsProcessorConstructorTestDecisionTaskNull()
		{
			var exception = Assert.ThrowsAny<ArgumentNullException>(
				() => new SDK.Workflow.WorkflowEventsProcessor(null, _workflowBase, _pollforDecisionTaskRequest, _amazonSwf));

			Assert.Equal(exception.Message, "Value cannot be null.\r\nParameter name: decisionTask");
		}

		/// <summary>
		/// A test case for new WorkflowEventsProcessor(dt, null, any, swf)
		/// </summary>
		[Fact]
		public void WorkflowEventsProcessorConstructorWorkflowBaseNull()
		{
			var exception = Assert.ThrowsAny<ArgumentNullException>(
				() => new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, null, _pollforDecisionTaskRequest, _amazonSwf));

			Assert.Equal(exception.Message, "Value cannot be null.\r\nParameter name: workflow");
		}

		/// <summary>
		/// A test case for new WorkflowEventsProcessor(dt, any, null, swf)
		/// </summary>
		[Fact]
		public void WorkflowEventsProcessorConstructorPollforDecisionTaskRequestNull()
		{
			var exception = Assert.ThrowsAny<ArgumentNullException>(
				() => new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, null, _amazonSwf));

			Assert.Equal(exception.Message, "Value cannot be null.\r\nParameter name: request");
		}

		/// <summary>
		/// A test case for new WorkflowEventsProcessor(dt, any, any, null)
		/// </summary>
		[Fact]
		public void WorkflowEventsProcessorConstructorAmazonSwfNull()
		{
			var exception = Assert.ThrowsAny<ArgumentNullException>(
				() => new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest, null));

			Assert.Equal(exception.Message, "Value cannot be null.\r\nParameter name: swfClient");
		}

		/// <summary>
		/// A test case for checking WorkflowEventIterator was initialized
		/// </summary>
		[Fact]
		public void WorkflowEventsProcessorConstructorIteratorTest()
		{
			var eventsProcessor = new SDK.Workflow.WorkflowEventsProcessor(_decisionTask, _workflowBase, _pollforDecisionTaskRequest,
				_amazonSwf);
			Assert.NotNull(eventsProcessor.Events);
		}
	}
}