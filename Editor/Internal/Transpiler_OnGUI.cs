using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Vertx.Attributes.Editor
{
	internal static class Transpiler_OnGUI
	{
		internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			OpCode opCode = OpCodes.Ldarg_2;

			bool injectedDrawer = false;
			bool injectedReferenceDropdown = false;
			bool readyToInjectReferenceDropdown = false;
			foreach (var instruction in instructions)
			{
				if (!injectedDrawer)
				{
					// There is not a ldarg_2 as the first instruction usually, so we know we have previously injected if this is the case.
					if (instruction.opcode != opCode)
					{
						// Loads the argument at the 2nd index ("property") onto the evaluation stack.
						yield return new CodeInstruction(opCode);
						// Loads "this" onto the evaluation stack.
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// Call our new method.
						var codeInstruction = new CodeInstruction(OpCodes.Call,
							typeof(DecoratorPropertyInjector).GetMethod(nameof(DecoratorPropertyInjector.OnGUIPrefix), BindingFlags.Static | BindingFlags.NonPublic)
						);
						yield return codeInstruction;
					}

					injectedDrawer = true;
				}

				if (!injectedReferenceDropdown)
				{
					if(!readyToInjectReferenceDropdown)
					{
						// The end of the first foreach
						if (instruction.opcode == OpCodes.Endfinally)
							readyToInjectReferenceDropdown = true;
					}
					
					//This attempts to inject this instruction after "position.height = x;" (line 166 in 2022.1)
					else if (instruction.opcode == OpCodes.Call)
					{
						// Use current instruction
						yield return instruction;
						
						// Fetches argument at the 1st index ("position")
						yield return new CodeInstruction(OpCodes.Ldarga_S, 1);
						// Call our new method.
						var codeInstruction = new CodeInstruction(OpCodes.Call,
							typeof(ReferenceDropdownDecoratorImgui).GetMethod(nameof(ReferenceDropdownDecoratorImgui.OnGUI), BindingFlags.Static | BindingFlags.Public)
						);
						yield return codeInstruction;

						injectedReferenceDropdown = true;
						continue;
					}
				}

				yield return instruction;
			}

			if (!injectedDrawer || !injectedReferenceDropdown)
				Debug.LogWarning("Missing injection. This likely means that Vertx.SerializeReferenceDropdown is not valid with this version of Unity.");
		}
	}
}