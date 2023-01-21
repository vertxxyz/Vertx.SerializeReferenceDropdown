#if IMGUI_REFERENCE_DROPDOWN
using System;
using System.Reflection;
using HarmonyLib;
using UnityEditor;

namespace Vertx.Attributes.Editor
{
	internal static partial class DecoratorPropertyInjector
	{
		internal static SerializedProperty Current => current?.Copy();
		private static SerializedProperty current;
		internal static object Handler;

		private static Type propertyHandlerType;
		private static Type PropertyHandlerType => propertyHandlerType ??= propertyHandlerType = Type.GetType(propertyHandlerTypeName);

		private static MethodInfo getHeightMethod;
		private static MethodInfo GetHeightMethod(Type handlerType) => getHeightMethod ??= handlerType.GetMethod(getHeightMethodName, BindingFlags.Public | BindingFlags.Instance);

		private const string propertyHandlerTypeName = "UnityEditor.PropertyHandler,UnityEditor";

		private const string propertyGuiMethodName = "OnGUI";
		private const string getHeightMethodName = "GetHeight";
		
#if UNITY_2021_1_OR_NEWER
		private static PropertyInfo isCurrentlyNested;
		internal static bool IsCurrentlyNested(object handler)
		{
			isCurrentlyNested ??= PropertyHandlerType.GetProperty("isCurrentlyNested", BindingFlags.NonPublic | BindingFlags.Instance);
			return (bool) isCurrentlyNested!.GetValue(handler);
		}
#endif

		internal static void OnGUIPrefix(SerializedProperty property, object ctx)
		{
			current = property;
			Handler = ctx;
		}
		
		internal static void GetHeightPrefix(ref float height, SerializedProperty property, object ctx)
		{
			current = property;
			Handler = ctx;
			
			height += ReferenceDropdownDecoratorImgui.GetPropertyHeight(property);
		}

		[InitializeOnLoadMethod]
		private static void Initialise() => Inject();

		private static void Inject()
		{
			var handlerType = PropertyHandlerType;
			if (handlerType == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}");
				return;
			}

			Harmony harmony = new Harmony("com.vertx.decorator.injection");
			PatchOnGUI(harmony, handlerType);
			PatchGetHeight(harmony, handlerType);
		}

		private static void PatchOnGUI(Harmony harmony, Type handlerType)
		{
			MethodInfo onGUIMethod = handlerType.GetMethod(propertyGuiMethodName, BindingFlags.NonPublic | BindingFlags.Instance);

			if (onGUIMethod == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}.{propertyGuiMethodName} method");
				return;
			}

			harmony.Patch
			(
				onGUIMethod,
				transpiler:
				new HarmonyMethod(
					typeof(Transpiler_OnGUI).GetMethod(nameof(Transpiler_OnGUI.Transpiler), BindingFlags.NonPublic | BindingFlags.Static)
				)
			);
		}

		private static void PatchGetHeight(Harmony harmony, Type handlerType)
		{
			MethodInfo getHeightMethod = GetHeightMethod(handlerType);

			if (getHeightMethod == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}.{getHeightMethodName} method");
				return;
			}

			harmony.Patch
			(
				getHeightMethod,
				transpiler:
				new HarmonyMethod(
					typeof(Transpiler_GetHeight).GetMethod(nameof(Transpiler_GetHeight.Transpiler), BindingFlags.NonPublic | BindingFlags.Static)
				)
			);
		}
	}
}
#endif