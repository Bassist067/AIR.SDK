using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIR.API.Core.Queue;
using AIR.API.Core.Storage;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
//using AIR.Diagnostic.Logging;

namespace AIR.SDK.NetStandard.QueueManager
{
	/// <summary>
	/// This is an only class you need to use for sending and receiving SQS messages
	/// </summary>
	public class SQSManager : IQueueManager
	{
		private static NLog.Logger _logger = NLog.LogManager.GetLogger("AWS.SQS");

		private readonly IAmazonSQS _sqsClient;
		private readonly IStorageManager _storageClient;

		// Magic const
		internal const int MaxAttempts = 3;

		/// <summary>
		/// It's a flag we use for subscribing 
		/// </summary>
		internal bool IsListening;

		private int _visibilityTimeout = 60; // in seconds

		/// <summary>
		/// It's a parallel dictionary for storing available queues
		/// </summary>
		internal ConcurrentDictionary<string, string> AvailableQueues = new ConcurrentDictionary<string, string>();

		public Dictionary<string, string> MessageAttributes { get; private set; }

		/// <summary>
		/// Gets and sets the property VisibilityTimeout.
		/// The duration (in seconds) that the received messages are hidden from subsequent
		/// retrieve requests after being retrieved.
		/// Value must be between 0 and 43200. If not specified the duration will be 60 seconds.
		/// </summary>
		public int VisibilityTimeout
		{
			get { return _visibilityTimeout; }
			set { _visibilityTimeout = value; }
		}


	    #region Constructors
	    
        public SQSManager(IAmazonSQS amazonSQS, IStorageManager storageManager = null)
		{
			_sqsClient = amazonSQS;
			_storageClient = storageManager;

			MessageAttributes = new Dictionary<string,string>();
		}

        #endregion


	    #region Public Methods

        /// <summary>
        /// Sends a message
        /// </summary>
        /// <typeparam name="T">type of the business object</typeparam>
        /// <param name="queueName">a SQS Queue name</param>
        /// <param name="payload">any busines object</param>
        public string SendMessage<T>(string queueName, T payload)
		{
			ValidateType(typeof (T));

			var sendMessageRequest = CreateMessageRequest(queueName, payload);

			return SendMessage(sendMessageRequest);
		}

        public async Task<string> SendMessageAsync<T>(string queueName, T payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateType(typeof(T));

            var sendMessageRequest = CreateMessageRequest(queueName, payload);

            return await SendMessageAsync(sendMessageRequest, cancellationToken);
        }

        public int SendMessageBatch<T>(string queueName, IEnumerable<T> entries)
        {
            ValidateType(typeof(T));

            var sendMesssageReqs = CreateMessageBatchRequestEntries(entries);

            if (_storageClient != null)
            {
                foreach (var item in sendMesssageReqs)
                {
                    if (IsLarge(item.MessageBody, item.MessageAttributes))
                        StoreMessage(item);
                }
            }

            return SendMessageBatch(GetQueueUrl<T>(queueName), sendMesssageReqs);
        }

