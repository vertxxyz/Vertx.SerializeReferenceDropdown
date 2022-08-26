#if UNITY_2022_2_OR_NEWER
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Vertx.Attributes.Editor.ReferenceDropdownDecoratorShared;

namespace Vertx.Attributes.Editor
{
	/// <summary>
	/// This implementation uses a DecoratorDrawer to allow for people to still use PropertyDrawers.
	/// Decorator drawers don't decorate individual array elements, so this implementation uses the stateful nature of VisualElements to perform a 'hack',
	/// Registering callbacks to the list view and injecting mock decorator drawers into its PropertyDrawers.
	/// </summary>
	[CustomPropertyDrawer(typeof(ReferenceDropdownAttribute))]
	internal class ReferenceDropdownUIToolkit : DecoratorDrawer
	{
		// This is zero because IMGUI support is not handled via this decorator.
		public override float GetHeight() => 0;

		public override VisualElement CreatePropertyGUI() => new ReferenceDropdown((ReferenceDropdownAttribute)attribute);
	}

	internal class ReferenceDropdown : VisualElement
	{
		private readonly ReferenceDropdownAttribute _attribute;
		private const string StylePath = "Packages/com.vertx.serializereference-dropdown/Editor/Assets/SerializeDropdownStyle.uss";
		private const string UssDecoratorDrawerContainer = "unity-decorator-drawers-container";
		private const string UssClassName = "vertx-reference-dropdown";
		private const string ContainerUssClassName = UssClassName + "__container";
		private const string BackgroundUssClassName = UssClassName + "__background";
		private const string Name = nameof(ReferenceDropdown) + " Container";
		private const string ArrayDriverName = nameof(ReferenceDropdown) + " Array Driver";
		private const string ArrayElementDecoratorName = nameof(ReferenceDropdown) + " Array Element Decorator";
		private const string BackgroundName = nameof(ReferenceDropdown) + " Background";

		private VisualElement _backgroundElement;
		private PropertyField _parentPropertyField;
		private ElementType _type;

		private enum ElementType
		{
			Standard,
			ArrayDriver,
			ArrayElement
		}

