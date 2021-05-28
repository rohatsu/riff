// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;
using System.IO;

namespace RIFF.Interfaces.Formats.HTML
{
    public static class RFHTMLRenderer
    {
        private static volatile object _sync = new object();

        public static byte[] Render(string html)
        {
            System.Drawing.Image image;
            return Render(html, out image);
        }

        public static byte[] Render(string html, out System.Drawing.Image image)
        {
            if (!string.IsNullOrWhiteSpace(html))
            {
                lock (_sync)
                {
#if (NETSTANDARD2_0)
                    throw new NotImplementedException("Not supported on .NET Standard");
#else
                    image = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.RenderToImage(html);
                    using (var memoryStream = new MemoryStream())
                    {
                        image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        return memoryStream.ToArray();
                    }
#endif
                }
            }
            image = null;
            return new byte[0];
        }
    }
}
