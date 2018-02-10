using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace TableTop
{
    // This is to separate out our overrides and events, so that FormMain.cs contains just useable functions
    partial class FormMain
    {
        private bool InitializedGraphics = false;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            //g.Clear(Color.Black); // Not needed if we're just doing one layer... and even then, if we build each layer each time
            if(!InitializedGraphics)
            {
                InitializeGraphics(g);
            }
            DrawBackground(g);
            DrawGrid(g);
            DrawTopMenu(g);
            DrawChatWindow(g);
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
            if (dragging)
            {
                ModifyOffsets(e.Location.X - dragBeginX, e.Location.Y - dragBeginY);
                dragBeginX = e.Location.X;
                dragBeginY = e.Location.Y;
                Refresh();
            }
        }

        private void _MouseUp(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                ModifyOffsets(e.Location.X - dragBeginX, e.Location.Y - dragBeginY);
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
            zoomMod += (zoomDirection * 0.1f);
            zoomMult = zoomMod * zoomMod;

            // Alright so.... 
            float multModifier = (zoomMult / zoomMultOld);
            // We take this, which is what we need to multiply the vector mousex,mousey by to get the projected mousex and y
            // Then we take the distances between those two and add it to each offset
            float projx = mousex * multModifier;
            float projy = mousey * multModifier;

            float translateFinalX = -(projx - mousex);
            float translateFinalY = -(projy - mousey);

            ModifyOffsets(translateFinalX, translateFinalY, multModifier);

            // And now we want to scale the Texture's size by the change in ZoomMult (ie multModifier) if it's not 1 or 0 cuz that'd be pointless
            if (multModifier != 1 && multModifier != 0)
            {
                Matrix ScaleMatrix = new Matrix();
                ScaleMatrix.Scale(multModifier, multModifier);
                GrassTextureBrush.MultiplyTransform(ScaleMatrix, MatrixOrder.Prepend);
                // TODO: Part of the problem must be here because I'm using zoomMult not multModifier (which is sqrt of zoommult)
                // But it also runs out of memory like immediately, idk what that's about
            }

            Refresh();
        }

        public void ModifyOffsets(float XMod, float YMod, float multModifier = 1f)
        {
            Offsets = new PointF(Offsets.X + XMod, Offsets.Y + YMod);
            
            // And translate it the same amount we just changed x and y offset
            GrassTextureBrush.TranslateTransform(XMod, YMod, MatrixOrder.Append);
        }
    }

    
}
