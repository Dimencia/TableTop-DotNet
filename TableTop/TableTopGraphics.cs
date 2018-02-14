using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace TableTop
{
    // This is to separate out our overrides and events, so that ZoomableGrid.cs contains just useable functions
    partial class ZoomableGrid
    {
        private bool InitializedGraphics = false;
        private int framerate = 0;
        private int lastframerate = 0;
        public int mousex = 0;
        public int mousey = 0;
        public PointF PositionInternal { get; protected set; }
        private Token interactingToken = null;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White); // Not needed if we're just doing one layer... and even then, if we build each layer each time
            if (!InitializedGraphics)
            {
                InitializeGraphics(g);
                InitializedGraphics = true;
            }
            DrawBackground(g);
            DrawGridOptimized(g); 
            //DrawTopMenu(g);
            //DrawChatWindow(g);
            // These aren't on us anymore
            // But, Tokens have to be set visible false so I can use floats... 
            // And the idea is I try to draw them here with their float values
            // But I want to iterate it backwards because I'd like the draw order to reflect the precedence for mouse events
            // IE when two are ontop and you click them, the one in front gets the mouse
            for(int i = Tokens.Count-1; i >= 0; i--)
            {
                Token t = Tokens[i];
                g.DrawImage(t.Image, new RectangleF(t.Location, t.Size));
            }
        }

        private void _GameTimerTick(object sender, EventArgs e)
        {
            Refresh();
        }

        private void _MouseDown(object sender, MouseEventArgs e)
        {
            if(interactingToken != null)
            {
                interactingToken.SimulateMouseDown(e);
                Refresh();
                return;
            }
            foreach(Token t in Tokens)
            {
                if(new RectangleF(t.Location, t.Size).Contains(e.Location))
                {
                    t.SimulateMouseDown(e);
                    interactingToken = t; // Pass all mouse events to this token til we get a mouseUP
                    // Also bring this token to front
                    Tokens.Remove(t);
                    Tokens.Insert(0, t);
                    Refresh();
                    return;
                }
            }

            dragging = true;
            dragBeginX = e.Location.X;
            dragBeginY = e.Location.Y;
            Refresh();
        }

        private void _MouseMove(object sender, MouseEventArgs e)
        {
            this.mousex = e.X;
            this.mousey = e.Y;
            PositionInternal = new PointF((-Offsets.X / zoomMult + mousex / zoomMult), (-Offsets.Y / zoomMult + mousey / zoomMult));
            if (dragging)
            {
                ModifyOffsetsAndScale(e.Location.X - dragBeginX, e.Location.Y - dragBeginY);
                dragBeginX = e.Location.X;
                dragBeginY = e.Location.Y;
                Refresh();
            }
            else
            {
                if (interactingToken != null)
                {
                    interactingToken.SimulateMouseMove(e);
                    Refresh();
                    return;
                }
                foreach (Token t in Tokens)
                {
                    if (new RectangleF(t.Location, t.Size).Contains(e.Location))
                    {
                        t.SimulateMouseMove(e);
                        Refresh();
                        return;
                    }
                }
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (interactingToken != null)
            {
                interactingToken.SimulateMouseUp(e);
                interactingToken.GridPosition = ConvertWorldToLocal(interactingToken.Location);
                Refresh();
                interactingToken = null;
                return;
            }
            // No need to test controls for this otherwise, they'd just get in the way of tring to scroll it
            if (dragging)
            {
                dragging = false;
                ModifyOffsetsAndScale(e.Location.X - dragBeginX, e.Location.Y - dragBeginY);
                Refresh();
            }
        }

        private void _MouseClick(object sender, EventArgs e)
        {

        }

        private void _Shown(object sender, EventArgs e)
        {
            // I want to get the damn graphics of the form and save it somewhere, but I can only do that on paint
            // And I don't want a conditional clogging up my Paint method like that since it's called at 30FPS

        }

        private void _MouseWheel(object sender, MouseEventArgs e)
        {
            // We should get the position of the mouse cursor... add xoffset to account for any off-screen stuff... 
            // Which gives us the position of the thing that needs to go under the mouse
            // We then multiple that position by new zoomMult, should give us the new position to put under the mouse
            // And += the offsets to that new position minus the old one

            float mousex = e.X - Offsets.X;
            float mousey = e.Y - Offsets.Y;

            int zoomDirection = Math.Sign(e.Delta);
            if (zoomMult > MaxZoom && zoomDirection > 0)
                return;
            else if (zoomMult < MinZoom && zoomDirection < 0)
                return;

            float zoomMultOld = zoomMult; // Store what zoomMult was before we changed anything in it

            // I think I need an x^2 pattern
            // So we add the sign of delta, /10, to zoomMult and square zoomMult before we use it
            zoomMod += (zoomDirection * 0.2f);
            zoomMult = zoomMod * zoomMod;

            // Alright so.... 
            float multModifier = (zoomMult / zoomMultOld);
            // We take this, which is what we need to multiply the vector mousex,mousey by to get the projected mousex and y
            // Then we take the distances between those two and add it to each offset
            float projx = mousex * multModifier;
            float projy = mousey * multModifier;

            float translateFinalX = -(projx - mousex);
            float translateFinalY = -(projy - mousey);

            ModifyOffsetsAndScale(translateFinalX, translateFinalY); // Note that this resets then modifiers and scales

            Refresh();
        }

        public PointF ConvertLocalToWorld(PointF localCoordinates)
        {
            // Just convenience functions because I'm tired of working this out
            return new PointF((Offsets.X  + localCoordinates.X * zoomMult), (Offsets.Y  + localCoordinates.Y * zoomMult));
        }

        public PointF ConvertWorldToLocal(PointF parentCoordinates)
        {
            return new PointF((-Offsets.X / zoomMult + parentCoordinates.X / zoomMult), (-Offsets.Y / zoomMult + parentCoordinates.Y / zoomMult));
        }

        public void ModifyOffsetsAndScale(float XMod, float YMod)
        {
            Image i = GrassTextureBrush.Image;
            Offsets = new PointF(Offsets.X + XMod, Offsets.Y + YMod);

            // And translate it the same amount we just changed x and y offset
            // Except at very high or low values this causes problems, so 
            // We translate it by each var modded by the width/height of one 'tile' of it
            // Which... is really hard to get, let's hope our math has been right
            // In which case it should be zoomMult*Width
            // But that doesn't seem to help... 
            // Turns out it's probably the translate matrix inside the texture that needs to be modded
            // Or I guess better to reset it and apply offsets... 
            // But then our scale transform would have to be reapplied which is expensive
            // But, we actually already set the scale transform right after this
            // We can use DesiredDimensions because this is basically the initial dimension of what we plug in
            // And we can't use the mods, we have to use the actual offsets modded by this same value
            // And goddammit, desireddimensions itself was a scaling operation... 
            // Fuckit, let's remove it entirely the default dimensions of this are fine

            // Well it doesn't always scale right after and idgaf anymore so
            GrassTextureBrush.ResetTransform();
            GrassTextureBrush.TranslateTransform(Offsets.X % (zoomMult * GrassTexture.Width), Offsets.Y % (zoomMult * GrassTexture.Height), MatrixOrder.Append);
            GrassTextureBrush.Scale(zoomMult);

            int opacity = (int)(100 * ((zoomMult) / 4)); // IDK
            if (opacity > 100)
                opacity = 100;
            if (opacity < 0)
                opacity = 0;

            // Set the zoom on the texture's mip, pretending we're less zoomed than we are so we get lower res mips... 
            GrassHDTexture.SetZoomLevel((zoomMult*.9f)*0.01f);

            // We should use the real HD texture here
            if (GrassHDTextureBrush != null)
                GrassHDTextureBrush.Dispose();
            GrassHDTextureBrush = GrassHDTexture.ToTransparentBrush(opacity, GrassWrapMode, Offsets, 0.01f * zoomMult);

            // And iterate all Controls, setting them at their appropriate new locations.  The GridLocation doesn't change
            foreach(Token t in Tokens)
            {
                SuspendLayout();
                t.Size = new SizeF((cellSize * zoomMult), (cellSize * zoomMult));
                t.Location = ConvertLocalToWorld(t.GridPosition); // This function will use the new values to put it in the right spot
                // But unfortunately it's an int Point... 
                // So we get the decimal values of GridPosition, ie GridPosition.X%1, and translate the image that amount
                ResumeLayout();
            }
            ZoomChanged.Invoke(this, new EventArgs());
        }
    }


}
