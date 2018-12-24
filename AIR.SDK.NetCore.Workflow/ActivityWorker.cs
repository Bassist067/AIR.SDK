using System;
using System.Diagnostics;
//using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;
using AIR.API.Core.Storage;
using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	/// <summary>
	/// This work polls for activities in a workflow
	/// </summary>
	public class ActivityWorker
	{
		private readonly IAmazonSimpleWorkflow _swfClient;
		private readonly IWorkflow _workflow;
		private readonly IStorageManager _storeClient;
		Task _task;
		CancellationToken _cancellationToken;

		private string _Domain;
		private string _TaskList;

		public Func<string, string> TaskProcessor { get; set; }

		public ActivityWorker(IWorkflow workflow, IAmazonSimpleWorkflow swfClient, IStorageManager storeManager) : this(workflow ,swfClient)
		{
			_storeClient = storeManager;
		}
		public ActivityWorker(IWorkflow workflow, IAmazonSimpleWorkflow swfClient)
		{
			Debug.Assert(workflow != null);
			Debug.Assert(swfClient != null);

			_Domain = workflow.Options.Domain;
			_TaskList = workflow.TaskList.Name;

			_workflow = workflow;
			_swfClient = swfClient;
		}
		public ActivityWorker(string domain, string taskList, IAmazonSimpleWorkflow swfClient, IStorageManager storeManager): this(domain, taskList, swfClient)
		{
			_storeClient = storeManager;
		}
		public ActivityWorker(string domain, string taskList, IAmazonSimpleWorkflow swfClient)
		{
			Debug.Assert(swfClient != null);

			_Domain = domain;
			_TaskList = taskList;

			_swfClient = swfClient;
		}

		/// <summary>
		/// Kick off the worker to poll and process activities
		/// </summary>
		/// <param name="cancellationToken"></param>
		public Task Start(CancellationToken cancellationToken = default(CancellationToken))
		{
			Debug.Assert(!string.IsNullOrEmpty(_Domain));
			Debug.Assert(!string.IsNullOrEmpty(_TaskList));

			if (_workflow == null && TaskProcessor == null)
				throw new Exception("TaskProcessor is undefined.");

			Logger.Info("");

			_cancellationToken = cancellationToken;
			_task = Task.Run((Action)PollAndProcessTasks, _cancellationToken);
			return _task;
		}

		/// <summary>
		/// Main loop for the worker that polls for tasks and processes them.
		/// </summary>
		protected async void PollAndProcessTasks()
		{
			try
			{
				while (!_cancellationToken.IsCancellationRequested)
				{
					try
					{
						Logger.Info("Polling task list");

						await PollAsync().ContinueWith(task =>
						{
							if (task != null)
							{
								var activityTask = task.Result;
								if (!String.IsNullOrEmpty(activityTask.TaskToken))
								{
									Logger.Debug("[{0}] Input: [{1}]", activityTask.ActivityId, activityTask.Input);

									ProcessActivityTask(activityTask, _workflow);
								}
							}
						}, _cancellationToken);

						//Sleep to avoid aggressive polling
						Thread.Sleep(200);
					}
					catch (Exception ex)
					{
						if (!_cancellationToken.IsCancellationRequested)
							Logger.Error(ex.Message);
					}
				}
			}
			finally
			{
				Logger.Info("Exit from Activity Worker.");
			}
		}

		protected string ProcessActivityTask(ActivityTask task, IWorkflow workflow)
		{
			Debug.Assert(task != null);
			var result = "";
			try
			{
				var input = Utils.GetDataFromStore(task.Input, _storeClient);

				if (workflow != null)
				{
					// Get activity and run processor
					var activity = workflow.GetActivity(task.ActivityId);
					if (activity != null)
					{
						result = activity.TaskProcessor(input);

						Logger.Fatal("[{0}] Result: [{1}]", task.ActivityId, result);
					}
					else
						result = Utils.SerializeToJSON(new StepResult<string>(
							$"Activity '{task.ActivityId}' not found.", false));
				}
				else
				{
					result = TaskProcessor(input);

					Logger.Fatal("[{0}] Result: [{1}]", task.ActivityId, result);
				}

				TaskCompleted(task.TaskToken, Utils.PutDataToStore(result, _storeClient));
			}
			catch (Exception e)
			{
				Logger.Error(e, "[{0}]. {1}", task.ActivityId, e.Message);

				result = Utils.SerializeToJSON(new StepResult<string>(e.Message, false));

				//TaskFailed(task.TaskToken, e.Message, "");
			}

			return result;
		}

		/// <summary>
		/// Poll the image processing activity task list to see if work needs to be done.
		/// </summary>
		/// <returns></returns>
		private async Task<ActivityTask> PollAsync()
		{
			PollForActivityTaskRequest request = new PollForActivityTaskRequest
			{
				Domain = _Domain,
				TaskList = new TaskList() { Name = _TaskList }
			};

			Logger.Debug("TaskList: {0}", request.TaskList.Name);

			PollForActivityTaskResponse response = await _swfClient.PollForActivityTaskAsync(request, _cancellationToken);
			return response.ActivityTask;
		}

		/// <summary>
		/// Respond back to SWF that the activity task is complete
		/// </summary>
		/// <param name="taskToken"></param>
		/// <param name="activityState"></param>
		private void TaskCompleted(string taskToken, string result)
		{
			RespondActivityTaskCompletedRequest request = new RespondActivityTaskCompletedRequest
			{
				Result = result,
				TaskToken = taskToken
			};
			RespondActivityTaskCompletedResponse response = _swfClient.RespondActivityTaskCompletedAsync(request).Result;
		}

		/// <summary>
		/// Respond back to SWF that the activity task is failed
		/// </summary>
		/// <param name="taskToken"></param>
		/// <param name="activityState"></param>
		private void TaskFailed(string taskToken, string details, string reason)
		{
			RespondActivityTaskFailedRequest request = new RespondActivityTaskFailedRequest
			{
				Details = details,
				Reason = reason,
				TaskToken = taskToken
			};
			RespondActivityTaskFailedResponse response = _swfClient.RespondActivityTaskFailedAsync(request).Result;
		}


	}
}