        public async Task<int> SendMessageBatchAsync<T>(string queueName, IEnumerable<T> entries, CancellationToken cancellationToken = default(CancellationToken))
        {
            ValidateType(typeof(T));

            var sendMesssageReqs = CreateMessageBatchRequestEntries(entries);

            if (_storageClient != null)
            {
                foreach (var item in sendMesssageReqs)
                {
                    if (IsLarge(item.MessageBody, item.MessageAttributes))
                    {
                        await StoreMessageAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return await SendMessageBatchAsync(GetQueueUrl<T>(queueName), sendMesssageReqs, cancellationToken).ConfigureAwait(false);
        }

		public HttpStatusCode DeleteMessage<T>(string queueName, string receiptHandle)
		{
			if (string.IsNullOrEmpty(queueName))
			{
				throw new SQSManagerException("DeleteMessage: param queueUrl cannot be empty.");
			}

			if (string.IsNullOrEmpty(receiptHandle))
			{
				throw new SQSManagerException("DeleteMessage: param receiptHandle cannot be empty.");
			}

			return DeleteMessage(new DeleteMessageRequest(GetQueueUrl<T>(queueName), receiptHandle));
		}

		/// <summary>
		/// Creates a constantly running task polling AWS for messages to receive
		/// </summary>
		/// <typeparam name="T">any business object</typeparam>
		/// <param name="queueName">a name of queue to subscribe</param>
		/// <param name="handler">a business logic method for processing messages</param>
		/// <param name="cancellationToken">a cancellation token to control execution</param>
		/// <param name="deleteMsgOnSuccess">defines whether store received messages or not. An AWS parameter</param>
		/// <param name="maxMessagesAtaTime">defines how many messages can be received at a one time. An AWS parameter</param>
		/// <param name="waitTimeSeconds">sets polling cooldown</param>
		public Task SubscribeAsync<T>(string queueName, Func<T, string, CancellationToken, bool> handler, CancellationToken cancellationToken, bool deleteMsgOnSuccess = true,
			int maxMessagesAtaTime = 10, int waitTimeSeconds = 20)
		{
			ValidateType(typeof (T));

			if (handler == null)
			{
				throw new ArgumentException("required parameter", nameof(handler));
			}

			IsListening = true;

			// A background task constantly polls SQS for incoming messages
			var task = Task.Run(() =>
			{
				_logger.Debug("Start loop IsListening");

				while (IsListening)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						Console.WriteLine("Task has been cancelled.");
						System.Diagnostics.Debug.WriteLine("Task has been cancelled.");
						//cancellationToken.ThrowIfCancellationRequested();
						break;
					}

					_logger.Debug("ReceiveMessage is about to be called. QueueName: {0}", queueName);
					//as soon we listen, we keep receiving messages
					try
					{
						ReceiveMessage(queueName, handler, cancellationToken, deleteMsgOnSuccess, maxMessagesAtaTime, waitTimeSeconds);
					}
					catch (Exception ex)
					{
						_logger.Error("ReceiveMessage Failed: {0}", ex.Message);
						throw;
					}
				}
			}, cancellationToken);

			_logger.Debug("return from SubscribeAsync.");
			return task;
		}

	    /// <summary>
	    /// Use this method to poll SQS for incoming messages.
	    /// </summary>
	    /// <typeparam name="T">Type of business object.</typeparam>
	    /// <param name="queueName">name of the queue to poll from</param>
	    /// <param name="handler">a handler for business object. Should take a T generic business object as input and returns bool success as output</param>
	    /// <param name="cancellationToken"></param>
	    /// <param name="deleteMsgOnSuccess">SQS can store messages even after successful handling. Generally this should be true</param>
	    /// <param name="maxMessagesAtaTime">Describes how many messages receiver can capture to local processing</param>
	    /// <param name="waitTimeSeconds">Delay between sqs message pollings</param>
	    public void ReceiveMessage<T>(string queueName, Func<T, string, CancellationToken, bool> handler, CancellationToken cancellationToken, bool deleteMsgOnSuccess = true, int maxMessagesAtaTime = 10, int waitTimeSeconds = 20)
		{
			_logger.Debug("ReceiveMessage called.");

		    string queueUrl;

            try
            {
                queueUrl = GetQueueUrl<T>(queueName);
            }
            catch (Exception ex)
            {
                _logger.Error("GetQueueUrl Failed: {0}", ex.Message);

                throw new Exception($"Error subscribing to Queue [{queueName}]: {ex}", ex);
            }

            _logger.Debug("queueUrl: [{0}]", queueUrl);

			ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest(queueUrl)
			{
				// Magic const
				VisibilityTimeout = VisibilityTimeout, //43200, //Max Value
				MaxNumberOfMessages = maxMessagesAtaTime,
				WaitTimeSeconds = waitTimeSeconds,
			};

			// TODO: this can be redone to use attributes passed directly to this method.
			foreach (var key in MessageAttributes.Keys)
				receiveMessageRequest.MessageAttributeNames.Add(key);

			receiveMessageRequest.MessageAttributeNames.Add(SQSConstants.RESERVED_ATTRIBUTE_NAME);
			receiveMessageRequest.MessageAttributeNames.Add(SQSConstants.MESSAGE_TYPE_NAME);

			ReceiveMessageResponse receiveMessageResponse = _sqsClient.ReceiveMessageAsync(receiveMessageRequest, cancellationToken).Result;

			_logger.Debug("HttpStatusCode: [{0}]", receiveMessageResponse.HttpStatusCode);

			//check if we're good 
			if (receiveMessageResponse.HttpStatusCode != HttpStatusCode.OK)
			{
				_logger.Error("Error Receiving from queue [{0}]: {1}; {2}",
					queueName, receiveMessageResponse.HttpStatusCode,
					receiveMessageResponse.ResponseMetadata);
				return;
			}

		    //_logger.Debug("Messages.Count: [{0}]", receiveMessageResponse.Messages.Count);

		    TimerCallback timerDelegate = TimerTask;

		    //Parse each message from the queue response 
		    foreach (Message message in receiveMessageResponse.Messages)
		    {
		        if (cancellationToken != null && cancellationToken.IsCancellationRequested)
		        {
		            //Console.WriteLine("Message processing has been canceled.");
		            //System.Diagnostics.Debug.WriteLine("Message processing has been canceled.");
		            //cancellationToken.ThrowIfCancellationRequested();
		            _logger.Trace("Message processing has been canceled.");
		            break;
		        }

		        // Compare Message Type
		        MessageAttributeValue messageTypeAttributeValue;
		        if (message.MessageAttributes.TryGetValue(SQSConstants.MESSAGE_TYPE_NAME, out messageTypeAttributeValue))
		        {
		            if (messageTypeAttributeValue.StringValue != typeof(T).FullName)
		            {
		                _logger.Trace("Message Type '{0}' does not match to '{1}'", messageTypeAttributeValue.StringValue, typeof(T).FullName);
		                continue;
		            }
		        }

		        // Compare Custom attributes.
		        // Search for at least one custom attribute (if indicated) in the message.
		        if (MessageAttributes.Count > 0)
		        {
		            if (!MessageAttributes.Any(x => message.MessageAttributes.ContainsKey(x.Key)))
		            {
		                _logger.Trace("Message has no matched attribuets.");
		                continue;
		            }
		        }

		        StateObject stateObj = new StateObject
		        {
		            TimerCanceled = false,
		            Attempts = 1,
		            DueTime = Math.Max(0, receiveMessageRequest.VisibilityTimeout * 1000 - 2000), // in milliseconds
                    Request = new ChangeMessageVisibilityRequest(queueUrl, message.ReceiptHandle, receiveMessageRequest.VisibilityTimeout),
		            TokenSource = new CancellationTokenSource()
		        };

		        try
		        {
		            bool success = false;

		            try
		            {
		                _logger.Debug("Message Body: [{0}]", message.Body);

		                MessageAttributeValue largePayloadAttributeValue;
		                if (message.MessageAttributes.TryGetValue(SQSConstants.RESERVED_ATTRIBUTE_NAME, out largePayloadAttributeValue) &&
		                    _storageClient != null)
		                {
		                    var pointer = (IStorageReference) JsonConvert.DeserializeObject(message.Body, _storageClient.StorageReferenceType);

		                    message.Body = _storageClient.GetData(pointer);
		                    message.ReceiptHandle = EmbedS3PointerInReceiptHandle(message.ReceiptHandle, pointer.GetStorageKey());
		                    message.MessageAttributes.Remove(SQSConstants.RESERVED_ATTRIBUTE_NAME);
		                }

		                if (message.Body.Length <= 256)
		                    _logger.Debug("Parse serializedObject: [{0}]", message.Body);

		                // Getting object from body
		                T dataObj;
		                try
		                {
		                    dataObj = JsonConvert.DeserializeObject<T>(message.Body);

		                    _logger.Debug("Object has been deserialized.");
		                }
		                catch (Exception)
		                {
		                    _logger.Error("Could not deserialize message body. Skipping this message.");
		                    continue;
		                }

		                if (stateObj.DueTime > 0)
		                {
		                    // Save a reference for Dispose.
		                    stateObj.TimerReference = new Timer(timerDelegate, stateObj, stateObj.DueTime, Timeout.Infinite);
		                }

		                CancellationToken ct = stateObj.TokenSource.Token;
		                // Each message is being handled here.
		                var task = Task<bool>.Factory.StartNew(() => handler(dataObj, stateObj.Request.ReceiptHandle, ct), ct);

		                task.Wait(ct);

		                stateObj.StopTimer();

		                success = task.Result;

		                _logger.Info("handler -> success = {0}", success);
		            }
		            catch (OperationCanceledException oce)
		            {
		                success = false;

		                _logger.Warn("SQS.ReceiveMessage <OperationCanceledException>: {0}", oce.Message);
		                Console.WriteLine("Message processing has been canceled.");
		                System.Diagnostics.Debug.WriteLine("Message processing has been canceled.");
		            }
		            catch (SQSManagerException sx)
		            {
		                _logger.Warn("SQS.ReceiveMessage <StoreManagerException>: {0} \nErrorCode: {1}. \nStatusCode: {2}.", sx.Message, sx.ErrorCode, sx.StatusCode);

		                if (sx.StatusCode == HttpStatusCode.NotFound)
		                {
		                    _logger.Info("DeleteMessage");
		                    DeleteMessage<T>(queueName, message.ReceiptHandle);
		                }
		                else
		                {
		                    throw;
		                }
		            }

		            if (success && deleteMsgOnSuccess)
		            {
		                _logger.Info("Success: DeleteMessage.");

		                DeleteMessage<T>(queueName, message.ReceiptHandle);
		            }
		        }
		        finally
		        {
		            stateObj.StopTimer();

		            stateObj.TokenSource?.Dispose();
		            stateObj.TimerReference?.Dispose();
		        }
		    }
		}

        /// <summary>
        /// Stop the subscriber task running
        /// </summary>
        public void UnSubscribe() { IsListening = false; }

        /// <summary>
        /// Gets a queue to poll from
        /// </summary>
        /// <param name="queueName">a name of message queue</param>
        /// <returns></returns>
        public string GetQueueUrl<T>(string queueName)
        {
            string queueUrl;

            var internalName = GetInternalQueueName(queueName, typeof(T));

            //the Dictionary is used as storage for available queues to have this unique
            if (!AvailableQueues.TryGetValue(internalName, out queueUrl))
            {
                //Create queue in SQS if queue with such name is not created
                CreateQueueResponse queue = _sqsClient.CreateQueueAsync(internalName).Result;

                //something is not right
                if (queue.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Unexpected result creating SQS: " + queue.HttpStatusCode);
                }

                queueUrl = queue.QueueUrl;
                //remember the newly created queue
                AvailableQueues[internalName] = queueUrl;
            }
            return queueUrl;
        }

        #endregion



        internal string SendMessage(SendMessageRequest sendMessageRequest)
		{
			if (string.IsNullOrEmpty(sendMessageRequest.MessageBody))
			{
				throw new SQSManagerException("MessageBody cannone be null or empty.");
			}

			try
			{
				if (_storageClient != null)
				{
					if (IsLarge(sendMessageRequest.MessageBody, sendMessageRequest.MessageAttributes))
					{
						sendMessageRequest = StoreMessage(sendMessageRequest);
					}
				}

				var response = _sqsClient.SendMessageAsync(sendMessageRequest).Result;
				return response.MessageId;
			}
			catch (AmazonSQSException e)
			{
				throw new SQSManagerException("Message was not sent.", e);
			}
		}

		internal async Task<string> SendMessageAsync(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken = default(CancellationToken))
		{
			//if (sendMessageRequest == null)
			//{
			//	throw new AmazonClientException("sendMessageRequest cannot be null.");
			//}

			if (string.IsNullOrEmpty(sendMessageRequest.MessageBody))
			{
				throw new SQSManagerException("MessageBody cannone be null or empty.");
			}
			try
			{
				if (_storageClient != null)
				{
					if (IsLarge(sendMessageRequest.MessageBody, sendMessageRequest.MessageAttributes))
					{
						sendMessageRequest = await StoreMessageAsync(sendMessageRequest, cancellationToken).ConfigureAwait(false);
					}
				}

				var response = await _sqsClient.SendMessageAsync(sendMessageRequest, cancellationToken).ConfigureAwait(false);
				return response.MessageId;
			}
			catch (AmazonSQSException e)
			{
				throw new SQSManagerException("Message was not sent.", e);
			}
		}

		internal int SendMessageBatch(string queueUrl, IEnumerable<SendMessageBatchRequestEntry> entries)
		{
			try
			{
				SendMessageBatchRequest batchReq = new SendMessageBatchRequest(queueUrl, entries.ToList());

				var result = _sqsClient.SendMessageBatchAsync(batchReq).Result;

				return result.Successful.Count;
			}
			catch (AmazonSQSException e)
			{
				throw new SQSManagerException("Messages were not sent.", e);
			}
		}

		internal async Task<int> SendMessageBatchAsync(string queueUrl, IEnumerable<SendMessageBatchRequestEntry> entries, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				SendMessageBatchRequest batchReq = new SendMessageBatchRequest(queueUrl, entries.ToList());

				var response = await _sqsClient.SendMessageBatchAsync(batchReq, cancellationToken).ConfigureAwait(false);

				return response.Successful.Count;
			}
			catch (AmazonSQSException e)
			{
				throw new SQSManagerException("Message was not sent.", e);
			}
		}

