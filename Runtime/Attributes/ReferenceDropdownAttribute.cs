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
		/// <summary>
		/// Shows a warning icon when the reference is null.
		/// </summary>
		ShowWarningForNull = 1 << 2,
		Default = ShowTypeConstraint | AllowSetToNull | ShowWarningForNull
	}

	/// <summary>
	/// Decorates a [<see cref="SerializeReference"/>] field, providing instances of a type that can easily be added via a dropdown.  
	/// </summary>
	public class ReferenceDropdownAttribute : PropertyAttribute
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