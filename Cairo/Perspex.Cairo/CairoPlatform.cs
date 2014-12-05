﻿// -----------------------------------------------------------------------
// <copyright file="CairoPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo
{
    using System;
    using global::Cairo;
    using Perspex.Cairo.Media;
    using Perspex.Cairo.Media.Imaging;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;

    public class CairoPlatform : IPlatformRenderInterface
    {
        private static CairoPlatform instance = new CairoPlatform();

        private static TextService textService = new TextService();

        public ITextService TextService
        {
            get { return textService; }
        }

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => instance, typeof(IPlatformRenderInterface));
            locator.Register(() => textService, typeof(ITextService));
        }

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            throw new NotImplementedException();
            ////return new BitmapImpl(imagingFactory, width, height);
        }

        public IRenderer CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            if (textService.Context == null)
            {
                textService.Context = this.GetPangoContext(handle);
            }

            return new Renderer(handle, width, height);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            ImageSurface result = new ImageSurface(fileName);
            return new BitmapImpl(result);
        }

        private Pango.Context GetPangoContext(IPlatformHandle handle)
        {
            switch (handle.HandleDescriptor)
            {
                case "GtkWindow":
                    var window = GLib.Object.GetObject(handle.Handle) as Gtk.Window;
                    return window.PangoContext;
                default:
                    throw new NotSupportedException(string.Format(
                        "Don't know how to get a Pango Context from a '{0}'.",
                        handle.HandleDescriptor));
            }
        }
    }
}