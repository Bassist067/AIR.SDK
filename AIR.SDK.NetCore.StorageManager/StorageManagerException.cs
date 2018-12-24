using System;
using System.Net;

namespace AIR.SDK.StorageManager
{
	public sealed class StorageManagerException : Exception
	{
		public HttpStatusCode StatusCode { get; set; }
		public string ErrorCode { get; set; }


		public StorageManagerException(string message) : base( message )
		{}

		public StorageManagerException(string message, Exception innerException) : base(message, innerException)
		{}

		public StorageManagerException(string message, Exception innerException, HttpStatusCode statusCode)
			: this(message, innerException)
		{
			StatusCode = statusCode;
		}

		public StorageManagerException(string message, Exception innerException, HttpStatusCode statusCode, string errorCode)
			: this(message, innerException)
		{
			StatusCode = statusCode;
			ErrorCode = errorCode;
		}

		public StorageManagerException(string message, HttpStatusCode statusCode)
			: this(message)
		{
			StatusCode = statusCode;
		}

		public StorageManagerException(string message, HttpStatusCode statusCode, string errorCode)
			: this(message)
		{
			StatusCode = statusCode;
			ErrorCode = errorCode;
		}

	}
}
