using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BrickEmulator.IO
{
    /// <summary>
    /// Reads al keys and values of config to use at the environment.
    /// </summary>
    class ConfigureFile
    {
        #region Fields
        private const string Path = "configure.ini";
        private readonly Object ReadLocker = new Object();

        private Dictionary<string, string> Cache;
        #endregion

        #region Constructors
        public ConfigureFile() {  }
        #endregion

        #region Methods

        public Boolean HandleFiles()
        {
            if (File.Exists(Path))
            {
                ImportConfig();
                return true;
            }
            else
            {
                BrickEngine.GetScreenWriter().PaintScreen(ConsoleColor.Red, PaintType.ForeColor);
                BrickEngine.GetScreenWriter().ScretchLine("File '" + Path + "' not exists!", WriteType.Incoming);
                return false;
            }
        }

        private void ImportConfig()
        {
            Cache = new Dictionary<string, string>();

            lock (ReadLocker)
            {
                var Items = File.ReadAllLines(Path);

                if (Items.Length <= 0)
                {
                    return;
                }

                foreach (string Line in Items)
                {
                    if (Line.Contains('=') && Line.Length >= 3)
                    {
                        string Key = Line.Split('=')[0];
                        string Value = Line.Substring(Key.Length + 1);

                        Cache.Add(Key, Value);
                    }
                }
            }
        }

        public Dictionary<string, string> GetFromCategoy(string Category)
        {
            var Dic = new Dictionary<string, string>();

            foreach(string Key in Cache.Keys.ToList())
            {
                var Cat = Key.Split('.')[0];

                if (Category.Equals(Cat))
                {
                    Dic.Add(Key, Cache[Key]);
                }
            }

            return Dic;
        }

        public string CallStringKey(string Value)
        {
            if (!Cache.ContainsKey(Value))
            {
                return string.Empty;
            }

            return Cache[Value];
        }

        public int CallIntKey(string Value)
        {
            if (!Cache.ContainsKey(Value))
            {
                return 0;
            }

            return Convert.ToInt32(Cache[Value]);
        }

        public Boolean CallBooleanKey(string Value)
        {
            if (!Cache.ContainsKey(Value))
            {
                return false;
            }

            return Cache[Value].ToLower().Equals("true");
        }

        #endregion
    }
}
