using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AIR.API.Core.Queue
{
	/// <summary>
	/// This is a generic queue provider interface for sending and receiving messages 
	/// </summary>
	public interface IQueueManager
	{
        /// <summary>
		/// Gets and sets the property VisibilityTimeout.
		/// The duration (in seconds) that the received messages are hidden from subsequent
		/// retrieve requests after being retrieved.
		/// Value must be between 0 and 43200. If not specified the duration will be 60 seconds.
		/// </summary>
        int VisibilityTimeout { get; set; }

        Dictionary<string, string> MessageAttributes { get; }

		/// <summary>
		/// Sends a message
		/// </summary>
		/// <typeparam name="T">type of the business object</typeparam>
		/// <param name="queueName">a SQS Queue name</param>
		/// <param name="payload">any busines object</param>
		string SendMessage<T>(string queueName, T payload);

		Task<string> SendMessageAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default(CancellationToken));

		int SendMessageBatch<T>(string queueName, IEnumerable<T> entries);

		Task<int> SendMessageBatchAsync<T>(string queueName, IEnumerable<T> entries, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Creates a constantly running task polling AWS for messages to receive
		/// </summary>
		/// <typeparam name="T">any business object</typeparam>
		/// <param name="queueName">a name of queue to subscribe</param>
		/// <param name="handler">a business logic method for processing messages</param>
		/// <param name="cancellationToken">a cancellation token for execution control</param>
		/// <param name="deleteMsgOnSuccess">defines whether store received messages or not. An AWS parameter</param>
		/// <param name="maxMessagesAtaTime">defines how many messages can be received at a one time. An AWS parameter</param>
		/// <param name="waitTimeSeconds">sets polling cooldown</param>
		Task SubscribeAsync<T>(string queueName, Func<T, string, CancellationToken, bool> handler, CancellationToken cancellationToken, bool deleteMsgOnSuccess = true,
			int maxMessagesAtaTime = 10, int waitTimeSeconds = 20);

	    void ReceiveMessage<T>(string queueName, Func<T, string, CancellationToken, bool> handler, CancellationToken cancellationToken, bool deleteMsgOnSuccess = true,
	        int maxMessagesAtaTime = 10, int waitTimeSeconds = 20);

	    HttpStatusCode DeleteMessage<T>(string queueName, string receiptHandle);

	    //*bassist067 temporarily removed the method definition due to compile errors
	   /// Task<HttpStatusCode> ChangeMessageVisibilityAsync<T>(string queueName, string receiptHandle, int visibilityTimeout, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Stop the subscriber task running
        /// </summary>
        void UnSubscribe();

		/// <summary>
		/// Gets a queue to poll from
		/// </summary>
		/// <param name="queueName">a name of message queue</param>
		/// <returns></returns>
		string GetQueueUrl<T>(string queueName);
	}
}