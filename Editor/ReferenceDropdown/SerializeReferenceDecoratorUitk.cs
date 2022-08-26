#if UNITY_2022_2_OR_NEWER
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Utilities.Editor;

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
		private const string ussClassName = "vertx-reference-dropdown";
		private const string containerUssClassName = ussClassName + "__container";
		private const string backgroundUssClassName = ussClassName + "__background";

		private static float HeaderHeight => 20;

		// This is zero because IMGUI support is not handled via this decorator.
		public override float GetHeight() => 0;

		public override VisualElement CreatePropertyGUI()
		{
			var container = new VisualElement
			{
				name = "ReferenceDropdown Container"
			};
			container.AddToClassList(containerUssClassName);
			container.RegisterCallback<AttachToPanelEvent, VisualElement>(AttachToPanel, container);

			return container;
		}

		private static StyleSheet _serializeDropdownStyle;

		private void AttachToPanel(AttachToPanelEvent evt, VisualElement target)
		{
			VisualElement root = evt.destinationPanel.visualTree;
			root = root.Children().SingleOrDefault(c => c.name.StartsWith("rootVisualContainer", StringComparison.Ordinal)) ?? root;
			_serializeDropdownStyle ??= AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.vertx.serializereference-dropdown/Editor/Assets/SerializeDropdownStyle.uss");
			if (!root.styleSheets.Contains(_serializeDropdownStyle))
				root.styleSheets.Add(_serializeDropdownStyle);

			VisualElement parent = target;
			do
			{
				parent = parent.parent;
			} while (parent is not (PropertyField or null));

			if (parent is not PropertyField propertyField)
				return;

			var serializedProperty = (SerializedProperty)typeof(PropertyField).GetProperty("serializedProperty", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(propertyField);
			if (serializedProperty == null)
			{
				Debug.LogWarning($"PropertyField not bound before {nameof(AttachToPanel)} was called.");
				return;
			}

			if (serializedProperty.isArray)
			{
				target.name = "ReferenceDropdown Array Driver";
				target.style.display = DisplayStyle.None;

				RegisterArrayElementFields(propertyField);
				return;
			}

			DropdownButton dropdownButton = new DropdownButton(serializedProperty.displayName, "Example", button => { });
			target.Add(dropdownButton);

			var background = new VisualElement
			{
				name = "ReferenceDropdown Background",
				style =
				{
					backgroundColor = EditorGUIUtils.HeaderColor,
					position = Position.Absolute,
					top = EditorGUIUtility.standardVerticalSpacing + HeaderHeight,
					bottom = 0,
					left = 0,
					right = 0,
					borderBottomLeftRadius = 3,
					borderBottomRightRadius = 3
				}
			};
			background.AddToClassList(backgroundUssClassName);
			parent.Insert(0, background);
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
					const string decoratorDrawerContainerUss = "unity-decorator-drawers-container";

					var field = element.Q<PropertyField>();
					if (field.Q<VisualElement>(null, decoratorDrawerContainerUss) != null)
						return; // Already set up!
					var drawerContainer = new VisualElement
					{
						name = "ReferenceDropdown Array Element Decorator"
					};
					drawerContainer.AddToClassList(decoratorDrawerContainerUss);
					drawerContainer.Add(CreatePropertyGUI());
					field.Insert(0, drawerContainer);
				}
			}
		}

		private void RegisterSerializedObjectBindEvent(VisualElement element, Action<SerializedObject> callback)
		{
			// TODO optimise this down into a cached delegate or two.
			var registerCallbackMethod = typeof(CallbackEventHandler).GetMethods().Single(m => m.IsGenericMethod && m.Name == "RegisterCallback" && m.GetGenericArguments().Length == 2);
			var serializedPropertyBindEventType = Type.GetType("UnityEditor.UIElements.SerializedObjectBindEvent,UnityEditor");
			Type argsType = typeof(Action<SerializedObject>);
			Type callbackType = typeof(EventCallback<,>).MakeGenericType(serializedPropertyBindEventType, argsType);
			var registerCallbackMethodGeneric = registerCallbackMethod.MakeGenericMethod(serializedPropertyBindEventType, argsType);
			MethodInfo callbackGeneric = typeof(ReferenceDropdownUIToolkit)
				.GetMethod(nameof(BindCallback), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(serializedPropertyBindEventType);
			var callbackDelegate = Delegate.CreateDelegate(callbackType, callbackGeneric);
			registerCallbackMethodGeneric.Invoke(element, new object[] { callbackDelegate, callback, TrickleDown.TrickleDown });
		}

		private static void BindCallback<T>(T evt, Action<SerializedObject> callback) => callback((SerializedObject)evt.GetType().GetProperty("bindObject").GetValue(evt));

		private static void AppendMethodToBindItemWithoutNotify(ListView listView, Action<VisualElement> callback)
		{
			Action<VisualElement, int> bind = listView.bindItem;

			void BindChain(VisualElement element, int index)
			{
				bind(element, index);
				callback(element);
			}

			typeof(ListView).GetMethod("SetBindItemWithoutNotify", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(listView, new object[] { (Action<VisualElement, int>)BindChain });
		}
	}
}
#endif