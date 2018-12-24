using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.API.Core.Storage;

namespace AIR.SDK.Workflow
{
	public class WorkflowWorker
	{
		private readonly IAmazonSimpleWorkflow _swfClient;
		private Task _task;
		private CancellationToken _cancellationToken;
		private readonly WorkflowBase _workflow;
		private readonly IStorageManager _storageClient;


		public WorkflowWorker(WorkflowBase workflow, IAmazonSimpleWorkflow swfClient, IStorageManager storageManager) : this(workflow , swfClient)
		{
			_storageClient = storageManager;
		}
		public WorkflowWorker(WorkflowBase workflow, IAmazonSimpleWorkflow swfClient)
		{
			_swfClient = swfClient;
			_workflow = workflow;
		}

		public Task Start(CancellationToken cancellationToken = default(CancellationToken))
		{
			Logger.Info("");

			_cancellationToken = cancellationToken;
			_task = Task.Run((Action)PollAndDecide, _cancellationToken);
			return _task;
		}

		/// <summary>
		/// Polls for descision tasks and decides what decisions to make.
		/// </summary>
		internal void PollAndDecide()
		{
			Debug.Assert(_workflow != null);

			while (!_cancellationToken.IsCancellationRequested)
			{
				var taskTolken = "";
				try
				{
					var decisionTaskRequest = new PollForDecisionTaskRequest
					{
						Domain = _workflow.Options.Domain,
						//Identity = _workflow.WorkflowId,
						TaskList = _workflow.TaskList // This could be a specific TaskList instead of default value from context.
					};

					Logger.Debug("TaskList: {0}", _workflow.TaskList.Name);

					PollForDecisionTaskResponse decisionTaskResponse = _swfClient.PollForDecisionTaskAsync(decisionTaskRequest).Result;
					DecisionTask decisionTask = decisionTaskResponse.DecisionTask;

					taskTolken = decisionTask.TaskToken;

					if (!string.IsNullOrEmpty(decisionTask.TaskToken))
					{
						Logger.Debug("Get Decision.");

						// Define a new WorkflowEventsProcessor object and let it make the decision!
						var workflowProcessor = new WorkflowEventsProcessor(decisionTask, _workflow, decisionTaskRequest, _swfClient, _storageClient);
						var decisionRequest = workflowProcessor.Decide();

						//var decisionRequest = _workflow.Decide(decisionTask, decisionTaskRequest);

						Logger.Debug("RespondDecisionTaskCompleted.");

						// We have our decision, send it away and do something more productive with the response
						_swfClient.RespondDecisionTaskCompletedAsync(decisionRequest).Wait();
					}

					//Sleep to avoid aggressive polling
					Thread.Sleep(200);

				}
				/*catch (AmazonSimpleWorkflowException ex)
				{
					Logger.Error(ex, "");

					//if (_workflow != null)
					//	_workflow.StopWorkers();

					//if (!string.IsNullOrEmpty(taskTolken))
					//{
					//	var respond = _workflow.FailWorkflowRespond(ex.Message, "");
					//	respond.TaskToken = taskTolken;
					//	try
					//	{
					//		// Just try to stop workflow.
					//		_swfClient.RespondDecisionTaskCompleted(respond);
					//	}
					//	catch
					//	{
					//	}
					//}
					//Console.WriteLine("Caught Exception: " + ex.Message);
					//Console.WriteLine("Response Status Code: " + ex.StatusCode);
					//Console.WriteLine("Error Code: " + ex.ErrorCode);
					//Console.WriteLine("Error Type: " + ex.ErrorType);
					//Console.WriteLine("Request ID: " + ex.RequestId);
					//Console.WriteLine("Data: " + ex.Data);
					//Console.WriteLine("Stacktrace: " + ex.StackTrace);
				}*/
				catch (Exception e)
				{
					Logger.Error(e, "");

					//if (_workflow != null)
					//	_workflow.StopWorkers();

					//if (!string.IsNullOrEmpty(taskTolken))
					//{
					//	var respond = _workflow.FailWorkflowRespond(e.Message, "");
					//	respond.TaskToken = taskTolken;
					//	try
					//	{
					//		// Just try to stop workflow.
					//		_swfClient.RespondDecisionTaskCompleted(respond);
					//	}
					//	catch 
					//	{
					//	}
					//}
				}
			}

			Logger.Info("Exit from Workflow Worker.");
		}

	}
}
