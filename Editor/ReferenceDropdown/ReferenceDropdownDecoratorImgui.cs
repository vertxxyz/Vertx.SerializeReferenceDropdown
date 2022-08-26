using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Vertx.Utilities;
using Vertx.Utilities.Editor;
using Object = UnityEngine.Object;
using static Vertx.Attributes.Editor.ReferenceDropdownDecoratorShared;

namespace Vertx.Attributes.Editor
{
	internal static class ReferenceDropdownDecoratorImgui
	{
		private static readonly int managedRefStringLength = "managedReference<".Length;
		private static Texture2D warnTexture;

		public static readonly float DecoratorHeight = EditorGUIUtils.HeightWithSpacing + EditorGUIUtility.standardVerticalSpacing;

		private static readonly Func<SerializedObject, int> GetInspectorMode =
			(Func<SerializedObject, int>)Delegate.CreateDelegate(
				typeof(Func<SerializedObject, int>),
				typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true)
			);

		public static float GetPropertyHeight(SerializedProperty property)
		{
			if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
				return 0;

			if (GetInspectorMode(property.serializedObject) != 0)
				return 0;

			ReferenceDropdownAttribute attribute = GetAttribute(property);
			if (attribute == null)
				return 0;

#if UNITY_2021_1_OR_NEWER
			// If you draw a property with a property field this may run twice. This will prevent multiple occurrences of the same property being drawn simultaneously.
			object handler = DecoratorPropertyInjector.Handler;
			if (DecoratorPropertyInjector.IsCurrentlyNested(handler))
				return 0;
#endif

			return DecoratorHeight;
		}

		private static readonly Dictionary<int, ReferenceDropdownAttribute> attributeLookup = new Dictionary<int, ReferenceDropdownAttribute>();

		private static ReferenceDropdownAttribute GetAttribute(SerializedProperty property)
		{
			int hash = property.serializedObject.targetObject.GetType().GetHashCode() ^ property.propertyPath.GetHashCode();
			if (attributeLookup.TryGetValue(hash, out ReferenceDropdownAttribute attribute))
				return attribute;
			FieldInfo fieldInfo = EditorUtils.GetFieldInfoFromProperty(property, out _);
			attribute = fieldInfo.GetCustomAttribute<ReferenceDropdownAttribute>();
			attributeLookup.Add(hash, attribute);
			return attribute;
		}

		/// <summary>
		/// Used to track whether properties are drawn in a nested fashion.
		/// This prevents the decorator header appearing twice in that case.
		/// </summary>
		public static void OnGUI(ref Rect totalPosition)
		{
			try
			{
				SerializedProperty property = DecoratorPropertyInjector.Current;

				if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
					return;

				if (GetInspectorMode(property.serializedObject) != 0)
					return;

				ReferenceDropdownAttribute attribute = GetAttribute(property);
				if (attribute == null)
					return;

#if UNITY_2021_1_OR_NEWER
				// If you draw a property with a property field this may run twice. This will prevent multiple occurrences of the same property being drawn simultaneously.
				object handler = DecoratorPropertyInjector.Handler;
				if (DecoratorPropertyInjector.IsCurrentlyNested(handler))
					return;
#endif
				ReferenceDropdownFeatures features = attribute.Features;

				//float totalPropertyHeight = totalPosition.height;
				totalPosition.height -= DecoratorHeight;
				Rect position = totalPosition;
				totalPosition.y += EditorGUIUtils.HeightWithSpacing + EditorGUIUtility.standardVerticalSpacing;
				position.height = DecoratorHeight;

				float border = EditorGUIUtility.standardVerticalSpacing;
				position.y += border;
				position.height -= border;
				Rect background = position;

				// Ideally we could use the total property height Unity has already calculated, but sadly this value does not function in all circumstances
				// We will need to recalculate the correct height to draw the background.
				float remainingHeight = DecoratorPropertyInjector.GetPropertyHeightRaw() - DecoratorHeight;
				float border2 = border * 2;
				background.x -= border;
				background.width += border2;
				background.y -= border;

				//Header
				EditorGUIUtils.DrawHeaderWithBackground(background, GUIContent.none);
				background.y += background.height;
				background.height = remainingHeight + border2;
				//Background
				EditorGUI.DrawRect(background, EditorGUIUtils.HeaderColor);

				position.y -= 1;
				//Header - Prefix Label
				Rect area = EditorGUI.PrefixLabel(position, EditorGUIUtility.TrTempContent(property.displayName), EditorStyles.boldLabel);

				(GUIContent label, bool referenceIsAssigned) = GetLabel(attribute, property);
				if (!referenceIsAssigned && (features & ReferenceDropdownFeatures.ShowWarningForNull) != 0)
				{
					// Draw warning icon if no reference is assigned.
					if (warnTexture == null)
						warnTexture = EditorGUIUtility.FindTexture("console.warnicon.inactive.sml");
					GUI.DrawTexture(new Rect(area.x - 17, area.y + 1, 16, 16), warnTexture);
				}

				Event e = Event.current;
				if (e.type == EventType.MouseDown && e.button == 1)
				{
					// Context Menu on the prefix label.
					Rect prefixLabelArea = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
					if (prefixLabelArea.Contains(e.mousePosition))
						ShowContextMenu(property, referenceIsAssigned, features);
				}

				// Header - Dropdown
				if (GUI.Button(area, label, EditorStyles.popup))
					ShowPropertyDropdown(position, property, attribute.Type);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}
}