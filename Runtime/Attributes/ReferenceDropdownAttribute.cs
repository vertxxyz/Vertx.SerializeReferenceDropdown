using System;
using UnityEngine;

namespace Vertx.Attributes
{
	[Flags]
	public enum ReferenceDropdownFeatures
	{
		NoFeatures = 0,
		/// <summary>
		/// Shows the constrained type in brackets (Type)
		/// </summary>
		ShowTypeConstraint = 1,
		/// <summary>
		/// Shows "Set to null" in the context menu.
		/// </summary>
		AllowSetToNull = 1 << 1,
		Default = ShowTypeConstraint | AllowSetToNull
	}

	/// <summary>
	/// Decorates a [<see cref="SerializeReference"/>] field, providing instances of a type that can easily be added via a dropdown.  
	/// </summary>
	public class ReferenceDropdownAttribute : PropertyAttribute
	{
		public readonly Type Type;
		public readonly ReferenceDropdownFeatures Features;

		public ReferenceDropdownAttribute() : this(null, ReferenceDropdownFeatures.Default) { }
		public ReferenceDropdownAttribute(ReferenceDropdownFeatures features = ReferenceDropdownFeatures.Default) : this(null, features) { }

		public ReferenceDropdownAttribute(Type type = null, ReferenceDropdownFeatures features = ReferenceDropdownFeatures.Default) :
			this(type, 100, features) { }

		public ReferenceDropdownAttribute(Type type = null, int order = 100, ReferenceDropdownFeatures features = ReferenceDropdownFeatures.Default)
		{
			Type = type;
			Features = features;
			this.order = order;
		}
	}
}