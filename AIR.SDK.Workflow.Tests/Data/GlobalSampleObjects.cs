using System;

namespace AIR.SDK.Workflow.Tests.Data
{
	[Serializable]
	public class TInput
	{
		public string Value { get; set; }
	}

	[Serializable]
	public class TOutput
	{
		public string Value { get; set; }
	}

	[Serializable]
	public class TActivityInput
	{
		public string Value { get; set; }
	}

	[Serializable]
	public class TActivityOutput
	{
		public string Value { get; set; }
	}
}
