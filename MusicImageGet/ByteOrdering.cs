namespace FLAC_Comment_Editor
{
    using System;

    public class ByteOrdering
    {
        public static short ReverseByteOrder(short number)
        {
            ushort num = (ushort) number;
            return (short) (((num >> 8) & 0xff) | ((num << 8) & 0xff00));
        }

        public static int ReverseByteOrder(int number)
        {
            uint num = (uint) number;
            return (int) (((((num >> 0x18) & 0xff) | ((num >> 8) & 0xff00)) | ((num << 8) & 0xff0000)) | ((num << 0x18) & 0xff000000));
        }

        public static long ReverseByteOrder(long number)
        {
            ulong num = (ulong) number;
            return (long) (((((((((num >> 0x38) & ((ulong) 0xffL)) | ((num >> 40) & ((ulong) 0xff00L))) | ((num >> 0x18) & ((ulong) 0xff0000L))) | ((num >> 8) & 0xff000000L)) | ((num << 8) & ((ulong) 0xff00000000L))) | ((num << 0x18) & ((ulong) 0xff0000000000L))) | ((num << 40) & ((ulong) 0xff000000000000L))) | ((num << 0x38) & 18374686479671623680L));
        }

        public static ushort ReverseByteOrder(ushort number)
        {
            return (ushort) (((number >> 8) & 0xff) | ((number << 8) & 0xff00));
        }

        public static uint ReverseByteOrder(uint number)
        {
            return (((((number >> 0x18) & 0xff) | ((number >> 8) & 0xff00)) | ((number << 8) & 0xff0000)) | ((number << 0x18) & 0xff000000));
        }

        public static ulong ReverseByteOrder(ulong number)
        {
            return (((((((((number >> 0x38) & ((ulong) 0xffL)) | ((number >> 40) & ((ulong) 0xff00L))) | ((number >> 0x18) & ((ulong) 0xff0000L))) | ((number >> 8) & 0xff000000L)) | ((number << 8) & ((ulong) 0xff00000000L))) | ((number << 0x18) & ((ulong) 0xff0000000000L))) | ((number << 40) & ((ulong) 0xff000000000000L))) | ((number << 0x38) & 18374686479671623680L));
        }

        public static short ToFromBigEndian(short number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static int ToFromBigEndian(int number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static long ToFromBigEndian(long number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static ushort ToFromBigEndian(ushort number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static uint ToFromBigEndian(uint number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static ulong ToFromBigEndian(ulong number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return number;
            }
            return ReverseByteOrder(number);
        }

        public static short ToFromLittleEndian(short number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }

        public static int ToFromLittleEndian(int number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }

        public static long ToFromLittleEndian(long number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }

        public static ushort ToFromLittleEndian(ushort number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }

        public static uint ToFromLittleEndian(uint number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }

        public static ulong ToFromLittleEndian(ulong number)
        {
            if (!BitConverter.IsLittleEndian)
            {
                return ReverseByteOrder(number);
            }
            return number;
        }
    }
}

