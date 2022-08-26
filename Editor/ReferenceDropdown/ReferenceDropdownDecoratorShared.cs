using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using Vertx.Utilities;
using Vertx.Utilities.Editor;
using Object = UnityEngine.Object;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident
// ReSharper disable ConvertToUsingDeclaration

namespace Vertx.Attributes.Editor
{
	internal static class ReferenceDropdownDecoratorShared
	{
		private static readonly int managedRefStringLength = "managedReference<".Length;
		private static readonly Dictionary<string, (string fullTypeName, GUIContent defaultLabel)> fullTypeNameLookup = new Dictionary<string, (string, GUIContent)>();
		private static readonly Dictionary<int, GUIContent> typeLabelLookup = new Dictionary<int, GUIContent>();

		public static (GUIContent label, bool referenceIsAssigned) GetLabel(
			ReferenceDropdownAttribute attribute,
			SerializedProperty property
		)
		{
			GUIContent label;
			ReferenceDropdownFeatures features = attribute.Features;
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

			bool referenceIsAssigned;
			if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				referenceIsAssigned = false;
				label = group.defaultLabel;
			}
			else
			{
				referenceIsAssigned = true;
				GetTypeLabel(property, features, in group, out label);
			}

			return (label, referenceIsAssigned);
		}

		private static void GetTypeLabel(SerializedProperty property, ReferenceDropdownFeatures features, in (string fullTypeName, GUIContent defaultLabel) group, out GUIContent typeLabel)
		{
			int hashCode = property.type.GetHashCode() ^ group.fullTypeName.GetHashCode();
			if (typeLabelLookup.TryGetValue(hashCode, out typeLabel))
				return;

			typeLabelLookup.Add(
				hashCode,
				typeLabel = new GUIContent(
					// Assigned Type (Type Constraint)
					(features & ReferenceDropdownFeatures.ShowTypeConstraint) != 0
						? $"{ObjectNames.NicifyVariableName(property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1))} ({group.fullTypeName})"
						: ObjectNames.NicifyVariableName(property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1))
				)
			);
		}

		public static SerializedProperty GetSerializedProperty(PropertyField propertyField)
		{
			var property = (SerializedProperty)typeof(PropertyField).GetProperty("serializedProperty", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(propertyField);
			if (property.propertyPath == "")
				property = property.serializedObject.FindProperty(propertyField.bindingPath);
			return property;
		}


		public static void ShowPropertyDropdown(Rect position, PropertyField propertyField, Type specifiedType, Action onSelected = null)
			=> ShowPropertyDropdown(position, GetSerializedProperty(propertyField), specifiedType, onSelected);

		public static void ShowPropertyDropdown(Rect position, SerializedProperty property, Type specifiedType, Action onSelected = null)
		{
			AdvancedDropdown dropdown;
			Type type = GetRelevantType(property, specifiedType);

			if (type.IsSubclassOf(typeof(AdvancedDropdownAttribute)))
			{
				dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute(
					type,
					property.displayName,
					element =>
					{
						PerformMultipleIfRequiredAndApplyModifiedProperties(
							property,
							p => p.managedReferenceValue = Activator.CreateInstance(element.Type)
						);
						onSelected?.Invoke();
					}
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
						onSelected?.Invoke();
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

		public static void ShowContextMenu(SerializedProperty serializedProperty, bool referenceIsAssigned, ReferenceDropdownFeatures features)
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
						serializedProperty
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
					serializedProperty
				);
			}
			else
			{
				if ((features & ReferenceDropdownFeatures.AllowSetToNull) != 0)
					menu.AddDisabledItem(new GUIContent("Set to Null"), false);
				menu.AddDisabledItem(new GUIContent("Reset Values To Defaults"), false);
			}

			menu.ShowAsContext();
		}
	}
}