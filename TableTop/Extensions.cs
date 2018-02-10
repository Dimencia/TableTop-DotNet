using System;
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

        public static Image Resize(this Image img, Size newSize)
        {
            return new Bitmap(img, newSize);
        }

        public static Image Resize(this Image img, SizeF newSize)
        {
            return img.Resize(newSize.ToSize());
        }

        public static void TileImageWithOpacity(this Graphics g, Image i, Size tileSize, int opacityPercent, Rectangle destinationRect = new Rectangle())
        {
            // Note the entire reason I have to do all this shit is because if I re-make a TexturedBrush every tick, shit gets slow
            // And there's no way to change the alpha on one without remaking it
            // Of course, I'm re-making the bitmap in resize... 
            float maxWidth = Core._FormMain.gridMaxSize * Core._FormMain.cellSize * Core._FormMain.zoomMult;
            float maxHeight = maxWidth; // We edit these if they specified their own destination
            // These are the highest values of X and Y we need to draw to, assuming they're within the destination

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
                for (int y = destinationRect.Y; y < destinationRect.Height; y += tileSize.Height-1)
                {
                    for (int x = destinationRect.X; x < destinationRect.Width; x += tileSize.Width-1)
                    { // We've already handled everything with destinationRect, just draw to it with the x and y we're at
                        g.DrawImage(i, new Rectangle(x, y, tileSize.Width, tileSize.Height), // Destination Width and Height.  destinationRect is the full rect to fill, destination is each tile
                                0.0f, 0.0f, i.Width, i.Height, // Source Rect
                                GraphicsUnit.Pixel, imageAtt);
                    }
                }
            imageAtt.Dispose();
        }
    }
}