		internal HttpStatusCode DeleteMessage(DeleteMessageRequest deleteMessageRequest)
		{
			if (deleteMessageRequest == null)
			{
				throw new SQSManagerException("deleteMessageRequest cannot be null");
			}

			if (IsS3ReceiptHandle(deleteMessageRequest.ReceiptHandle))
			{
				DeleteMessagePayloadFromStore(deleteMessageRequest.ReceiptHandle);

				deleteMessageRequest.ReceiptHandle = GetOriginalReceiptHandle(deleteMessageRequest.ReceiptHandle);
			}

			var response = _sqsClient.DeleteMessageAsync(deleteMessageRequest).Result;
			return response.HttpStatusCode;
		}



		/// <summary>
		/// Represents the method that handles calls from a System.Threading.Timer.
		/// </summary>
		/// <param name="stateObj"></param>
		private void TimerTask(object stateObj)
		{
			try
			{
				StateObject state = (StateObject) stateObj;

				_logger.Debug("TimerTask: {0}", state);

				if (state.TokenSource.IsCancellationRequested)
				{
					state.TimerReference.Change(Timeout.Infinite, Timeout.Infinite);
					return;
				}

				if (state.TimerCanceled || (state.Attempts > MaxAttempts))
				{
					state.AbortTask();
					return;
				}

				_logger.Info("Call ChangeMessageVisibility method.");
				var response = _sqsClient.ChangeMessageVisibilityAsync(state.Request).Result;
				if (response.HttpStatusCode != HttpStatusCode.OK)
				{
					_logger.Error("Abort Task. StatusCode: {0}", response.HttpStatusCode);
					state.AbortTask();
					return;
				}

				// Use the interlocked class to increment the counter variable.
				Interlocked.Increment(ref state.Attempts);

				_logger.Debug("Restart timer.");
				// Restart timer.
				state.TimerReference.Change(state.DueTime, Timeout.Infinite);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Error: {0}", e.Message);
			}
		}

