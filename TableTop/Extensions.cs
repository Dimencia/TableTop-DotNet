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

        /*
        private static Image resizeImage(this Image imgToResize, SizeF newSize)
        {
            float xPercent = newSize.Width / imgToResize.Size.Width;
            float yPercent = newSize.Height / imgToResize.Size.Height;

            using (Graphics gr = Graphics.FromImage(imgToResize))
            {
                gr.ScaleTransform(xPercent, yPercent);
                imgToResize = new Bitmap((int)(xPercent * imgToResize.Width), (int)(yPercent * imgToResize.Height), gr);
            }
            return imgToResize;
        }
        */
        // Didn't work.

        public static Bitmap Resize(this Bitmap img, Size newSize)
        {
            return new Bitmap(img, newSize);
        }

        public static Bitmap Resize(this Bitmap img, SizeF newSize)
        {
            return img.Resize(newSize.ToSize());
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
                for (int y = destinationRect.Y; y < destinationRect.Height; y += tileSize.Height-5)
                {
                    for (int x = destinationRect.X; x < destinationRect.Width; x += tileSize.Width-5)
                    { // We've already handled everything with destinationRect, just draw to it with the x and y we're at
                        // But we can check if it's on-screen cuz don't draw it if not
                        if(x+tileSize.Width > 0 && x < Core._FormMain.Width && y+tileSize.Height > 0 && y < Core._FormMain.Height)
                            g.DrawImage(i, new Rectangle(x, y, tileSize.Width, tileSize.Height), // Destination Width and Height.  destinationRect is the full rect to fill, destination is each tile
                                0.0f, 0.0f, i.Width, i.Height, // Source Rect
                                GraphicsUnit.Pixel, imageAtt);
                    }
                }
            imageAtt.Dispose();
        }

        public static TextureBrush ToTransparentBrush(this Bitmap img, SizeF size, int TransparencyPercent)
        {
            return ToTransparentBrush(img, size.ToSize(), TransparencyPercent);
        }

        public static void Scale(this TextureBrush t, float mult)
        {
            Matrix m = new Matrix();
            m.Scale(mult, mult);
            t.MultiplyTransform(m);
        }

        public static TextureBrush ToTransparentBrush(this Bitmap img, Size size, int TransparencyPercent)
        {
            // The idea is I do a using(TextureBrush HDGrass.ToTransparentBrush(...
            // See if that's any faster.  
            img.SetAlpha((int)(255*(TransparencyPercent/100f)));
            Matrix ScaleMatrix = new Matrix();
            ScaleMatrix.Scale(Core._FormMain.GrassScaleHD.X, Core._FormMain.GrassScaleHD.Y);
            TextureBrush result = new TextureBrush(img, Core._FormMain.GrassWrapMode, new Rectangle(0,0,size.Width, size.Height));
            result.MultiplyTransform(ScaleMatrix, MatrixOrder.Prepend);
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
                        currentLine[x + 3] = (byte)(Alpha);
                    }
                });
                processedBitmap.UnlockBits(bitmapData);
                
            }
        }
    }

    public class ConcurrentList<T>
    {
        private List<T> list;
        public int Count { get; protected set; }

        public ConcurrentList() {
            list = new List<T>();
        }

        public void Add(T item)
        {
            lock(Extensions.aLock)
            list.Add(item);
        }

        
    }
}
