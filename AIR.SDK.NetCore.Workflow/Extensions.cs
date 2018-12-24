using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AIR.SDK.Workflow
{
    /// <summary>
    /// Class of useful extensions. 
    /// </summary>
    public static class Extensions
    {
		/// <summary>
		/// Perform a deep Copy of the object, using Json as a serialisation method. NOTE: Private members are not cloned using this method.
		/// </summary>
		/// <typeparam name="T">The type of object being copied.</typeparam>
		/// <param name="source">The object instance to copy.</param>
		/// <returns>The copied object.</returns>
		public static T Clone<T>(this T source)
		{
			// Don't serialize a null object, simply return the default for that object
			if (Object.ReferenceEquals(source, null))
			{
				return default(T);
			}

			// initialize inner objects individually
			// for example in default constructor some list property initialized with some values,
			// but in 'source' these items are cleaned -
			// without ObjectCreationHandling.Replace default constructor values will be added to result
			var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
		}


		internal static string ToStringOrNone(this int value)
		{
			return (value >= 0) ? value.ToString(CultureInfo.InvariantCulture) : "NONE";
		}


		/// <summary>
		/// The specified string must not start or end with whitespace. 
		/// It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f). 
		/// Also, it must not contain the literal string "arn".
		/// </summary>
		public static string TrimForID(this string value)
		{
			return Regex.Replace(value, @"[\u0000-\u001F]|[\u007f-\u009f]|[\|\(\)/\._]|arn", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
		}

		/// <summary>
		/// Cuts off the string on the right and returns a new string of the given length.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="length">Length cannot be less than zero.</param>
		/// <returns></returns>
		internal static string CutToLength(this string value, int length)
		{
			if (length < 0)
				length = 0;

			return value.Length > length ? value.Substring(0, length) : value;
		}

    }
}
