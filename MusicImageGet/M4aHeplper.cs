using FLAC_Comment_Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MusicImageGet
{
    class M4aHeplper : MusicHelper
    {
        enum CMP4TAGATOM_ID
        {
            CMP4TAGATOM_ERROR = 0,     // 初始化值  
            CMP4TAGATOM_ALBUM,         // 专辑  
            CMP4TAGATOM_ALBUMARTIST,     
            CMP4TAGATOM_ARTIST,        // 艺术家  
            CMP4TAGATOM_NAME,          // 名称  
            CMP4TAGATOM_DATE,          // 日期  
            CMP4TAGATOM_GENRE,         // 流派  
            CMP4TAGATOM_COVER,         // 封面  
        }

        public override int RemoveCovrMeta()
        {
            FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            BinaryReader fileBRead = new BinaryReader(input);
            BinaryWriter fileBWriter = new BinaryWriter(input);

            try
            {
                int headsize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());

                if (fileBRead.ReadByte() != 'f' ||
                    fileBRead.ReadByte() != 't' ||
                    fileBRead.ReadByte() != 'y' ||
                    fileBRead.ReadByte() != 'p' )
                {
                    throw new Exception("This is not a valid ACCA M4A file (stream marker not found).");
                }

                //移动到ATOM开始位置
                fileBRead.BaseStream.Seek(headsize, SeekOrigin.Begin);

                byte[] atom = new byte[4];

                Stack<long> node_index = new Stack<long>();

                do
                {
                    if (fileBRead.BaseStream.Position == fileBRead.BaseStream.Length)
                    {
                        //表示文件完整!正常结束
                        break;
                    }
                    //读取当前atom总长度
                    int atomSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                    //读取atom类型
                    fileBRead.Read(atom, 0, 4);

                    CMP4TAGATOM_ID currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ERROR;

                    if ((atom[0] == 'm' && atom[1] == 'o' && atom[2] == 'o' && atom[3] == 'v') // moov  
                        || (atom[0] == 't' && atom[1] == 'r' && atom[2] == 'a' && atom[3] == 'k') // trak  
                        || (atom[0] == 'm' && atom[1] == 'd' && atom[2] == 'i' && atom[3] == 'a') // mdia  
                        || (atom[0] == 'm' && atom[1] == 'i' && atom[2] == 'n' && atom[3] == 'f') // minf  
                        || (atom[0] == 's' && atom[1] == 't' && atom[2] == 'b' && atom[3] == 'l') // stbl  
                        || (atom[0] == 'u' && atom[1] == 'd' && atom[2] == 't' && atom[3] == 'a') // udta  
                        || (atom[0] == 'i' && atom[1] == 'l' && atom[2] == 's' && atom[3] == 't') // ilst，TAG信息都在这个ATOM之下  

                       )
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        continue;
                    }
                    // 此ATOM之后的4个字节是大小需要向后移动4个字节的文件指针  
                    else if (atom[0] == 'm' && atom[1] == 'e' && atom[2] == 't' && atom[3] == 'a')
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        fileBRead.BaseStream.Seek(4, SeekOrigin.Current);
                        continue;
                    }
                    else if (atom[0] == 's' && atom[1] == 't' && atom[2] == 'c' && atom[3] == 'o')
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                        continue;
                    }
                    // 解析封面图片  
                    else if (atom[0] == 'c' && atom[1] == 'o' && atom[2] == 'v' && atom[3] == 'r')
                    {
                        int skipSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                        long posBegin = fileBRead.BaseStream.Position - 4 - 4 - 4;

                        fileBRead.BaseStream.Seek(skipSize - 4, SeekOrigin.Current);

                        long left_size = fileBRead.BaseStream.Length - fileBRead.BaseStream.Position;
                        byte[] tmp = new byte[left_size];
                        fileBRead.Read(tmp, 0, (int)left_size);

                        fileBWriter.BaseStream.Seek(posBegin, SeekOrigin.Begin);
                        fileBWriter.Write(tmp);
                        fileBWriter.BaseStream.SetLength(fileBRead.BaseStream.Length - skipSize - 4 - 4);

                        //节长度
                        long del_bengin = posBegin;
                        while (node_index.Count > 0)
                        {
                            long index = node_index.Pop();
                            fileBRead.BaseStream.Seek(index, SeekOrigin.Begin);
                            int len = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());

                            fileBRead.Read(atom, 0, 4);
                            if ((atom[0] == 'm' && atom[1] == 'o' && atom[2] == 'o' && atom[3] == 'v') // moov  
                                || (atom[0] == 'i' && atom[1] == 'l' && atom[2] == 's' && atom[3] == 't') // ilst，TAG信息都在这个ATOM之下  
                                || (atom[0] == 'm' && atom[1] == 'e' && atom[2] == 't' && atom[3] == 'a')
                                || (atom[0] == 'u' && atom[1] == 'd' && atom[2] == 't' && atom[3] == 'a')
                               )
                            {
                                //if (del_bengin - index <= len + 4)
                                {
                                    fileBWriter.BaseStream.Seek(index, SeekOrigin.Begin);
                                    fileBWriter.Write(ByteOrdering.ToFromBigEndian(len - skipSize - 4 - 4));
                                }
                            }
                            else if (atom[0] == 's' && atom[1] == 't' && atom[2] == 'c' && atom[3] == 'o')
                            {
                                //原子偏移
                                //stco长度
                                fileBRead.BaseStream.Seek(-8, SeekOrigin.Current);
                                len = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32()) - 4;
                                fileBRead.BaseStream.Seek(12, SeekOrigin.Current);
                                fileBWriter.BaseStream.Seek(fileBRead.BaseStream.Position, SeekOrigin.Begin);
                                len -= 12;
                                while (len > 0)
                                {
                                    int offset = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                                    fileBWriter.BaseStream.Seek(-4, SeekOrigin.Current);
                                    fileBWriter.Write(ByteOrdering.ToFromBigEndian(offset - skipSize - 4 - 4));
                                    len -= 4;
                                }
                            }
                        }
                        fileBRead.Close();
                        fileBWriter.Close();
                        return 0;
                    }
                    // 是需要解析的ATOM  
                    if (currentAtom == CMP4TAGATOM_ID.CMP4TAGATOM_ERROR)
                    {
                        //总长度包含自身长度
                        fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                    }
                } while (true);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                fileBRead.Close();
                fileBWriter.Close();
            }
            return 1;
        }

        public override int AddCovrMeta(byte [] pic_metadata)
        {
            FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            BinaryReader fileBRead = new BinaryReader(input);
            BinaryWriter fileBWriter = new BinaryWriter(input);

            try
            {
                int headsize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());

                if (fileBRead.ReadByte() != 'f' ||
                    fileBRead.ReadByte() != 't' ||
                    fileBRead.ReadByte() != 'y' ||
                    fileBRead.ReadByte() != 'p' )
                {
                    throw new Exception("This is not a valid ACCA M4A file (stream marker not found).");
                }

                //移动到ATOM开始位置
                fileBRead.BaseStream.Seek(headsize, SeekOrigin.Begin);

                byte[] atom = new byte[4];

                Stack<long> node_index = new Stack<long>();

                do
                {
                    if (fileBRead.BaseStream.Position == fileBRead.BaseStream.Length)
                    {
                        //表示文件完整!正常结束
                        break;
                    }
                    //读取当前atom总长度
                    int atomSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                    //读取atom类型
                    fileBRead.Read(atom, 0, 4);

                    CMP4TAGATOM_ID currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ERROR;

                    if ((atom[0] == 'm' && atom[1] == 'o' && atom[2] == 'o' && atom[3] == 'v') // moov  
                        || (atom[0] == 't' && atom[1] == 'r' && atom[2] == 'a' && atom[3] == 'k') // trak  
                        || (atom[0] == 'm' && atom[1] == 'd' && atom[2] == 'i' && atom[3] == 'a') // mdia  
                        || (atom[0] == 'm' && atom[1] == 'i' && atom[2] == 'n' && atom[3] == 'f') // minf  
                        || (atom[0] == 's' && atom[1] == 't' && atom[2] == 'b' && atom[3] == 'l') // stbl  
                        || (atom[0] == 'u' && atom[1] == 'd' && atom[2] == 't' && atom[3] == 'a') // udta  
                        || (atom[0] == 'i' && atom[1] == 'l' && atom[2] == 's' && atom[3] == 't') // ilst，TAG信息都在这个ATOM之下  

                       )
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        continue;
                    }
                    // 此ATOM之后的4个字节是大小需要向后移动4个字节的文件指针  
                    else if (atom[0] == 'm' && atom[1] == 'e' && atom[2] == 't' && atom[3] == 'a')
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        fileBRead.BaseStream.Seek(4, SeekOrigin.Current);
                        continue;
                    }
                    else if (atom[0] == 's' && atom[1] == 't' && atom[2] == 'c' && atom[3] == 'o')
                    {
                        //保存node的节点位置
                        node_index.Push(fileBRead.BaseStream.Position - 4 - 4);
                        fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                        continue;
                    }
                    // 解析专辑、艺术家、名称、年份日期，这些第一个字节值为0xA9  
                    else if (atom[0] == 0xA9)
                    {
                        int flag = 0;

                        if (atom[1] == 't' && atom[2] == 'o' && atom[3] == 'o')
                        {
                            int len = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                            fileBRead.BaseStream.Seek(len - 4 - 4, SeekOrigin.Current);
                            atomSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                            fileBRead.Read(atom, 0, 4);
                            if (atom[0] == 0xA9 && atom[1] == 'd' && atom[2] == 'a' && atom[3] == 'y')
                            {
                                fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                                flag = 1;
                            }
                            else
                            {
                                //有时候只有这个节但没day
                                fileBRead.BaseStream.Seek(fileBRead.BaseStream.Position - 4, SeekOrigin.Begin);
                                flag = 1;
                            }
                        }
                        if(flag == 1)
                        {
                            long left_size = fileBRead.BaseStream.Length - fileBRead.BaseStream.Position;
                            byte[] tmp = new byte[left_size];
                            fileBRead.Read(tmp, 0, (int)left_size);
                            //增加空间
                        // |    XXXX     |     AAAA     |    xxxx     |     data    |    ver    |   flag   |  reserved   | realdata    |  
                        //  总长度4字节     标识符4字节   长度4字节    固定符号4字节     1字节      3字节     保留4字节    剩余实际数据  
                        // | 00 00 00 1C | A9 61 6C 62  | 00 00 00 14 | 64 61 74 61 |     00    | 00 00 01 | 00 00 00 00 | 43 43 43 43 |  
                        //     总长28      标识符A9alb      长度20       字符data                                          实际数据CCCC  
                        // 其中流派的实际数据为2个字节，给出的是索引值，需要拿这个索
                            fileBWriter.BaseStream.SetLength(fileBRead.BaseStream.Length + pic_metadata.Length + 4 * 6);
                            fileBWriter.BaseStream.Seek(-left_size, SeekOrigin.Current);
                            //写入长度
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(pic_metadata.Length + 4 * 6));
                            //covr
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(0x636F7672));
                            //长度
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(pic_metadata.Length + 4 * 4));
                            //data
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(0x64617461));
                            //
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(0x0D));
                            fileBWriter.Write(ByteOrdering.ToFromBigEndian(0x00));
                            fileBWriter.Write(pic_metadata);
                            fileBWriter.Write(tmp);

                            //节长度
                            while (node_index.Count > 0)
                            {
                                long index = node_index.Pop();
                                fileBRead.BaseStream.Seek(index, SeekOrigin.Begin);
                                int len = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());

                                fileBRead.Read(atom, 0, 4);
                                if ((atom[0] == 'm' && atom[1] == 'o' && atom[2] == 'o' && atom[3] == 'v') // moov  
                                    || (atom[0] == 'i' && atom[1] == 'l' && atom[2] == 's' && atom[3] == 't') // ilst，TAG信息都在这个ATOM之下  
                                    || (atom[0] == 'm' && atom[1] == 'e' && atom[2] == 't' && atom[3] == 'a')
                                    || (atom[0] == 'u' && atom[1] == 'd' && atom[2] == 't' && atom[3] == 'a')
                                   )
                                {
                                    //if (del_bengin - index <= len + 4)
                                    {
                                        fileBWriter.BaseStream.Seek(index, SeekOrigin.Begin);
                                        fileBWriter.Write(ByteOrdering.ToFromBigEndian(len + pic_metadata.Length + 4 * 6));
                                    }
                                }
                                else if (atom[0] == 's' && atom[1] == 't' && atom[2] == 'c' && atom[3] == 'o')
                                {
                                    //原子偏移
                                    //stco长度
                                    fileBRead.BaseStream.Seek(-8, SeekOrigin.Current);
                                    len = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32()) - 4;
                                    fileBRead.BaseStream.Seek(12, SeekOrigin.Current);
                                    fileBWriter.BaseStream.Seek(fileBRead.BaseStream.Position, SeekOrigin.Begin);
                                    len -= 12;
                                    while (len > 0)
                                    {
                                        int offset = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                                        fileBWriter.BaseStream.Seek(-4, SeekOrigin.Current);
                                        fileBWriter.Write(ByteOrdering.ToFromBigEndian(offset + pic_metadata.Length + 4 * 6));
                                        len -= 4;
                                    }
                                }
                            }
                            fileBRead.Close();
                            fileBWriter.Close();
                            return 0;
                        }
                    }
                    // 是需要解析的ATOM  
                    if (currentAtom == CMP4TAGATOM_ID.CMP4TAGATOM_ERROR)
                    {
                        //总长度包含自身长度
                        fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                    }
                } while (true);
            }
            catch (Exception)
            {
                throw;
            }
            fileBRead.Close();
            fileBWriter.Close();
            return 1;
        }

        public override int GetComment()
        {
            try
            {

                FileStream input = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryReader fileBRead = new BinaryReader(input);

                int headsize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());

                if (fileBRead.ReadByte() != 'f' ||
                    fileBRead.ReadByte() != 't' ||
                    fileBRead.ReadByte() != 'y' ||
                    fileBRead.ReadByte() != 'p' )
                {
                    throw new Exception("This is not a valid ACCA M4A file (stream marker not found).");
                }

                //移动到ATOM开始位置
                fileBRead.BaseStream.Seek(headsize, SeekOrigin.Begin);

                byte[] atom = new byte[4];

                do
                {
                    if (fileBRead.BaseStream.Position == fileBRead.BaseStream.Length)
                    {
                        //表示文件完整!正常结束
                        break;
                    }
                    //读取当前atom总长度
                    int atomSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32());
                    //读取atom类型
                    fileBRead.Read(atom, 0, 4);

                    CMP4TAGATOM_ID currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ERROR;

                    if ((atom[0] == 'm' && atom[1] == 'o' && atom[2] == 'o' && atom[3] == 'v') // moov  
                        || (atom[0] == 't' && atom[1] == 'r' && atom[2] == 'a' && atom[3] == 'k') // trak  
                        || (atom[0] == 'm' && atom[1] == 'd' && atom[2] == 'i' && atom[3] == 'a') // mdia  
                        || (atom[0] == 'm' && atom[1] == 'i' && atom[2] == 'n' && atom[3] == 'f') // minf  
                        || (atom[0] == 's' && atom[1] == 't' && atom[2] == 'b' && atom[3] == 'l') // stbl  
                        || (atom[0] == 'u' && atom[1] == 'd' && atom[2] == 't' && atom[3] == 'a') // udta  
                        || (atom[0] == 'i' && atom[1] == 'l' && atom[2] == 's' && atom[3] == 't') // ilst，TAG信息都在这个ATOM之下  
                       )
                    {
                        continue;
                    }
                    // 此ATOM之后的4个字节是大小需要向后移动4个字节的文件指针  
                    else if ((atom[0] == 'm' && atom[1] == 'e' && atom[2] == 't' && atom[3] == 'a'))
                    {
                        fileBRead.BaseStream.Seek(4, SeekOrigin.Current);
                        continue;
                    }
                    // 解析专辑、艺术家、名称、年份日期，这些第一个字节值为0xA9  
                    else if (atom[0] == 0xA9)
                    {
                        // 专辑  
                        if (atom[1] == 'a' && atom[2] == 'l' && atom[3] == 'b')
                        {
                            currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ALBUM;
                        }
                        // 艺术家  
                        else if (atom[1] == 'A' && atom[2] == 'R' && atom[3] == 'T')
                        {
                            currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ARTIST;
                        }
                        // 名称  
                        else if (atom[1] == 'n' && atom[2] == 'a' && atom[3] == 'm')
                        {
                            currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_NAME;
                        }
                        // 日期  
                        else if (atom[1] == 'd' && atom[2] == 'a' && atom[3] == 'y')
                        {
                            currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_DATE;
                        }
                    }
                    // 解析流派  
                    else if (atom[0] == 'g' && atom[1] == 'n' && atom[2] == 'r' && atom[3] == 'e')
                    {
                        currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_GENRE;
                    }
                    // 解析封面图片  
                    else if (atom[0] == 'c' && atom[1] == 'o' && atom[2] == 'v' && atom[3] == 'r')
                    {
                        currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_COVER;
                    }
                    // 解析流派  
                    else if (atom[0] == 'a' && atom[1] == 'A' && atom[2] == 'R' && atom[3] == 'T')
                    {
                        currentAtom = CMP4TAGATOM_ID.CMP4TAGATOM_ALBUMARTIST;
                    }

                    // 是需要解析的ATOM  
                    if (currentAtom != CMP4TAGATOM_ID.CMP4TAGATOM_ERROR)
                    {
                        // 给出需要解析的ATOM的格式及实际数据例子如下  
                        // |    XXXX     |     AAAA     |    xxxx     |     data    |    ver    |   flag   |  reserved   | realdata    |  
                        //  总长度4字节     标识符4字节   长度4字节    固定符号4字节     1字节      3字节     保留4字节    剩余实际数据  
                        // | 00 00 00 1C | A9 61 6C 62  | 00 00 00 14 | 64 61 74 61 |     00    | 00 00 01 | 00 00 00 00 | 43 43 43 43 |  
                        //     总长28      标识符A9alb      长度20       字符data                                          实际数据CCCC  
                        // 其中流派的实际数据为2个字节，给出的是索引值，需要拿这个索引值在流派类型数组中取出流派字符串  
                        // 专辑、艺术家、名称、日期的实际数据是UTF-8编码  
                        // 封面的实际数据就是整个图片数据  
                        // 当前文件指针位置为长度xxxx的起始位置  
                        // 读取长度及标识符，读取失败直接跳出循环结束  
                        int atomRealSize = ByteOrdering.ToFromBigEndian(fileBRead.ReadInt32()) - 4 - 1 - 3 - 4 - 4;

                        if (fileBRead.ReadByte() != 'd' ||
                            fileBRead.ReadByte() != 'a' ||
                            fileBRead.ReadByte() != 't' ||
                            fileBRead.ReadByte() != 'a')
                        {
                            throw new Exception("This is not a valid ACCA M4A file (atom [data] not found).");
                        }

                        fileBRead.BaseStream.Seek(1 + 3 + 4, SeekOrigin.Current);

                        switch (currentAtom)
                        {
                            case CMP4TAGATOM_ID.CMP4TAGATOM_ALBUM:
                                album = Encoding.UTF8.GetString(fileBRead.ReadBytes(atomRealSize));
                                break;
                            case CMP4TAGATOM_ID.CMP4TAGATOM_ARTIST:
                                artist = Encoding.UTF8.GetString(fileBRead.ReadBytes(atomRealSize));
                                break;
                            case CMP4TAGATOM_ID.CMP4TAGATOM_NAME:
                                title = Encoding.UTF8.GetString(fileBRead.ReadBytes(atomRealSize));
                                break;
                            case CMP4TAGATOM_ID.CMP4TAGATOM_ALBUMARTIST:
                                album_artist = Encoding.UTF8.GetString(fileBRead.ReadBytes(atomRealSize));
                                break;
                            case CMP4TAGATOM_ID.CMP4TAGATOM_COVER:
                                cover = new byte[atomRealSize];
                                fileBRead.Read(cover, 0, atomRealSize);
                                break;
                            default:
                                fileBRead.BaseStream.Seek(atomRealSize, SeekOrigin.Current);
                                break;
                        }
                    }
                    else
                    {
                        //总长度包含自身长度
                        fileBRead.BaseStream.Seek(atomSize - 4 - 4, SeekOrigin.Current);
                    }
                } while (true);
                fileBRead.Close();
            }
            catch (Exception)
            {
                throw;
            }
            return 0;
        }

        public M4aHeplper(string filepath):base(filepath)
        {
        }
    }
}
