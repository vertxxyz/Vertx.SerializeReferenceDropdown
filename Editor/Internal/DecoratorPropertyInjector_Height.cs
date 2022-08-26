using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Attributes.Editor
{
	internal static partial class DecoratorPropertyInjector
	{
		private static FieldInfo m_DecoratorDrawers;
		private static FieldInfo DecoratorDrawers =>
			m_DecoratorDrawers ??= m_DecoratorDrawers = PropertyHandlerType.GetField("m_DecoratorDrawers", BindingFlags.NonPublic | BindingFlags.Instance);
		
		private static PropertyInfo propertyDrawer;
		private static PropertyInfo PropertyDrawer =>
			propertyDrawer ??= propertyDrawer = PropertyHandlerType.GetProperty("propertyDrawer", BindingFlags.NonPublic | BindingFlags.Instance);

		private static readonly object[] getHeightParams = new object[3];

		internal static float GetPropertyHeightRaw()
		{
			MethodInfo getHeightMethod = GetHeightMethod(PropertyHandlerType);
			getHeightParams[0] = Current;
			getHeightParams[1] = GUIContent.none;
			getHeightParams[2] = true;

			return (float) getHeightMethod.Invoke(Handler, getHeightParams);
		}
	}
}