using System;
using System.Collections.Generic;

namespace OBJEditor;

public class ObjHelper(byte[] script, byte version) {
    public Dictionary<int, string?>? Actors;
    private Obj editor = new(script, version);

    //Ryuuji「(Aaaaaaaaagh!!!!)」
    public string[] Import()
    {
        string[] strings = editor.Import();
//         Console.WriteLine($"Import: strings.Length={strings.Length}");
        Actors = new();
        for (int i = 0; i < strings.Length; i++)
        {
            string line = strings[i];
            Actors[i] = null;
            if (line.EndsWith('」') && line.Contains('「'))
            {
                string actor = line[..line.IndexOf('「')];
                line = line.Substring(actor.Length, line.Length - actor.Length);
                Actors[i] = actor;
            }

            strings[i] = line;
        }

        return strings;
    }

    public byte[] Export(string[] strings) {
        if (Actors == null) throw new ArgumentNullException(nameof(strings));
        string[] tmp = new string[strings.Length];
        for (int i = 0; i < strings.Length; i++)
        {
            string line = strings[i];
            if (Actors[i] != null)
                line = Actors[i] + line;

            tmp[i] = line;
        }

        return editor.Export(tmp);
    }
}