		/// <summary>
		/// Returns a compound key based on user defined queueName and name of generic type T.
		/// Used to build a real queue name.
		/// </summary>
		private string GetInternalQueueName(string queueName, Type t)
		{
			return $"{queueName}_{t.Name}";
		}

		private bool IsLarge(string messageBody, Dictionary<string, MessageAttributeValue> attributes)
		{
			var contentSize = Encoding.UTF8.GetBytes(messageBody).Length;// LongLength;
			var attributesSize = GetAttributesSize(attributes);

			return (contentSize + attributesSize > SQSConstants.DEFAULT_MESSAGE_SIZE_THRESHOLD);
		}

		private int GetAttributesSize(Dictionary<string, MessageAttributeValue> attributes)
		{
			var attributesSize = 0;
			foreach (var messageAttributeValue in attributes)
			{
				attributesSize += Encoding.UTF8.GetByteCount(messageAttributeValue.Key);
				if (!string.IsNullOrEmpty(messageAttributeValue.Value.DataType))
				{
					attributesSize += Encoding.UTF8.GetByteCount(messageAttributeValue.Value.DataType);
				}

				var stringValue = messageAttributeValue.Value.StringValue;
				if (!string.IsNullOrEmpty(stringValue))
				{
					attributesSize += Encoding.UTF8.GetByteCount(stringValue);
				}

				var binaryValue = messageAttributeValue.Value.BinaryValue;
				if (binaryValue != null)
				{
					attributesSize += binaryValue.ToArray().Length;
				}
			}

			return attributesSize;
		}

