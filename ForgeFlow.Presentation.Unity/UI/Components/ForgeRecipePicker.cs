using ForgeFlow.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgeFlow.Presentation.Unity.UI.Components
{
    /// <summary>
    /// Icon grid for selecting recipes or items. Each cell shows a colored square
    /// (placeholder for icons) and a localized tooltip label.
    /// Content-only component — wrap in a <see cref="ForgePanel"/> for dialog use.
    /// </summary>
    internal sealed class ForgeRecipePicker : ForgeStyledVisualElement
    {
        private readonly VisualElement _container;
        private readonly VisualElement _gridContainer;
        private readonly Label _emptyLabel;
        private readonly int _cellSize;
        private List<RecipeEntry> _recipes = new();

        public event Action<string>? RecipeSelected;

        public ForgeRecipePicker(int cellSize = 48)
        {
            _cellSize = cellSize;

            style.flexDirection = FlexDirection.Column;

            _container = new VisualElement();
            _container.style.flexGrow = 1;

            _gridContainer = new VisualElement();
            _gridContainer.style.flexDirection = FlexDirection.Row;
            _gridContainer.style.flexWrap = Wrap.Wrap;
            _gridContainer.style.flexGrow = 1;

            _emptyLabel = new Label(L(LocalizationKeys.RecipePickerNoRecipes));
            _emptyLabel.style.color = C("text.secondary");
            _emptyLabel.style.fontSize = ThemeFontNormal;
            _emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            Add(_container);
            ApplyTheme();
        }

        public void SetRecipes(List<RecipeEntry> recipes)
        {
            _recipes = recipes;
            RebuildCells();
        }

        private void RebuildCells()
        {
            _container.Clear();
            _gridContainer.Clear();

            if (_recipes.Count == 0)
            {
                _emptyLabel.text = L(LocalizationKeys.RecipePickerNoRecipes);
                _container.Add(_emptyLabel);
                return;
            }

            foreach (var recipe in _recipes)
            {
                var cell = CreateCell(recipe);
                _gridContainer.Add(cell);
            }

            _container.Add(_gridContainer);
        }

        private VisualElement CreateCell(RecipeEntry recipe)
        {
            var cell = new VisualElement();
            cell.style.width = _cellSize;
            cell.style.height = _cellSize;
            cell.style.marginRight = 4;
            cell.style.marginBottom = 4;
            cell.style.backgroundColor = recipe.DisplayColor;
            cell.style.alignItems = Align.Center;
            cell.style.justifyContent = Justify.Center;

            var cellRadius = ThemeIsMinimalist ? 2 : 6;
            SetBorderOn(cell, C("border.normal"), ThemeBorderWidth + 1, cellRadius);

            var label = new Label(recipe.ShortName);
            label.style.color = C("text.primary");
            label.style.fontSize = ThemeFontSmall;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.pickingMode = PickingMode.Ignore;
            cell.Add(label);

            var id = recipe.Id;
            cell.RegisterCallback<ClickEvent>(_ => RecipeSelected?.Invoke(id));

            cell.RegisterCallback<MouseEnterEvent>(_ =>
            {
                SetBorderOn(cell, C("accent.primary"), ThemeBorderWidth + 2, cellRadius);
                cell.style.backgroundColor = new Color(
                    recipe.DisplayColor.r * 1.2f,
                    recipe.DisplayColor.g * 1.2f,
                    recipe.DisplayColor.b * 1.2f,
                    recipe.DisplayColor.a);
            });
            cell.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                SetBorderOn(cell, C("border.normal"), ThemeBorderWidth + 1, cellRadius);
                cell.style.backgroundColor = recipe.DisplayColor;
            });

            return cell;
        }

        public override void ApplyTheme()
        {
            _emptyLabel.style.color = C("text.secondary");
            _emptyLabel.style.fontSize = ThemeFontNormal;
            RebuildCells();
        }

        // ── Fluent builder API ──

        public static RecipePickerBuilder Create(int cellSize = 48)
            => new(new ForgeRecipePicker(cellSize));

        internal sealed class RecipePickerBuilder : ForgeBuilder<RecipePickerBuilder, ForgeRecipePicker>
        {
            internal RecipePickerBuilder(ForgeRecipePicker el) : base(el) { }
            public RecipePickerBuilder Recipes(List<RecipeEntry> recipes) { _el.SetRecipes(recipes); return this; }
            public RecipePickerBuilder OnRecipeSelected(Action<string> cb) { _el.RecipeSelected += cb; return this; }
        }
    }

    /// <summary>Lightweight recipe display data for the picker grid.</summary>
    internal readonly struct RecipeEntry
    {
        public string Id { get; }
        public string ShortName { get; }
        public Color DisplayColor { get; }

        public RecipeEntry(string id, string shortName, Color displayColor)
        {
            Id = id;
            ShortName = shortName;
            DisplayColor = displayColor;
        }
    }
}
