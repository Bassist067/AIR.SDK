using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime.Internal;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;
using AIR.API.Core.Storage;
using AIR.SDK.NetStandard.QueueManager;
using AIR.SDK.StorageManager;


namespace AIR.SDK.QueueManager.Tests
{
	public class QueueManagerTest
	{
		/// <summary>
		/// A test class for message sending
		/// </summary>
		[Serializable]
		internal class LoanMessage
		{
			/// <summary>
			/// Just a fake Loan ID
			/// </summary>
			public int LoanID { get; set; }

			/// <summary>
			/// Just a fake Loan number
			/// </summary>
			public string LoanNumber { get; set; }

			/// <summary>
			/// Just a fake loan Amount
			/// </summary>
			public int Amount { get; set; }
		}


		private readonly IAmazonSQS _amazon;
		private readonly SQSManager _subscriber;

		private readonly LoanMessage _message;
		private ILogger _logger;

		//public const string QueueName = "TestQueue";
		public const string TestQueueURL = "TestQueue_LoanMessage";

		//public static IStorageManager S3 { get; set; }

		//private static NLog.Logger _logger = NLog.LogManager.GetLogger("AWS.SQS");


		//private const string _bucketName = "estestsqs";
		//private const string _Prefix = "_Tests_Received";
		private const string QueueName = "TestQueue";

		public QueueManagerTest()
		{
			_amazon = Substitute.For<IAmazonSQS>();
			_logger = Substitute.For<ILogger>();
			_subscriber = new SQSManager(_amazon);

			_message = new LoanMessage
			{
				LoanID = 1,
				LoanNumber = "067",
				Amount = 1000
			};
			//_subscriber.GetInternalQueueName(QueueName, typeof(LoanMessage)).r
			_amazon.CreateQueueAsync(QueueName)
				.ReturnsForAnyArgs(info =>
					new CreateQueueResponse {QueueUrl = "TestQueue_LoanMessage", HttpStatusCode = HttpStatusCode.OK});

			_amazon.SendMessageAsync(new SendMessageRequest(TestQueueURL, JsonConvert.SerializeObject(_message)))
				.Returns(new SendMessageResponse() {HttpStatusCode = HttpStatusCode.OK});

			_amazon.SendMessageAsync(Arg.Any<SendMessageRequest>())
				.Returns(new SendMessageResponse() {HttpStatusCode = HttpStatusCode.OK});
			_amazon.GetQueueUrlAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(new GetQueueUrlResponse() {HttpStatusCode = HttpStatusCode.OK, QueueUrl = TestQueueURL});

			_amazon.ReceiveMessageAsync(TestQueueURL)
				.Returns(
					new ReceiveMessageResponse
					{
						HttpStatusCode = HttpStatusCode.OK,
						Messages = new AutoConstructedList<Message>
						{
							new Message
							{
								Body = JsonConvert.SerializeObject(_message),
								ReceiptHandle = "Test_ReceiptHandle"
							}
						}
					});

			ReceiveMessageRequest request = new ReceiveMessageRequest(TestQueueURL);
			_amazon.ReceiveMessageAsync(request).ReturnsForAnyArgs(
				new ReceiveMessageResponse
				{
					HttpStatusCode = HttpStatusCode.OK,
					Messages = new AutoConstructedList<Message>
					{
						new Message
						{
							Body = JsonConvert.SerializeObject(_message),
							ReceiptHandle = "Test_ReceiptHandle"
						}
					}
				});


			_amazon.DeleteMessageAsync(Arg.Any<string>(), Arg.Any<string>()).ReturnsForAnyArgs(
				new DeleteMessageResponse
				{
					HttpStatusCode = HttpStatusCode.OK,
				});

			_amazon.DeleteMessageAsync(Arg.Any<DeleteMessageRequest>()).ReturnsForAnyArgs(
				new DeleteMessageResponse
				{
					HttpStatusCode = HttpStatusCode.OK,
				});
		}

