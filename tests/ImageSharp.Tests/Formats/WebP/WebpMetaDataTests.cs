// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

// ReSharper disable InconsistentNaming
namespace SixLabors.ImageSharp.Tests.Formats.Webp
{
    [Trait("Format", "Webp")]
    public class WebpMetaDataTests
    {
        private static WebpDecoder WebpDecoder => new() { IgnoreMetadata = false };

        [Theory]
        [WithFile(TestImages.Webp.Lossy.BikeWithExif, PixelTypes.Rgba32, false)]
        [WithFile(TestImages.Webp.Lossy.BikeWithExif, PixelTypes.Rgba32, true)]
        public void IgnoreMetadata_ControlsWhetherExifIsParsed_WithLossyImage<TPixel>(TestImageProvider<TPixel> provider, bool ignoreMetadata)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var decoder = new WebpDecoder { IgnoreMetadata = ignoreMetadata };

            using Image<TPixel> image = provider.GetImage(decoder);
            if (ignoreMetadata)
            {
                Assert.Null(image.Metadata.ExifProfile);
            }
            else
            {
                ExifProfile exifProfile = image.Metadata.ExifProfile;
                Assert.NotNull(exifProfile);
                Assert.NotEmpty(exifProfile.Values);
                Assert.Contains(exifProfile.Values, m => m.Tag.Equals(ExifTag.Software) && m.GetValue().Equals("GIMP 2.10.2"));
            }
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossless.WithExif, PixelTypes.Rgba32, false)]
        [WithFile(TestImages.Webp.Lossless.WithExif, PixelTypes.Rgba32, true)]
        public void IgnoreMetadata_ControlsWhetherExifIsParsed_WithLosslessImage<TPixel>(TestImageProvider<TPixel> provider, bool ignoreMetadata)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var decoder = new WebpDecoder { IgnoreMetadata = ignoreMetadata };

            using Image<TPixel> image = provider.GetImage(decoder);
            if (ignoreMetadata)
            {
                Assert.Null(image.Metadata.ExifProfile);
            }
            else
            {
                ExifProfile exifProfile = image.Metadata.ExifProfile;
                Assert.NotNull(exifProfile);
                Assert.NotEmpty(exifProfile.Values);
                Assert.Contains(exifProfile.Values, m => m.Tag.Equals(ExifTag.Make) && m.GetValue().Equals("Canon"));
                Assert.Contains(exifProfile.Values, m => m.Tag.Equals(ExifTag.Model) && m.GetValue().Equals("Canon PowerShot S40"));
                Assert.Contains(exifProfile.Values, m => m.Tag.Equals(ExifTag.Software) && m.GetValue().Equals("GIMP 2.10.2"));
            }
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossy.WithIccp, PixelTypes.Rgba32, false)]
        [WithFile(TestImages.Webp.Lossy.WithIccp, PixelTypes.Rgba32, true)]
        [WithFile(TestImages.Webp.Lossless.WithIccp, PixelTypes.Rgba32, false)]
        [WithFile(TestImages.Webp.Lossless.WithIccp, PixelTypes.Rgba32, true)]
        public void IgnoreMetadata_ControlsWhetherIccpIsParsed<TPixel>(TestImageProvider<TPixel> provider, bool ignoreMetadata)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var decoder = new WebpDecoder { IgnoreMetadata = ignoreMetadata };

            using Image<TPixel> image = provider.GetImage(decoder);
            if (ignoreMetadata)
            {
                Assert.Null(image.Metadata.IccProfile);
            }
            else
            {
                Assert.NotNull(image.Metadata.IccProfile);
                Assert.NotEmpty(image.Metadata.IccProfile.Entries);
            }
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossy.WithXmp, PixelTypes.Rgba32, false)]
        [WithFile(TestImages.Webp.Lossy.WithXmp, PixelTypes.Rgba32, true)]
        public async Task IgnoreMetadata_ControlsWhetherXmpIsParsed<TPixel>(TestImageProvider<TPixel> provider, bool ignoreMetadata)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            var decoder = new WebpDecoder { IgnoreMetadata = ignoreMetadata };

            using Image<TPixel> image = await provider.GetImageAsync(decoder);
            if (ignoreMetadata)
            {
                Assert.Null(image.Metadata.XmpProfile);
            }
            else
            {
                Assert.NotNull(image.Metadata.XmpProfile);
                Assert.NotEmpty(image.Metadata.XmpProfile.Data);
            }
        }

        [Theory]
        [InlineData(WebpFileFormatType.Lossy)]
        [InlineData(WebpFileFormatType.Lossless)]
        public void Encode_WritesExifWithPadding(WebpFileFormatType fileFormatType)
        {
            // arrange
            using var input = new Image<Rgba32>(25, 25);
            using var memoryStream = new MemoryStream();
            var expectedExif = new ExifProfile();
            string expectedSoftware = "ImageSharp";
            expectedExif.SetValue(ExifTag.Software, expectedSoftware);
            input.Metadata.ExifProfile = expectedExif;

            // act
            input.Save(memoryStream, new WebpEncoder() { FileFormat = fileFormatType });
            memoryStream.Position = 0;

            // assert
            using var image = Image.Load<Rgba32>(memoryStream);
            ExifProfile actualExif = image.Metadata.ExifProfile;
            Assert.NotNull(actualExif);
            Assert.Equal(expectedExif.Values.Count, actualExif.Values.Count);
            Assert.Equal(expectedSoftware, actualExif.GetValue(ExifTag.Software).Value);
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossy.BikeWithExif, PixelTypes.Rgba32)]
        public void EncodeLossyWebp_PreservesExif<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            // arrange
            using Image<TPixel> input = provider.GetImage(WebpDecoder);
            using var memoryStream = new MemoryStream();
            ExifProfile expectedExif = input.Metadata.ExifProfile;

            // act
            input.Save(memoryStream, new WebpEncoder() { FileFormat = WebpFileFormatType.Lossy });
            memoryStream.Position = 0;

            // assert
            using var image = Image.Load<Rgba32>(memoryStream);
            ExifProfile actualExif = image.Metadata.ExifProfile;
            Assert.NotNull(actualExif);
            Assert.Equal(expectedExif.Values.Count, actualExif.Values.Count);
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossless.WithExif, PixelTypes.Rgba32)]
        public void EncodeLosslessWebp_PreservesExif<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            // arrange
            using Image<TPixel> input = provider.GetImage(WebpDecoder);
            using var memoryStream = new MemoryStream();
            ExifProfile expectedExif = input.Metadata.ExifProfile;

            // act
            input.Save(memoryStream, new WebpEncoder() { FileFormat = WebpFileFormatType.Lossless });
            memoryStream.Position = 0;

            // assert
            using var image = Image.Load<Rgba32>(memoryStream);
            ExifProfile actualExif = image.Metadata.ExifProfile;
            Assert.NotNull(actualExif);
            Assert.Equal(expectedExif.Values.Count, actualExif.Values.Count);
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossy.WithIccp, PixelTypes.Rgba32, WebpFileFormatType.Lossless)]
        [WithFile(TestImages.Webp.Lossy.WithIccp, PixelTypes.Rgba32, WebpFileFormatType.Lossy)]
        public void Encode_PreservesColorProfile<TPixel>(TestImageProvider<TPixel> provider, WebpFileFormatType fileFormat)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            using (Image<TPixel> input = provider.GetImage(new WebpDecoder()))
            {
                ImageSharp.Metadata.Profiles.Icc.IccProfile expectedProfile = input.Metadata.IccProfile;
                byte[] expectedProfileBytes = expectedProfile.ToByteArray();

                using (var memStream = new MemoryStream())
                {
                    input.Save(memStream, new WebpEncoder()
                    {
                        FileFormat = fileFormat
                    });

                    memStream.Position = 0;
                    using (var output = Image.Load<Rgba32>(memStream))
                    {
                        ImageSharp.Metadata.Profiles.Icc.IccProfile actualProfile = output.Metadata.IccProfile;
                        byte[] actualProfileBytes = actualProfile.ToByteArray();

                        Assert.NotNull(actualProfile);
                        Assert.Equal(expectedProfileBytes, actualProfileBytes);
                    }
                }
            }
        }

        [Theory]
        [WithFile(TestImages.Webp.Lossy.WithExifNotEnoughData, PixelTypes.Rgba32)]
        public void WebpDecoder_IgnoresInvalidExifChunk<TPixel>(TestImageProvider<TPixel> provider)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            Exception ex = Record.Exception(() =>
            {
                using Image<TPixel> image = provider.GetImage();
            });
            Assert.Null(ex);
        }
    }
}
