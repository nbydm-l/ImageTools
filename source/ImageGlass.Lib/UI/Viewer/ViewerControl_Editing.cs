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
using Avalonia;
using Avalonia.Media;
using ImageGlass.Common;
using ImageGlass.Common.Extensions;
using ImageGlass.Common.Photoing;
using ImageGlass.Common.Types;
using SkiaSharp;

namespace ImageGlass.UI.Viewer;

public partial class ViewerControl
{

    #region Control Methods

    /// <summary>
    /// Gets a rendered bitmap of the current image or the selected region.
    /// </summary>
    public SKBitmap? GetRenderedBitmap(bool selectionOnly = false)
    {
        SKImageRef.ImageLease? imgLease = null;
        Rect selectionRect;

        try
        {
            lock (_lock)
            {
                var imageRef = _imgRender ?? _imgSource;
                if (imageRef is null) return null;

                // Acquire a lease to keep the image alive while we copy pixels.
                imgLease = imageRef.Acquire();
                var leaseImage = imgLease?.Image;
                if (leaseImage is null || leaseImage.IsDisposed()) return null;
                if (selectionOnly && SourceSelection.IsEmpty) return null;

                // Determine the source rectangle to copy (in source image coords).
                selectionRect = selectionOnly
                    ? SourceSelection.Normalize()
                    : new Rect(0, 0, leaseImage.Width, leaseImage.Height);
            }

            // Validate the leased image again after exiting the lock.
            var img = imgLease?.Image;
            if (img is null || img.IsDisposed()) return null;

            // Intersect selection with actual image bounds to avoid out-of-range
            // reads and to handle partially out-of-bounds selections.
            var bounds = new Rect(0, 0, img.Width, img.Height);
            selectionRect = selectionRect.GetIntersection(bounds);
            if (selectionRect.IsEmpty) return null;

            // prepare output bitmap
            var rect = selectionRect.ToSKRectI();
            var info = new SKImageInfo(rect.Width, rect.Height, img.ColorType, img.AlphaType, img.ColorSpace);
            var bmpOutput = new SKBitmap(info);

            // copy the image pixels to the output bitmap
            if (!img.ReadPixels(info, bmpOutput.GetPixels(), bmpOutput.RowBytes, rect.Left, rect.Top))
            {
                bmpOutput.Dispose();
                return null;
            }

            return bmpOutput;
        }
        finally
        {
            imgLease?.Dispose();
        }
    }


