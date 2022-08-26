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

namespace Vertx.Attributes.Editor
{
	internal static class ReferenceDropdownDecoratorImgui
	{
		private static readonly int managedRefStringLength = "managedReference<".Length;

		private static readonly Dictionary<string, (string fullTypeName, GUIContent defaultLabel)> fullTypeNameLookup = new Dictionary<string, (string, GUIContent)>();
		private static readonly Dictionary<int, GUIContent> typeLabelLookup = new Dictionary<int, GUIContent>();
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

				string propName = ObjectNames.NicifyVariableName(property.name);

				Type specifiedType = attribute.Type;
				string typeNameSimple = specifiedType?.Name ?? property.managedReferenceFieldTypename;

				if (!fullTypeNameLookup.TryGetValue(typeNameSimple, out (string fullTypeName, GUIContent defaultLabel) group))
				{
					// Populate the name of the type associated with the property
					string fullTypeName = GetRelevantType(property, specifiedType).Name;
					if (fullTypeName.EndsWith("Attribute"))
						fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 9);
					group = (
						fullTypeName, // fullTypeName
						(features & ReferenceDropdownFeatures.ShowTypeConstraint) != 0 ? new GUIContent($"Null ({fullTypeName})") : new GUIContent("Null") // defaultLabel
					);
					fullTypeNameLookup.Add(typeNameSimple, group);
				}

				GUIContent label;
				bool referenceIsAssigned;
				if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
				{
					// No reference has been assigned.
					referenceIsAssigned = false;
					label = group.defaultLabel;
				}
				else
				{
					referenceIsAssigned = true;

					int hashCode = property.type.GetHashCode() ^ group.fullTypeName.GetHashCode();
					if (!typeLabelLookup.TryGetValue(hashCode, out GUIContent typeLabel))
					{
						typeLabelLookup.Add(
							hashCode,
							typeLabel = new GUIContent(
								// Assigned Type (Type Constraint)
								(features & ReferenceDropdownFeatures.ShowTypeConstraint) != 0 ?
								$"{ObjectNames.NicifyVariableName(property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1))} ({group.fullTypeName})" :
								ObjectNames.NicifyVariableName(property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1))
							)
						);
					}

					label = typeLabel;
				}

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

				if (!referenceIsAssigned)
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
					{
						GenericMenu menu = new GenericMenu();
						if (referenceIsAssigned)
						{
							if ((features & ReferenceDropdownFeatures.AllowSetToNull) != 0)
							{
								menu.AddItem(new GUIContent("Set to Null"), false,
									property => PerformMultipleIfRequiredAndApplyModifiedProperties(
										(SerializedProperty)property,
										p => p.managedReferenceValue = null
									),
									property
								);
							}

							menu.AddItem(new GUIContent("Reset Values To Defaults"), false,
								property => PerformMultipleIfRequiredAndApplyModifiedProperties(
									(SerializedProperty)property,
									p =>
									{
										Type t = EditorUtils.GetObjectFromProperty(p, out _, out _).GetType();
										p.managedReferenceValue = Activator.CreateInstance(t);
									}
								),
								property
							);
						}
						else
						{
							if((features & ReferenceDropdownFeatures.AllowSetToNull) != 0)
								menu.AddDisabledItem(new GUIContent("Set to Null"), false);
							menu.AddDisabledItem(new GUIContent("Reset Values To Defaults"), false);
						}

						menu.ShowAsContext();
					}
				}

				// Header - Dropdown
				if (GUI.Button(area, label, EditorStyles.popup))
					ShowPropertyDropdown();

				void ShowPropertyDropdown()
				{
					AdvancedDropdown dropdown;
					Type type = GetRelevantType(property, specifiedType);

					if (type.IsSubclassOf(typeof(AdvancedDropdownAttribute)))
					{
						dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute(
							type,
							propName,
							OnSelected
						);
					}
					else
					{
						dropdown = new AdvancedDropdownOfSubtypes(new AdvancedDropdownState(), t =>
							{
								PerformMultipleIfRequiredAndApplyModifiedProperties(
									property,
									p => p.managedReferenceValue = Activator.CreateInstance(t)
								);
							},
							type,
							t => 
								!t.Assembly.IsDynamic && // Dynamic types are completely irrelevant.
								!t.IsInterface && // No idea how to serialize an interface alone.
								!t.IsPointer && // Again, no.
								!t.IsAbstract && // Cannot serialize an abstract instance.
								!t.IsArray && // Array types will not work.
								!t.IsSubclassOf(typeof(Object)) && // UnityEngine.Object types cannot be assigned.
								t.GetCustomAttribute<CompilerGeneratedAttribute>() == null // Compiler generated code is irrelevant.
							// There are likely more constraints that can be added here. I am unsure what the exact restrictions around generics are.
						);
					}

					dropdown.Show(position);

					void OnSelected(AdvancedDropdownElement element)
						=> PerformMultipleIfRequiredAndApplyModifiedProperties(
							property,
							p => p.managedReferenceValue = Activator.CreateInstance(element.Type)
						);
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private static void PerformMultipleIfRequiredAndApplyModifiedProperties(SerializedProperty property, Action<SerializedProperty> action)
		{
			if (!property.serializedObject.isEditingMultipleObjects)
			{
				action(property);
				property.serializedObject.ApplyModifiedProperties();
				return;
			}

			// For some reason this is required to support multi-object editing at times.
			foreach (Object target in property.serializedObject.targetObjects)
			{
				using (var localSerializedObject = new SerializedObject(target))
				{
					SerializedProperty localProperty = localSerializedObject.FindProperty(property.propertyPath);
					action(localProperty);
					localSerializedObject.ApplyModifiedProperties();
				}
			}
		}

		private static Type GetRelevantType(SerializedProperty property, Type type)
		{
			if (type != null)
				return type;
			EditorUtils.GetObjectFromProperty(property, out _, out FieldInfo fieldInfo);
			return EditorUtils.GetSerializedTypeFromFieldInfo(fieldInfo);
		}
	}
}