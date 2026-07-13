/*
ImageGlass - A Fast, Seamless Photo Viewer
Copyright (C) 2010 - 2026 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Input;
using Avalonia.Interactivity;
using ImageGlass.Common;
using ImageGlass.Common.Types;

namespace ImageGlass.UI;

public partial class ToolbarButton : PhToolButton, IToolbarItem
{
    public ToolbarItemModel VM => (ToolbarItemModel)DataContext!;

    /// <summary>
    /// Occurs when this button is right-clicked.
    /// </summary>
    public event TEventHandler<ToolbarButton, PointerPressedEventArgs>? RightClicked;


    public ToolbarButton()
    {
        DataContext = new ToolbarItemModel();
        InitializeComponent();

        // Avalonia's Button class handlers mark PointerPressed as handled for all buttons,
        // preventing right-click from ever reaching OnPointerPressed. Use tunnel routing
        // with handledEventsToo so we capture it before the Button class handlers do.
        AddHandler(PointerPressedEvent, PointerPressedHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
    }



    #region Control Events

    private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            e.Handled = true;
            RightClicked?.Invoke(this, e);
        }
    }


    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Core.Config.PropertyChanged += Config_PropertyChanged;
    }


    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Core.Config.PropertyChanged -= Config_PropertyChanged;
    }


    protected override void OnIgDropdownMenuOpened(RoutedEventArgs e)
    {
        base.OnIgDropdownMenuOpened(e);
        IsChecked = true;
    }


    protected override void OnIgDropdownMenuClosed(RoutedEventArgs e)
    {
        base.OnIgDropdownMenuClosed(e);
        IsChecked = false;
    }


    protected override void OnIgThemeChanged(ThemePackChangedEventArgs e)
    {
        base.OnIgThemeChanged(e);

        _ = VM.OnPropertyChanged(nameof(VM.ImagePath));
    }


    private void Config_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (nameof(Core.Config.ToolbarIconHeight).Equals(e.PropertyName))
        {
            _ = VM.OnPropertyChanged(nameof(VM.InnerSpacing));
            _ = VM.OnPropertyChanged(nameof(VM.ItemPadding));
            _ = VM.OnPropertyChanged(nameof(VM.SeparatorEndPoint));
        }
    }

    #endregion Control Events


}