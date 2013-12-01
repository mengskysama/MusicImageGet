using FLAC_Comment_Editor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace MusicImageGet
{
    class METADATA_BLOCK
    {
        //<1>
        public int END_BLOCK = 0;

        public enum ENUM_BLOCK_TYPE
        {
            STREAMINFO,
            PADDING,
            APPLICATION,
            SEEKTABLE,
            VORBIS_COMMENT,
            CUESHEET,
            PICTURE
        }

        //<7>
        public ENUM_BLOCK_TYPE BLOCK_TYPE;

        //<24>	 Length (in bytes) of metadata to follow (does not include the size of the METADATA_BLOCK_HEADER)

        public Byte[] data = null;

        public Byte[] pack_block()
        {
            Byte[] bytes = new Byte[1 + 3 + data.Length];

            bytes[0] = ((Byte)(END_BLOCK << 7));
            bytes[0] |= (Byte)BLOCK_TYPE;

            bytes[1] = (Byte)(data.Length >> 16);
            bytes[2] = (Byte)(data.Length >> 8);
            bytes[3] = (Byte)(data.Length >> 0);

            Array.Copy(data, 0, bytes, 4, data.Length);
            return bytes;
        }
    }

    class METADATA_BLOCK_PICTURE : METADATA_BLOCK
    {
        //ID3v2
        public enum ENUM_PICTURE_TYPE
        {
            Other,
            iconPNG,
            icon,
            Coverfront,
            Coverback,
            Leafletpage,
            Media
            //...
        }

        //32
        public ENUM_PICTURE_TYPE PICTURE_TYPE;

        /*
          <32>	 The length of the MIME type string in bytes.
          <n*8>	 The MIME type string, in printable ASCII characters 0x20-0x7e. The MIME type may also be --> to signify that the data part is a URL of the picture instead of the picture data itself.
          <32>	 The length of the description string in bytes.
          <n*8>	 The description of the picture, in UTF-8.
         */

        //32
        public int width;
        //32
        public int height;
        //32
        public int depth;

        //<32>	 For indexed-color pictures (e.g. GIF), the number of colors used, or 0 for non-indexed pictures.
        //public int length;
        //public Byte[] pic_data = null;

        public Bitmap pic = null;

        public METADATA_BLOCK_PICTURE()
        {
            BLOCK_TYPE = ENUM_BLOCK_TYPE.PICTURE;
        }

        public Byte[] pack_pic()
        {
            MemoryStream ms = new MemoryStream();
            //转换成jpeg
            pic.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            Byte[] pic_data = ms.GetBuffer();
            ms.Close();

            Byte[] bytes = new Byte[4 + 4 + 0 + 4 + 0 + 4 + 4 + 4 + 4 + 4 + pic_data.Length];
            FlacHelper.int2byte(bytes, (int)PICTURE_TYPE, 0);
            for (int i = 4; i < 12; i++)
                bytes[i] = (Byte)0;
            FlacHelper.int2byte(bytes, pic.Width, 12);
            FlacHelper.int2byte(bytes, pic.Height, 16);
            FlacHelper.int2byte(bytes, 24, 20);
            FlacHelper.int2byte(bytes, 0, 24);
            FlacHelper.int2byte(bytes, pic_data.Length, 28);
            Array.Copy(pic_data, 0, bytes, 32, pic_data.Length);

            data = bytes;

            return pack_block();
        }
    }

    class METADATA_BLOCK_VORBIS_COMMENT : METADATA_BLOCK
    {

    }

    class FlacHelper : MusicHelper
    {
        static public void int2byte(Byte[] buff, int n, int index)
        {
            buff[index + 0] = (Byte)(n >> 24);
            buff[index + 1] = (Byte)(n >> 16);
            buff[index + 2] = (Byte)(n >> 8);
            buff[index + 3] = (Byte)(n >> 0);
        }

        public static int _3byte2Int(byte[] buff, int index)
        {
            int intValue = 0;
            for (int i = 0; i < 3; i++)
                intValue += (buff[index + i] & 0xFF) << (8 * (2 - i));
            return intValue;
        }

        public override int RemoveCovrMeta()
        {
            Byte[] buff = System.IO.File.ReadAllBytes(filepath);

            int offset = 4;
            int isfinshed = 0;
            int last_meta_offset = -1;
            int last_meta_offset_len = -1;

            while (offset < buff.Length && isfinshed == 0)
            {
                int type = buff[offset] & 0x7F;
                isfinshed = buff[offset] >> 7;
                int t = offset;
                offset++;
                
                int block_size = _3byte2Int(buff, offset);
                offset += 3;

                offset += block_size;

                if (type == 6)
                {
                    if (isfinshed == 1)
                    {
                        //删除图片的是最后一个元素
                        if (last_meta_offset != -1)
                            buff[last_meta_offset] |= 0x80;
                        else
                            buff[4] |= 0x80;
                    }
                    Byte[] newbuff = new Byte[buff.Length - (block_size + 3 + 1)];
                    Array.Copy(buff, newbuff, t);
                    Array.Copy(buff, offset, newbuff, t, buff.Length - offset);
                    System.IO.File.WriteAllBytes(filepath, newbuff);
                    return 0;
                }

                last_meta_offset = t;
                last_meta_offset_len = offset - last_meta_offset;
            }
            return 1;
        }

        public override int AddCovrMeta(Byte[] pic_metadata)
        {
            try
            {
                Byte[] buff = System.IO.File.ReadAllBytes(filepath);

                int block_size = _3byte2Int(buff, 4 + 1);
                int offset = 4 + 1 + 3 + block_size;

                Byte[] n = new Byte[buff.Length + pic_metadata.Length];
                Array.Copy(buff, 0, n, 0, offset);
                Array.Copy(pic_metadata, 0, n, offset, pic_metadata.Length);
                Array.Copy(buff, offset, n, pic_metadata.Length + offset, buff.Length - offset);

                System.IO.File.WriteAllBytes(filepath, n);
                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        static public List<METADATA_BLOCK> parse_metadata_block(Byte[] buff)
        { 
            List<METADATA_BLOCK> l = new List<METADATA_BLOCK>();

            int offset = 4;
            int isfinshed = 0;

            while (offset < buff.Length && isfinshed == 0)
            {
                int n = buff[offset] & 0x7F;
                isfinshed = buff[offset] >> 7;
                offset++;
                int block_size = _3byte2Int(buff, offset);
                offset += 3;

                switch (n)
                {
                    case 6:
                        //METADATA_BLOCK_PICTURE m = new METADATA_BLOCK_PICTURE();
                        //
                        break;
                }

                offset += block_size;
            }

            return l;
        }

        public FlacHelper(string filepath)
            : base(filepath)
        { }

        public override int GetComment()
        {
            FLACComments coments = new FLACComments(filepath);
            coments.LoadComments();

            int n = coments.commentNames.IndexOf("album");
            if (n != -1 && coments.commentValues[n].ToString() != "")
                album = coments.commentValues[n].ToString();

            n = coments.commentNames.IndexOf("artist");
            if (n != -1 && coments.commentValues[n].ToString() != "")
                artist = coments.commentValues[n].ToString();

            n = coments.commentNames.IndexOf("title");
            if (n != -1 && coments.commentValues[n].ToString() != "")
                title = coments.commentValues[n].ToString();

            n = coments.commentNames.IndexOf("albumartist");
            if (n != -1 && coments.commentValues[n].ToString() != "")
                album_artist = coments.commentValues[n].ToString();

            return 0;
        }
    }
}
