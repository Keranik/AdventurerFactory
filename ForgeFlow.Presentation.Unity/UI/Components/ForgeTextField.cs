using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>Themed text field with localized placeholder and label.</summary>
    internal sealed class ForgeTextField : ForgeStyledVisualElement
    {
        private readonly TextField _textField;
        private readonly string _locKey;

        public event Action<string>? TextChanged;

        public ForgeTextField(string localizationKey, string initialValue = "")
        {
            _locKey = localizationKey;
            _textField = new TextField(L(localizationKey)) { value = initialValue };
            _textField.RegisterValueChangedCallback(evt => TextChanged?.Invoke(evt.newValue));

            // Focus indication — highlight border on focus
            _textField.RegisterCallback<FocusInEvent>(_ =>
            {
                SetBorder(C("border.focused"), ThemeBorderWidth + 1, ThemeBorderRadius);
            });
            _textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                SetBorder(C("border.normal"), ThemeBorderWidth, ThemeBorderRadius);
            });

            Add(_textField);
            ApplyTheme();
        }

        public string Value
        {
            get => _textField.value;
            set => _textField.value = value;
        }

        public bool IsReadOnly
        {
            get => _textField.isReadOnly;
            set => _textField.isReadOnly = value;
        }

        public override void ApplyTheme()
        {
            _textField.label = L(_locKey);
            _textField.style.color = C("input.text");
            _textField.style.fontSize = ThemeFontNormal;

            // Outer container styling
            style.backgroundColor = C("bg.secondary");
            var radius = ThemeIsMinimalist ? 2 : 6;
            SetBorder(C("border.normal"), ThemeBorderWidth, radius);
            SetPadding(ThemePaddingSmall + 2);

            // Style the label portion
            var label = _textField.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.color = C("text.secondary");
                label.style.minWidth = 60;
            }

            // Style the actual text input element inside the TextField
            _textField.RegisterCallback<AttachToPanelEvent>(_ => StyleInputElement());
            StyleInputElement();
        }

        private void StyleInputElement()
        {
            var inputEl = _textField.Q(className: "unity-text-field__input");
            if (inputEl == null) return;

            inputEl.style.backgroundColor = C("input.bg");
            inputEl.style.color = C("input.text");
            inputEl.style.paddingTop = ThemePaddingSmall;
            inputEl.style.paddingBottom = ThemePaddingSmall;
            inputEl.style.paddingLeft = ThemePaddingNormal;
            inputEl.style.paddingRight = ThemePaddingNormal;

            var inputRadius = ThemeIsMinimalist ? 2 : 4;
            SetBorderOn(inputEl, C("border.normal"), ThemeBorderWidth, inputRadius);
        }

        // ── Fluent builder API ──

        public static TextFieldBuilder Create(string locKey, string initialValue = "")
            => new(new ForgeTextField(locKey, initialValue));

        internal sealed class TextFieldBuilder : ForgeBuilder<TextFieldBuilder, ForgeTextField>
        {
            internal TextFieldBuilder(ForgeTextField el) : base(el) { }
            public TextFieldBuilder Value(string v) { _el.Value = v; return this; }
            public TextFieldBuilder ReadOnly(bool r) { _el.IsReadOnly = r; return this; }
            public TextFieldBuilder OnTextChanged(Action<string> cb) { _el.TextChanged += cb; return this; }
        }
    }
}
