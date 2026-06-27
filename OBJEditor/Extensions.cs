using System;
using System.IO;
using System.Text;

namespace OBJEditor;

internal static class Extensions {
    private static readonly Encoding Encoding = Encoding.GetEncoding("UTF-16LE");
    internal static int GetInt32(this byte[] arr, int at) => BitConverter.ToInt32(arr, at);
    internal static int GetInt16(this byte[] arr, int at) => BitConverter.ToInt16(arr, at);

    internal static string GetString(this byte[] arr, int at) {
        int strLen = arr.GetInt32(at);
        at += 4;

//         Console.WriteLine($"GetString: strLen={strLen}, at={at}, arr.Length={arr.Length}");
        byte[] buffer = new byte[strLen * 2];
        for (int i = 0; i < strLen; i++) {
            buffer[i * 2] = arr[i * 2 + at];
            buffer[i * 2 + 1] = arr[i * 2 + at + 1];
        }
//         Console.WriteLine(Encoding.GetString(buffer));
        return Encoding.GetString(buffer);
    }

    internal static string GetNullTerminatedString(this byte[] arr, int at, int maxLen) {
        int length = 0;
        while (length + 1 < maxLen && (arr[at + length] != 0 || arr[at + length + 1] != 0)) {
            length += 2;
        }
        return Encoding.GetString(arr, at, length);
    }

    internal static void WriteTo(this string @string, Stream output) {
        byte[] data = Encoding.GetBytes(@string);
        BitConverter.GetBytes(data.Length / 2).CopyTo(output, 0, 4);
        output.Write(data, 0, data.Length);
    }

    internal static void WriteNullTerminatedTo(this string @string, Stream output) {
        byte[] data = Encoding.GetBytes(@string);
        output.Write(data, 0, data.Length);
        output.WriteByte(0);
        output.WriteByte(0);
    }

    internal static void CopyTo(this byte[] arr, Stream buffer, int readIndex, int length) => buffer.Write(arr, readIndex, length);
}