		private void CheckMessageAttributes(Dictionary<string, MessageAttributeValue> attributes)
		{
			var attributesSize = GetAttributesSize(attributes);

			if (attributesSize > SQSConstants.DEFAULT_MESSAGE_SIZE_THRESHOLD)
			{
				var errorMessage =
					$"Total size of Message attributes is {attributesSize} bytes which is larger than the threshold of {SQSConstants.DEFAULT_MESSAGE_SIZE_THRESHOLD}  Bytes. " +
					"Consider including the payload in the message body instead of message attributes.";
				throw new AmazonClientException(errorMessage);
			}

			if (attributes.Count > SQSConstants.MAX_AllOWED_ATTRIBUTES)
			{
				var errorMessage =
					$"Number of message attributes [{attributes.Count}] ] exceeds the maximum allowed for large-payload messages [{SQSConstants.MAX_AllOWED_ATTRIBUTES}]";
				throw new AmazonClientException(errorMessage);
			}

			MessageAttributeValue largePayloadAttributeValue;
			if (attributes.TryGetValue(SQSConstants.RESERVED_ATTRIBUTE_NAME, out largePayloadAttributeValue))
			{
				var errorMessage =
					$"Message attribute name {SQSConstants.RESERVED_ATTRIBUTE_NAME}  is reserved for use by SQS extended client.";
				throw new AmazonClientException(errorMessage);
			}
		}


