using System.Runtime.CompilerServices;

namespace PlaceOnSlabs.Source.Utils;

public sealed class PackedOffsetArray
{
    private const int BitsPerValue = 4; // 16 values
    private const ulong ValueMask = (1UL << BitsPerValue) - 1UL;

    private readonly ulong[] data;
    private readonly int length;

    public PackedOffsetArray(int length)
    {
        this.length = length;
        int totalBits = length * BitsPerValue;
        data = new ulong[(totalBits + 63) >> 6];
    }

    public int Length => length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Get(int index)
    {
        int bitIndex = index * BitsPerValue;
        int wordIndex = bitIndex >> 6;
        int bitOffset = bitIndex & 63;

        ulong lo = data[wordIndex];
        ulong hi = (wordIndex + 1 < data.Length) ? data[wordIndex + 1] : 0UL;

        ulong combined = (lo >> bitOffset) | (hi << (64 - bitOffset));

        return (uint)(combined & ValueMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, uint value)
    {
        ulong v = value & ValueMask;

        int bitIndex = index * BitsPerValue;
        int wordIndex = bitIndex >> 6;
        int bitOffset = bitIndex & 63;

        ulong loMask = ValueMask << bitOffset;
        ulong hiMask = ValueMask >> (64 - bitOffset);

        ulong lo = data[wordIndex];
        ulong hi = (wordIndex + 1 < data.Length) ? data[wordIndex + 1] : 0UL;

        lo = (lo & ~loMask) | (v << bitOffset);
        hi = (hi & ~hiMask) | (v >> (64 - bitOffset));

        data[wordIndex] = lo;

        if (wordIndex + 1 < data.Length)
            data[wordIndex + 1] = hi;
    }

    public uint this[int index]
    {
        get => Get(index);
        set => Set(index, value);
    }
}