using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CppPorts;

struct EntryStruct {
    public uint EntryOffset;
    public int EntrySize;
    public int EntryNameOffset;
}

public static class ModSeekMap {
    private static readonly Encoding Code = Encoding.GetEncoding(932);

    /// <returns>The status of the process</returns>
    public static Task<bool> ProcessAsync(string resourceDat = "RES.dat", string seekmapTxt = "seekmap.txt", string outputFile = "seekmap.new") =>
        Task.Run(() => Process(resourceDat, seekmapTxt, outputFile));

    /// <returns>The status of the process</returns>
    public static bool Process(string resourceDat = "RES.dat", string seekmapTxt = "seekmap.txt", string outputFile = "seekmap.new") {
        StreamReader inputMapReader; 
        BinaryReader inputResReader; 
        StreamWriter outputWriter;
        
        try {
            FileStream inputResStream = File.OpenRead(resourceDat);
            inputMapReader = new StreamReader(seekmapTxt);
            inputResReader = new BinaryReader(inputResStream);
            outputWriter = new StreamWriter(outputFile, false, Code);

            outputWriter.Write("resource.dat 0 ");
            ParseGpda(0, 1);

            inputResReader.Dispose();
            inputResStream.Dispose();
            inputMapReader.Dispose();
            outputWriter.Flush();
            outputWriter.Dispose();
        }
        catch (Exception e) {
            File.WriteAllText("error", e.ToString());
            return false;
        }
        
        return true;

        void ParseGpda(uint currentOffset, int nesting) {
            inputResReader.BaseStream.Seek(currentOffset, SeekOrigin.Begin);

            inputResReader.ReadBytes(4);
            uint archiveSize = inputResReader.ReadUInt32();
            inputResReader.ReadBytes(4);
            int entries = inputResReader.ReadInt32();

            outputWriter.Write(archiveSize + " ");
            ReadUnknown();
            outputWriter.Write(" 0 " + entries + " \r\n");

            EntryStruct[] entryArray = new EntryStruct[entries];
            string[] fileNames = new string[entries];

            for (int i = 0; i < entries; i++) {
                entryArray[i].EntryOffset = inputResReader.ReadUInt32();
                inputResReader.ReadBytes(4);
                entryArray[i].EntrySize = inputResReader.ReadInt32();
                entryArray[i].EntryNameOffset = inputResReader.ReadInt32();
            }

            for (int i = 0; i < entries; i++) {
                int nameSize = inputResReader.ReadInt32();
                byte[] nameBytes = inputResReader.ReadBytes(nameSize);
                fileNames[i] = Code.GetString(nameBytes);
            }

            for (int i = 0; i < entries; i++) {
                uint totalOffset = currentOffset + entryArray[i].EntryOffset;

                for (int t = 0; t < nesting; t++)
                    outputWriter.Write('\t');

                inputResReader.BaseStream.Seek(totalOffset, SeekOrigin.Begin);
                string sigString = Encoding.ASCII.GetString(inputResReader.ReadBytes(4));

                if (sigString == "GPDA") {
                    //Is this entry also a GPDA? If so recurse this function on it
                    outputWriter.Write(fileNames[i] + " " + totalOffset + " ");
                    ParseGpda(totalOffset, nesting + 1);
                    continue;
                }

                outputWriter.Write(fileNames[i] + " " + totalOffset + " " + entryArray[i].EntrySize + " ");
                ReadUnknown();
                outputWriter.Write(" \r\n");
            }
        }

        // Not sure what the third number in the seekmap is, so I'll just copy the existing one
        // 25/12/29 It's likely a custom Crc32 hash
        void ReadUnknown() {
            string? line = inputMapReader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                throw new EndOfStreamException();

            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ulong unknown = ulong.Parse(parts[3]);
            outputWriter.Write(unknown);
        }
    }
}