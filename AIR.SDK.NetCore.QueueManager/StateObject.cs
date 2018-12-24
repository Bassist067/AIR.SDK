using System.Runtime.CompilerServices;
using System.Threading;
using Amazon.SQS.Model;

[assembly: InternalsVisibleTo("AIR.SDK.QueueManager.Tests")]
namespace AIR.SDK.NetStandard.QueueManager
{
    internal class StateObject
    {
	   internal ChangeMessageVisibilityRequest Request;
	   internal int Attempts;
	   internal Timer TimerReference;
	   internal int DueTime; // in milliseconds
	   internal bool TimerCanceled;
	   internal CancellationTokenSource TokenSource;

	   internal void StopTimer()
	   {
		  TimerCanceled = true;
		  TimerReference?.Change(Timeout.Infinite, Timeout.Infinite);
	   }

	   internal void AbortTask()
	   {
		   TokenSource?.Cancel();
	   }

	   public override string ToString()
	   {
		  //return base.ToString();

		  return
			  $"Attempts: {Attempts}, DueTime: {DueTime}, TimerCanceled: {TimerCanceled}, IsCancellationRequested: {TokenSource?.IsCancellationRequested ?? false}";
	   }
    }
}
