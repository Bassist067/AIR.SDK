using AIR.SDK.Workflow.Core;

namespace AIR.SDK.Workflow
{
	public class ParallelCollectionItem<T> : ICollectionItemInput 
		where T : class
	{
		public int DelayTimeoutInSeconds { get; set; }

		public T Input
		{
			get { return Utils.DeserializeFromJSON<T>(_input); }
			set { _input = Utils.SerializeToJSON<T>(value); }
		}

		private string _input;
		string ICollectionItemInput.Input { get { return _input; } set { _input = value; } }
	}
}
