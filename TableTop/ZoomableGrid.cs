using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TableTop
{
    [ComVisibleAttribute(true)]
    [ClassInterfaceAttribute(ClassInterfaceType.AutoDispatch)]
    [DockingAttribute(DockingBehavior.Ask)]
    public partial class ZoomableGrid : Panel
    {
        // This is meant to encapsulate the entire zoomable grid and background
        // And also will be able to take objects that should be drawn, with coordinates and size
        // It will save these objects until manually removed, even if they scroll off screen
        // And if they scroll back on it'll draw them again
        // It will of course draw these objects at the right position and size for its size, zoom and offsets
        // Will not detect if there are objects in front of it.  There shouldn't be. This would be resized instead

        /// <summary>
        /// Contains our X and Y offset.  <see cref="ZoomableGrid.ModifyOffsetsAndScale"/> with a custom wrapper function for changing them that updates everything relevant, like the texturebrush
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
        public readonly float MinZoom = 0.37f;

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

        private List<Token> Tokens = new List<Token>();

        public event EventHandler ZoomChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomableGrid"/> class.
        /// </summary>
        public ZoomableGrid()
        {
            AllowDrop = true; // So you can just drop an image and boom, token
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

            PositionInternal = new PointF();

            GrassHDTextureBrush = new TextureBrush(GrassTexture, GrassWrapMode); // This is easier than making it 0% opacity to start...

            GrassTextureBrush = new TextureBrush(GrassTexture, GrassWrapMode);
            
            TimerMain = new Timer();
            TimerMain.Tick += _GameTimerTick;
            TimerMain.Start();
            TimerFPS = new Timer();
            //TimerFPS.Tick += TimerFPS_Tick;
            TimerFPS.Start();
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            DragOver += new DragEventHandler(_DragOver);
        }

        void _DragOver(object sender, DragEventArgs e)
        {
            Point client = PointToClient(new Point(e.X, e.Y));
            OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, client.X, client.Y, 0));
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                // Make sure they're images, load them as new Tokens and place them where they were dropped
                using(FileStream stream = File.Open(file, FileMode.Open))
                {
                    if(stream.IsImage())
                    {
                        // Uhh... 
                        // Well I guess make a Token out of it
                        // But then what, give the Token to the grid or the form?
                        // Considering the grid and token are already super closely linked...
                        // Yeah, tokens should be a part of this
                        // But we should also trigger a TokenAdded event with the token attached just in case
                        Token t = new Token();
                        
                        t.SizeMode = PictureBoxSizeMode.Zoom;
                        t.Size = new SizeF((cellSize * zoomMult),(cellSize * zoomMult));
                        stream.Seek(0, SeekOrigin.Begin);
                        Bitmap b = (Bitmap)Image.FromStream(stream);
                        b.MakeTransparent();
                        // If this bitmap is too big, say over 512x512, let's halve its size repeatedly until its within acceptable params
                        while(b.Width > 512 || b.Height > 512)
                        {
                            b = b.Resize(b.Width / 2, b.Height / 2);
                        }
                        t.Image = b;
                        // And then set its location to actually draw it, to draw at the actual mouse X and Y
                        Point p = PointToClient(new Point(e.X, e.Y));
                        t.Location = new PointF(p.X - (t.Size.Width/2f), p.Y - (t.Size.Width/2f));
                        t.GridPosition = ConvertWorldToLocal(t.Location);

                        Tokens.Add(t);

                        Refresh();
                    }
                }
            }
        }

        public Timer TimerMain;
        private Timer TimerFPS;

        private void InitializeComponents()
        {

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

        }

        /// <summary>
        /// Draws the background.
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            g.FillRectangle(GrassTextureBrush, new RectangleF(0, 0, Width, Height));
            g.FillRectangle(GrassHDTextureBrush, new Rectangle(0, 0, Width, Height));
            // Note that for zoom, offsets, and etc, the TextureBrushes themselves are edited when necessary
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
            if (workingcellSize > 6)
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
            string positionString = (-Offsets.X / zoomMult + mousex / zoomMult) + ", " + (-Offsets.Y / zoomMult + mousey / zoomMult);
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
