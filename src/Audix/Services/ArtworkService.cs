using System;
using System.Drawing;
using System.IO;

namespace Audix.Services
{
    public class ArtworkService
    {
        public Image? Extract(string filePath)
        {
            try
            {
                var file = TagLib.File.Create(filePath);
                if (file.Tag.Pictures != null && file.Tag.Pictures.Length > 0)
                {
                    var picture = file.Tag.Pictures[0];
                    using (var ms = new MemoryStream(picture.Data.Data))
                    {
                        return Image.FromStream(ms);
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
