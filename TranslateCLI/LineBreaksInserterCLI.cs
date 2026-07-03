using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TranslateCLI;

public class LineBreaksInserterCLI
{
    private readonly Dictionary<char, int> glyphsWidth;
    private readonly int maxLineLength;

    public LineBreaksInserterCLI(string pathToDumpedFont, int maxLineLength)
    {
        glyphsWidth = new Dictionary<char, int>();

        LoadDumpedFont(pathToDumpedFont);
        this.maxLineLength = maxLineLength;
    }

    private void LoadDumpedFont(string pathToDumpedFont)
    {
        string[] dumpedFontLines = File.ReadAllLines(pathToDumpedFont);

        for (int i = 0; i < dumpedFontLines.Length; i++)
        {
            string line = dumpedFontLines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TryParseJsonFontLine(line, out char curChar, out int charWidthAccurate))
            {
                glyphsWidth[curChar] = charWidthAccurate;
                continue;
            }

            if (i + 1 >= dumpedFontLines.Length)
                continue;

            int charCode = int.Parse(line, NumberStyles.HexNumber);
            char parsedChar = Char.ConvertFromUtf32(charCode).ToCharArray()[0];

            double charWidthInaccurate = double.Parse(dumpedFontLines[i + 1], CultureInfo.InvariantCulture);
            int parsedWidthAccurate = (int)Math.Ceiling(charWidthInaccurate);

            glyphsWidth[parsedChar] = parsedWidthAccurate;
            i++;
        }
    }

    private static bool TryParseJsonFontLine(string line, out char curChar, out int charWidthAccurate)
    {
        curChar = '\0';
        charWidthAccurate = 0;

        int separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
            return false;

        string prefix = line[..separatorIndex];
        string jsonCandidate = line[(separatorIndex + 1)..];
        if (string.IsNullOrWhiteSpace(jsonCandidate) || !jsonCandidate.StartsWith('{'))
            return false;

        curChar = prefix.Length > 0 ? prefix[0] : '\0';

        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonCandidate);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("advance", out JsonElement advance) && advance.TryGetProperty("x", out JsonElement advanceX))
            {
                charWidthAccurate = (int)Math.Ceiling(advanceX.GetDouble());
                return true;
            }

            if (root.TryGetProperty("width", out JsonElement width))
            {
                charWidthAccurate = (int)Math.Ceiling(width.GetDouble());
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private int GetStringLength(string str, bool isSpeech)
    {
        char[] strChars = str.ToCharArray();
        int length = 0;

        for (int i = 0; i < strChars.Length; i++)
            length += glyphsWidth[strChars[i]];           
            
        if (isSpeech)
            length += glyphsWidth['「'];

        return length;
    }

    public string InsertLineBreaks(string insertTo, bool isSpeech)
    {
        string newString = "";
        string secondString = null;
        if (insertTo.Contains("[") && insertTo.Contains("]"))
        {
            secondString = Regex.Match(insertTo, @"\[(.*?)\]").Groups[1].Value;
            insertTo = insertTo.Replace("[" + secondString + "]", "");
        }

        if (GetStringLength(insertTo, isSpeech) > maxLineLength && !insertTo.Contains('＿'))
        {
            string[] words = insertTo.Split();

            for (int j = 0; j < words.Length; j++)
            {
                string tempString;
                if (newString.Contains('＿'))
                    // Symbol '＿' should be included in new line, because it affects the length of the line, although it is not visible
                    tempString = newString.Substring(newString.LastIndexOf('＿')) + " " + words[j];
                else
                    tempString = newString + " " + words[j];

                if (GetStringLength(tempString, isSpeech) > maxLineLength)
                    newString += "＿" + words[j];
                else
                    newString += " " + words[j];
            }
            newString = newString.Trim();
        }
        else
            newString = insertTo;

        if (secondString != null)
            newString += "[" + InsertLineBreaks(secondString, isSpeech) + "]";

        return newString;
    }
}