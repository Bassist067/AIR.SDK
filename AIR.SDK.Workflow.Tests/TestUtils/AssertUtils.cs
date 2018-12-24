using System;
using System.Collections;
using System.Reflection;
using Xunit;

namespace AIR.SDK.Workflow.Tests.TestUtils
{
	/// <summary>
	/// A test utility for checking properties of two objects 
	/// </summary>
	public static class AssertObjectEquals
	{
	    /// <summary>
	    /// Checks two objects equality by checking each property values. Of course, using Reflection 
	    /// </summary>
	    /// <param name="actual"></param>
	    /// <param name="expected"></param>
		public static void PropertyValuesAreEqual(object actual, object expected)
		{
			PropertyInfo[] properties = expected.GetType().GetProperties();
			foreach (PropertyInfo property in properties)
			{
				object expectedValue = property.GetValue(expected, null);
				object actualValue = property.GetValue(actual, null);

				IList value = actualValue as IList;
				if (value != null)
				{
					AssertListsAreEquals(property, value, (IList) expectedValue);
					
				}
				else if (!Equals(expectedValue, actualValue))
				{
					if (property.DeclaringType != null)
					{
						Assert.True(false, //this means Assert.Fail in other frameworks
							$"Property {property.DeclaringType.Name}.{property.Name} does not match. Expected: {expectedValue} but was: {actualValue}");
					}
				}
			}
		}

		private static void AssertListsAreEquals(PropertyInfo property, IList actualList, IList expectedList)
		{
			if (actualList.Count != expectedList.Count)
				Assert.True(false, //this means Assert.Fail in other frameworks
					$"Property {property.PropertyType.Name}.{property.Name} does not match. Expected IList containing {expectedList.Count} elements but was IList containing {actualList.Count} elements");

			for (int i = 0; i < actualList.Count; i++)
				if (!Equals(actualList[i], expectedList[i]))
					Assert.True(false, //this means Assert.Fail in other frameworks
						String.Format(
							"Property {0}.{1} does not match. Expected IList with element {1} equals to {2} but was IList with element {1} equals to {3}",
							property.PropertyType.Name, property.Name, expectedList[i], actualList[i]));
		}
	}
}