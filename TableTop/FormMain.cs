using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TableTop
{

    /// <summary>
    /// The primary form/window to display all content
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Form" />
    public partial class FormMain : Form
    {

        /// <summary>
        /// Contains our X and Y offset.  <see cref="FormMain.ModifyOffsetsAndScale"/> with a custom wrapper function for changing them that updates everything relevant, like the texturebrush
        /// </summary>
        public PointF Offsets { get; set; }
        // basically each square gets this offset X and Y added to its position, adjusts if the player pans the camera/map
        public float zoomMult { get; protected set; }
        public float zoomMod { get; protected set; }
        public bool dragging { get; protected set; }
        public int dragBeginX { get; protected set; }
        public int dragBeginY { get; protected set; }
        public int gridMaxSize { get; protected set; }
        public float cellSize { get; protected set; }

        public readonly float MaxZoom = 80;
        public readonly float MinZoom = 0.08f;

        public int ChatBoxHeight { get; protected set; } // Default, will allow click-drag to resize it

        public readonly Bitmap GrassTexture = new Bitmap("forest_default.jpg");
        public readonly MipMapBitmap GrassHDTexture = new MipMapBitmap("grass_HD3.jpg");
        public readonly Bitmap GrassHDMipLQ; // The Low Quality mipmap of the HD texture

        public readonly WrapMode GrassWrapMode = WrapMode.Tile;
        public TextureBrush GrassTextureBrush { get; protected set; } // I'm sorry I like my other classes to be able to get data from here so you get all these protected sets to 
        public TextureBrush GrassHDTextureBrush { get; protected set; }

        public readonly Font MenuFont = new Font("Arial", 14);
        public readonly SolidBrush MenuBackgroundBrush = new SolidBrush(SystemColors.Menu);
        public readonly SolidBrush MenuOptionBrush = new SolidBrush(SystemColors.MenuBar);
        public readonly SolidBrush MenuSelectedBrush = new SolidBrush(SystemColors.MenuHighlight);

        private ConcurrentList<string> ChatMessages = new ConcurrentList<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        public FormMain()
        {
            ChatBoxHeight = 200;

            zoomMult = 1;
            zoomMod = 1;
            dragging = false;
            dragBeginX = 0;
            dragBeginY = 0;
            gridMaxSize = 400;
            cellSize = 10;
            Offsets = new PointF(0, 0); // Initialize shit

            DoubleBuffered = true; // Prevents image flicker
            MouseWheel += _MouseWheel;
            Click += _MouseClick;
            MouseDown += _MouseDown;
            MouseUp += _MouseUp;
            MouseMove += _MouseMove;
            Shown += _Shown;

            // Turns out, I don't really have to use a brush
            // I can use g.DrawImage with an attribute to tile
            // But the brush is already set up and scrolls well... 
            // So the trees will be with a brush, and the grass will paint after it with opacity
            
            //GrassHDMipLQ = GrassHDTexture.Resize(GrassHDTexture.Width / 10, GrassHDTexture.Height / 10);
            GrassHDTextureBrush = new TextureBrush(GrassTexture, GrassWrapMode); // This is easier than making it 0% opacity to start...

            // X * Width = TargetWidth, X = TargetWidth/Width
            //GrassScaleDefault = new PointF(GrassDesiredDimensions.Width / GrassTexture.Width, GrassDesiredDimensions.Height / GrassTexture.Height);
            GrassTextureBrush = new TextureBrush(GrassTexture, GrassWrapMode);
            
            
            //Matrix ScaleMatrix = new Matrix();
            //ScaleMatrix.Scale(GrassScaleDefault.X, GrassScaleDefault.Y);
            //GrassTextureBrush.MultiplyTransform(ScaleMatrix, MatrixOrder.Prepend);
            
            //GrassScaleHD = new PointF(GrassHDDesiredDimensions.Width / GrassHDTexture.Width, GrassHDDesiredDimensions.Height / GrassHDTexture.Height);

            // Make a new thread that waits for g to not be null and then initializes the rest
            /*
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                do
                {
                    Thread.Sleep(100);
                } while (g == null);
                Invoke((MethodInvoker)delegate
                   {
                       InitializeGraphics();
                   });
            }).Start();
            */

            InitializeComponent();
            TimerMain.Tick += _GameTimerTick;
            TimerMain.Start();
            TimerFPS.Start();
        }

        private void log(string s)
        {
            ChatMessages.Add(s);
        }

        /// <summary>
        /// Initializes anything relying on g, called as soon as we get a Graphics object
        /// </summary>
        private void InitializeGraphics(Graphics g)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            // So we iterate each menu...
            float posx = 0;
            float poxy = 0;
            foreach (Menu m in TableTopMenus.GameMenus)
            {
                // And we place each one at posx and posy
                // Then we store that location in the Menu, then calculate and store the Bounding Box
                // TableTopMenus.GameMenus only holds the top-level menus that are always displayed

            }
        }

        /// <summary>
        /// Draws the background.
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            // If we're going to use a texture, we need to fill the entire potential CellGrid Area and use offsets...
            g.FillRectangle(GrassTextureBrush, new RectangleF(0,0,Width,Height));
            // Must draw the opacity-altered image second
            // Opacity goes in as a percent, I'd like 100% at half of max zoom or so

            // You know what, this whole bit is stupid... but I want it
            // It was all moved to only update when it needs it
            // This is a fresh texture that's been multiply transformed and offset already
            g.FillRectangle(GrassHDTextureBrush, new Rectangle(0,0,Width,Height));
        }

        /// <summary>
        /// Draws the grid.
        /// This is the old one and isn't used for anything now... 
        /// </summary>
        /// <param name="g">The g.</param>
        private void DrawGrid(Graphics g)
        { // Right so we should really just draw one square the size of the window, and make lines as appropriate to make the cells
            // But only if either the X or Y is within view range
            // That is, if the X > 0 and < width of the form , or y > 0 and y < width of the form

            float workingcellSize = cellSize * zoomMult;
                for (float x = 0; x < gridMaxSize; x++)
                {
                    //if ((x - Offsets.X) > 0 && (x - Offsets.X) < this.Width)
                    {
                        // Draw vertical line
                        g.DrawLine(Pens.Black, Offsets.X + (x * workingcellSize), Offsets.Y, Offsets.X + (x * workingcellSize), Offsets.Y + ((gridMaxSize - 1) * workingcellSize));
                    }
                }
                for (float y = 0; y < gridMaxSize; y++)
                { // Draw horizontal line
                    //if ((y - Offsets.Y) > 0 && (y - Offsets.Y) < this.Height)
                    {
                        g.DrawLine(Pens.Black, Offsets.X, Offsets.Y + (y * workingcellSize), Offsets.X + ((gridMaxSize - 1) * workingcellSize), Offsets.Y + (y * workingcellSize));
                    }
                }
                // TODO: Move the entire background/grid/moveable area to its own Container that I can put into a form, so I can put like a chatbox around it easier
                // I kinda like the idea of doing it manually though... 
        }


        /// <summary>
        /// The new drawgrid which uses modulus to infinitely draw it
        /// </summary>
        /// <param name="g">The g.</param>
        private void DrawGridOptimized(Graphics g)
        {
            // I'll swap to this if it works...
            // The idea is that for a given width and height, and a given cellsize
            // If we iterate each variable and draw a line at, for example, X % cellsize == 0... we don't draw any unnnecessary lines
            // And if we internally add offset to each iterable var, it will appear to move

            // It's not bad... there's a weird display artifact on high zoom when panned to positive offsets though
            // The first line on the left seems to render twice, or two pixels
            float workingcellSize = cellSize * zoomMult;
            if (workingcellSize > 4)
            {
                for (float tx = 0; tx < Width; tx++)
                {
                    float x = (tx + workingcellSize) - (Offsets.X % workingcellSize);
                    // this makes sure x is positive without affecting the outcome of the if
                    if (x % workingcellSize < 1)
                        g.DrawLine(Pens.Black, tx, 0, tx, Height);
                }
                for (float ty = 0; ty < Height; ty++)
                { // Draw horizontal line
                    float y = (ty + workingcellSize) - (Offsets.Y % workingcellSize);
                    if (Math.Abs(y % workingcellSize) < 1)
                        g.DrawLine(Pens.Black, 0, ty, Width, ty);
                }
            }
        }

        /// <summary>
        /// Draws the top menu.
        /// </summary>
        /// <param name="g">The g.</param>
        private void DrawTopMenu(Graphics g)
        {
            // This should be drawn last-ish so it's always on top
            // This will be the menu bar at the top with things like File, Edit, etc.
            // Not expected to be moveable, even if moveable windows are implemented
            // First we draw a box from 0,0 to width,20 or so
            // Offsets are used to offset the grid when the player moves it, menus should not be affected by them
            g.FillRectangle(MenuBackgroundBrush, 0, 0, this.Width, 25);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;


            // For now I'd like an FPS counter and an X,Y position counter... 
            string positionString = (-Offsets.X / zoomMult + mousex/zoomMult) +  ", " + (-Offsets.Y / zoomMult + mousey / zoomMult);
            SizeF posStringSize = g.MeasureString(positionString, MenuFont);
            g.DrawString(positionString, MenuFont, System.Drawing.Brushes.Black, new PointF(Width - posStringSize.Width, 0));
            string fpsString = "FPS: " + lastframerate;
            SizeF drawStringSize = g.MeasureString(fpsString, MenuFont);
            g.DrawString(fpsString, MenuFont, System.Drawing.Brushes.Black, new PointF(Width - posStringSize.Width - 10 - drawStringSize.Width, 0));
        }

        /// <summary>
        /// Draws the chat window.
        /// </summary>
        /// <param name="g">The g.</param>
        private void DrawChatWindow(Graphics g)
        {
            // Draws a chat window at the bottom of the screen
            // Intended to be moveable if we bother doing that
            using (SolidBrush chatBackground = new SolidBrush(System.Drawing.Color.White))
            {
                g.FillRectangle(chatBackground, new Rectangle(0, Height, Width, ChatBoxHeight));
            };
            // Iterate through our list of chat messages and write each of them onto the rectangle
            // I guess I should really use an actual Control for this like RichText with a scrollbar... 
        }

        private void TimerFPS_Tick(object sender, EventArgs e)
        {
            lastframerate = framerate;
            framerate = 0;
        }
    }


}
