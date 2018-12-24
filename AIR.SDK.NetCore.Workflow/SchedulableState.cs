using System;
using AIR.SDK.Workflow.Core;
using Newtonsoft.Json;

namespace AIR.SDK.Workflow
{
	[JsonObject]
	internal class SchedulableState
	{
		DateTime _startedDate;

		[JsonProperty]
		internal int StepNumber { get; set; }
		[JsonProperty]
		internal string StepKey { get; set; }
		[JsonProperty]
		internal int AttemptNumber { get; set; }
		[JsonProperty]
		internal int MaxAttempts { get; set; }
		/// <summary>
		/// Zero-based index of the action within the <see cref="StepNumber"/>.
		/// </summary>
		[JsonProperty]
		internal int ActionNumber { get; set; }
		/// <summary>
		/// Total number of actions scheduled together within the <see cref="StepNumber"/>.
		/// </summary>
		[JsonProperty]
		internal int TotalActions { get; set; }
		[JsonProperty]
		internal int DelayTimeoutInSeconds { get; set; }

		/// <summary>
		/// Gets and sets UTC datetime value when corresponding step was scheduled.
		/// Used for <see cref="ISuspendable"/> activities.
		/// </summary>
		[JsonProperty]
		internal DateTime StartedDate
		{
			get { return _startedDate; }
			set
			{
				if (_startedDate == DateTime.MinValue)
					_startedDate = value.ToUniversalTime();
			}
		}

		public override bool Equals(object value)
		{
			if (ReferenceEquals(value, null))
				return false;

			if (ReferenceEquals(this, value))
				return true;

			if (GetType() != value.GetType())
				return false;

			SchedulableState state = value as SchedulableState;

			return ((StepNumber == state.StepNumber) && (ActionNumber == state.ActionNumber));
		}

		public override int GetHashCode()
		{
			return (StepNumber << 16) ^ (ActionNumber << 8);
			/*
			unchecked
			{
				// Magic number.
				int hash = 13;
				hash = (hash * 7) + StepNumber.GetHashCode();
				hash = (hash * 7) + ActionNumber.GetHashCode();

				return hash;
			}*/
		}

	}
}
