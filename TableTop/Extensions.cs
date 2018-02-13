using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableTop
{
    public static class Extensions
    {
        public static Object aLock = new object();

        /// <summary>
        /// Vectors the mult.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="mult">The mult.</param>
        /// <returns><see cref="SizeF"/></returns>
        public static SizeF VectorMult(SizeF size, float mult)
        {
            SizeF result = new SizeF(size.Width * mult, size.Height * mult);
            return result;
        }

        /// <summary>
        /// Vectors the mult.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <param name="mult">The mult.</param>
        /// <returns><see cref="RectangleF"/></returns>
        public static RectangleF VectorMult(RectangleF rect, float mult)
        {
            RectangleF result = new RectangleF(rect.Location.X * mult, rect.Location.Y * mult, rect.Width * mult, rect.Height * mult);
            return result;
        }

        public static void TileImageWithOpacity(this Graphics g, Image i, Size tileSize, int opacityPercent, Rectangle destinationRect = new Rectangle())
        {
            // So, new idea, or old idea that I didn't do right... 
            // We still set up the HD TexturedBrush, and then we apply it to a bitmap the size of the viewport, with offsets sent to the brush
            // Then we draw that bitmap ontop with opacity


            // Note the entire reason I have to do all this shit is because if I re-make a TexturedBrush every tick, shit gets slow
            // And there's no way to change the alpha on one without remaking it
            // Of course, I'm re-making the bitmap in resize... 
            float maxWidth = Core._FormMain.gridMaxSize * Core._FormMain.cellSize * Core._FormMain.zoomMult;
            float maxHeight = maxWidth; // We edit these if they specified their own destination
            // These are the highest values of X and Y we need to draw to, assuming they're within the destination

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            ImageAttributes imageAtt = new ImageAttributes();

            if (opacityPercent < 100)
            {
                float[][] matrixItems ={
               new float[] {1, 0, 0, 0, 0},
               new float[] {0, 1, 0, 0, 0},
               new float[] {0, 0, 1, 0, 0},
               new float[] {0, 0, 0, opacityPercent/100f, 0},
               new float[] {0, 0, 0, 0, 1}};
                ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

                imageAtt.SetColorMatrix(
                   colorMatrix,
                   ColorMatrixFlag.Default,
                   ColorAdjustType.Bitmap);
            }

            if (destinationRect.IsEmpty)
            {
                destinationRect = new Rectangle((int)Core._FormMain.Offsets.X, (int)Core._FormMain.Offsets.Y, (int)maxWidth, (int)maxHeight);
            }

            // Left this possible so I can be lazy and this means I need to tile across the entire thing
            // I have to tile these manually tho... dammit... 
            // I'm stupid.  I don't have to resize it, I just need to make my destRect the right size and it'll do it for me
            for (int y = destinationRect.Y; y < destinationRect.Height; y += tileSize.Height - 5)
            {
                for (int x = destinationRect.X; x < destinationRect.Width; x += tileSize.Width - 5)
                { // We've already handled everything with destinationRect, just draw to it with the x and y we're at
                  // But we can check if it's on-screen cuz don't draw it if not
                    if (x + tileSize.Width > 0 && x < Core._FormMain.Width && y + tileSize.Height > 0 && y < Core._FormMain.Height)
                        g.DrawImage(i, new Rectangle(x, y, tileSize.Width, tileSize.Height), // Destination Width and Height.  destinationRect is the full rect to fill, destination is each tile
                            0.0f, 0.0f, i.Width, i.Height, // Source Rect
                            GraphicsUnit.Pixel, imageAtt);
                }
            }
            imageAtt.Dispose();
        }

        public static void Scale(this TextureBrush t, float mult)
        {
            Matrix m = new Matrix();
            m.Scale(mult, mult);
            t.MultiplyTransform(m);
        }

        public static Bitmap Resize(this Bitmap _currentBitmap, int newWidth, int newHeight)
        {
            Bitmap animage = new Bitmap(newWidth, newHeight);
            using (Graphics gr = Graphics.FromImage(animage))
            {
                //gr.SmoothingMode = SmoothingMode.HighSpeed;
                //gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                //gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //gr.CompositingQuality = CompositingQuality.HighQuality;
                gr.DrawImage(_currentBitmap, new Rectangle(0, 0, newWidth, newHeight));
            }
            return animage;
        }

        public static TextureBrush ToTransparentBrush(this Bitmap img, int TransparencyPercent, WrapMode wrap = WrapMode.Tile, PointF offset = new PointF(), float scale = 1)
        {
            img.SetAlpha((int)(255 * (TransparencyPercent / 100f)));
            TextureBrush result = new TextureBrush(img, wrap);
            if (!offset.IsEmpty)
            {
                result.TranslateTransform(offset.X % (scale * img.Width), offset.Y % (scale * img.Height), MatrixOrder.Prepend);
            }
            result.Scale(scale);
            return result;
        }

        private static void SetAlpha(this Bitmap processedBitmap, int Alpha)
        {
            unsafe
            {
                BitmapData bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);

                int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* PtrFirstPixel = (byte*)bitmapData.Scan0;

                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = PtrFirstPixel + (y * bitmapData.Stride);
                    for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                    {
                        int oldBlue = currentLine[x];
                        int oldGreen = currentLine[x + 1];
                        int oldRed = currentLine[x + 2];
                        int oldA = currentLine[x + 3];

                        currentLine[x] = (byte)oldBlue;
                        currentLine[x + 1] = (byte)oldGreen;
                        currentLine[x + 2] = (byte)oldRed;
                        currentLine[x + 3] = (byte)Alpha;
                    }
                });
                processedBitmap.UnlockBits(bitmapData);

            }
        }
    }

    public class ConcurrentList<T>
    { // Untested and unneeded but idk maybe
        private List<T> list;
        public int Count { get; protected set; }

        public ConcurrentList()
        {
            list = new List<T>();
        }

        public void Add(T item)
        {
            lock (Extensions.aLock)
                list.Add(item);
        }

        public void Remove(T item)
        {
            lock (Extensions.aLock)
                list.Remove(item);
        }
    }


    public class MipMapBitmap
    {
        // So this should initialize with a bitmap, generate mipmaps (3 or user defined)
        // And then whenever they try to get the image at a particular size, we give them the appropriate one
        // We can do sizes 1/4, 1/8, 1/16

        public Bitmap Bitmap
        {
            get
            {
                if (HasMipMaps)
                {
                    if (ZoomLevel >= 1)
                        return original;
                    return MipMaps[currentIndex];
                }
                return original;
            }
        }
        private Bitmap original;
        private float ZoomLevel = 1;
        public bool HasMipMaps { get; protected set; }
        public List<Bitmap> MipMaps { get; protected set; }
        private int StepDivisor = 2; // By default each step is 1/2 resolution of the last
        private int currentIndex = 0; // We set this internally every time zoomlevel is altered because we don't need to do all that work in the Get
        public int Width { get { return MipMaps[currentIndex].Width; } }
        public int Height { get { return MipMaps[currentIndex].Height; } }

        public MipMapBitmap(string s, float sizeMult = 1)
        {
            original = new Bitmap(s);
            if (sizeMult != 1)
                original = original.Resize((int)(original.Width * sizeMult), (int)(original.Height * sizeMult));
            original.MakeTransparent(); // important if using much of anything
            HasMipMaps = false;
            GenerateMipMaps();
        }

        public void GenerateMipMaps()
        {
            // This will generate mipmaps at recursive 50% resolution of the previous one until any part of the mip is below 32x32
            // I'll make one to let you pick all that later
            int currentWidth = original.Width;
            int currentHeight = original.Height;
            MipMaps = new List<Bitmap>();
            MipMaps.Add(original); // First the full resolution one.
            do
            {
                currentWidth = currentWidth / StepDivisor;
                currentHeight = currentHeight / StepDivisor;
                MipMaps.Add(original.Resize(currentWidth, currentHeight));
            } while (currentWidth > 32 && currentHeight > 32);
            if (MipMaps.Count > 1)
                HasMipMaps = true;
        }

        public Bitmap SetZoomLevel(float z)
        {
            // Sets the zoom and returns the appropriate bitmap
            ZoomLevel = z;
            /*
            * Zoom level:          
            1 - 0.5            
            2 - 0.25               
            3 - 0.125
            4 - 0.0625


            So divisor^n = ZoomLevel
            log(divisor^n) = log(ZoomLevel)
            n * log(divisor) = log(ZoomLevel)
            n = log(ZoomLevel)/log(divisor)
            */
            if (!HasMipMaps)
                return original;

            currentIndex = (int)(Math.Log(ZoomLevel) / Math.Log(1f/StepDivisor));
            if (currentIndex >= MipMaps.Count)
                currentIndex = MipMaps.Count - 1;
            // ... just in case I guess
            if (currentIndex < 0)
                currentIndex = 0;
            return MipMaps[currentIndex];
        }

        public TextureBrush ToTransparentBrush(int opacity, WrapMode wrap, PointF offset, float sizeMult)
        {
            if(!HasMipMaps)
                return original.ToTransparentBrush(opacity, wrap, offset, sizeMult);
            // We already resized this but we didn't do it perfectly and this wants a sizemult
            // sizemult we pass is ... currentindex's size vs originals size * sizeMult.  They scaled the same percent so width should work alone
            sizeMult =  (original.Size.Width * sizeMult) / MipMaps[currentIndex].Size.Width;

            return MipMaps[currentIndex].ToTransparentBrush(opacity, wrap, offset, sizeMult); // We already resized it... 
        }

        public static explicit operator Bitmap(MipMapBitmap i) // Allows someone to use MipMapBitmap where a Bitmap would be required
        {
            if (i.HasMipMaps)
            {
                if (i.ZoomLevel >= 1)
                    return i.original;
                return i.MipMaps[i.currentIndex];
            }
            return i.original;
        }

    }
}
