using System;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Represents the method that will handle the workflow events.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="WorkflowEventArgs"/> that contains the event data.</param>
	public delegate void WorkflowEventHandler(ISchedulable sender, WorkflowEventArgs e);

	/// <summary>
	/// Represents the method that will handle the actvity events.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="ActivityEventArgs"/> that contains the event data.</param>
	public delegate void ActivityEventHandler(ISchedulable sender, ActivityEventArgs e);


	/// <summary>
	/// Provides data for various workflow events.
	/// </summary>
	public class WorkflowEventArgs : EventArgs
	{
		private readonly object _data;
		private readonly string _details;
		private readonly string _reason;

		/// <summary>
		/// Gets a data associated with event.
		/// </summary>
		public object Data
		{
			get { return _data; }
		}

		/// <summary>
		/// The details of the failure (if any).
		/// </summary>
		public string Details
		{
			get { return _details; }
		}

		/// <summary>
		/// The reason provided for the failure (if any).
		/// </summary>
		public string Reason
		{
			get { return _reason; }
		}


		/// <summary>
		/// Initializes a new instance of the WorkflowEventArgs class.
		/// </summary>
		/// <param name="data">Data associated with event.</param>
		public WorkflowEventArgs(object data) { _data = data; }

		/// <summary>
		/// Initializes a new instance of the WorkflowEventArgs class.
		/// </summary>
		/// <param name="details">The details of the failure.</param>
		/// <param name="reason">The reason provided for the failure</param>
		public WorkflowEventArgs(string details, string reason)
		{
			_details = details;
			_reason = reason;
		}
	}

	/// <summary>
	/// Provides data for various workflow events.
	/// </summary>
	public class ActivityEventArgs : EventArgs
	{
		private readonly string _details;
		private readonly string _reason;
		//private bool _continue;

		/// <summary>
		/// The details of the failure (if any).
		/// </summary>
		public string Details
		{
			get { return _details; }
		}

		/// <summary>
		/// The reason provided for the failure (if any).
		/// </summary>
		public string Reason
		{
			get { return _reason; }
		}

		/// <summary>
		/// Initializes a new instance of the WorkflowEventArgs class.
		/// </summary>
		/// <param name="details">The details of the failure.</param>
		/// <param name="reason">The reason provided for the failure</param>
		public ActivityEventArgs(string details, string reason)
		{
			_details = details;
			_reason = reason;
		}
	}

}