		/// <summary>
		/// Test of GetQueue method. Actually it creates one if no queue with specified name found.
		/// </summary>
		[Fact, Trait("Category", "Unit")]
		public void GetQueueTest()
		{
			var response = new CreateQueueResponse {QueueUrl = "TestQueue_LoanMessage", HttpStatusCode = HttpStatusCode.OK};

			_amazon.CreateQueueAsync(QueueName).ReturnsForAnyArgs(response);
			_logger = Substitute.For<ILogger>();
			var subscriber = new SQSManager(_amazon);


			string testQueueURL = subscriber.GetQueueUrl<LoanMessage>(QueueName);

			Assert.Equal(testQueueURL, TestQueueURL);
		}

		/// <summary>
		/// Tests SQS receive. Pass if aws sqs method was called
		/// </summary>
		[Fact, Trait("Category", "Unit")]
		public void MessageReceiveTest()
		{
			_subscriber.ReceiveMessage<LoanMessage>(QueueName, Handler, default(CancellationToken));
			_amazon.ReceivedWithAnyArgs().ReceiveMessageAsync(new ReceiveMessageRequest());
		}

		/// <summary>
		/// Tests SQS receive. Pass if aws sqs method was called
		/// </summary>
		[Fact, Trait("Category", "Unit")]
		public void ReceiveMessage_WithStoreManagerTest()
		{
			var amazonSQS = Substitute.For<IAmazonSQS>();
			var storeManager = Substitute.For<IStorageManager>();
			_logger = Substitute.For<ILogger>();

			storeManager.StorageReferenceType.Returns(typeof(S3StorageReference));
			storeManager.When(x => x.DeleteData(Arg.Any<S3StorageReference>())).DoNotCallBase();

			SQSManager subscriber = new SQSManager(amazonSQS, storeManager);

			var content = new LoanMessage
			{
				LoanID = 1,
				LoanNumber = "067",
				Amount = 1000
			};

			var serializedContent = JsonConvert.SerializeObject(content);
			var expectedRef = new S3StorageReference("ValidKey");

			//storeManager.PutData(serializedContent).Returns(info => expectedRef);
			storeManager.GetData(Arg.Any<S3StorageReference>()).ReturnsForAnyArgs(info => serializedContent);

			var message = new Message
			{
				Body = JsonConvert.SerializeObject(expectedRef),
				ReceiptHandle = "Test_ReceiptHandle"
			};
			message.MessageAttributes.Add("SQSLargePayloadSize",
				new MessageAttributeValue {DataType = "Number", StringValue = "123456"});

			amazonSQS.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>()).ReturnsForAnyArgs(
				info =>
					new ReceiveMessageResponse
					{
						HttpStatusCode = HttpStatusCode.OK,
						Messages = new AutoConstructedList<Message>
						{
							message
						}
					});


			amazonSQS.CreateQueueAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(info => new CreateQueueResponse {QueueUrl = TestQueueURL, HttpStatusCode = HttpStatusCode.OK});

			amazonSQS.DeleteMessageAsync(Arg.Any<DeleteMessageRequest>()).ReturnsForAnyArgs(
				info =>
					new DeleteMessageResponse
					{
						HttpStatusCode = HttpStatusCode.OK,
					}
			);


			subscriber.ReceiveMessage<LoanMessage>(QueueName, Handler, default(CancellationToken));

			storeManager.Received().GetData(Arg.Is<S3StorageReference>(x => x.GetStorageKey() == expectedRef.GetStorageKey()));
		}


