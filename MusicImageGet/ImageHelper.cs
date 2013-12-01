using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace MusicImageGet
{
    class ImageHelper
    {
        //调整图片宽高
        static public Bitmap PicSized(Bitmap originBmp, int maxSize = 400)
        {
            int w,h;
            if (originBmp.Width > maxSize)
            {
                w = maxSize;
                h = (int)(originBmp.Height * ( w / (float)originBmp.Width));
            }
            else if (originBmp.Height > maxSize)
            {
                h = maxSize;
                w = (int)(originBmp.Width * ( h / (float)originBmp.Height));
            }
            else
            {
                return originBmp;
            }
            Bitmap resizedBmp = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(resizedBmp);
            //设置高质量插值法  
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
            //设置高质量,低速度呈现平滑程度  
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.Default;
            //消除锯齿
            g.DrawImage(originBmp, new Rectangle(0, 0, w, h), new Rectangle(0, 0, originBmp.Width, originBmp.Height), GraphicsUnit.Pixel);
            g.Dispose();
            originBmp.Dispose();
            return resizedBmp;
        }

        public static byte [] ProcessImg(byte [] img)
        {
            MemoryStream ms1 = new MemoryStream(img); 
            Bitmap   bm   =   (Bitmap)Image.FromStream(ms1);
            //resize
            bm = ImageHelper.PicSized(bm, 650);
            MemoryStream ms = new MemoryStream();
            bm.Save(ms, ImageFormat.Png);
            ms1.Close();
            return ms.GetBuffer();
        }
    }
}
