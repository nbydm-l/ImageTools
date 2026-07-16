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
using ImageGlass.Common.Actions;
using ImageGlass.Common.AppThemes;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Localization;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.ServiceProviders;
using ImageGlass.Common.Types;
using ImageGlass.Tools;
using ImageGlass.UI;
using ImageGlass.UI.Viewer;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
namespace ImageGlass.Common;

public partial class Config
{
    #region Public static properties

    /// <summary>
    /// App setting specs version, to check for compatibility.
    /// </summary>
    [JsonIgnore]
    public static float SPEC_VERSION => 10f;


    /// <summary>
    /// Gets the user config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_USER => "igconfig.json";


    /// <summary>
    /// Gets the default config file located.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_DEFAULT => "igconfig.default.json";


    /// <summary>
    /// Gets the admin config file name.
    /// </summary>
    [JsonIgnore]
    public static string CONFIG_ADMIN => "igconfig.admin.json";


    /// <summary>
    /// Gets the exception while loading app settings.
    /// </summary>
    [JsonIgnore]
    public static Exception? LoadingException { get; private set; } = null;


    /// <summary>
    /// Gets the default image formats.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultFileFormats { get; } = new(
        Const.IMAGE_FORMATS.Split(';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        )
    );


    /// <summary>
    /// Gets the default image info tags.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<string> DefaultImageInfoTags { get; } = new([
        nameof(AppStatusInfo.Name),
        nameof(AppStatusInfo.ListCount),
        nameof(AppStatusInfo.FrameCount),
        nameof(AppStatusInfo.Zoom),
        nameof(AppStatusInfo.Dimension),
        nameof(AppStatusInfo.FileSize),
        nameof(AppStatusInfo.ColorSpace),
        nameof(AppStatusInfo.HdrInfo),
        nameof(AppStatusInfo.ExifRating),
        nameof(AppStatusInfo.DateTimeAuto),
        nameof(AppStatusInfo.AppName),
    ]);


    /// <summary>
    /// Gets the default mouse wheel actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseWheelEvent, MouseWheelAction> DefaultMouseWheelActions { get; } = new()
    {
        [MouseWheelEvent.Scroll] = MouseWheelAction.Zoom,
        [MouseWheelEvent.CtrlAndScroll] = MouseWheelAction.PanVertically,
        [MouseWheelEvent.ShiftAndScroll] = MouseWheelAction.PanHorizontally,
        [MouseWheelEvent.AltAndScroll] = MouseWheelAction.BrowseImages,
    };


    /// <summary>
    /// Gets the default mouse click actions.
    /// </summary>
    [JsonIgnore]
    public static Dictionary<MouseClickEvent, SingleAction> DefaultMouseClickActions { get; } = new()
    {
        [MouseClickEvent.LeftDoubleClick] = new SingleAction(API.IG_SetZoomForMouseClick),
        [MouseClickEvent.RightClick] = new SingleAction(API.IG_OpenContextMenu),
        [MouseClickEvent.WheelClick] = new SingleAction(API.IG_Refresh),
        [MouseClickEvent.XButton1Click] = new SingleAction(API.IG_ViewPrevious),
        [MouseClickEvent.XButton2Click] = new SingleAction(API.IG_ViewNext),
    };


    /// <summary>
    /// Gets the default toolbar items.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<ToolbarItemModel> DefaultToolbarItems =>
    [
        // open file
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.OpenFile) }",
            Image = nameof(IgThemeIcon.OpenFile),
            Text = Lang.KeysMap[LangId.Menu_MnuOpenFile],
            Alignment = ToolbarItemAlignment.Right,
            OnClick = new(LangId.Menu_MnuOpenFile, API.IG_OpenFile),
        },



        // view previous
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewPreviousImage)}",
            Image = nameof(IgThemeIcon.ViewPreviousImage),
            Text = Lang.KeysMap[LangId.Menu_MnuViewPrevious],
            OnClick = new(LangId.Menu_MnuViewPrevious, API.IG_ViewPrevious),
        },
        // view next
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewNextImage)}",
            Image = nameof(IgThemeIcon.ViewNextImage),
            Text = Lang.KeysMap[LangId.Menu_MnuViewNext],
            OnClick = new(LangId.Menu_MnuViewNext, API.IG_ViewNext),
        },
        ToolbarItemModel.Separator,


        // rotate left
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateLeft)}",
            Image = nameof(IgThemeIcon.RotateLeft),
            Text = Lang.KeysMap[LangId.Menu_MnuRotateLeft],
            OnClick = new(LangId.Menu_MnuRotateLeft, API.IG_Rotate, nameof(RotateOption.Left)),
        },
        // rotate right
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateRight)}",
            Image = nameof(IgThemeIcon.RotateRight),
            Text = Lang.KeysMap[LangId.Menu_MnuRotateRight],
            OnClick = new(LangId.Menu_MnuRotateRight, API.IG_Rotate, nameof(RotateOption.Right)),
        },
        // flip horz
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipHorz)}",
            Image = nameof(IgThemeIcon.FlipHorz),
            Text = Lang.KeysMap[LangId.Menu_MnuFlipHorizontal],
            OnClick = new(LangId.Menu_MnuFlipHorizontal, API.IG_FlipImage, nameof(FlipOptions.Horizontal)),
        },
        // flip vert
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipVert)}",
            Image = nameof(IgThemeIcon.FlipVert),
            Text = Lang.KeysMap[LangId.Menu_MnuFlipVertical],
            OnClick = new(LangId.Menu_MnuFlipVertical, API.IG_FlipImage, nameof(FlipOptions.Vertical)),
        },
        // crop
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Crop)}",
            Image = nameof(IgThemeIcon.Crop),
            Text = Lang.KeysMap[LangId.Menu_MnuCropTool],
            ConfigBinding = nameof(Config.LastOpenedTool),
            ConfigBindingValue = CropImageToolControl.TOOL_ID,
            OnClick = new(LangId.Menu_MnuCropTool, API.IG_ToggleTool, CropImageToolControl.TOOL_ID),
        },
        // color picker
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ColorPicker)}",
            Image = nameof(IgThemeIcon.ColorPicker),
            Text = Lang.KeysMap[LangId.Menu_MnuColorPicker],
            ConfigBinding = nameof(Config.LastOpenedTool),
            ConfigBindingValue = ColorPickerToolControl.TOOL_ID,
            OnClick = new(LangId.Menu_MnuColorPicker, API.IG_ToggleTool, ColorPickerToolControl.TOOL_ID),
        },
        // scale to fit — always fit image to the viewer; re-fits on photo change
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFit)}",
            Image = nameof(IgThemeIcon.ScaleToFit),
            Text = Lang.KeysMap[LangId.Menu_MnuScaleToFit],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFit.ToString(),
            OnClick = new(LangId.Menu_MnuScaleToFit, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFit)),
        },
        ToolbarItemModel.Separator,


        // refresh
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Refresh)}",
            Image = nameof(IgThemeIcon.Refresh),
            Text = Lang.KeysMap[LangId.Menu_MnuRefresh],
            OnClick = new(LangId.Menu_MnuRefresh, API.IG_Refresh),
        },
        // toggle gallery
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Gallery)}",
            Image = nameof(IgThemeIcon.Gallery),
            Text = Lang.KeysMap[LangId.Menu_MnuToggleGallery],
            ConfigBinding = nameof(Config.ShowGallery),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuToggleGallery, API.IG_ToggleGallery),
        },
        // toggle checkerboard
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Checkerboard)}",
            Image = nameof(IgThemeIcon.Checkerboard),
            Text = Lang.KeysMap[LangId.Menu_MnuToggleCheckerboard],
            ConfigBinding = nameof(Config.CheckerboardMode),
            ConfigBindingValue = $"!{nameof(CheckerboardType.None)}",
            OnClick = new(LangId.Menu_MnuToggleCheckerboard, API.IG_ToggleCheckerboard),
        },
        // toggle fullscreen
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FullScreen)}",
            Image = nameof(IgThemeIcon.FullScreen),
            Text = Lang.KeysMap[LangId.Menu_MnuFullScreen],
            ConfigBinding = nameof(Config.EnableFullScreen),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuFullScreen, API.IG_ToggleFullScreen),
        },
        // toggle slildeshow
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Slideshow)}",
            Image = nameof(IgThemeIcon.Slideshow),
            Text = Lang.KeysMap[LangId.Menu_MnuSlideshow],
            ConfigBinding = nameof(Config.EnableSlideshow),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuSlideshow, API.IG_ToggleSlideshow),
        },
        ToolbarItemModel.Separator,


        // delete
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Delete)}",
            Image = nameof(IgThemeIcon.Delete),
            Text = Lang.KeysMap[LangId.Menu_MnuMoveToRecycleBin],
            OnClick = new(LangId.Menu_MnuMoveToRecycleBin, API.IG_Delete),
        },
        ToolbarItemModel.Separator,

        // color channel R
        new() {
            Id = "Btn_ColorChannelR",
            Image = nameof(IgThemeIcon.ColorChannelR),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelR],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelR),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelR, API.IG_ToggleColorChannel, nameof(ColorChannels.R)),
            OnRightClick = new(LangId.Menu_MnuColorChannelR, API.IG_SetSingleColorChannel, nameof(ColorChannels.R)),
        },
        // color channel G
        new() {
            Id = "Btn_ColorChannelG",
            Image = nameof(IgThemeIcon.ColorChannelG),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelG],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelG),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelG, API.IG_ToggleColorChannel, nameof(ColorChannels.G)),
            OnRightClick = new(LangId.Menu_MnuColorChannelG, API.IG_SetSingleColorChannel, nameof(ColorChannels.G)),
        },
        // color channel B
        new() {
            Id = "Btn_ColorChannelB",
            Image = nameof(IgThemeIcon.ColorChannelB),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelB],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelB),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelB, API.IG_ToggleColorChannel, nameof(ColorChannels.B)),
            OnRightClick = new(LangId.Menu_MnuColorChannelB, API.IG_SetSingleColorChannel, nameof(ColorChannels.B)),
        },
        // color channel A
        new() {
            Id = "Btn_ColorChannelA",
            Image = nameof(IgThemeIcon.ColorChannelA),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelA],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelA),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelA, API.IG_ToggleColorChannel, nameof(ColorChannels.A)),
            OnRightClick = new(LangId.Menu_MnuColorChannelA, API.IG_SetSingleColorChannel, nameof(ColorChannels.A)),
        }
    ];


    /// <summary>
    /// Gets the catalog of all built-in toolbar buttons the user can add to the toolbar.
    /// Used by the Toolbar settings page to populate the "Available buttons" list.
    /// </summary>
    [JsonIgnore]
    public static ReadOnlyCollection<ToolbarItemModel> BuiltInToolbarItems =>
    [
        // File
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.OpenFile)}",
            Image = nameof(IgThemeIcon.OpenFile),
            Text = Lang.KeysMap[LangId.Menu_MnuOpenFile],
            OnClick = new(LangId.Menu_MnuOpenFile, API.IG_OpenFile),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Save)}",
            Image = nameof(IgThemeIcon.Save),
            Text = Lang.KeysMap[LangId.Menu_MnuSave],
            OnClick = new(LangId.Menu_MnuSave, API.IG_Save),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Print)}",
            Image = nameof(IgThemeIcon.Print),
            Text = Lang.KeysMap[LangId.Menu_MnuPrint],
            OnClick = new(LangId.Menu_MnuPrint, API.IG_Print),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Export)}",
            Image = nameof(IgThemeIcon.Export),
            Text = Lang.KeysMap[LangId.Menu_MnuExportFrames],
            OnClick = new(LangId.Menu_MnuExportFrames, API.IG_ExportImageFrames),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Edit)}",
            Image = nameof(IgThemeIcon.Edit),
            Text = Lang.KeysMap[LangId.Menu_MnuEdit],
            OnClick = new(LangId.Menu_MnuEdit, API.IG_OpenEditingApp),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Delete)}",
            Image = nameof(IgThemeIcon.Delete),
            Text = Lang.KeysMap[LangId.Menu_MnuMoveToRecycleBin],
            OnClick = new(LangId.Menu_MnuMoveToRecycleBin, API.IG_Delete, "true"),
        },

        // Navigation
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewPreviousImage)}",
            Image = nameof(IgThemeIcon.ViewPreviousImage),
            Text = Lang.KeysMap[LangId.Menu_MnuViewPrevious],
            OnClick = new(LangId.Menu_MnuViewPrevious, API.IG_ViewPrevious),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewNextImage)}",
            Image = nameof(IgThemeIcon.ViewNextImage),
            Text = Lang.KeysMap[LangId.Menu_MnuViewNext],
            OnClick = new(LangId.Menu_MnuViewNext, API.IG_ViewNext),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewFirstImage)}",
            Image = nameof(IgThemeIcon.ViewFirstImage),
            Text = Lang.KeysMap[LangId.Menu_MnuGoToFirst],
            OnClick = new(LangId.Menu_MnuGoToFirst, API.IG_GotoFirst),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ViewLastImage)}",
            Image = nameof(IgThemeIcon.ViewLastImage),
            Text = Lang.KeysMap[LangId.Menu_MnuGoToLast],
            OnClick = new(LangId.Menu_MnuGoToLast, API.IG_GotoLast),
        },

        // Zoom
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.AutoZoom)}",
            Image = nameof(IgThemeIcon.AutoZoom),
            Text = Lang.KeysMap[LangId.Menu_MnuAutoZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.AutoZoom.ToString(),
            OnClick = new(LangId.Menu_MnuAutoZoom, API.IG_SetZoomMode, nameof(ZoomMode.AutoZoom)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.LockZoom)}",
            Image = nameof(IgThemeIcon.LockZoom),
            Text = Lang.KeysMap[LangId.Menu_MnuLockZoom],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.LockZoom.ToString(),
            OnClick = new(LangId.Menu_MnuLockZoom, API.IG_SetZoomMode, nameof(ZoomMode.LockZoom)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToWidth)}",
            Image = nameof(IgThemeIcon.ScaleToWidth),
            Text = Lang.KeysMap[LangId.Menu_MnuScaleToWidth],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToWidth.ToString(),
            OnClick = new(LangId.Menu_MnuScaleToWidth, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToWidth)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToHeight)}",
            Image = nameof(IgThemeIcon.ScaleToHeight),
            Text = Lang.KeysMap[LangId.Menu_MnuScaleToHeight],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToHeight.ToString(),
            OnClick = new(LangId.Menu_MnuScaleToHeight, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToHeight)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFit)}",
            Image = nameof(IgThemeIcon.ScaleToFit),
            Text = Lang.KeysMap[LangId.Menu_MnuScaleToFit],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFit.ToString(),
            OnClick = new(LangId.Menu_MnuScaleToFit, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFit)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ScaleToFill)}",
            Image = nameof(IgThemeIcon.ScaleToFill),
            Text = Lang.KeysMap[LangId.Menu_MnuScaleToFill],
            ConfigBinding = nameof(Config.ZoomMode),
            ConfigBindingValue = ZoomMode.ScaleToFill.ToString(),
            OnClick = new(LangId.Menu_MnuScaleToFill, API.IG_SetZoomMode, nameof(ZoomMode.ScaleToFill)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ActualSize)}",
            Image = nameof(IgThemeIcon.ActualSize),
            Text = Lang.KeysMap[LangId.Menu_MnuActualSize],
            OnClick = new(LangId.Menu_MnuActualSize, API.IG_SetZoom, "1"),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ZoomIn)}",
            Image = nameof(IgThemeIcon.ZoomIn),
            Text = Lang.KeysMap[LangId.Menu_MnuZoomIn],
            OnClick = new(LangId.Menu_MnuZoomIn, API.IG_ZoomIn),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ZoomOut)}",
            Image = nameof(IgThemeIcon.ZoomOut),
            Text = Lang.KeysMap[LangId.Menu_MnuZoomOut],
            OnClick = new(LangId.Menu_MnuZoomOut, API.IG_ZoomOut),
        },

        // Image transforms
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateLeft)}",
            Image = nameof(IgThemeIcon.RotateLeft),
            Text = Lang.KeysMap[LangId.Menu_MnuRotateLeft],
            OnClick = new(LangId.Menu_MnuRotateLeft, API.IG_Rotate, nameof(RotateOption.Left)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.RotateRight)}",
            Image = nameof(IgThemeIcon.RotateRight),
            Text = Lang.KeysMap[LangId.Menu_MnuRotateRight],
            OnClick = new(LangId.Menu_MnuRotateRight, API.IG_Rotate, nameof(RotateOption.Right)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipHorz)}",
            Image = nameof(IgThemeIcon.FlipHorz),
            Text = Lang.KeysMap[LangId.Menu_MnuFlipHorizontal],
            OnClick = new(LangId.Menu_MnuFlipHorizontal, API.IG_FlipImage, nameof(FlipOptions.Horizontal)),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FlipVert)}",
            Image = nameof(IgThemeIcon.FlipVert),
            Text = Lang.KeysMap[LangId.Menu_MnuFlipVertical],
            OnClick = new(LangId.Menu_MnuFlipVertical, API.IG_FlipImage, nameof(FlipOptions.Vertical)),
        },

        // Tools
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.ColorPicker)}",
            Image = nameof(IgThemeIcon.ColorPicker),
            Text = Lang.KeysMap[LangId.Menu_MnuColorPicker],
            OnClick = new(LangId.Menu_MnuColorPicker, API.IG_ToggleTool, ColorPickerToolControl.TOOL_ID),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Crop)}",
            Image = nameof(IgThemeIcon.Crop),
            Text = Lang.KeysMap[LangId.Menu_MnuCropTool],
            OnClick = new(LangId.Menu_MnuCropTool, API.IG_ToggleTool, CropImageToolControl.TOOL_ID),
        },

        // View modes
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Refresh)}",
            Image = nameof(IgThemeIcon.Refresh),
            Text = Lang.KeysMap[LangId.Menu_MnuRefresh],
            OnClick = new(LangId.Menu_MnuRefresh, API.IG_Refresh),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Gallery)}",
            Image = nameof(IgThemeIcon.Gallery),
            Text = Lang.KeysMap[LangId.Menu_MnuToggleGallery],
            ConfigBinding = nameof(Config.ShowGallery),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuToggleGallery, API.IG_ToggleGallery),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Checkerboard)}",
            Image = nameof(IgThemeIcon.Checkerboard),
            Text = Lang.KeysMap[LangId.Menu_MnuToggleCheckerboard],
            ConfigBinding = nameof(Config.CheckerboardMode),
            ConfigBindingValue = $"!{nameof(CheckerboardType.None)}",
            OnClick = new(LangId.Menu_MnuToggleCheckerboard, API.IG_ToggleCheckerboard),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.WindowFit)}",
            Image = nameof(IgThemeIcon.WindowFit),
            Text = Lang.KeysMap[LangId.Menu_MnuWindowFit],
            ConfigBinding = nameof(Config.EnableWindowFit),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuWindowFit, API.IG_ToggleWindowFit),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.FullScreen)}",
            Image = nameof(IgThemeIcon.FullScreen),
            Text = Lang.KeysMap[LangId.Menu_MnuFullScreen],
            ConfigBinding = nameof(Config.EnableFullScreen),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuFullScreen, API.IG_ToggleFullScreen),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Slideshow)}",
            Image = nameof(IgThemeIcon.Slideshow),
            Text = Lang.KeysMap[LangId.Menu_MnuSlideshow],
            ConfigBinding = nameof(Config.EnableSlideshow),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuSlideshow, API.IG_ToggleSlideshow),
        },
        new() {
            Id = $"Btn_{nameof(IgThemeIcon.Exit)}",
            Image = nameof(IgThemeIcon.Exit),
            Text = Lang.KeysMap[LangId.Menu_MnuExit],
            OnClick = new(LangId.Menu_MnuExit, API.IG_Exit),
        },
        ToolbarItemModel.Separator,

        // color channel R
        new() {
            Id = "Btn_ColorChannelR",
            Image = nameof(IgThemeIcon.ColorChannelR),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelR],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelR),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelR, API.IG_ToggleColorChannel, nameof(ColorChannels.R)),
            OnRightClick = new(LangId.Menu_MnuColorChannelR, API.IG_SetSingleColorChannel, nameof(ColorChannels.R)),
        },
        new() {
            Id = "Btn_ColorChannelG",
            Image = nameof(IgThemeIcon.ColorChannelG),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelG],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelG),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelG, API.IG_ToggleColorChannel, nameof(ColorChannels.G)),
            OnRightClick = new(LangId.Menu_MnuColorChannelG, API.IG_SetSingleColorChannel, nameof(ColorChannels.G)),
        },
        new() {
            Id = "Btn_ColorChannelB",
            Image = nameof(IgThemeIcon.ColorChannelB),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelB],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelB),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelB, API.IG_ToggleColorChannel, nameof(ColorChannels.B)),
            OnRightClick = new(LangId.Menu_MnuColorChannelB, API.IG_SetSingleColorChannel, nameof(ColorChannels.B)),
        },
        new() {
            Id = "Btn_ColorChannelA",
            Image = nameof(IgThemeIcon.ColorChannelA),
            Text = Lang.KeysMap[LangId.Menu_MnuColorChannelA],
            ShowText = true,
            ConfigBinding = nameof(Config.ColorChannelA),
            ConfigBindingValue = "True",
            OnClick = new(LangId.Menu_MnuColorChannelA, API.IG_ToggleColorChannel, nameof(ColorChannels.A)),
            OnRightClick = new(LangId.Menu_MnuColorChannelA, API.IG_SetSingleColorChannel, nameof(ColorChannels.A)),
        }
    ];

    #endregion // Public static properties



    // Public static methods
    #region Public static methods

    /// <summary>
    /// Loads and merges configs from multiple sources.
    /// Priority (lowest -> highest):
    /// developer defaults -> igconfig.default.json -> igconfig.json -> CLI args -> igconfig.admin.json.
    /// </summary>
    public static Config Load(string configFileName, string[]? cliArgs = null)
    {
        Config? appConfig = null;

        try
        {
            var jsonOptions = BHelper.CreateJsonOptions();
            var jsonContext = new ConfigJsonContext(jsonOptions);

            if (Const.ENABLE_CONFIG_MERGE)
            {
                // 1. read igconfig.default.json (Startup Dir, then Config Dir fallback)
                using var defaultDoc = ReadConfigJsonDocument(
                    BHelper.BaseDir(CONFIG_DEFAULT),
                    BHelper.ConfigDir(CONFIG_DEFAULT));

                // 2. read igconfig.json (Config Dir only)
                var userConfigPath = BHelper.ConfigDir(configFileName);
                using var userDoc = BHelper.ReadJsonDocFromFile(userConfigPath);

                // 3. parse CLI -p: args
                var cliOverrides = ParseCliConfigArgs(cliArgs);

                // 4. read igconfig.admin.json (install BaseDir ONLY; a ConfigDir fallback would let
                // a user drop an admin config in AppData and seize top precedence)
                using var adminDoc = ReadConfigJsonDocument(BHelper.BaseDir(CONFIG_ADMIN));

                // 5. merge all layers into a single JSON byte array
                var mergedJson = MergeJsonLayers(defaultDoc, userDoc, cliOverrides, adminDoc);

                // 6. deserialize the merged JSON into Config
                var config = JsonSerializer.Deserialize(mergedJson, jsonContext.Config)
                    ?? throw new FileLoadException("IGE: Could not parse merged config.");

                // 7. migrate if config version changed
                appConfig = MigrateUserConfigFile(config);
            }
            else
            {
                // simple single-file load (no merge)
                var configPath = BHelper.ConfigDir(configFileName);

                if (File.Exists(configPath))
                {
                    var config = BHelper.ReadJsonFromFile(configPath, jsonContext.Config)
                        ?? throw new FileLoadException($"IGE: Could not parse settings from file: {configPath}");

                    appConfig = MigrateUserConfigFile(config);
                }
            }
        }
        catch (Exception ex)
        {
            LoadingException = ex;
        }

        appConfig ??= new();
        return appConfig;
    }


    /// <summary>
    /// Applies CLI config overrides (<c>-p:Key=Value</c>) to the current config instance.
    /// Used when the first instance receives forwarded args from a second instance.
    /// </summary>
    public static void ApplyCliOverrides(Config config, string[]? cliArgs)
    {
        if (!Const.ENABLE_CONFIG_MERGE) return;

        var overrides = ParseCliConfigArgs(cliArgs);
        if (overrides.Count == 0) return;

        try
        {
            var jsonOptions = BHelper.CreateJsonOptions();
            var jsonContext = new ConfigJsonContext(jsonOptions);

            // serialize current config to JSON
            var currentJson = JsonSerializer.SerializeToUtf8Bytes(config, jsonContext.Config);
            using var currentDoc = JsonDocument.Parse(currentJson);

            // merge CLI overrides on top
            var mergedJson = MergeJsonLayers(null, currentDoc, overrides, null);

            // deserialize back into a new Config
            var updated = JsonSerializer.Deserialize(mergedJson, jsonContext.Config);
            if (updated == null) return;

            // copy all values from the updated config
            foreach (var kvp in updated._values)
            {
                config.Set(kvp.Key, kvp.Value);
            }
        }
        catch { }
    }


    /// <summary>
    /// Migrates user config file.
    /// </summary>
    private static Config MigrateUserConfigFile(Config config)
    {
        var configVersion = config._Metadata.Version;

        // update config version
        config._Metadata.Version = SPEC_VERSION;

        // no change
        if (SPEC_VERSION <= configVersion) return config;


        // Migration v9 to v10
        if (configVersion < 10)
        {
            // ShowCheckerboard + ShowCheckerboardOnlyImageRegion: merged into CheckerboardMode
            // ZoomLevels: change type: number[] to string
        }

        return config;
    }


    /// <summary>
    /// Gets the user theme-packs folder (in the Config dir), where installed theme packs live.
    /// </summary>
    [JsonIgnore]
    public static string ThemePacksDir => BHelper.ConfigDir(Dir.Themes);


    /// <summary>
    /// Loads every installed theme pack: built-in packs from the app base dir and user packs from
    /// the Config dir. Packs are de-duplicated by folder name (a user pack shadows a built-in one of
    /// the same name); invalid packs are skipped. The result is sorted by display name.
    /// </summary>
    public static async Task<List<IgTheme>> LoadAllThemePacksAsync()
    {
        var found = new Dictionary<string, IgTheme>(StringComparer.OrdinalIgnoreCase);

        // base dir (built-in) first, then Config dir (user) so user packs win on a name clash
        foreach (var rootDir in new[] { BHelper.BaseDir(Dir.Themes), BHelper.ConfigDir(Dir.Themes) })
        {
            if (!Directory.Exists(rootDir)) continue;

            foreach (var themeDir in Directory.EnumerateDirectories(rootDir))
            {
                var th = await new IgTheme().LoadAsync(themeDir).ConfigureAwait(false);
                if (th.IsValid) found[th.FolderName] = th;
            }
        }

        return found.Values
            .OrderBy(t => t.Info.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }


    /// <summary>
    /// Whether a theme pack is a built-in (shipped under the app base dir) and therefore not removable.
    /// </summary>
    public static bool IsBuiltInThemePack(IgTheme theme)
    {
        var baseThemesDir = BHelper.BaseDir(Dir.Themes);
        return theme.FolderPath.StartsWith(baseThemesDir, StringComparison.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Installs theme packs from the given <c>.igtheme</c> (zip) files into the user theme folder.
    /// Returns the number of packs extracted successfully.
    /// </summary>
    public static async Task<int> InstallThemePacksAsync(IEnumerable<string> igThemeFilePaths)
    {
        var destDir = ThemePacksDir;
        Directory.CreateDirectory(destDir);

        return await Task.Run(() =>
        {
            var count = 0;
            foreach (var file in igThemeFilePaths)
            {
                if (!File.Exists(file)) continue;
                try
                {
                    ZipFile.ExtractToDirectory(file, destDir, overwriteFiles: true);
                    count++;
                }
                catch { }
            }

            return count;
        }).ConfigureAwait(false);
    }


    /// <summary>
    /// Removes a user-installed theme pack folder. Built-in packs cannot be removed.
    /// Returns <c>true</c> when the pack folder was deleted.
    /// </summary>
    public static bool UninstallThemePack(IgTheme theme)
    {
        if (IsBuiltInThemePack(theme)) return false;
        if (string.IsNullOrEmpty(theme.FolderPath) || !Directory.Exists(theme.FolderPath)) return false;

        try
        {
            Directory.Delete(theme.FolderPath, true);
            return true;
        }
        catch { }

        return false;
    }


    #endregion // Public static methods



    // Public methods
    #region Public methods


    /// <summary>
    /// Writes configs to file.
    /// </summary>
    public async Task SaveAsync()
    {
        var jsonFilePath = BHelper.ConfigDir(CONFIG_USER);
        var jsonOptions = BHelper.CreateJsonOptions();
        var jsonContext = new ConfigJsonContext(jsonOptions);

        await BHelper.WriteJsonToFileAsync(jsonFilePath, this, jsonContext.Config);
    }


    /// <summary>
    /// Loads app language <see cref="Config.Lang"/>.
    /// </summary>
    public async Task LoadCurrentLanguageAsync()
    {
        var langPath = Lang.ResolveFilePath(Language);
        var lang = new Lang(langPath);

        // load language pack
        await lang.LoadAsync();

        // set app language
        Core.Lang = lang;
    }


    /// <summary>
    /// Loads theme pack <see cref="Config.Theme"/>.
    /// </summary>
    /// <param name="darkMode">
    /// Determine which theme should be loaded: <see cref="DarkTheme"/> or <see cref="LightTheme"/>.
    /// </param>
    /// <param name="useFallBackTheme">
    /// If theme pack is invalid, should load the default theme pack <see cref="Const.DEFAULT_THEME"/>.
    /// </param>
    /// <param name="throwIfThemeInvalid">
    /// If theme pack is invalid, should throw exception.
    /// </param>
    /// <param name="forceUpdateBackground">Force updating background according to theme value</param>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> LoadCurrentThemeAsync(bool darkMode,
        bool useFallBackTheme, bool throwIfThemeInvalid, bool forceUpdateBackground)
    {
        // 1. get the theme folder name
        var themeFolderName = darkMode ? DarkTheme : LightTheme;
        if (string.IsNullOrEmpty(themeFolderName))
        {
            themeFolderName = Const.DEFAULT_THEME;
        }

        // 2. check if theme pack is already loaded
        if (themeFolderName.Equals(Core.Theme.FolderName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 3. load theme pack
        var th = await FindAndLoadThemePackAsync(themeFolderName, useFallBackTheme, throwIfThemeInvalid);

        // 4. update the name of dark/light theme
        if (darkMode) DarkTheme = th.FolderName;
        else LightTheme = th.FolderName;


        // 5. follow the theme's bg unless the user set a custom one. "Not custom" = empty, or it
        //    matches a theme it could have been following: the previous/active theme, or — after an
        //    OS-mode switch across restarts — the other mode's configured theme.
        var currentBg = BHelper.ColorFromHex(BackgroundColor);
        var isFollowingTheme = currentBg.IsEmpty
            || currentBg == BHelper.ColorFromHex(Core.Theme.Colors.BgColor)
            || currentBg == BHelper.ColorFromHex(th.Colors.BgColor);

        // not matched? at startup Core.Theme is a placeholder, so the value may have been following
        // the other OS-mode's theme from the previous session — check that pack's bg too
        if (!isFollowingTheme && !forceUpdateBackground)
        {
            var otherName = darkMode ? LightTheme : DarkTheme;
            if (!string.IsNullOrEmpty(otherName)
                && !otherName.Equals(th.FolderName, StringComparison.OrdinalIgnoreCase))
            {
                var other = await FindAndLoadThemePackAsync(otherName, useFallBackTheme: false,
                    throwIfThemeInvalid: false);
                isFollowingTheme = other.IsValid && currentBg == BHelper.ColorFromHex(other.Colors.BgColor);
            }
        }

        if (isFollowingTheme || forceUpdateBackground)
        {
            BackgroundColor = th.Colors.BgColor;
        }


        // 6. set to the current theme
        var success = Core.SetTheme(th);
        return success;
    }


    /// <summary>
    /// Finds the correct location of theme name and loads it.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private static async Task<IgTheme> FindAndLoadThemePackAsync(string themeFolderName,
        bool useFallBackTheme, bool throwIfThemeInvalid)
    {
        // 1. look for theme pack in the Config dir
        var themeConfigPath = BHelper.ConfigDir(Dir.Themes, themeFolderName);
        var th = await new IgTheme().LoadAsync(themeConfigPath);

        if (!th.IsValid)
        {
            // 2. look for theme pack in the base dir
            var baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, themeFolderName);
            th = await new IgTheme().LoadAsync(baseThemeConfigPath);

            // 3. cannot find theme, use fall back theme
            if (!th.IsValid && useFallBackTheme)
            {
                // 4. load default theme
                baseThemeConfigPath = BHelper.BaseDir(Dir.Themes, Const.DEFAULT_THEME);
                th = await new IgTheme().LoadAsync(baseThemeConfigPath);
            }
        }

        // 5. throw error if theme is invalid
        if (!th.IsValid && throwIfThemeInvalid)
        {
            throw new ArgumentException($"IGE: Unable to load '{themeFolderName}' theme pack. " +
                $"Please make sure '{themeConfigPath}' file is valid.", nameof(themeFolderName));
        }

        return th;
    }


    /// <summary>
    /// Gets control layout position.
    /// </summary>
    public static LayoutPosition GetControlLayout(LayoutControl control)
    {
        var defaultPos = control == LayoutControl.Toolbar
            ? LayoutPosition.Top
            : LayoutPosition.Bottom;


        // 1. read control's layouts from setting
        var pos = Core.Config.Layout.GetValueOrDefault(control, defaultPos);


        // 2. standardize toolbar position
        if (control == LayoutControl.Toolbar)
        {
            if (pos is LayoutPosition.Left or LayoutPosition.Right)
            {
                pos = LayoutPosition.Top;
            }
        }

        return pos;
    }


    #endregion // Public methods



    // Private static methods (config merge)
    #region Private static methods (config merge)

    /// <summary>
    /// Security-sensitive keys blocked from the CLI <c>-p:</c> layer (file-only).
    /// </summary>
    private static readonly HashSet<string> _cliBlockedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Tools),
        nameof(PluginTrust),
    };


    /// <summary>
    /// Reads a JSON config file: primary path first, then an optional fallback path.
    /// Returns <c>null</c> if neither exists.
    /// </summary>
    private static JsonDocument? ReadConfigJsonDocument(string primaryPath, string? fallbackPath = null)
    {
        var path = File.Exists(primaryPath) ? primaryPath
            : (!string.IsNullOrEmpty(fallbackPath) && File.Exists(fallbackPath)) ? fallbackPath
            : null;

        if (string.IsNullOrEmpty(path)) return null;

        return BHelper.ReadJsonDocFromFile(path);
    }


    /// <summary>
    /// Parses CLI arguments with <see cref="Const.CONFIG_CMD_PREFIX"/> prefix
    /// into a dictionary of property-name -> raw-JSON-value pairs.
    /// Example: <c>-p:ShowGallery=true</c> -> <c>{ "ShowGallery": "true" }</c>.
    /// </summary>
    private static Dictionary<string, string> ParseCliConfigArgs(string[]? args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (args == null) return result;

        foreach (var arg in args)
        {
            if (!arg.StartsWith(Const.CONFIG_CMD_PREFIX, StringComparison.OrdinalIgnoreCase))
                continue;

            // strip prefix, e.g. "-p:ShowGallery=true" -> "ShowGallery=true"
            var kvPart = arg[Const.CONFIG_CMD_PREFIX.Length..];
            var eqIdx = kvPart.IndexOf('=');
            if (eqIdx <= 0) continue;

            var key = kvPart[..eqIdx].Trim();
            var value = kvPart[(eqIdx + 1)..].Trim();
            if (key.Length == 0) continue;

            // Security: never accept security-sensitive keys from the command line.
            if (_cliBlockedKeys.Contains(key))
            {
                System.Diagnostics.Debug.WriteLine($"[Config] CLI override for '{key}' ignored (not allowed from the command line).");
                continue;
            }

            result[key] = value;
        }

        return result;
    }


    /// <summary>
    /// Merges multiple JSON config layers into a single UTF-8 byte array.
    /// Later layers override earlier ones at the top-level property level (shallow merge).
    /// CLI overrides are written as raw JSON values.
    /// </summary>
    private static byte[] MergeJsonLayers(
        JsonDocument? defaultDoc,
        JsonDocument? userDoc,
        Dictionary<string, string> cliOverrides,
        JsonDocument? adminDoc)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false,
        });

        writer.WriteStartObject();

        // index each layer once for O(1) case-insensitive lookups (was O(n^2): a linear
        // EnumerateObject scan per key, up to 4x per key across the layers)
        var defaultMap = IndexProperties(defaultDoc);
        var userMap = IndexProperties(userDoc);
        var adminMap = IndexProperties(adminDoc);

        // collect all property names across all layers
        var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(defaultMap.Keys);
        allKeys.UnionWith(userMap.Keys);
        foreach (var k in cliOverrides.Keys) allKeys.Add(k);
        allKeys.UnionWith(adminMap.Keys);

        foreach (var key in allKeys)
        {
            // admin > CLI > user > default (last wins)
            if (adminMap.TryGetValue(key, out var adminVal))
            {
                writer.WritePropertyName(key);
                adminVal.WriteTo(writer);
            }
            else if (cliOverrides.TryGetValue(key, out var cliRaw))
            {
                WriteCliValue(writer, key, cliRaw);
            }
            else if (userMap.TryGetValue(key, out var userVal))
            {
                writer.WritePropertyName(key);
                userVal.WriteTo(writer);
            }
            else if (defaultMap.TryGetValue(key, out var defVal))
            {
                writer.WritePropertyName(key);
                defVal.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }


    /// <summary>
    /// Indexes a <see cref="JsonDocument"/>'s top-level properties into a case-insensitive map.
    /// First occurrence wins, matching the previous first-match lookup behavior.
    /// </summary>
    private static Dictionary<string, JsonElement> IndexProperties(JsonDocument? doc)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (doc == null) return map;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            map.TryAdd(prop.Name, prop.Value);
        }

        return map;
    }


    /// <summary>
    /// Writes a CLI override value as a JSON property.
    /// Attempts to parse the value as JSON first; falls back to writing as a string.
    /// </summary>
    private static void WriteCliValue(Utf8JsonWriter writer, string key, string rawValue)
    {
        writer.WritePropertyName(key);

        // try parsing as valid JSON (handles true, false, null, numbers, arrays, objects)
        try
        {
            using var doc = JsonDocument.Parse(rawValue);
            doc.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            // not valid JSON, write as a quoted string
            writer.WriteStringValue(rawValue);
        }
    }

    #endregion // Private static methods (config merge)


}
