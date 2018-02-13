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
        private int framerate = 0;
        private int lastframerate = 0;
        private int mousex = 0;
        private int mousey = 0;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            framerate++;
            Graphics g = e.Graphics;
            //g.Clear(Color.White); // Not needed if we're just doing one layer... and even then, if we build each layer each time
            if (!InitializedGraphics)
            {
                InitializeGraphics(g);
                InitializedGraphics = true;
            }
            DrawBackground(g);
            DrawGridOptimized(g); // TODO: This might be temp if it doesn't work right
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
            this.mousex = e.X;
            this.mousey = e.Y;
            if (dragging)
            {
                ModifyOffsetsAndScale(e.Location.X - dragBeginX, e.Location.Y - dragBeginY);
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

            ModifyOffsetsAndScale(translateFinalX, translateFinalY); // Note that this resets then modifiers and scales

            Refresh();
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

            // Set the zoom on the texture's mip
            GrassHDTexture.SetZoomLevel(zoomMult*0.01f);

            // We should use the real HD texture here
            if (GrassHDTextureBrush != null)
                GrassHDTextureBrush.Dispose();
            GrassHDTextureBrush = GrassHDTexture.ToTransparentBrush(opacity, GrassWrapMode, Offsets, 0.01f * zoomMult);

        }
    }


}
