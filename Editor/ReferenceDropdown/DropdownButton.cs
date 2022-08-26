#if UNITY_2022_2_OR_NEWER
using System;
using UnityEngine.UIElements;

namespace Vertx.Attributes.Editor
{
	/// <summary>
	/// A UIToolkit GenericMenu dropdown button.
	/// </summary>
	internal class DropdownButton : BaseField<string>
	{
		public const string UssClassName = "vertx-dropdown-button";
		public const string LabelUssClassName = UssClassName + "__label";
		public const string DropdownUssClassName = UssClassName + "__dropdown";
		public const string WarningUssClassName = UssClassName + "__warning";
		
		public string Text
		{
			get => textElement.text;
			set => textElement.text = value;
		}

		public bool ShowWarning
		{
			get => warningElement?.visible ?? false;
			set
			{
				if (warningElement == null)
				{
					if (!value) return;
					warningElement = new VisualElement();
					warningElement.AddToClassList(HelpBox.iconUssClassName);
					warningElement.AddToClassList(HelpBox.iconwarningUssClassName);
					warningElement.AddToClassList(WarningUssClassName);
					VisualInput.Add(warningElement);
				}

				warningElement.visible = value;
			}
		}
		
		private readonly TextElement textElement;
		private VisualElement warningElement;
		private VisualElement internalVisualInput;
		private readonly Action<DropdownButton> onSelect;
		private VisualElement VisualInput => internalVisualInput ??= this.Q<VisualElement>(null, inputUssClassName);

		public DropdownButton(string displayValue, Action<DropdownButton> onSelect, bool showWarning = false)
			: this(null, displayValue, onSelect, showWarning) { }

		public DropdownButton(string label, string displayValue, Action<DropdownButton> onSelect, bool showWarning = false) : base(label, null)
		{
			this.onSelect = onSelect;
			AddToClassList(BasePopupField<string, string>.ussClassName);
			AddToClassList(UssClassName);
			labelElement.AddToClassList(BasePopupField<string, string>.labelUssClassName);
			labelElement.AddToClassList(LabelUssClassName);
			TextElement popupTextElement = new TextElement { pickingMode = PickingMode.Ignore };
			textElement = popupTextElement;
			textElement.AddToClassList(BasePopupField<string, string>.textUssClassName);
			VisualInput.AddToClassList(BasePopupField<string, string>.inputUssClassName);
			VisualInput.AddToClassList(DropdownUssClassName);
			textElement.text = displayValue;
			VisualInput.Add(textElement);
			var arrowElement = new VisualElement();
			arrowElement.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
			arrowElement.pickingMode = PickingMode.Ignore;
			VisualInput.Add(arrowElement);
			RegisterCallback<PointerDownEvent>(ClickEvent);
			ShowWarning = showWarning;
		}

		private void ClickEvent(PointerDownEvent evt)
		{
			evt.StopPropagation();
			onSelect?.Invoke(this);
		}
	}
}
#endif