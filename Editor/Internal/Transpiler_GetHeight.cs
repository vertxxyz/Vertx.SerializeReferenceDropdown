using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Vertx.Attributes.Editor
{
	internal static class Transpiler_GetHeight
	{
		internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			OpCode opCode = OpCodes.Ldarg_1;

			bool injectedDrawer = false;
			bool canInjectDrawer = false;
			foreach (var instruction in instructions)
			{
				if (!injectedDrawer)
				{
					if (!canInjectDrawer)
					{
						if(instruction.opcode == OpCodes.Stloc_0)
							canInjectDrawer = true;
					}
					else
					{
						// There is not a ldarg_1 as the first instruction usually, so we know we have previously injected if this is the case.
						if (instruction.opcode != opCode)
						{
							// Loads reference to the local variable at index 0 onto the evaluation stack.
							yield return new CodeInstruction(OpCodes.Ldloca_S, 0);

							// Loads the argument at the 1st index ("property") onto the evaluation stack.
							yield return new CodeInstruction(opCode);

							// Loads "this" onto the evaluation stack.
							yield return new CodeInstruction(OpCodes.Ldarg_0);

							// Call our new method.
							var codeInstruction = new CodeInstruction(OpCodes.Call,
								typeof(DecoratorPropertyInjector).GetMethod(nameof(DecoratorPropertyInjector.GetHeightPrefix), BindingFlags.Static | BindingFlags.NonPublic)
							);
							yield return codeInstruction;
						}

						injectedDrawer = true;
					}
				}

				yield return instruction;
			}

			if (!injectedDrawer)
				Debug.Log("Missing Injection");
		}
	}
}