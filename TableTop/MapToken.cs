using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TableTop
{
    public class Token : PictureBox
    {
        // This map should contain everything needed for a token to be displayed (and probably moved) on a zoomablegrid
        // This means it needs...
        
        // Everything a PictureBox has.  

        // It will need mouse events still... that's about it
        // Oh and we need to tie it to the grid's zoomchanged event and zoom it automatically

        // The token should also always Draw itself with this BoundingBox, so something can resize the Token by editing this

        // List<TokenAction> Actions - TODO: Need TokenAction class... 

        // Note that if we handle tokens right, we can iterate all tokens in between updates and have a list of to-draw tokens by the next frame
        // But I think Forms handles everything
        public PointF GridPosition { get; set; }
        new public PointF Location { get; set; } // Note, if I run into problems it's probably because I can't override so I'm just hiding
        new public SizeF Size { get; set; } // So if something calls for Size and needs a regular Size and not SizeF, it'll get the wrong one
        new public PaintEventHandler Paint;
        private bool dragging = false;
        private Point dragStart;

        public Token()
        {
            Visible = false;
        }

        public void SimulateMouseDown(MouseEventArgs e)
        {
            dragging = true;
            dragStart = e.Location;
        }

        public void SimulateMouseMove(MouseEventArgs e)
        {
            if(dragging)
            {
                Location = new PointF(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y));
                dragStart = e.Location;
            }
        }

        public void SimulateMouseUp(MouseEventArgs e)
        {
            dragging = false;
        }
    }
}