		private SendMessageRequest StoreMessage(SendMessageRequest sendMessageRequest)
		{
			if (_storageClient == null)
			{
				return sendMessageRequest;
			}

			CheckMessageAttributes(sendMessageRequest.MessageAttributes);

			var messageContentStr = sendMessageRequest.MessageBody;
			var messageContentSize = Encoding.UTF8.GetBytes(messageContentStr).Length;// LongLength;

			var messageAttributeValue = new MessageAttributeValue
			{
				DataType = "Number", StringValue = messageContentSize.ToString()
			};

			sendMessageRequest.MessageAttributes.Add(SQSConstants.RESERVED_ATTRIBUTE_NAME, messageAttributeValue);

			sendMessageRequest.MessageBody = JsonConvert.SerializeObject(_storageClient.PutData(sendMessageRequest.MessageBody));

			return sendMessageRequest;
		}

		private async Task<SendMessageRequest> StoreMessageAsync(SendMessageRequest sendMessageRequest, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_storageClient == null)
			{
				return sendMessageRequest;
			}

			CheckMessageAttributes(sendMessageRequest.MessageAttributes);

			var messageContentStr = sendMessageRequest.MessageBody;
			var messageContentSize = Encoding.UTF8.GetBytes(messageContentStr).Length;// LongLength;

