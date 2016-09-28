using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ses.Abstracts
{
    public static class SequentialGuid
    {
        [DllImport("rpcrt4.dll", SetLastError = true)]
        static extern int UuidCreateSequential(out Guid guid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid NewGuid()
        {
            Guid guid;
            UuidCreateSequential(out guid);
            var s = guid.ToByteArray();
            var buffer = new byte[16];
            buffer[3] = s[0];
            buffer[2] = s[1];
            buffer[1] = s[2];
            buffer[0] = s[3];
            buffer[5] = s[4];
            buffer[4] = s[5];
            buffer[7] = s[6];
            buffer[6] = s[7];
            buffer[8] = s[8];
            buffer[9] = s[9];
            buffer[10] = s[10];
            buffer[11] = s[11];
            buffer[12] = s[12];
            buffer[13] = s[13];
            buffer[14] = s[14];
            buffer[15] = s[15];
            return new Guid(buffer);
        }
    }
}