		public ReferenceDropdown(ReferenceDropdownAttribute attribute)
		{
			_attribute = attribute;
			name = Name;
			AddToClassList(ContainerUssClassName);
			pickingMode = PickingMode.Ignore;
			RegisterCallback<AttachToPanelEvent>(AttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
		}

		private static StyleSheet _serializeDropdownStyle;

		private void AttachToPanel(AttachToPanelEvent evt)
		{
			if (_parentPropertyField != null)
				return;
			VisualElement root = evt.destinationPanel.visualTree;
			root = root.Children().SingleOrDefault(c => c.name.StartsWith("rootVisualContainer", StringComparison.Ordinal)) ?? root;
			_serializeDropdownStyle ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
			if (!root.styleSheets.Contains(_serializeDropdownStyle))
				root.styleSheets.Add(_serializeDropdownStyle);

			VisualElement parentQuery = this;
			do
			{
				parentQuery = parentQuery.parent;
			} while (parentQuery is not (PropertyField or null));

			if (parentQuery is not PropertyField propertyField)
				return;
			_parentPropertyField = propertyField;

			var serializedProperty = GetSerializedProperty(propertyField);
			if (serializedProperty == null)
			{
				Debug.LogWarning($"PropertyField not bound before {nameof(AttachToPanel)} was called.");
				return;
			}

			if (serializedProperty.isArray)
			{
				name = ArrayDriverName;
				_type = ElementType.ArrayDriver;
				style.display = DisplayStyle.None;

				RegisterArrayElementFields(propertyField);
				return;
			}

			// Dropdown button
			(GUIContent label, _) = GetLabel(_attribute, serializedProperty);
			DropdownButton dropdownButton = new DropdownButton(
				serializedProperty.displayName,
				label.text,
				(evt, button) =>
				{
					switch (evt.button)
					{
						case 0:
							ShowPropertyDropdown(
								button.worldBound,
								propertyField,
								_attribute.Type,
								() => UpdateDropdownVisual(GetSerializedProperty(propertyField), button, _attribute, true)
							);
							break;
						case 1:
							ShowContextMenu(GetSerializedProperty(propertyField), _attribute);
							UpdateDropdownVisual(GetSerializedProperty(propertyField), button, _attribute, true);
							break;
					}
				});
			UpdateDropdownVisual(serializedProperty, dropdownButton, _attribute);
			Add(dropdownButton);

			// Background fill
			if (_backgroundElement == null)
			{
				_backgroundElement = new VisualElement
				{
					name = BackgroundName,
					pickingMode = PickingMode.Ignore
				};
				_backgroundElement.AddToClassList(BackgroundUssClassName);
			}

			parentQuery.Insert(0, _backgroundElement);
		}

		private void DetachFromPanel(DetachFromPanelEvent evt)
		{
			if (_type == ElementType.ArrayElement && evt.destinationPanel == null && _parentPropertyField != null && _parentPropertyField.panel == evt.originPanel)
			{
				// Damn property field removes our decorator and obviously never re-adds it.
				// Rebind is also not called, so this nightmare continues.
				_parentPropertyField.schedule.Execute(() =>
				{
					// Already present, don't re-add.
					if (_parentPropertyField.Q<VisualElement>(null, UssDecoratorDrawerContainer) != null)
						return;
					// Reattach hack.
					_parentPropertyField.Insert(0, _backgroundElement);
					_parentPropertyField.Insert(1, parent);
				});
				return;
			}

			_backgroundElement?.RemoveFromHierarchy();
		}

		private void RegisterArrayElementFields(
			VisualElement root,
			int depth = 0
		)
		{
			var listView = root.Q<ListView>();
			if (listView == null)
			{
				if (depth == 0)
					EditorApplication.delayCall += () => RegisterArrayElementFields(root, depth + 1);
				else
					Debug.LogWarning($"{root} should have a child \"{nameof(ListView)}\", this would only happen if Unity changed the UI that makes up a collection or there is an unhandled UI initialisation sequence.");
				return;
			}

			VisualElement contentContainer = listView.Q<VisualElement>(null, ScrollView.contentUssClassName);
			if (contentContainer == null)
			{
				if (depth == 0)
					EditorApplication.delayCall += () => RegisterArrayElementFields(root, depth + 1);
				else
					Debug.LogWarning($"{nameof(ListView)}'s {nameof(ScrollView)}.{nameof(ScrollView.contentUssClassName)} (.{ScrollView.contentUssClassName}) could not be found.");
				return;
			}

			// ListViewSerializedObjectBinding will come along later when the serialized object is bound
			// and nuke all our work! But this doesn't happen on domain reload, so the other setup must remain too!
			// At least we're not dealing with the IL hell that is the IMGUI implementation 🙂
			RegisterSerializedObjectBindEvent(listView, _ => listView.schedule.Execute(() => RefreshListViewSerializeReferenceDropdown(listView)));

			RefreshListViewSerializeReferenceDropdown(listView);

			void RefreshListViewSerializeReferenceDropdown(ListView list)
			{
				AppendMethodToBindItemWithoutNotify(list, CreateDecorator);
				for (int i = 0; i < contentContainer.childCount; i++)
					CreateDecorator(contentContainer[i]);

				void CreateDecorator(VisualElement element)
				{
					var field = element.Q<PropertyField>();
					if (field.Q<VisualElement>(null, UssDecoratorDrawerContainer) != null)
						return; // Already set up!
					// Append a fake decorator drawer to handle serialize references in collections.
					var drawerContainer = new VisualElement
					{
						name = ArrayElementDecoratorName,
						pickingMode = PickingMode.Ignore
					};
					drawerContainer.AddToClassList(UssDecoratorDrawerContainer);
					drawerContainer.Add(new ReferenceDropdown(_attribute)
					{
						name = Name,
						_type = ElementType.ArrayElement
					});
					field.Insert(0, drawerContainer);
				}
			}
		}

		private static void UpdateDropdownVisual(SerializedProperty property, DropdownButton dropdown, ReferenceDropdownAttribute attribute, bool updateLabel = false)
		{
			bool referenceIsAssigned;
			if (updateLabel)
			{
				GUIContent label;
				(label, referenceIsAssigned) = GetLabel(attribute, property);
				dropdown.Text = label.text;
			}
			else
				referenceIsAssigned = ReferenceIsAssigned(property);

			if ((attribute.Features & ReferenceDropdownFeatures.ShowWarningForNull) != 0)
				dropdown.IconType = referenceIsAssigned ? HelpBoxMessageType.None : HelpBoxMessageType.Warning;
		}

		private static void RegisterSerializedObjectBindEvent(VisualElement element, Action<SerializedObject> callback)
		{
			// TODO optimise this down into a cached delegate or two.
			var registerCallbackMethod = typeof(CallbackEventHandler).GetMethods().Single(m => m.IsGenericMethod && m.Name == "RegisterCallback" && m.GetGenericArguments().Length == 2);
			var serializedPropertyBindEventType = Type.GetType("UnityEditor.UIElements.SerializedObjectBindEvent,UnityEditor");
			Type argsType = typeof(Action<SerializedObject>);
			Type callbackType = typeof(EventCallback<,>).MakeGenericType(serializedPropertyBindEventType, argsType);
			var registerCallbackMethodGeneric = registerCallbackMethod.MakeGenericMethod(serializedPropertyBindEventType, argsType);
			MethodInfo callbackGeneric = typeof(ReferenceDropdown)
				.GetMethod(nameof(BindCallback), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(serializedPropertyBindEventType);
			var callbackDelegate = Delegate.CreateDelegate(callbackType, callbackGeneric);
			registerCallbackMethodGeneric.Invoke(element, new object[] { callbackDelegate, callback, TrickleDown.TrickleDown });
		}

		private static void BindCallback<T>(T evt, Action<SerializedObject> callback) => callback((SerializedObject)evt.GetType().GetProperty("bindObject").GetValue(evt));

		private static void AppendMethodToBindItemWithoutNotify(ListView listView, Action<VisualElement> callback)
		{
			Action<VisualElement, int> bind = listView.bindItem;

			if (bind == null)
				return;

			void BindChain(VisualElement element, int index)
			{
				bind(element, index);
				callback(element);
			}

			typeof(ListView).GetMethod("SetBindItemWithoutNotify", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(listView, new object[] { (Action<VisualElement, int>)BindChain });
		}

		/*private void RegisterSerializedPropertyBindEvent(VisualElement element, Action<SerializedProperty> callback)
		{
			// TODO optimise this down into a cached delegate or two.
			var registerCallbackMethod = typeof(CallbackEventHandler).GetMethods().Single(m => m.IsGenericMethod && m.Name == "RegisterCallback" && m.GetGenericArguments().Length == 2);
			var serializedPropertyBindEventType = Type.GetType("UnityEditor.UIElements.SerializedPropertyBindEvent,UnityEditor");
			Type argsType = typeof(Action<SerializedProperty>);
			Type callbackType = typeof(EventCallback<,>).MakeGenericType(serializedPropertyBindEventType, argsType);
			var registerCallbackMethodGeneric = registerCallbackMethod.MakeGenericMethod(serializedPropertyBindEventType, argsType);
			MethodInfo callbackGeneric = typeof(ReferenceDropdown)
				.GetMethod(nameof(BindCallbackProperty), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(serializedPropertyBindEventType);
			var callbackDelegate = Delegate.CreateDelegate(callbackType, callbackGeneric);
			registerCallbackMethodGeneric.Invoke(element, new object[] { callbackDelegate, callback, TrickleDown.TrickleDown });
		}
		
		private static void BindCallbackProperty<T>(T evt, Action<SerializedProperty> callback) => callback((SerializedProperty)evt.GetType().GetProperty("bindProperty").GetValue(evt));*/
	}
}
#endif