			var messageAttributeValue = new MessageAttributeValue
			{
				DataType = "Number", StringValue = messageContentSize.ToString()
			};

			// !!!
			sendMessageRequest.MessageAttributes.Add(SQSConstants.RESERVED_ATTRIBUTE_NAME, messageAttributeValue);

			var storeReference = await _storageClient.PutDataAsync(sendMessageRequest.MessageBody, null, cancellationToken).ConfigureAwait(false);

			sendMessageRequest.MessageBody = JsonConvert.SerializeObject(storeReference);

			return sendMessageRequest;
		}

		private SendMessageBatchRequestEntry StoreMessage(SendMessageBatchRequestEntry sendMessageRequest)
		{
			if (_storageClient == null)
			{
				return sendMessageRequest;
			}

			CheckMessageAttributes(sendMessageRequest.MessageAttributes);

			var messageContentStr = sendMessageRequest.MessageBody;
			var messageContentSize = Encoding.UTF8.GetBytes(messageContentStr).Length;// LongLength;

			var messageAttributeValue = new MessageAttributeValue
			{
				DataType = "Number",
				StringValue = messageContentSize.ToString()
			};

			sendMessageRequest.MessageAttributes.Add(SQSConstants.RESERVED_ATTRIBUTE_NAME, messageAttributeValue);

			sendMessageRequest.MessageBody = JsonConvert.SerializeObject(_storageClient.PutData(sendMessageRequest.MessageBody));

			return sendMessageRequest;
		}

		private async Task<SendMessageBatchRequestEntry> StoreMessageAsync(SendMessageBatchRequestEntry sendMessageRequest, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_storageClient == null)
			{
				return sendMessageRequest;
			}

			CheckMessageAttributes(sendMessageRequest.MessageAttributes);

			var messageContentStr = sendMessageRequest.MessageBody;
			var messageContentSize = Encoding.UTF8.GetBytes(messageContentStr).Length;// LongLength;

			var messageAttributeValue = new MessageAttributeValue
			{
				DataType = "Number",
				StringValue = messageContentSize.ToString()
			};

			// !!!
			sendMessageRequest.MessageAttributes.Add(SQSConstants.RESERVED_ATTRIBUTE_NAME, messageAttributeValue);

			var storeReference = await _storageClient.PutDataAsync(sendMessageRequest.MessageBody, null, cancellationToken).ConfigureAwait(false);

			sendMessageRequest.MessageBody = JsonConvert.SerializeObject(storeReference);

