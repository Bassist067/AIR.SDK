using System.Collections.Generic;
using Amazon.SimpleWorkflow.Model;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// Used to properly build decision completed request.
	/// </summary>
	internal class StepDecision
	{
		internal List<Decision> Decisions { get; set; }
		internal WorkflowState WfState { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public StepDecision()
		{
			Decisions = new List<Decision>();
		}

		public StepDecision(List<Decision> decisions, WorkflowState wfState = null)
		{
			Decisions = decisions ?? new List<Decision>();
			WfState = wfState;
		}

		public StepDecision(Decision decision, WorkflowState wfState = null)
			: this()
		{
			if (decision != null)
				Decisions.Add(decision);
			WfState = wfState;
		}

		public StepDecision(WorkflowState wfState)
			: this()
		{
			WfState = wfState;
		}

		internal void Add(Decision decision)
		{
			if (decision != null)
				Decisions.Add(decision);
		}
	}
}
