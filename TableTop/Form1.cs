using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TableTop
{

    public partial class FormMain : Form
    {

        private float xoffset = 0;
        private float yoffset = 0; // These are used so the grid can appear to stay in place when zooming
        // basically each square gets these numbers added to its position, adjusts if the player moves the camera
        private float zoomMult = 1;
        private float zoomMod = 1;
        private bool dragging = false;
        int dragBeginX = 0;
        int dragBeginY = 0;
        int gridMaxSize = 10000;
        float cellSize = 10;



        public FormMain()
        {
            Form mainPanel = this;
            this.DoubleBuffered = true;
            mainPanel.MouseWheel += _MouseWheel;
            mainPanel.Click += _MouseClick;
            mainPanel.MouseDown += _MouseDown;
            mainPanel.MouseUp += _MouseUp;
            mainPanel.MouseMove += _MouseMove;
            InitializeComponent();
            TimerMain.Tick += _GameTimerTick; 
            TimerMain.Start();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black); // Not needed if we're just doing one layer... and even then, if we build each layer each time
            DrawBackground(g);
        }

        private void _GameTimerTick(object sender, EventArgs e)
        {
            Refresh();
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragBeginX = e.Location.X;
            dragBeginY = e.Location.Y;
            Refresh();
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            if(dragging)
            {
                xoffset += e.Location.X - dragBeginX;
                yoffset += e.Location.Y - dragBeginY;
                dragBeginX = e.Location.X;
                dragBeginY = e.Location.Y;
                Refresh();
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if(dragging)
            {
                dragging = false;
                xoffset += e.Location.X - dragBeginX;
                yoffset += e.Location.Y - dragBeginY;
                Refresh();
            }
        }

        private void _MouseClick(object sender, EventArgs e)
        {

        }

        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            // We should get the position of the mouse cursor... add xoffset to account for any off-screen stuff... 
            // Which gives us the position of the thing that needs to go under the mouse
            // We then multiple that position by new zoomMult, should give us the new position to put under the mouse
            // And += the offsets to that new position minus the old one
            
            float mousex = e.X - xoffset;
            float mousey = e.Y - yoffset;

            float zoomMultOld = zoomMult;

            // I think I need an x^2 pattern
            // So we add the sign of delta, /10, to zoomMult and square zoomMult before we use it
            zoomMod += (Math.Sign(e.Delta) / 10f);
            zoomMult = zoomMod * zoomMod;

            // Alright so.... 
            float multModifier = (zoomMult / zoomMultOld);
            // We take this, which is what we need to multiply the vector mousex,mousey by to get the projected mousex and y
            // Then we take the distances between those two and add it to each offset
            float projx = mousex * multModifier;
            float projy = mousey * multModifier;

            xoffset -= (projx - mousex);
            yoffset -= (projy - mousey);

            Refresh();
        }

        private void MoveOffsetTowards(int x, int y)
        {
            MoveOffsetTowards(new Point(x, y));
        }

        private void MoveOffsetTowards(Point p)
        {
            // This should be used after updating zoom amount, and will move each offset half a cellsize times new zoom amount
            // The point is the vector itself because we're getting vectors from 0,0
            // So we need to get the length, then divide x and y of p individually by that, and it's normalized
            float length = (float)Math.Sqrt(p.X * p.X * +p.Y * p.Y);
            float x = p.X / length;
            float y = p.Y / length;
            xoffset += ((cellSize * zoomMult) / 2) * x;
            yoffset += ((cellSize * zoomMult) / 2) * y;
        }

        private void DrawBackground(Graphics g)
        {
            // Draws the background and grid if enabled
            g.FillRectangle(Brushes.DarkGreen, 10, 10, this.Width - 20, this.Height - 40);
            DrawGrid(g);
        }

        private void DrawGrid(Graphics g)
        { // Right so we should really just draw one square the size of the window, and make lines as appropriate to make the cells
            // But only if either the X or Y is within view range
            // That is, if the X > 0 and < width of the form , or y > 0 and y < width of the form

            float workingcellSize = cellSize * zoomMult;
            for(float x = 0; x < gridMaxSize; x++)
            {
                //if ((x - xoffset) > 0 && (x - xoffset) < this.Width)
                {
                    // Draw vertical line
                    g.DrawLine(Pens.Black, xoffset + (x * workingcellSize), yoffset, xoffset + (x * workingcellSize), yoffset + ((gridMaxSize - 1) * workingcellSize));
                }
            }
            for (float y = 0; y < gridMaxSize; y++)
            { // Draw horizontal line
                //if ((y - yoffset) > 0 && (y - yoffset) < this.Height)
                {
                    g.DrawLine(Pens.Black, xoffset, yoffset + (y * workingcellSize), xoffset + ((gridMaxSize - 1) * workingcellSize), yoffset + (y * workingcellSize));
                }
            }
        }

        
        
        private void Form_Shown(Object sender, EventArgs e)
        {
            
            //DrawGrid();
        }




        class SelectablePanel : Panel
        {
            public SelectablePanel()
            {
                this.SetStyle(ControlStyles.Selectable, true);
                this.TabStop = true;
            }
            protected override void OnMouseDown(MouseEventArgs e)
            {
                this.Focus();
                base.OnMouseDown(e);
            }
            protected override bool IsInputKey(Keys keyData)
            {
                if (keyData == Keys.Up || keyData == Keys.Down) return true;
                if (keyData == Keys.Left || keyData == Keys.Right) return true;
                return base.IsInputKey(keyData);
            }
            protected override void OnEnter(EventArgs e)
            {
                this.Invalidate();
                base.OnEnter(e);
            }
            protected override void OnLeave(EventArgs e)
            {
                this.Invalidate();
                base.OnLeave(e);
            }
            protected override void OnPaint(PaintEventArgs pe)
            {
                base.OnPaint(pe);
                if (this.Focused)
                {
                    var rc = this.ClientRectangle;
                    rc.Inflate(-2, -2);
                    ControlPaint.DrawFocusRectangle(pe.Graphics, rc);
                }
            }
        }

    }
}
