using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OBJEditor;

public class Obj(byte[] script) {
    private const short Dialogue = 0x64;
    private const short Dialogue2 = 0x68;

    private const short Choice = 0x69;
    private const short Choice2 = 0x67;

    private const short Question = 0x0323;

    private const short Chapter = 0x2BC;

    public string[] Import()
    {
        List<string> strings = [];
        int blockCount = script.GetInt32(0x00);
        int blockLen = script.GetInt32(0x04);
//         Console.WriteLine($"Import: blockCount={blockCount}, blockLen={blockLen}");

        for (int i = blockLen, x = 0; x < blockCount; x++, i += blockLen)
        {
            blockLen = script.GetInt32(i);
            int index = i + 6;
            int entries;
            switch (script.GetInt16(i + 4))
            {
                case Dialogue2:
                case Dialogue:
                    int textOffsetToradora = 10; // Toradora
                    int textOffsetOreimo = 11;	 // Oreimo

//                     Console.WriteLine("********************************************");
//                     Console.WriteLine($"{script[i + textOffsetOreimo + 1]:x2}");
//                     Console.WriteLine($"{script[i + textOffsetOreimo + 2]:x2}");
//                     Console.WriteLine($"{script[i + textOffsetOreimo + 3]:x2}");
//                     Console.WriteLine("********************************************");

                    int textOffset = 0;
                    if (script[i + textOffsetOreimo + 1] == 0 && script[i + textOffsetOreimo + 2] == 0 && script[i + textOffsetOreimo + 3] == 0)	{
                        textOffset = textOffsetOreimo;
//                         Console.WriteLine($"Import: Detected Oreimo format, using textOffset={textOffset}");
                    } else {
                        textOffset = textOffsetToradora;
//                         Console.WriteLine($"Import: Detected Toradora format, using textOffset={textOffset}");
                    }
                    strings.Add(script.GetString(i + textOffset));
                    break;

                case Choice:
                    entries = script.GetInt32(index);
                    for (int y = 0; y < entries; y++)
                    {
                        index += 0x8;
                        strings.Add(script.GetString(index));
                        index += script.GetInt32(index) * 2 + 4;
                    }
                    break;

                case Choice2:
                    entries = script.GetInt32(index);
                    index += 0x8;

                    for (int y = 0; y < entries; y++)
                    {

                        strings.Add(script.GetString(index));
                        index += script.GetInt32(index) * 2 + 4;

                        if (script.GetInt32(index) == 0x00)
                        {
                            index += 8;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(script.GetInt32(index) == 0x01);

                            index += 4;
                            index += script.GetInt32(index) * 2 + 4;
                            index += 4;
                        }
                    }
                    break;

                case Question:
                    index += 4;
                    entries = script.GetInt32(index);
                    index += 4;

                    strings.Add(script.GetString(index));
                    index += 0x4 + script.GetInt32(index) * 2;

                    for (int y = 0; y < entries; y++)
                    {
                        strings.Add(script.GetString(index));
                        index += 0x4 + script.GetInt32(index) * 2 + 0x24;
                    }
                    break;
                case Chapter:
                    strings.Add(script.GetString(index));
                    break;
            }
        }

        return strings.ToArray();
    }

