using System;
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
        /// Contains our X and Y offset.  <see cref="FormMain.ModifyOffsets"/> with a custom wrapper function for changing them that updates everything relevant, like the texturebrush
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

        public readonly float MaxZoom = 8;
        public readonly float MinZoom = 0.4f;

        public float ChatBoxHeight { get; protected set; } // Default, will allow click-drag to resize it

        public readonly Image GrassTexture = Image.FromFile("forest_default.jpg");
        public Bitmap GrassHDTexture { get; protected set; }
        public Bitmap GrassHDResized { get; set; }
        public readonly SizeF GrassDesiredDimensions = new SizeF(256, 256);
        public readonly SizeF GrassHDDesiredDimensions = new SizeF(32, 32);
        public PointF GrassScaleDefault; // We calculate this from DesiredDimensions so we have multiple ways to scale things
        public readonly WrapMode GrassWrapMode = WrapMode.TileFlipXY;
        public TextureBrush GrassTextureBrush { get; protected set; } // I'm sorry I like my other classes to be able to get data from here so you get all these protected sets to 
                                                                      // keep them from actually changing any of the data without the proper validation/updates
        public readonly Font MenuFont = new Font("Arial", 14);
        public readonly SolidBrush MenuBackgroundBrush = new SolidBrush(SystemColors.Menu);
        public readonly SolidBrush MenuOptionBrush = new SolidBrush(SystemColors.MenuBar);
        public readonly SolidBrush MenuSelectedBrush = new SolidBrush(SystemColors.MenuHighlight);

        /// <summary>
        /// Initializes a new instance of the <see cref="FormMain"/> class.
        /// </summary>
        public FormMain()
        {
            ChatBoxHeight = 200f;

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

            // X * Width = TargetWidth, X = TargetWidth/Width
            GrassScaleDefault = new PointF(GrassDesiredDimensions.Width / GrassTexture.Width, GrassDesiredDimensions.Height / GrassTexture.Height);
            GrassTextureBrush = new TextureBrush(GrassTexture, GrassWrapMode);
            Matrix ScaleMatrix = new Matrix();
            ScaleMatrix.Scale(GrassScaleDefault.X, GrassScaleDefault.Y);
            GrassTextureBrush.MultiplyTransform(ScaleMatrix, MatrixOrder.Prepend);

            GrassHDTexture = new Bitmap("grass_default.jpg");

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
            
            // Let's start by scaling down the HD texture so it's not 2600 pixels wide
            // Should speed up future scaling
            GrassHDTexture = (Bitmap)GrassHDTexture.Resize(new Size((int)(GrassHDDesiredDimensions.Width), (int)(GrassHDDesiredDimensions.Height)));
            GrassHDResized = (Bitmap)GrassHDTexture.Clone(); // Cloning it ensures that we aren't fucking with the HD tex

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
            g.FillRectangle(GrassTextureBrush, new RectangleF(Offsets.X, Offsets.Y, gridMaxSize * cellSize * zoomMult, gridMaxSize * cellSize * zoomMult));
            // Must draw the opacity-altered image second
            // Opacity goes in as a percent, I'd like 100% at half of max zoom or so
            int opacity = (int)(100 * ((zoomMult * 2) / MaxZoom));
            if (opacity >= 40) // Arbitrary to prevent it from freaking out at low opacity levels... 
            {
                Bitmap b = GrassHDResized;
                Bitmap t = (Bitmap)GrassHDResized.Clone();
                Bitmap w = GrassHDTexture;
                Bitmap ut = (Bitmap)GrassHDTexture.Clone();
                // And we need to make sure everyting's good with g

                g.TileImageWithOpacity(GrassHDResized, new Size((int)(GrassHDDesiredDimensions.Width * zoomMult), (int)(GrassHDDesiredDimensions.Height * zoomMult)), opacity);
            }
            // TODO: Fix the above, having trouble resizing the image... 
        }

        /// <summary>
        /// Draws the grid.
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

        }

        /// <summary>
        /// Draws the chat window.
        /// </summary>
        /// <param name="g">The g.</param>
        private void DrawChatWindow(Graphics g)
        {
            // Draws a chat window at the bottom of the screen
            // Intended to be moveable if we bother doing that

        }

    }


}