		[Fact, Trait("Category", "Unit")]
		public async Task MessageSendTest()
		{
			/*var message = new LoanMessage
			{
				LoanID = 1,
				LoanNumber = "067",
				Amount = 1000
			};*/

			await _subscriber.SendMessageAsync(QueueName, _message);
			var expectedMessageBody = JsonConvert.SerializeObject(_message);
			await _amazon.Received().SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.MessageBody == expectedMessageBody));
		}

		/// <summary>
		/// Tests message sending using S3
		/// </summary>
		[Fact, Trait("Category", "Unit")]
		public async Task SendMessageAsync_WithStoreManagerTest()
		{
			var amazonSQS = Substitute.For<IAmazonSQS>();
			var storeManager = Substitute.For<IStorageManager>();
			SQSManager subscriber = Substitute.ForPartsOf<SQSManager>(amazonSQS, storeManager);

			var document = new byte[700000];
			for (int j = 0; j < document.Length; j++)
			{
				document[j] = 0x20;
			}

			var content = new LoanMessage
			{
				LoanID = 1,
				LoanNumber = System.Text.Encoding.Default.GetString(document),
				Amount = 1000
			};

			var serializedContent = JsonConvert.SerializeObject(content);
			var expectedRef = new S3StorageReference("ValidKey");

			storeManager.PutDataAsync(Arg.Any<string>()).ReturnsForAnyArgs(info => expectedRef);

			amazonSQS.CreateQueueAsync(Arg.Any<string>())
				.ReturnsForAnyArgs(info => new CreateQueueResponse {QueueUrl = TestQueueURL, HttpStatusCode = HttpStatusCode.OK});


			amazonSQS.SendMessageAsync(Arg.Any<SendMessageRequest>())
				.ReturnsForAnyArgs(new SendMessageResponse() {HttpStatusCode = HttpStatusCode.OK});


			await subscriber.SendMessageAsync(QueueName, content);

			//if storemanager was called, pass
			await storeManager.ReceivedWithAnyArgs().PutDataAsync(serializedContent);
			await amazonSQS.Received()
				.SendMessageAsync(Arg.Is<SendMessageRequest>(x => x.MessageBody == JsonConvert.SerializeObject(expectedRef)));
		}


		/// <summary>
		/// A test handler. If message received, checks the object. If not null, it's ok
		/// </summary>
		/// <param name="loanMessage">a test business object</param>
		/// <returns>true if object is not null. Otherwise, fail</returns>
		internal bool Handler(LoanMessage loanMessage, string key, CancellationToken cancellationToken)
		{
			Assert.NotNull(loanMessage);
			return true;
		}

		/*private static bool LoanHandler(Loan loan, string key, CancellationToken cancellationToken)
		{
			try
			{
				Debug.Print("\nLoanID={0}, LoanNumber={1}, Owner={2}", loan.ID, loan.LoanNumber, loan.Owner);
				Debug.Print("\nWaiting 10 seconds");
				//Thread.Sleep(10000);
				Debug.Print("\nWaiting complete");
				var container = GetResolver();
				var s3 = new S3StorageManager(_bucketName, _Prefix, container.Resolve<IAmazonS3>());

				var keyRef = s3.PutData(JsonConvert.SerializeObject(loan));
				//Debug.Print("\nStoreSerializedObject: [{0}]", keyRef.StorageKey);

				return true;
			}
			catch (AggregateException e)
			{
				Debug.Print("\nLoanHandler Failed: {0}", e.Message);
				Debug.Print(e.Message);
				return false;
			}
		}*/


		/*public static bool ComparePropertyValues(object actual, object expected)
		{
			PropertyInfo[] properties = expected.GetType().GetProperties();
			foreach (PropertyInfo property in properties)
			{
				object expectedValue = property.GetValue(expected, null);
				object actualValue = property.GetValue(actual, null);

				IList value = actualValue as IList;
				if (value != null)
				{
					if (value.Count != ((IList) expectedValue).Count)
						return false;

					for (int i = 0; i < value.Count; i++)
						if (!Equals(value[i], ((IList) expectedValue)[i]))
							return false;
				}

				else if (!Equals(expectedValue, actualValue))
				{
					if (property.DeclaringType != null)
					{
						return false;
					}
				}
			}

			return true;
		}*/
	}


	//public class StateObjectTests
	//{
	//	[Fact, Trait("Category", "Unit")]
	//	public void StateObject_ToStringTest()
	//	{
	//		var obj = new StateObject {Attempts = 1, DueTime = 1000, TimerCanceled = true};

	//		var expected =
	//			$"Attempts: {obj.Attempts}, DueTime: {obj.DueTime}, TimerCanceled: {obj.TimerCanceled}, IsCancellationRequested: {(obj.TokenSource != null ? obj.TokenSource.IsCancellationRequested : false)}";

	//		Assert.Equal(expected, obj.ToString());
	//	}
	//}
}