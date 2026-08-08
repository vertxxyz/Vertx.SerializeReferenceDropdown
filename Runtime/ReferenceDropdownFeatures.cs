using System;

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
}