    /// <summary>
    /// Gets the color of the pixel at the specified coordinates from the image source.
    /// </summary>
    /// <returns>
    /// Empty color (<c>#0000</c>) if the photo source is none.
    /// </returns>
    public Color GetColorAt(int x, int y)
    {
        SKImageRef.ImageLease? imgLease = null;

        try
        {
            lock (_lock)
            {
                var imageRef = _imgRender ?? _imgSource;
                if (imageRef is null) return Const.COLOR_EMPTY;

                imgLease = imageRef.Acquire();
            }

            var img = imgLease?.Image;
            if (img.IsDisposed()) return Const.COLOR_EMPTY;

            if (x < 0 || x >= img.Width || y < 0 || y >= img.Height)
                return Const.COLOR_EMPTY;

            // read a single pixel using a 1x1 bitmap to avoid allocating a full copy
            var info = new SKImageInfo(1, 1, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            using var pixel = new SKBitmap(info);
            if (!img.ReadPixels(info, pixel.GetPixels(), info.RowBytes, x, y))
                return Const.COLOR_EMPTY;

            var skColor = pixel.GetPixel(0, 0);
            return new Color(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);
        }
        finally
        {
            imgLease?.Dispose();
        }
    }


    /// <summary>
    /// Attempts to apply HDR tone mapping and/or the destination Skia color profile
    /// to the current photo.
    /// </summary>
    private bool TryApplySkiaColorSpace(SKImage? srcImage, out SKImage? output)
    {
        output = null;
        if (srcImage.IsDisposed()) return false;

        // 1. HDR tone mapping (applies regardless of color profile setting)
        if (Core.Config.EnableHdrToneMapping && Photo?.Metadata?.IsHdr == true)
        {
            // Tone-map to standard sRGB (no monitor profile yet).
            // The monitor color profile will be applied below via TryApplyColorSpace,
            // same as SDR images, for consistent color handling.
            var toneMapped = HdrToneMapper.ToneMapToSdr(srcImage,
                Photo.Metadata.HdrTransferFn, Core.HdrToneMappingConfig);

            if (!toneMapped.IsDisposed())
            {
                // Apply monitor color profile to the tone-mapped SDR image
                if (CanApplySkiaColorSpace()
                    && SkiaCodec.TryApplyColorSpace(toneMapped, Core.DestColorProfile, out var profiled))
                {
                    toneMapped.Dispose();
                    output = profiled;
                }
                else
                {
                    output = toneMapped;
                }

                return true;
            }
        }


        // 2. apply new color space for source image
        if (!CanApplySkiaColorSpace()) return false;

        if (SkiaCodec.TryApplyColorSpace(srcImage, Core.DestColorProfile, out var imgFrameColored))
        {
            output = imgFrameColored;
            return true;
        }

        return false;
    }


    /// <summary>
    /// Checks if Skia color space profile can be applied to the current photo.
    /// </summary>
    private bool CanApplySkiaColorSpace()
    {
        // 1. check if the destination profile is supported
        if (!Core.IsDestColorProfileSupported) return false;

        // 2. check user configs
        if (Core.Config.EnableAlwaysApplyColorProfile || Photo?.Metadata?.SkiaColorSpace is not null)
        {
            return true;
        }

        return false;
    }


    /// <summary>
    /// Inverts image colors.
    /// </summary>
    public bool InvertColor(bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated/vector images or when there is no source
            if (_animator is not null || IsVectorSource()) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var invertedImage = SkiaCodec.InvertImageColors(srcImage);
            if (invertedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, invertedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            IsColorInverted = !IsColorInverted;
        }


        // render the transformation
        if (requestRerender)
        {
            Refresh(resetZoom: false);
        }

        return true;
    }


    /// <summary>
    /// Rotates the image.
    /// </summary>
    public bool RotateImage(double degree, bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated images or when there is no source
            if (_animator is not null || IsVectorSource()) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var rotatedImage = SkiaCodec.RotateImage(srcImage, degree);
            if (rotatedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, rotatedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;

            // update source size
            BitmapSize = new(rotatedImage.Width, rotatedImage.Height);
        }

        // render the transformation
        if (requestRerender)
        {
            Refresh();
        }

        return true;
    }


    /// <summary>
    /// Flips the image.
    /// </summary>
    public bool FlipImage(FlipOptions options, bool requestRerender = true)
    {
        lock (_lock)
        {
            // do nothing for animated images or when there is no source
            if (_animator is not null || IsVectorSource()) return false;

            var srcImage = (_imgRender ?? _imgSource)?.Image;
            var flippedImage = SkiaCodec.FlipImage(srcImage, options);
            if (flippedImage.IsDisposed()) return false;

            // update the render cache, keep _imgSource intact
            SKImageRef.Set(ref _imgRender, flippedImage);
            _mipmapCache?.Dispose();
            _mipmapCache = null;
        }

        // render the transformation
        if (requestRerender)
        {
            Refresh(resetZoom: false);
        }

        return true;
    }


    /// <summary>
    /// Filters image color channels.
    /// </summary>
    public bool FilterColorChannels(ColorChannels colors, bool requestRerender = true)
    {
        lock (_lock)
        {
            // 1. do nothing for animated/vector images or when there is no source
            if (_animator is not null || IsVectorSource()) return false;

            var srcImage = _imgSource?.Image;
            if (srcImage.IsDisposed()) return false;


            // 2. reset render cache to start from original source
            SKImageRef.Set(ref _imgRender, null);
            _mipmapCache?.Dispose();
            _mipmapCache = null;
            _loadingOptions.Channels = colors;

            SKImage? straightSrc = null;
            try
            {
                straightSrc = SkiaCodec.ToStraightUnpremulImage(srcImage);
                var filterSource = straightSrc ?? srcImage;

                // 3. skip filtering when all channels (RGBA) are selected
                if (!colors.HasFlag(ColorChannels.RGBA))
                {
                    var filteredImage = SkiaCodec.FilterImageColorChannels(filterSource, colors);
                    if (filteredImage.IsDisposed()) return false;

                    SKImageRef.Set(ref _imgRender, filteredImage);
                }
            }
            finally
            {
                if (straightSrc is not null && !ReferenceEquals(straightSrc, srcImage))
                    straightSrc.Dispose();
            }
        }


        // 4. render the transformation
        if (requestRerender)
        {
            Refresh(false);
        }

        return true;
    }


    #endregion // Control Methods


}
