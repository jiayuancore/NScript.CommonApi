using System.Runtime.InteropServices;

namespace NScript.CommonApi;

public struct OutputPayload
{
    public static OutputPayload Empty => new OutputPayload(IntPtr.Zero, 0);
    public unsafe static OutputPayload TransferFromBytes(byte[] bytes)
    {
        var payload = OutputPayload.Empty;
        fixed (byte* pData = bytes)
        {
            payload = new OutputPayload((IntPtr)pData, bytes.Length);
        }

        return payload;
    }

    public IntPtr DataPointer;
    public int Length;

    public OutputPayload(IntPtr dataPointer, int length)
    {
        DataPointer = dataPointer;
        Length = length;
    }

    public bool IsEmpty => Length <= 0 || DataPointer == IntPtr.Zero;

    public unsafe Span<byte> AsSpan()
    {
        return new Span<byte>(DataPointer.ToPointer(), Length);
    }

    public unsafe byte[] ToArray()
    {
        byte[] array = new byte[Length];
        fixed (byte* pArray = array)
        {
            Buffer.MemoryCopy(DataPointer.ToPointer(), pArray, Length, Length);
        }
        return array;
    }
}
