using System;


namespace DownloadPage
{
    class Program
    {
        static void Main(string[] args)
        {
            WebManager manager = new WebManager();
            int threadCount;
            int imageCount;
            string url;
            while (true)
            {
                try
                {
                    Console.WriteLine("write separated by a space: Url page, count thread, count image");
                    string[] req = Console.ReadLine().Split(" ");
                    url = req[0];
                    threadCount = Convert.ToInt32(req[1]);
                    imageCount = Convert.ToInt32(req[2]);
                    break;
                }
                catch
                {
                    Inform.WriteLog("Не корректное введение данных");
                    Console.WriteLine("Data entered incorrectly, please try again");
                }
            }
            
           
            manager.GetImage(url, threadCount, imageCount);

            // manager.downloadImgAsync();
        }

    }

}
