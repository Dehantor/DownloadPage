using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DownloadPage
{
    class Inform
    {
        static public void WriteConsole(object str)
        {
            Console.WriteLine(str.ToString());
        }
        static public void WriteLog(object str)
        {
            File.WriteAllText("log.txt", DateTime.Now+" "+str.ToString()+"\n");
        }
    }
}
