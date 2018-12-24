using System.Collections.Generic;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.WorkflowEventsProcessor
{
	public class WorkflowEventsIteratorTest
	{
		private DecisionTask _decisionTask;
		private readonly PollForDecisionTaskRequest _pollforDecisionTaskRequest;
		private readonly IAmazonSimpleWorkflow _amazonSwf;

		private const int event1ID = 1;
		private const int event2ID = 2;


		public WorkflowEventsIteratorTest()
		{
			_decisionTask = Substitute.For<DecisionTask>();
			_pollforDecisionTaskRequest = Substitute.For<PollForDecisionTaskRequest>();
			_amazonSwf = Substitute.For<IAmazonSimpleWorkflow>();

			var workflowExecutionStartedEventAttributes = Substitute.For<WorkflowExecutionStartedEventAttributes>();
			workflowExecutionStartedEventAttributes.Input = "Input";

			var workflowExecutionCompletedEventAttributes = Substitute.For<WorkflowExecutionCompletedEventAttributes>();
			workflowExecutionCompletedEventAttributes.Result = "Output";

			var historyEvent1 = Substitute.For<HistoryEvent>();
			historyEvent1.EventId = event1ID;
			historyEvent1.WorkflowExecutionStartedEventAttributes = workflowExecutionStartedEventAttributes;
			historyEvent1.EventType = EventType.WorkflowExecutionStarted;

			var historyEvent2 = Substitute.For<HistoryEvent>();
			historyEvent2.EventId = event2ID;
			historyEvent2.WorkflowExecutionCompletedEventAttributes = Substitute.For<WorkflowExecutionCompletedEventAttributes>();
			historyEvent2.EventType = EventType.ChildWorkflowExecutionCompleted;

			_decisionTask.Events = new List<HistoryEvent>
			{
				historyEvent1,
				historyEvent2
			};
		}

		[Theory]
		[InlineData(event1ID)]
		[InlineData(event2ID)]
		public void WorkflowEventsIteratorConstructorEventsTest(int eventID)
		{
			var iterator = new WorkflowEventsIterator(ref _decisionTask, _pollforDecisionTaskRequest, _amazonSwf);
			Assert.NotNull(iterator[eventID]);
		}

		[Fact]
		public void WorkflowEventsIteratorGetEnumeratorTest()
		{
			var iterator = new WorkflowEventsIterator(ref _decisionTask, _pollforDecisionTaskRequest, _amazonSwf);
			foreach (HistoryEvent historyEvent in iterator)
			{
				Assert.NotNull(historyEvent.EventType);
			}
		}
	}
}