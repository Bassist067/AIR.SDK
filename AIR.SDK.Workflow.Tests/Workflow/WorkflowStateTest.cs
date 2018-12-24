using System;
using System.Collections.Generic;
using Amazon.SimpleWorkflow.Model;
using NSubstitute;
using Xunit;

namespace AIR.SDK.Workflow.Tests.Workflow
{
	public class WorkflowStateTest
	{
		[Fact]
		public void TestWorkflowStateConstructorStepNum()
		{
			const int stepNum = 0;
			var workflowState = new WorkflowState(stepNum);

			Assert.Equal(workflowState.CurrentStepNumber, stepNum);
			Assert.NotNull(workflowState.Results);
		}

		[Fact]
		public void TestWorkflowStateConstructor()
		{
			var workflowState = new WorkflowState();
			Assert.NotNull(workflowState.Results);
		}


		[Fact]
		internal void SchedulableState_StartedDateTest()
		{
			var state = new SchedulableState();

			Assert.True(state.StartedDate == DateTime.MinValue);

			var expected = DateTime.Now;

			state.StartedDate = expected;

			Assert.True(state.StartedDate == expected.ToUniversalTime());

			state.StartedDate = expected.AddHours(1);

			Assert.True(state.StartedDate == expected.ToUniversalTime());
		}

		[Fact]
		internal void SchedulableState_EqualsTest()
		{
			var a = new SchedulableState() {StepNumber = 1, ActionNumber = 1};
			var b = new SchedulableState() {StepNumber = 2, ActionNumber = 1};

			Assert.False(a.Equals(null));

			Assert.True(a.Equals(a));

			Assert.False(a.Equals("data"));

			Assert.False(a.Equals(b));
			
			var c = a;

			Assert.True(a.Equals(c));

			var a1 = new SchedulableState() { StepNumber = 1, ActionNumber = 1 };

			Assert.True(a.Equals(a1));

			var d = new SchedulableState() { StepNumber = 1, ActionNumber = 2 };

			Assert.False(a.Equals(d));
		}

		[Fact]
		internal void SchedulableState_GetHashCodeTest()
		{
			var a = new SchedulableState() { StepNumber = 3, ActionNumber = 5 };

			var expected = 197888;

			Assert.True(a.GetHashCode() == expected);
		}

		[Fact]
		internal void StepDecision_CtorNullParams()
		{
			var st = new StepDecision((List<Decision>)null);

			Assert.NotNull(st.Decisions);
		}
				
		[Fact]
		internal void StepDecision_Ctor()
		{
			var decision = Substitute.ForPartsOf<Decision>();

			var list = new List<Decision>() {decision};

			var st = new StepDecision(list);

			Assert.True(st.Decisions.Count == list.Count);

			Assert.Equal(list[0], st.Decisions[0]);
		}

		[Fact]
		internal void StepDecision_AddTest()
		{
			var st = new StepDecision();

			st.Add(null);

			Assert.True(st.Decisions.Count == 0, "Null decision is not allowed.");

			var decision = Substitute.ForPartsOf<Decision>();

			st.Add(decision);

			Assert.True(st.Decisions.Count == 1);
		}



	}
}