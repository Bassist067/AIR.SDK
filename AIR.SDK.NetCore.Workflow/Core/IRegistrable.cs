using Amazon.SimpleWorkflow;

namespace AIR.SDK.Workflow.Core
{
    /// <summary>
    /// This interface represents any workflow step should be registered in Amazon SWF
    /// </summary>
	public interface IRegistrable: ISchedulable
    {
	    /// <summary>
	    /// Register in amazon method
	    /// </summary>
	    /// <param name="domainName">a name of the domain to registered</param>
	    /// <param name="client">AWS SWF interface</param>
	    void Register(string domainName, IAmazonSimpleWorkflow client);

		void Validate();
    }
}
