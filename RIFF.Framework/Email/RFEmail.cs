// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RazorEngine.Templating;
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.Serialization;

namespace RIFF.Framework
{
    public abstract class RFEmail<T> : IRFEmail where T : ITemplate
    {
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public RFEmailConfig Config { get; set; }

        protected RFEmail(RFEmailConfig config)
        {
            Config = config;
        }

        public void AddAttachment(string fileName, byte[] content, string mimeType)
        {
            Attachments.Add(new Attachment(new MemoryStream(content), fileName, mimeType));
        }

        public MailMessage PrepareMessage(string subject, params object[] formats)
        {
            var sender = RFSettings.GetAppSetting("SmtpSender", null);
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new RFLogicException(this, "E-mail subject cannot be empty.");
            }
            var body = GenerateBody();
            var mailMessage = new MailMessage
            {
                Body = body,
                IsBodyHtml = true,
                From = new MailAddress(sender),
                Subject = String.Format(subject, formats),
                Sender = new MailAddress(sender)
            };
            foreach (var recipient in Config.To.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                mailMessage.To.Add(recipient.Trim());
            }
            if (!string.IsNullOrWhiteSpace(Config.Cc))
            {
                foreach (var cc in Config.Cc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.CC.Add(cc.Trim());
                }
            }
            if (!string.IsNullOrWhiteSpace(Config.Bcc))
            {
                foreach (var bcc in Config.Bcc.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.Bcc.Add(bcc.Trim());
                }
            }
            foreach (var att in Attachments)
            {
                mailMessage.Attachments.Add(att);
            }
            return PostProcess(mailMessage);
        }

        public void Send(string subject, params object[] formats)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Config.To) && Config.Enabled)
                {
                    var mailMessage = PrepareMessage(subject, formats);

                    var smtpClient = new SmtpClient();
                    smtpClient.Send(mailMessage);
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, "Send", ex);
                throw;
            }
        }

        protected void ConvertToImage(MailMessage message)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var typePrefix = GetType().Name.ToLower().Replace(".", "");

            System.Drawing.Image image = null;
            var byteStream = RIFF.Interfaces.Formats.HTML.RFHTMLRenderer.Render(message.Body, out image);
            if (byteStream != null && byteStream.Length > 0)
            {
                if (image.Height > 1000) // auto-split long images into parts
                {
                    var images = new List<Image>();
                    var bitmap = new Bitmap(image);
                    for (int i = 0; i < image.Height; i += 1000)
                    {
                        var imagePart = bitmap.Clone(new Rectangle { X = 0, Y = i, Width = image.Width, Height = Math.Min(1000, image.Height - i) }, System.Drawing.Imaging.PixelFormat.DontCare);
                        if (imagePart != null)
                        {
                            var tag = String.Format("Image{0}{1}{2}", datePrefix, typePrefix, i);
                            imagePart.Tag = tag;
                            images.Add(imagePart);
                        }
                    }
                    RenderImageList(images, message, null, null);
                }
                else
                {
                    var imageAttachment = new Attachment(new MemoryStream(byteStream), "Image.png", "image/png");
                    imageAttachment.ContentId = "Image";
                    message.Attachments.Add(imageAttachment);
                    message.Body = RFRazor.RunTemplate(typeof(ImageEmail), image);
                }
            }
            else
            {
                throw new RFSystemException(this, "Unable to reder email to image.");
            }
        }

        protected void ConvertToImageList(string[] sections, MailMessage message, string headerHTML = null, string footerHTML = null)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var typePrefix = GetType().Name.ToLower().Replace(".", "");
            int i = 1;
            var images = new List<System.Drawing.Image>();
            foreach (var section in sections.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                System.Drawing.Image image = null;
                var tag = String.Format("Image{0}{1}{2}", datePrefix, typePrefix, i);
                var byteStream = RIFF.Interfaces.Formats.HTML.RFHTMLRenderer.Render(section, out image);
                if (image != null && byteStream != null && byteStream.Length > 0)
                {
                    image.Tag = tag;
                    images.Add(image);
                }
                else
                {
                    throw new RFSystemException(this, "Unable to render email to image.");
                }
                i++;
            }
            RenderImageList(images, message, headerHTML, footerHTML);
        }

        protected virtual string GenerateBody()
        {
            return RFRazor.RunTemplate(typeof(T), this);
        }

        protected virtual MailMessage PostProcess(MailMessage message)
        {
            if (Config.SendAsImage)
            {
                ConvertToImage(message);
            }
            return message;
        }

        protected void RenderImageList(List<Image> images, MailMessage message, string headerHTML = null, string footerHTML = null)
        {
            foreach (var image in images)
            {
                using (var memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = memoryStream.ToArray();
                    var tag = image.Tag.ToString();
                    var imageAttachment = new Attachment(new MemoryStream(bytes), tag + ".png", "image/png");
                    imageAttachment.ContentId = image.Tag.ToString();
                    message.Attachments.Add(imageAttachment);
                }
            }
            message.Body = RFRazor.RunTemplate(typeof(ImageListEmail), new RFImageListEmailModel
            {
                Images = images.ToArray(),
                HeaderHTML = headerHTML,
                FooterHTML = footerHTML
            });
        }
    }

    [DataContract]
    public class RFEmailConfig
    {
        [DataMember]
        public string Bcc { get; set; }

        [DataMember]
        public string Cc { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        public bool SendAsImage { get; set; }

        [DataMember]
        public string To { get; set; }

        public static RFEmailConfig ReadFromConfig(IRFUserConfig config, string section, string emailName)
        {
            return new RFEmailConfig
            {
                Enabled = config.GetBool(section, emailName, true, true, "Enabled"),
                To = config.GetString(section, emailName, true, "To"),
                Cc = config.GetString(section, emailName, false, "Cc"),
                Bcc = config.GetString(section, emailName, false, "Bcc"),
                SendAsImage = config.GetBool(section, emailName, false, false, "Send as Image")
            };
        }
    }

    public class RFGenericEmail : RFEmail<GenericEmail>
    {
        public string Body { get; set; }

        public RFGenericEmail(RFEmailConfig config, string body) : base(config)
        {
            Body = body;
        }
    }

    public interface IRFEmail
    {
        void Send(string subject, params object[] formats);
    }
}
