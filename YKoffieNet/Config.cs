/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKoffieNet
{
    internal class Config
    {
        public string GetTokenFromConfig()
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            try
            {
                FileStream tokenFile = File.OpenRead(currentDir + "/token.txt");
            }
            catch (FileNotFoundException)
            {
                FileStream tokenFile = File.Create(currentDir + "/token.txt");
            }
        }
    }
}
*/