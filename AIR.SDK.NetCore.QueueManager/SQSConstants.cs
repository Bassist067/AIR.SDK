using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AIR.SDK.QueueManager.Tests")]

namespace AIR.SDK.NetStandard.QueueManager
{
    internal static class SQSConstants
    {
	   public const string RESERVED_ATTRIBUTE_NAME = "SQSLargePayloadSize";

	   public const int MAX_AllOWED_ATTRIBUTES = 9;

	   public const int DEFAULT_MESSAGE_SIZE_THRESHOLD = 262144;

	   //public const string S3_BUCKET_NAME_MARKER = "-..s3BucketName..-";

	   public const string S3_KEY_MARKER = "-..s3Key..-";

	   public const string MESSAGE_TYPE_NAME = "MESSAGE_TYPE_NAME";

	   internal static bool IsValidAttribute(string customAttributeName)
	   {
		  return
			  !RESERVED_ATTRIBUTE_NAME.Equals(customAttributeName,
				  StringComparison.OrdinalIgnoreCase)
			  && !S3_KEY_MARKER.Equals(customAttributeName, StringComparison.OrdinalIgnoreCase)
			  && !MESSAGE_TYPE_NAME.Equals(customAttributeName, StringComparison.OrdinalIgnoreCase);
	   }
    }
}