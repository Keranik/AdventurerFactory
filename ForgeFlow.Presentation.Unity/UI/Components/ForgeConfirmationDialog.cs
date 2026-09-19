using ForgeFlow.Core.Localization;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Themed modal confirmation dialog with OK/Cancel or Yes/No buttons.
    /// Blocks interaction with elements behind it via a semi-transparent overlay.
    /// </summary>
    internal sealed class ForgeConfirmationDialog : ForgeStyledVisualElement
    {
        private readonly VisualElement _overlay;
        private readonly VisualElement _dialogBox;
        private readonly Label _titleLabel;
        private readonly Label _messageLabel;
        private readonly Button _primaryButton;
        private readonly Button _secondaryButton;

        public event Action? Confirmed;
        public event Action? Cancelled;

        public ForgeConfirmationDialog()
        {
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            style.display = DisplayStyle.None;

            // Semi-transparent overlay
            _overlay = new VisualElement();
            _overlay.style.position = Position.Absolute;
            _overlay.style.left = 0;
            _overlay.style.top = 0;
            _overlay.style.right = 0;
            _overlay.style.bottom = 0;
            _overlay.style.backgroundColor = new UnityEngine.Color(0f, 0f, 0f, 0.5f);
            _overlay.RegisterCallback<ClickEvent>(_ => Cancel());
            Add(_overlay);

            // Dialog box centered
            _dialogBox = new VisualElement();
            _dialogBox.style.position = Position.Absolute;
            _dialogBox.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _dialogBox.style.top = new StyleLength(new Length(50, LengthUnit.Percent));
            _dialogBox.style.translate = new Translate(-50f, -50f, 0f);
            _dialogBox.style.width = 400;
            _dialogBox.style.flexDirection = FlexDirection.Column;
            Add(_dialogBox);

            // Title
            _titleLabel = new Label(L(LocalizationKeys.ConfirmTitle));
            _titleLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            _dialogBox.Add(_titleLabel);

            // Message
            _messageLabel = new Label();
            _messageLabel.style.whiteSpace = WhiteSpace.Normal;
            _messageLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            _dialogBox.Add(_messageLabel);

            // Button row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;
            buttonRow.style.marginTop = 12;

            _primaryButton = new Button(() => Confirm()) { text = L(LocalizationKeys.ConfirmOk) };
            _primaryButton.style.flexGrow = 1;
            _primaryButton.style.marginRight = 8;

            _secondaryButton = new Button(() => Cancel()) { text = L(LocalizationKeys.ConfirmCancel) };
            _secondaryButton.style.flexGrow = 1;

            buttonRow.Add(_primaryButton);
            buttonRow.Add(_secondaryButton);
            _dialogBox.Add(buttonRow);

            ApplyTheme();
        }

        public void ShowOkCancel(string titleKey, string messageKey)
        {
            _titleLabel.text = L(titleKey);
            _messageLabel.text = L(messageKey);
            _primaryButton.text = L(LocalizationKeys.ConfirmOk);
            _secondaryButton.text = L(LocalizationKeys.ConfirmCancel);
            style.display = DisplayStyle.Flex;
            BringToFront();
        }

        public void ShowYesNo(string titleKey, string messageKey)
        {
            _titleLabel.text = L(titleKey);
            _messageLabel.text = L(messageKey);
            _primaryButton.text = L(LocalizationKeys.ConfirmYes);
            _secondaryButton.text = L(LocalizationKeys.ConfirmNo);
            style.display = DisplayStyle.Flex;
            BringToFront();
        }

        private void Confirm()
        {
            style.display = DisplayStyle.None;
            Confirmed?.Invoke();
        }

        private void Cancel()
        {
            style.display = DisplayStyle.None;
            Cancelled?.Invoke();
        }

        public override void ApplyTheme()
        {
            var radius = ThemeIsMinimalist ? 2 : 10;

            _dialogBox.style.backgroundColor = C("bg.primary");
            SetBorderOn(_dialogBox, C("accent.primary"), ThemeBorderWidth + 2, radius);

            _titleLabel.style.color = C("text.accent");
            _titleLabel.style.fontSize = ThemeFontLarge;
            _titleLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            _titleLabel.style.marginBottom = 12;

            _messageLabel.style.color = C("text.primary");
            _messageLabel.style.fontSize = ThemeFontNormal;

            // Primary button: accent colored with raised border
            _primaryButton.style.backgroundColor = C("btn.active");
            _primaryButton.style.color = C("btn.text");
            _primaryButton.style.fontSize = ThemeFontNormal;
            _primaryButton.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            var btnRadius = ThemeIsMinimalist ? 2 : 6;
            SetRaisedBorderOn(_primaryButton, C("btn.active"), ThemeBorderWidth + 1, btnRadius);
            SetPaddingOn(_primaryButton, ThemePaddingSmall + 2);

            // Secondary button: muted with standard border
            _secondaryButton.style.backgroundColor = C("btn.normal");
            _secondaryButton.style.color = C("btn.text");
            _secondaryButton.style.fontSize = ThemeFontNormal;
            SetRaisedBorderOn(_secondaryButton, C("btn.normal"), ThemeBorderWidth + 1, btnRadius);
            SetPaddingOn(_secondaryButton, ThemePaddingSmall + 2);

            _dialogBox.style.paddingTop = ThemePaddingLarge;
            _dialogBox.style.paddingBottom = ThemePaddingLarge;
            _dialogBox.style.paddingLeft = ThemePaddingLarge;
            _dialogBox.style.paddingRight = ThemePaddingLarge;
        }

        // ── Fluent builder API ──

        public static DialogBuilder Create()
            => new(new ForgeConfirmationDialog());

        internal sealed class DialogBuilder : ForgeBuilder<DialogBuilder, ForgeConfirmationDialog>
        {
            internal DialogBuilder(ForgeConfirmationDialog el) : base(el) { }
            public DialogBuilder OnConfirmed(Action cb) { _el.Confirmed += cb; return this; }
            public DialogBuilder OnCancelled(Action cb) { _el.Cancelled += cb; return this; }
        }
    }
}
