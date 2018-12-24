using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow.Tests.Data
{
	public class ActivityWorkerSimple : ActivityWorker
	{
		public string Expose_ProcessActivityTask(ActivityTask task, IWorkflow workflow)
		{
			return ProcessActivityTask(task, workflow);
		}

		public ActivityWorkerSimple(WorkflowBase workflow, IAmazonSimpleWorkflow swfClient)
			: base(workflow, swfClient)
		{

		}
	}
}
