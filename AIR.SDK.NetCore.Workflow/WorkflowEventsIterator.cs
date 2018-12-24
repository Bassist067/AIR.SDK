using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

[assembly: InternalsVisibleTo("AIR.SDK.Workflow.Tests")]

namespace AIR.SDK.Workflow
{
	internal interface IWorkflowEventsIterator
	{
		/// <summary>
		/// Enumerator for history events needed for the decision.
		/// </summary>
		/// <returns>IEnumerator for the scoped events.</returns>
		IEnumerator<HistoryEvent> GetEnumerator();

		/// <summary>
		/// Indexer access based on event ID.
		/// </summary>
		/// <param name="eventId">Event ID.</param>
		/// <returns>HistoryEvent.</returns>
		HistoryEvent this[int eventId] { get; }
	}

	/// <summary>
    /// Provides an iterator for the history events in a decision request.
    /// </summary>
    internal class WorkflowEventsIterator : IEnumerable<HistoryEvent>, IWorkflowEventsIterator
	{
        private readonly List<HistoryEvent> _historyEvents;
        private readonly PollForDecisionTaskRequest _request;
        private readonly IAmazonSimpleWorkflow _swfClient;
        private DecisionTask _lastResponse;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowEventsIterator"/> class.
        /// </summary>
        /// <param name="decisionTask">Reference to the decision task passed in from </param>
        /// <param name="request">The request used to retrieve <paramref name="decisionTask"/>, which will be used to retrieve subsequent history event pages.</param>
        /// <param name="swfClient">An SWF client.</param>
        public WorkflowEventsIterator(ref DecisionTask decisionTask, PollForDecisionTaskRequest request, IAmazonSimpleWorkflow swfClient)
        {
            _lastResponse = decisionTask;
            _request = request;
            _swfClient = swfClient;

            _historyEvents = decisionTask.Events;
        }

        /// <summary>
        /// Enumerator for history events needed for the decision.
        /// </summary>
        /// <returns>IEnumerator for the scoped events.</returns>
        public IEnumerator<HistoryEvent> GetEnumerator()
        {
            foreach (HistoryEvent e in _historyEvents)
            {
                yield return e;
            }

            while (!string.IsNullOrEmpty(_lastResponse.NextPageToken))
            {
                var events = GetNextPage();
                _historyEvents.AddRange(events);

                foreach (HistoryEvent e in events)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Indexer access based on event ID.
        /// </summary>
        /// <param name="eventId">Event ID.</param>
        /// <returns>HistoryEvent.</returns>
        public HistoryEvent this[int eventId]
        {
            get
            {
                // While the eventId is not in range and there are more history pages to retrieve,
                // retrieve more history events.
                while (eventId != 0 && eventId > _historyEvents.Count && !string.IsNullOrEmpty(_lastResponse.NextPageToken))
                {
                    var events = GetNextPage();
                    _historyEvents.AddRange(events);
                }

                if (eventId < 0 || eventId > _historyEvents.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(eventId));
                }

                return _historyEvents[eventId - 1];
            }
        }

        /// <summary>
        /// Retrieves the next page of history from 
        /// </summary>
        /// <returns>The next page of history events.</returns>
        private List<HistoryEvent> GetNextPage()
        {
            var request = new PollForDecisionTaskRequest {
                Domain = _request.Domain,
                NextPageToken = _lastResponse.NextPageToken,
                TaskList = _request.TaskList,
                MaximumPageSize = _request.MaximumPageSize
            };

            const int retryCount = 10;
            int currentTry = 1;
            bool pollFailed;

            do
            {
                pollFailed = false;

                try
                {
                    _lastResponse = _swfClient.PollForDecisionTaskAsync(request).Result.DecisionTask;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Poll request failed with exception: " + ex);
                    pollFailed = true;
                }

                currentTry += 1;
            } while (pollFailed && currentTry <= retryCount);

            return _lastResponse.Events;
        }
    }
}
