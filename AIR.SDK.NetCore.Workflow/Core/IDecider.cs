using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Context;

namespace AIR.SDK.Workflow.Core
{
	/// <summary>
	/// IDecider interface provides Workflow event. handler. All the available event.s are listed below
	/// </summary>
	public interface IDecider
	{
		/// <summary>
		/// Handles Workflow started event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnWorkflowExecutionStarted(WorkflowDecisionContext context);

		/// <summary>
		/// Handles  Workflow Execution Continued As New event. (erase all the state)
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnWorkflowExecutionContinuedAsNew(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Workflow execution canceled event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnWorkflowExecutionCancelRequested(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Activity task completed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnActivityTaskCompleted(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Activity task failed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnActivityTaskFailed(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Activity task timed out
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnActivityTaskTimedOut(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Activity scheduled failed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnScheduleActivityTaskFailed(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Child Workflow started event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnChildWorkflowExecutionStarted(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Child Workflow completed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnChildWorkflowExecutionCompleted(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Child Workflow failed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnChildWorkflowExecutionFailed(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Child Workflow terminated event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnChildWorkflowExecutionTerminated(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Workflow timed out event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnChildWorkflowExecutionTimedOut(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Child Workflow failed event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnStartChildWorkflowExecutionFailed(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Timer started event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnTimerStarted(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Timer fired event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnTimerFired(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Timer canceled event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnTimerCanceled(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Start Child dWorkflow Execution Initiated event.
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnStartChildWorkflowExecutionInitiated(WorkflowDecisionContext context);

		/// <summary>
		/// Handles Workflow signaled event. Currently not implemented. Supports external signals
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnWorkflowExecutionSignaled(WorkflowDecisionContext context);

		/// <summary>
		/// Handles main workflow completed event
		/// </summary>
		/// <param name="context"><see cref="WorkflowDecisionContext"/></param>
		/// <returns><see cref="RespondDecisionTaskCompletedRequest"/></returns>
		RespondDecisionTaskCompletedRequest OnWorkflowExecutionCompleted(WorkflowDecisionContext context);
	}
}