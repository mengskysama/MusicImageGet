namespace FLAC_Comment_Editor
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    public class FLACComments
    {
        private const byte BlockType_Padding = 1;
        private const byte BlockType_VorbisComment = 4;
        public ArrayList commentNames = new ArrayList();
        public ArrayList commentValues = new ArrayList();
        public bool containsVorbisComments;
        private string filePath;
        private const int LastBlockFlag = -2147483648;
        public string vendorString = string.Empty;

        public FLACComments(string filePath)
        {
            this.filePath = filePath;
        }

        private int GetLengthAvailableForComments(BinaryReader fileBRead)
        {
            bool flag;
            int num3 = 0;
            fileBRead.BaseStream.Seek(4L, SeekOrigin.Begin);
            do
            {
                byte num;
                int num2;
                this.ReadBlockHeader(fileBRead, out flag, out num, out num2);
                switch (num)
                {
                    case 1:
                    case 4:
                        num3 += 4 + num2;
                        break;
                }
                fileBRead.BaseStream.Seek((long) num2, SeekOrigin.Current);
            }
            while (!flag);
            return num3;
        }

        private string GetTempPath(string filePath)
        {
            Random random = new Random();
            for (int i = 0; i < 20; i++)
            {
                string path = filePath + ".edit" + random.Next(0x3e8, 0x270f).ToString();
                if (!File.Exists(path))
                {
                    return path;
                }
            }
            throw new Exception("Unable to find an available temporary path.");
        }

        public void LoadComments()
        {
            FileStream input = new FileStream(this.filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader fileBRead = new BinaryReader(input);
            if (ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32()) != 0x664c6143)
            {
                throw new Exception("This is not a valid FLAC file (stream marker not found).");
            }
            if (this.SeekToVorbisCommentBlock(fileBRead) != -1)
            {
                this.containsVorbisComments = true;
                this.ReadVorbisCommentData(fileBRead, ref this.vendorString, this.commentNames, this.commentValues);
            }
            else
            {
                this.containsVorbisComments = false;
            }
            fileBRead.Close();
        }

        private void ReadBlockHeader(BinaryReader fileBRead, out bool isLastMetaBlock, out byte blockType, out int blockLength)
        {
            int num = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
            isLastMetaBlock = Convert.ToBoolean((long) (((long) ((ulong) num)) & -2147483648L));
            blockType = (byte) ((num & 0x7f000000) >> 0x18);
            blockLength = num & 0xffffff;
        }

        private void ReadVorbisCommentData(BinaryReader fileBRead, ref string vendorString, ArrayList commentNames, ArrayList commentValues)
        {
            int count = ByteOrdering.ToFromLittleEndian(fileBRead.ReadInt32());
            if (count > 0)
            {
                vendorString = Encoding.UTF8.GetString(fileBRead.ReadBytes(count));
            }
            int num2 = ByteOrdering.ToFromLittleEndian(fileBRead.ReadInt32());
            for (int i = 0; i < num2; i++)
            {
                int num4 = ByteOrdering.ToFromLittleEndian(fileBRead.ReadInt32());
                string str = Encoding.UTF8.GetString(fileBRead.ReadBytes(num4));
                int index = str.IndexOf("=");
                if (index == -1)
                {
                    throw new Exception("Invalid comment string (no \"=\" found).");
                }
                commentNames.Add(str.Substring(0, index).ToLower());
                commentValues.Add(str.Substring(index + 1).ToLower());
            }
        }

        public void SaveComments()
        {
            bool flag2;
            int num10;
            bool flag = true;
            int number = 0;
            int lengthAvailableForComments = 0;
            bool flag3 = false;
            int offset = 0;
            int count = 0x1000;
            byte[] buffer = new byte[count];
            string tempPath = this.GetTempPath(this.filePath);
            FileStream input = new FileStream(this.filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            BinaryReader fileBRead = new BinaryReader(input);
            FileStream output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
            BinaryWriter fileBWrite = new BinaryWriter(output);
            int num8 = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
            if (num8 != 0x664c6143)
            {
                throw new Exception("This is not a valid FLAC file (stream marker not found).");
            }
            fileBWrite.Write(ByteOrdering.ToFromBigEndian(num8));
            lengthAvailableForComments = this.GetLengthAvailableForComments(fileBRead);
            fileBRead.BaseStream.Seek(4L, SeekOrigin.Begin);
            do
            {
                byte num3;
                int num4;
                this.ReadBlockHeader(fileBRead, out flag2, out num3, out num4);
                if ((num3 != 4) && (num3 != 1))
                {
                    offset = (int) fileBWrite.BaseStream.Position;
                    number = (num3 << 0x18) | num4;
                    fileBWrite.Write(ByteOrdering.ToFromBigEndian(number));
                    if (num4 != 0)
                    {
                        fileBWrite.Write(fileBRead.ReadBytes(num4));
                    }
                }
                else
                {
                    fileBRead.BaseStream.Seek((long) num4, SeekOrigin.Current);
                }
                if ((this.containsVorbisComments && (num3 == 4)) || (!this.containsVorbisComments && flag))
                {
                    offset = (int) fileBWrite.BaseStream.Position;
                    fileBWrite.Write(0);
                    int num = this.WriteVorbisCommentData(fileBWrite, this.vendorString, this.commentNames, this.commentValues);
                    number = 0x4000000 | (num & 0xffffff);
                    fileBWrite.Seek(-(4 + num), SeekOrigin.Current);
                    fileBWrite.Write(ByteOrdering.ToFromBigEndian(number));
                    fileBWrite.Seek(num, SeekOrigin.Current);
                    lengthAvailableForComments -= 4 + num;
                    if (lengthAvailableForComments == 0)
                    {
                        flag3 = false;
                    }
                    else
                    {
                        int num6;
                        if (lengthAvailableForComments >= 4)
                        {
                            flag3 = false;
                            num6 = lengthAvailableForComments - 4;
                        }
                        else
                        {
                            flag3 = true;
                            num6 = 0x400;
                        }
                        offset = (int) fileBWrite.BaseStream.Position;
                        number = 0x1000000 | (num6 & 0xffffff);
                        fileBWrite.Write(ByteOrdering.ToFromBigEndian(number));
                        if (num6 != 0)
                        {
                            fileBWrite.Write(new byte[num6]);
                        }
                    }
                }
                flag = false;
            }
            while (!flag2);
            number |= -2147483648;
            fileBWrite.Seek(offset, SeekOrigin.Begin);
            fileBWrite.Write(ByteOrdering.ToFromBigEndian(number));
            if (!flag3)
            {
                fileBWrite.Flush();
                input.Seek(0L, SeekOrigin.Begin);
                output.Seek(0L, SeekOrigin.Begin);
                while ((num10 = output.Read(buffer, 0, count)) != 0)
                {
                    input.Write(buffer, 0, num10);
                }
                fileBRead.Close();
                fileBWrite.Close();
                File.Delete(tempPath);
            }
            else
            {
                fileBWrite.Seek(number & 0xffffff, SeekOrigin.Current);
                while ((num10 = input.Read(buffer, 0, count)) != 0)
                {
                    output.Write(buffer, 0, num10);
                }
                fileBRead.Close();
                fileBWrite.Close();
                File.Delete(this.filePath);
                File.Move(tempPath, this.filePath);
            }
        }

        private int SeekToVorbisCommentBlock(BinaryReader fileBRead)
        {
            bool flag;
            fileBRead.BaseStream.Seek(4L, SeekOrigin.Begin);
            do
            {
                byte num;
                int num2;
                this.ReadBlockHeader(fileBRead, out flag, out num, out num2);
                if (num == 4)
                {
                    return num2;
                }
                fileBRead.BaseStream.Seek((long) num2, SeekOrigin.Current);
            }
            while (!flag);
            return -1;
        }

        private int WriteVorbisCommentData(BinaryWriter fileBWrite, string vendorString, ArrayList commentNames, ArrayList commentValues)
        {
            int count = commentNames.Count;
            int num3 = 0;
            byte[] bytes = Encoding.UTF8.GetBytes(vendorString);
            fileBWrite.Write(ByteOrdering.ToFromLittleEndian(bytes.Length));
            fileBWrite.Write(bytes);
            num3 += 4 + bytes.Length;
            fileBWrite.Write(ByteOrdering.ToFromLittleEndian(count));
            num3 += 4;
            for (int i = 0; i < count; i++)
            {
                bytes = Encoding.UTF8.GetBytes(commentNames[i] + "=" + commentValues[i]);
                fileBWrite.Write(ByteOrdering.ToFromLittleEndian(bytes.Length));
                fileBWrite.Write(bytes);
                num3 += 4 + bytes.Length;
            }
            return num3;
        }
    }
}

