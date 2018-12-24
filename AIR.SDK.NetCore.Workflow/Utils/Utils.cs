using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AIR.API.Core.Storage;
using AIR.SDK.StorageManager;
using AIR.SDK.Workflow.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AIR.SDK.Workflow
{
	public static class Utils
	{
		internal const string PathDelimiter = "__";
		internal const string TemplateIDs = "{0}" + PathDelimiter + "{1}._{2}._{3}._{4}";

		public static T DeserializeFromJSON<T>(string json)
		{
			if (string.IsNullOrEmpty(json))
				return default(T);

			if (typeof(T) == typeof(string))
				return (T)Convert.ChangeType(json, typeof(T));

			var settings = new JsonSerializerSettings
			{
				Error = HandleDeserializationError,
				Converters = new List<JsonConverter>() { new ConcurrentDictionaryConverter()}
			};
			return JsonConvert.DeserializeObject<T>(json, settings);
			
			//JavaScriptSerializer serializer = new JavaScriptSerializer();
			//T input = serializer.Deserialize<T>(json);
			//return input;
		}

		internal static void HandleDeserializationError(object sender, ErrorEventArgs errorArgs)
		{
			var currentError = errorArgs.ErrorContext.Error.Message;
			errorArgs.ErrorContext.Handled = true;
		}

		public static string SerializeToJSON<T>(T obj)
		{
			if (obj == null)
				return null;

			if (obj.GetType() == typeof(string))
				return obj.ToString();

			return JsonConvert.SerializeObject(obj);

			//JavaScriptSerializer serializer = new JavaScriptSerializer();
			//StringBuilder builder = new StringBuilder();
			//serializer.Serialize(obj, builder);
			//return builder.ToString();
		}

		internal static void ValidateType(Type t)
		{
			if (//!t.GetTypeInfo().IsSerializable 
				//&& !(typeof(ISerializable).IsAssignableFrom(t)) && 
				!IsValidGeneric(t))
				throw new InvalidOperationException("A serializable type " + t.Name + " is required.");
		}

		internal static bool IsValidGeneric(Type t)
		{
			bool result = false;

			if (t.GetTypeInfo().IsGenericType)
			{
				result = t.GenericTypeArguments.Any();
				result = t.GenericTypeArguments
				    .Aggregate(result, (current, gt) => current && (gt.GetTypeInfo().IsSerializable /*|| typeof (ISerializable).IsAssignableFrom(gt)*/));
			}

			return result;
		}

		/// <summary>
		/// Generates version of activity.
		/// </summary>
		/// <param name="stepNum">Step number of activity.</param>
		/// <param name="activity">Activity instance.</param>
		internal static string CreateActivityVersion(int stepNum, string name,  string version, string activityVersion)
		{
			return !string.IsNullOrEmpty(activityVersion)
				? $"{name}.{version}.{stepNum}.{activityVersion}"
				: $"{name}.{version}.{stepNum}";
		}

		/// <summary>
		/// Generates unique compound ID for an instance <see cref="IActivity"/> based on the current <see cref="SchedulableState"/> data.
		/// </summary>
		/// <param name="name">Name of activity.</param>
		/// <param name="state">Data corresponding to the current step.</param>
		/// <returns>Unique Id matched to template WorkflowPath__{Name}._{StepNumber}._{ActionNumber}._{Attempt}</returns>
		/// <remarks>
		/// The specified string must not start or end with whitespace. 
		/// It must not contain a : (colon), / (slash), | (vertical bar), or any control characters (\u0000-\u001f | \u007f - \u009f).
		/// Also, it must not contain the literal string "arn".
		/// Length constraints: Minimum length of 1. Maximum length of 256.
		/// </remarks>
		internal static string CreateActionId(string name, string treePath, int stepNumber, int actionNumber, int attemptNumber)
		{
			return String.Format(TemplateIDs, treePath, name.TrimForID(), stepNumber, actionNumber, attemptNumber)
				.CutToLength(256);
		}

		/// <summary>
		/// Creates unique compound ID for an instance of <see cref="IWorkflow"/>.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		internal static string CreateWorkflowId(string name, string treePath, int stepNumber, int actionNumber, int attemptNumber)
		{
			return (CreateActionId(name, treePath, stepNumber, actionNumber, attemptNumber) + "-" + Guid.NewGuid().ToString().TrimForID()).CutToLength(256);
		}


		/// <summary>
		/// Returns serialized Object by reference key.
		/// If <paramref name="store"/> is not defined it returns <paramref name="referenceKey"/> as desired object.
		/// </summary>
		/// <param name="referenceKey">Reference to a serialized object in <see cref="IStorageManager"/>.</param>
		/// <param name="store">Reference to a store manager instance.</param>
		internal static string GetDataFromStore(string referenceKey, IStorageManager store)
		{
			return (!(store == null || string.IsNullOrEmpty(referenceKey)))
				? store.GetData(new S3StorageReference(referenceKey))
				: referenceKey;
		}
		/// <summary>
		/// Returns reference key to a serialized Object in <see cref="IStorageManager"/>.
		/// If <paramref name="store"/> is not defined it returns data <paramref name="input"/> as is.
		/// </summary>
		/// <param name="data">Serialized data to be stored.</param>
		/// <param name="store">Reference to a store manager instance.</param>
		internal static string PutDataToStore(string data, IStorageManager store)
		{
			return (!(store == null || string.IsNullOrEmpty(data)))
				? store.PutData(data).GetStorageKey()
				: data;
		}

		internal static void DeleteFromStore(string referenceKey, IStorageManager store)
		{
			if (!(store == null || string.IsNullOrEmpty(referenceKey)))
			{
				store.DeleteData(new S3StorageReference(referenceKey));
			}
		}
	}

	public class ConcurrentDictionaryConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ConcurrentDictionary<int, string>);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			// Load JObject from stream
			JObject jObject = JObject.Load(reader);

			var target = new ConcurrentDictionary<int, string>();

			foreach (var item in jObject)
			{
				var key = int.Parse(item.Key);
				target[key] = (string)((Newtonsoft.Json.Linq.JValue)(item.Value)).Value;
			}

			return target;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value);
		}
	}


	internal static class SchedulableStateSerializer
	{
		internal static string Serialize(SchedulableState input)
		{
			return Utils.SerializeToJSON<SchedulableState>(input);
		}

		internal static SchedulableState Deserialize(string input)
		{
			return Utils.DeserializeFromJSON<SchedulableState>(input);
		}
	}

	internal static class WorkflowStateSerializer
	{
		internal static string Serialize(WorkflowState input)
		{	
			return Utils.SerializeToJSON<WorkflowState>(input);
		}

		internal static WorkflowState Deserialize(string input)
		{
			return Utils.DeserializeFromJSON<WorkflowState>(input);
		}
	}

	/*internal static class MarkerSerializer
	{
		internal static string Serialize(Marker input)
		{
			return Utils.SerializeToJSON<Marker>(input);
		}

		internal static Marker Deserialize(string input)
		{
			return Utils.DeserializeFromJSON<Marker>(input);
		}
	}*/
}