    public byte[] Export(string[] strings)
    {
        int blockCount = script.GetInt32(0x00);
        int blockLen = script.GetInt32(0x04);
        List<List<int>> jumpUpdates = [];

        MemoryStream output = new();
        script.CopyTo(output, 0, blockLen);

        for (int i = blockLen, x = 0, id = 0; x < blockCount; x++, i += blockLen)
        {
            blockLen = script.GetInt32(i);
            MemoryStream newBlock;
            int index = i;
            int count;

            switch (script.GetInt16(i + 4))
            {
                case Dialogue2:
                case Dialogue:
                    newBlock = new MemoryStream();
                    script.CopyTo(newBlock, i + 4, 0x6);

                    string phrase = strings[id++];
                    string secondPhrase = null;
                    if (phrase.Contains("[DEL]"))
                    {
                        List<int> jumpInfo = [x, -1];
                        jumpUpdates.Add(jumpInfo);
                        x--;
                        blockCount--;
                        break;
                    }
                    if (phrase.Contains("[") && phrase.Contains("]"))
                    {
                        secondPhrase = Regex.Match(phrase, @"\[(.*?)\]").Groups[1].Value;
                        phrase = phrase.Replace("[" + secondPhrase + "]", "");

                        if (phrase.Contains("（") && phrase.Contains("）"))
                            secondPhrase = "（" + secondPhrase + "）";
                        if (phrase.EndsWith("」"))
                            secondPhrase = phrase.Substring(0, phrase.IndexOf("「") + 1) + secondPhrase + "」";
                    }
                    phrase.WriteTo(newBlock);

                    WriteBlock(newBlock, output);
                    if (secondPhrase != null)
                    {
                        newBlock = new MemoryStream();
                        script.CopyTo(newBlock, i + 4, 0x2);
                        newBlock.Write([0xFF, 0xFF, 0xFF, 0xFF], 0, 4);

                        secondPhrase.WriteTo(newBlock);
                        WriteBlock(newBlock, output);

                        List<int> jumpInfo = [x, 1];
                        jumpUpdates.Add(jumpInfo);
                        x++;
                        blockCount++;
                    }

                    break;

                case Choice:
                    newBlock = new MemoryStream();
                    count = script.GetInt32(index + 0x6);
                    script.CopyTo(newBlock, index + 4, 0x2);
                    index += 0x06;

                    for (int y = 0; y < count; y++)
                    {
                        script.CopyTo(newBlock, index, 0x8);
                        LimitString(strings[id++]).WriteTo(newBlock);

                        index += 0x8;
                        index += script.GetInt32(index) * 2 + 4;
                    }

                    WriteBlock(newBlock, output);
                    break;
                case Choice2:
                    newBlock = new MemoryStream();
                    index += 4;

                    count = script.GetInt32(index + 0x2);

                    script.CopyTo(newBlock, index, 0xA);
                    index += 0xA;


                    for (int y = 0; y < count; y++)
                    {
                        LimitString(strings[id++]).WriteTo(newBlock);
                        index += script.GetInt32(index) * 2 + 4;


                        script.CopyTo(newBlock, index, 0x4);
                        index += 4;

                        if (script.GetInt32(index - 4) == 0x00)
                        {
                            script.CopyTo(newBlock, index, 0x4);
                            index += 4;
                        }
                        else
                        {
                            int labelLen = script.GetInt32(index) * 2 + 4;
                            script.CopyTo(newBlock, index, labelLen);
                            index += labelLen;

                            script.CopyTo(newBlock, index, 0x4);
                            index += 4;
                        }
                    }

                    WriteBlock(newBlock, output);
                    break;

                case Question:
                    newBlock = new MemoryStream();

                    count = script.GetInt32(index + 0xA);
                    script.CopyTo(newBlock, index + 4, 0xA);
                    index += 0xE;

                    index += script.GetInt32(index) * 2 + 4;
                    LimitString(strings[id++]).WriteTo(newBlock);

                    for (int y = 0; y < count; y++)
                    {
                        index += script.GetInt32(index) * 2 + 4;

                        LimitString(strings[id++]).WriteTo(newBlock);


                        script.CopyTo(newBlock, index, 0x24);
                        index += 0x24;
                    }

                    WriteBlock(newBlock, output);
                    break;

                case Chapter:
                    newBlock = new MemoryStream();
                    script.CopyTo(newBlock, index + 4, 0x2);

                    index += 0x6;
                    index += script.GetInt32(index) * 2 + 4;

                    strings[id++].WriteTo(newBlock);
                    script.CopyTo(newBlock, index, 0x6);

                    WriteBlock(newBlock, output);
                    break;

                default:
                    script.CopyTo(output, i, blockLen);
                    break;
            }
        }

        byte[] blockCountBytes = BitConverter.GetBytes(blockCount);
        output.Position = 0;
        output.Write(blockCountBytes, 0, blockCountBytes.Length);

        for (int i = 0; i < jumpUpdates.Count; i++)
            UpdateJumps(output, jumpUpdates[i][0], jumpUpdates[i][1]);

        return output.ToArray();
    }

    private void UpdateJumps(MemoryStream stream, int minBlock, int change)
    {
        stream.Seek(0, SeekOrigin.Begin);
        for (int i = 0; i < stream.Length; i += 16)
        {
            byte[] line = new byte[16];
            stream.Seek(i, SeekOrigin.Begin);
            stream.Read(line, 0, 15);
            // jump is 10 00 00 00 BE 02 xx xx xx xx 00 00 00 00 00 00, where "xx xx xx xx" is block number
            if (line[0] == 0x10 && line[1] == 0x00 && line[2] == 0x00 && line[3] == 0x00 && line[4] == 0xBE && line[5] == 0x02)
            {
                int blockNumber = line.GetInt32(0x06);
                if (blockNumber < minBlock) continue;
                blockNumber += change;
                byte[] blockIdBytes = BitConverter.GetBytes(blockNumber);
                line[0x06] = blockIdBytes[0];
                line[0x07] = blockIdBytes[1];
                stream.Seek(i, SeekOrigin.Begin);
                stream.Write(line, 0, 15);
            }
        }
    }

    private string LimitString(string input)
    {
        string result = input.Replace("＿", " ");
        return result;
    }
    public void WriteBlock(Stream content, Stream output)
    {
        int newLen = (int)content.Length + 4;
        int blank = 0;

        while ((newLen + blank) % 0x10 != 0x00)
            blank++;

        if (blank <= 0x8)
            blank += 0x10;

        newLen += blank;
        BitConverter.GetBytes(newLen).CopyTo(output, 0, 4);
        content.Seek(0, 0);
        content.CopyTo(output);
        new byte[blank].CopyTo(output, 0, blank);
    }
}