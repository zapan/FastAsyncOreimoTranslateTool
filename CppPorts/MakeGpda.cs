using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global

namespace CppPorts;

public static class MakeGpda {
    public static void Process(string lstDir) { _ = Process(lstDir, Directory.GetCurrentDirectory()); }

    public static async Task Process(string lstDir, string workingDir) {
        string targetName = lstDir;
        string targetList = targetName + ".lst";
        string targetFile = targetName + ".dat";

        if (!File.Exists(targetList)) {
            _ = Console.Error.WriteLineAsync("Unable to open list file " + targetList);
            return;
        }

        List<FileNameStruct> fileNames = [];
        List<Entry> entries = [];
        int totalEntries = 0;
        int entryNameOffset = 0;

        using (StreamReader reader = new(targetList)) {
            bool delim = false;
            int firstChar = reader.Read();
            await reader.ReadLineAsync(); // skip rest of line
            if (firstChar == 'Y')
                delim = true;

            while (!reader.EndOfStream) {
                string line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('\t');
                if (parts.Length < 2) continue;

                string inName = parts[0];
                if (!File.Exists(inName))
                    inName = Path.Combine(workingDir, inName);
                string internalName = parts[1];
                int inNameSize = internalName.Length;

                if (delim)
                    inNameSize++;

                fileNames.Add(new FileNameStruct {
                    FileName = inName.Trim(),
                    InternalName = internalName,
                    NameSize = inNameSize,
                    RelOffset = entryNameOffset
                });

                totalEntries++;
                entryNameOffset += inNameSize + 4;
            }
        }

        uint totalFileSize = 0;

        foreach (FileNameStruct fn in fileNames) {
            string tmpFilename = fn.FileName;
            if (!File.Exists(tmpFilename)) {
               tmpFilename += ".dat";
               if (!File.Exists(tmpFilename)) {
                    _ = Console.Error.WriteLineAsync("Unable to open input file " + fn.FileName);
                    return;
                }
            }

            byte[] fileData = await File.ReadAllBytesAsync(tmpFilename);
            int bufferSize = CalculateBuffer((uint)fileData.Length);
            int totalEntrySize = fileData.Length + bufferSize;

            byte[] buffer = new byte[totalEntrySize];
            Array.Copy(fileData, buffer, fileData.Length);

            entries.Add(new Entry {
                EntryOffset = totalFileSize,
                EntrySize = fileData.Length,
                Data = buffer,
                TotalEntrySize = totalEntrySize
            });

            totalFileSize += (uint)totalEntrySize;
        }

        int entryHeader = totalEntries * 16;
        int totalHeaderSize = entryHeader + entryNameOffset + 16;
        int headerBufferSize = CalculateBuffer((uint)totalHeaderSize);

        totalFileSize += (uint)(totalHeaderSize + headerBufferSize);
        int endBufferSize = CalculateBuffer(totalFileSize);
        totalFileSize += (uint)endBufferSize;

        Stream fileStream = File.Open(targetFile, FileMode.Create, FileAccess.Write);
        BinaryWriter writer = new(fileStream);
        writer.Write("GPDA"u8.ToArray());
        writer.Write((long)totalFileSize);
        writer.Write(totalEntries);

        int addOffset = 16 + entryHeader;

        foreach (var entry in entries) {
            uint correctedOffset = entry.EntryOffset + (uint)(entryHeader + entryNameOffset + headerBufferSize + 16);
            int currentFileNameOffset = fileNames[entries.IndexOf(entry)].RelOffset + addOffset;

            writer.Write((long)correctedOffset);
            writer.Write(entry.EntrySize);
            writer.Write(currentFileNameOffset);
        }

        foreach (FileNameStruct fn in fileNames) {
            writer.Write(fn.NameSize);

            if (fn.InternalName == null) continue;

            writer.Write(Encoding.ASCII.GetBytes(fn.InternalName));
            if (fn.NameSize > fn.InternalName.Length) writer.Write((byte)0x20);
        }

        WriteBuffer(writer, headerBufferSize);

        foreach (Entry entry in entries)
            writer.Write(entry.Data, 0, entry.TotalEntrySize);

        WriteBuffer(writer, endBufferSize);
        await writer.DisposeAsync();
        await fileStream.DisposeAsync();
    }

    static int CalculateBuffer(uint inputSize) {
        int mod8 = (int)(inputSize % 0x800);
        return mod8 != 0 ? 0x800 - mod8 : 0;
    }

    static void WriteBuffer(BinaryWriter writer, int size) {
        for (int i = 0; i < size; i++)
            writer.Write((byte)0x00);
    }
}

class Entry {
    public uint EntryOffset;
    public int EntrySize;
    public byte[] Data;
    public int TotalEntrySize;
}

class FileNameStruct {
    public string FileName;
    public string InternalName;
    public int NameSize;
    public int RelOffset;
}