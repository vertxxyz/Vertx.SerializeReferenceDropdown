using System;
using UnityEngine;

namespace Vertx.Attributes
{
	/// <summary>
	/// Decorates a [<see cref="SerializeReference"/>] field, providing instances of a type that can easily be added via a dropdown.  
	/// </summary>
	public sealed class ReferenceDropdownAttribute : PropertyAttribute
	{
		public readonly Type Type;
		public readonly ReferenceDropdownFeatures Features;

		public ReferenceDropdownAttribute(ReferenceDropdownFeatures features = ReferenceDropdownFeatures.Default, int order = 100) : this(null, features, order) { }

		public ReferenceDropdownAttribute(Type type, ReferenceDropdownFeatures features = ReferenceDropdownFeatures.Default, int order = 100)
		{
			Type = type;
			Features = features;
			this.order = order;
		}
	}
}