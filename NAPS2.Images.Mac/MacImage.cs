﻿using NAPS2.Images.Bitwise;

namespace NAPS2.Images.Mac;

public class MacImage : IMemoryImage
{
    private readonly NSImage _image;
    internal readonly NSBitmapImageRep _imageRep;

    public MacImage(NSImage image)
    {
        _image = image;
        // TODO: Better error checking
        lock (MacImageContext.ConstructorLock)
        {
#if MONOMAC
            _imageRep = new NSBitmapImageRep(_image.Representations()[0].Handle, false);
#else
            _imageRep = (NSBitmapImageRep) _image.Representations()[0];
#endif
        }
        // TODO: Also verify color spaces.
        // TODO: How to handle samplesperpixel = 3 here?
        if (_imageRep.BitsPerPixel == 32 && _imageRep.BitsPerSample == 8) // && _imageRep.SamplesPerPixel == 4)
        {
            PixelFormat = ImagePixelFormat.ARGB32;
        }
        else if (_imageRep.BitsPerPixel == 24 && _imageRep.BitsPerSample == 8 && _imageRep.SamplesPerPixel == 3)
        {
            PixelFormat = ImagePixelFormat.RGB24;
        }
        else if (_imageRep.BitsPerPixel == 8 && _imageRep.BitsPerSample == 8 && _imageRep.SamplesPerPixel == 1)
        {
            PixelFormat = ImagePixelFormat.Gray8;
        }
        else if (_imageRep.BitsPerPixel == 1 && _imageRep.BitsPerSample == 1 && _imageRep.SamplesPerPixel == 1)
        {
            PixelFormat = ImagePixelFormat.BW1;
        }
        else
        {
            // TODO: Draw on a known format image
            // See https://stackoverflow.com/questions/52675655/how-to-keep-nsbitmapimagerep-from-creating-lots-of-intermediate-cgimages
            throw new Exception("Unexpected image representation");
        }
    }

    public void Dispose()
    {
        _image.Dispose();
        // TODO: Does this need to dispose the imageRep?
    }

    public int Width => (int) _imageRep.PixelsWide;
    public int Height => (int) _imageRep.PixelsHigh;
    public float HorizontalResolution => (float) _image.Size.Width / Width * 72;
    public float VerticalResolution => (float) _image.Size.Height / Height * 72;

    public void SetResolution(float xDpi, float yDpi)
    {
        // TODO: Image size or imagerep size?
        if (xDpi > 0 && yDpi > 0)
        {
            _image.Size = new CGSize(xDpi / 72 * Width, yDpi / 72 * Height);
        }
    }

    public ImagePixelFormat PixelFormat { get; }

    public ImageLockState Lock(LockMode lockMode, out BitwiseImageData imageData)
    {
        var ptr = _imageRep.BitmapData;
        var stride = (int) _imageRep.BytesPerRow;
        var subPixelType = PixelFormat switch
        {
            // TODO: Base subpixel type on _imageRep.BitmapFormat
            ImagePixelFormat.RGB24 => SubPixelType.Rgb,
            ImagePixelFormat.ARGB32 => SubPixelType.Rgba,
            ImagePixelFormat.Gray8 => SubPixelType.Gray,
            ImagePixelFormat.BW1 => SubPixelType.Bit,
            _ => throw new InvalidOperationException("Unsupported pixel format")
        };
        imageData = new BitwiseImageData(ptr, new PixelInfo(Width, Height, subPixelType, stride));
        return new MacImageLockState();
    }

    // TODO: Should we implement some kind of actual locking?
    public class MacImageLockState : ImageLockState
    {
        public override void Dispose()
        {
        }
    }

    public ImageFileFormat OriginalFileFormat { get; set; }

    public void Save(string path, ImageFileFormat imageFormat = ImageFileFormat.Unspecified, int quality = -1)
    {
        if (imageFormat == ImageFileFormat.Unspecified)
        {
            imageFormat = ImageContext.GetFileFormatFromExtension(path);
        }
        var rep = GetRepForSaving(imageFormat, quality);
        if (!rep.Save(path, false, out var error))
        {
            throw new IOException(error!.Description);
        }
    }

    public void Save(Stream stream, ImageFileFormat imageFormat, int quality = -1)
    {
        if (imageFormat == ImageFileFormat.Unspecified)
        {
            throw new ArgumentException("Format required to save to a stream", nameof(imageFormat));
        }
        var rep = GetRepForSaving(imageFormat, quality);
        rep.AsStream().CopyTo(stream);
    }

    private NSData GetRepForSaving(ImageFileFormat imageFormat, int quality)
    {
        lock (MacImageContext.ConstructorLock)
        {
            if (imageFormat == ImageFileFormat.Jpeg)
            {
                var props = quality == -1
                    ? null
                    : NSDictionary.FromObjectAndKey(NSNumber.FromDouble(quality / 100.0),
                        NSBitmapImageRep.CompressionFactor);
                return _imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, props);
            }
            if (imageFormat == ImageFileFormat.Png)
            {
                return _imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png, null);
            }
            if (imageFormat == ImageFileFormat.Bmp)
            {
                return _imageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Bmp, null);
            }
            // TODO: Do we need/want to handle tiff saving?
            throw new InvalidOperationException("Unsupported image format");
        }
    }

    public IMemoryImage Clone()
    {
        lock (MacImageContext.ConstructorLock)
        {
            if (PixelFormat == ImagePixelFormat.BW1)
            {
                // TODO: Trying to copy the NSImage seems to fail specifically for black and white images.
                // I'm not sure why.
                var image = new MacImageContext().Create(Width, Height, PixelFormat);
                new CopyBitwiseImageOp().Perform(this, image);
                return image;
            }

#if MONOMAC
            var nsImage = new NSImage(_image.Copy().Handle, true);
#else
            var nsImage = (NSImage) _image.Copy();
#endif
            return new MacImage(nsImage)
            {
                OriginalFileFormat = OriginalFileFormat
            };
        }
    }

    public IMemoryImage SafeClone()
    {
        return Clone();
    }
}