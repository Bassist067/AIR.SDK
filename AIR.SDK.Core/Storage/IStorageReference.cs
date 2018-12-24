namespace AIR.API.Core.Storage
{
	/// <summary>
	/// This interface represents a generic reference to any store service used
	/// </summary>
	public interface IStorageReference
	{
		/// <summary>
		/// Use this method to encapsulate store-specific logic for generating object reference
		/// </summary>
		/// <returns>stringified object key</returns>
		string GetStorageKey();
	}
}