			return sendMessageRequest;
		}



		private void DeleteMessagePayloadFromStore(string receiptHandle)
		{
			_logger.Debug("DeleteMessagePayloadFromStore.");

			var s3Key = GetValueFromReceiptHandleByMarker(receiptHandle, SQSConstants.S3_KEY_MARKER);

			try
			{
				_logger.Debug("s3Key");
				_storageClient.DeleteData(s3Key);
				_logger.Info("OK");
			}
			catch (Exception e)
			{
				_logger.Error(e);
			}
		}

		private SendMessageRequest CreateMessageRequest<T>(string queueName, T payload)
		{
			var sendMessageRequest = new SendMessageRequest(GetQueueUrl<T>(queueName), JsonConvert.SerializeObject(payload));

			// TODO: this can be redone to use attributes passed directly to this method.
			AppendCustomAttributes(MessageAttributes, sendMessageRequest.MessageAttributes);

			AppendMessageTypeAttribute((typeof(T)).FullName, sendMessageRequest.MessageAttributes);

			return sendMessageRequest;
		}

		private List<SendMessageBatchRequestEntry> CreateMessageBatchRequestEntries<T>(IEnumerable<T> entries)
		{
			List<SendMessageBatchRequestEntry> sendMesssageReqs = new List<SendMessageBatchRequestEntry>();

			int id = 0;

			foreach (var payload in entries)
			{
				SendMessageBatchRequestEntry batchReqEntry = new SendMessageBatchRequestEntry(id.ToString(), JsonConvert.SerializeObject(payload));
				//batchReqEntry.DelaySeconds = DelaySeconds;

				if (string.IsNullOrEmpty(batchReqEntry.MessageBody))
					continue;

				// TODO: this can be redone to use attributes passed directly to this method.
				AppendCustomAttributes(MessageAttributes, batchReqEntry.MessageAttributes);

				AppendMessageTypeAttribute((typeof(T)).FullName, batchReqEntry.MessageAttributes);

				sendMesssageReqs.Add(batchReqEntry);

				id++; //increment message id;
			}

			return sendMesssageReqs;
		}



		private bool IsS3ReceiptHandle(string receiptHandle) { return receiptHandle.Contains(SQSConstants.S3_KEY_MARKER); }

		private string EmbedS3PointerInReceiptHandle(string receiptHandle, string storeKey)
		{
			return string.Concat(
				SQSConstants.S3_KEY_MARKER,
				storeKey,
				SQSConstants.S3_KEY_MARKER,
				receiptHandle);
		}

		private string GetOriginalReceiptHandle(string receiptHandle)
		{
			var secondOccurence = receiptHandle.IndexOf(
				SQSConstants.S3_KEY_MARKER,
				receiptHandle.IndexOf(SQSConstants.S3_KEY_MARKER, StringComparison.Ordinal) + 1,
				StringComparison.Ordinal);
			return receiptHandle.Substring(secondOccurence + SQSConstants.S3_KEY_MARKER.Length);
		}

		private string GetValueFromReceiptHandleByMarker(string receiptHandle, string marker)
		{
			var firstOccurence = receiptHandle.IndexOf(marker, StringComparison.Ordinal);
			var secondOccurence = receiptHandle.IndexOf(marker, firstOccurence + 1, StringComparison.Ordinal);
			return receiptHandle.Substring(firstOccurence + marker.Length, secondOccurence - (firstOccurence + marker.Length));
		}

		/// <summary>
		/// Adds custom attibutes to a sqs message.
		/// </summary>
		/// <param name="customAttributes"></param>
		/// <param name="messageAttributes"></param>
		private void AppendCustomAttributes(Dictionary<string, string> customAttributes, Dictionary<string, MessageAttributeValue> messageAttributes)
		{
			if (customAttributes == null)
				return;

			foreach (var item in customAttributes)
			{
				if (SQSConstants.IsValidAttribute(item.Key))
				{
					var messageAttributeValue = new MessageAttributeValue
					{
						DataType = "String",
						StringValue = item.Value
					};
					messageAttributes.Add(item.Key, messageAttributeValue);
				}
			}
		}

		private void AppendMessageTypeAttribute(string messageType, Dictionary<string, MessageAttributeValue> messageAttributes)
		{
			var messageAttributeValue = new MessageAttributeValue
			{
				DataType = "String",
				StringValue = messageType
			};
			messageAttributes.Add(SQSConstants.MESSAGE_TYPE_NAME, messageAttributeValue);
		}


		internal static void ValidateType(Type t)
		{
			//if (//!t.IsSerializable &&
			//    //!(typeof (ISerializable).IsAssignableFrom(t)) &&
			//    !IsValidGeneric(t))
			//{
			//	throw new InvalidOperationException("A serializable type " + t.Name + " is required.");
			//}
		}

		internal static bool IsValidGeneric(Type t)
		{
			if (!t.GetTypeInfo().IsGenericType)
			{
				return false;
			}

			var result = t.GenericTypeArguments.Any();

			result = t.GenericTypeArguments
				.Aggregate(result, (current, gt) => current & (gt.GetTypeInfo().IsSerializable /*|| typeof (ISerializable).IsAssignableFrom(gt)*/));

			return result;
		}
	}
}