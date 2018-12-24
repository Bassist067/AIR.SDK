using System;
using System.Net;

namespace AIR.SDK.NetStandard.QueueManager
{
	public sealed class SQSManagerException : Exception
	{
		public HttpStatusCode StatusCode { get; set; }
		public string ErrorCode { get; set; }


		public SQSManagerException(Exception innerException)
		{}

		public SQSManagerException(string message) : base( message )
		{}

		public SQSManagerException(string message, Exception innerException) : base(message, innerException)
		{}

		public SQSManagerException(string message, Exception innerException, HttpStatusCode statusCode)
			: this(message, innerException)
		{
			StatusCode = statusCode;
		}

		public SQSManagerException(string message, Exception innerException, HttpStatusCode statusCode, string errorCode)
			: this(message, innerException)
		{
			StatusCode = statusCode;
			ErrorCode = errorCode;
		}

		public SQSManagerException(string message, HttpStatusCode statusCode)
			: this(message)
		{
			StatusCode = statusCode;
		}

		public SQSManagerException(string message, HttpStatusCode statusCode, string errorCode)
			: this(message)
		{
			StatusCode = statusCode;
			ErrorCode = errorCode;
		}

	}
}
