using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace YKoffieNet
{
    internal class Config
    {
        public static string GetTokenFromConfig()
        {
            string currentDir = Environment.CurrentDirectory;
            currentDir += "//token.txt";
            if (File.Exists(currentDir)){
                StreamReader reader = new StreamReader(currentDir);
                string token = reader.ReadToEnd();
                return token;
            }
            else
            {
                File.Create(currentDir);
                Environment.Exit(0);
                return "";
            }
        }
    }
}