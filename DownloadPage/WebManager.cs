using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Configuration;

namespace DownloadPage
{

    class WebManager
    {
        WebClient client;
        string page;
        List<string> imgUrl;
        public CancellationTokenSource ts = null;
        List<MyImage> images;
        object block = new object();
        object block2 = new object();
        int countDownImg = 0;
        string jsonString = "";
        int timeWait = 1000;
        public WebManager()
        {
            client = new WebClient();
            try
            {

                timeWait = Convert.ToInt32(ConfigurationSettings.AppSettings["time"]);
            }
            catch
            {
                Inform.WriteLog("не получилось считать задержку с файла конфигурации");
            }
           
        }

        //загрузка страницы
        void DownloadPage(string uri)
        {
            try
            {
                ts = new CancellationTokenSource();
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCallback2);
                client.DownloadStringAsync(new Uri(uri), ts.Token);
                while (page == null)
                { }
                File.WriteAllText("page.html", page);
            }
            catch
            {
                Inform.WriteLog("ошибка при загрузки страницы");
            }
        }
        //сохранение результатов после загрузки страницы
        private void DownloadStringCallback2(object sender, DownloadStringCompletedEventArgs e)
        {
            // page = ;
            try
            {
                if (e.Result != null)
                    page = e.Result;
                else
                    page = "<!DOCTYPE html>";
            }
            catch
            {
                Inform.WriteLog("URL не найден");
                page = "<!DOCTYPE html>";
            }
        }

        //поиск ссылок на картинки
        void seachImg(int imageCount)
        {
            try
            {
                imgUrl = new List<string>();
                string regularExpressionPatternImg = @"(?<=<img)(.*?)(?=\>)";
                Regex regexImg = new Regex(regularExpressionPatternImg, RegexOptions.Singleline);

                MatchCollection collection = regexImg.Matches(page);
                int k = 0;
                foreach (var item in collection)
                {
                    k++;
                    Match m = item as Match;
                    imgUrl.Add(m.Value);
                    //ограничение на количество загружаемых файлов
                    if (k >= imageCount)
                        return;
                }
            }
            catch
            {
                Inform.WriteConsole("ошибка при поиске картинок на странице");
            }
        }

        //метод обработки страницы и загрузки картинок
        public void GetImage(string url, int threadCount, int imageCount)
        {
            Inform.WriteLog("URL не найден");
            //загрузка страницы
            DownloadPage(url);
            if (page == "<!DOCTYPE html>")
            {
                Inform.WriteConsole("Адреса не существует");
                return;
            }
            //получение ссылок на картинки
            Inform.WriteLog("Анализ страницы на присуствие изображений");
            seachImg(imageCount);
            Inform.WriteLog("Найдено картинок:" +imgUrl.Count);
            //загрузка картинок
            Inform.WriteLog("Вычисление потоков для загрузки картинок");
            downloadImgAsync(threadCount);
            Inform.WriteLog("Вывод json на экран");
            Inform.WriteConsole(jsonString);
            
        }
        //загрузка картинок
        void downloadImg(int start, int imgCount)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo("images");
                FileInfo file;
                //если файл не создан
                try
                {
                    if (!directory.Exists)
                        directory.Create();
                }
                catch
                {
                    lock (block)
                    {
                        Inform.WriteLog("ошибка создания папки images");
                    }
                }
                string regularExpressionPatternSrc = @"(?<=src="")(.*?)(?="")";
                string regularExpressionPatternHost = @"(?<=https://)(.*?)(?=/)";
                string regularExpressionPatternAlt = @"(?<=alt="")(.*?)(?="")";
                Regex regexSrc = new Regex(regularExpressionPatternSrc, RegexOptions.Singleline);
                Regex regexHost = new Regex(regularExpressionPatternHost, RegexOptions.Singleline);
                Regex regexAlt = new Regex(regularExpressionPatternAlt, RegexOptions.Singleline);
                for (int i = start; i < imgCount; i++)
                {

                    string src = regexSrc.Match(imgUrl[i]).Value;
                    try
                    {

                        int z = src.LastIndexOf("/");
                        int o = src.Length;
                        string name = src.Remove(0, z + 1);
                        using (var c = new WebClient())
                            c.DownloadFile(src, "images\\" + name);
                        file = new FileInfo("images\\" + name);
                    }
                    catch (Exception ex)
                    {
                        lock (block)
                        {
                            countDownImg++;
                            Inform.WriteLog("URL к картинке не правильный:" + src);
                            continue;
                        }
                    }
                    //записываем информацию с скаченном файле

                    string alt = regexAlt.Match(imgUrl[i]).Value;
                    Dictionary<string, string> pairs = new Dictionary<string, string>();
                    pairs.Add("alt", alt);
                    pairs.Add("src", src);
                    pairs.Add("size", (file.Length / 8.0).ToString());
                    lock (block2)
                    {
                        if (images.Find(x => x.host == regexHost.Match(src).Value) == null)
                        {
                            MyImage myImage = new MyImage();
                            myImage.host = regexHost.Match(src).Value;
                            myImage.images.Add(pairs);
                            images.Add(myImage);
                        }
                        else
                        {

                            int z = images.FindIndex(x => x.host == regexHost.Match(src).Value);

                            images[z].images.Add(pairs);
                        }
                        countDownImg++;
                    }
                }
            }
            catch
            {
                lock (block)
                {
                    Inform.WriteLog("Ошибка при загрузки картинок");
                }
            }
        }
        //сгенерировал свой JsonSerializer
        void createJson()
        {
            try
            {
                string info = "";
                if (imgUrl.Count != countDownImg && images.Count != 0)
                    info = "info:\"Картинки не докачались\",\n";
                if (imgUrl.Count != countDownImg && images.Count == 0)
                    info = "info:\"Картинки не докачались\"\n";
                jsonString = "[\n";
                jsonString += info;
                foreach (var item in images)
                {
                    jsonString += "{\n";

                    jsonString += "\t\"host\":\"" + item.host + "\"\n";
                    jsonString += "\t\"images\":\"[\n";
                    foreach (var img in item.images)
                    {
                        jsonString += "\t\t{\n";
                        jsonString += "\t\talt:\"" + img["alt"] + "\",\n";
                        jsonString += "\t\tsrc:\"" + img["src"] + "\",\n";
                        jsonString += "\t\tsize:\"" + img["size"] + "\"\n";
                        jsonString += "\t\t},\n";
                    }
                    jsonString += "\t]\n";
                    jsonString += "\n},";
                }
                jsonString = jsonString.Remove(jsonString.Length - 1);
                jsonString += "]\n";
                File.WriteAllText("res.json", jsonString);
            }
            catch
            {
                Inform.WriteLog("Ошибка при создании json файла");
            }
        }

        void downloadImgAsync(int threadCount)
        {
            try
            {
                //количество поток не более количество процессоров
                int proc = Environment.ProcessorCount;
                if (threadCount > proc)
                    threadCount = proc;
                //количество изображений для одного потока
                int iCount = imgUrl.Count / threadCount;
                int[] imgCount = new int[threadCount];
                int delta = imgUrl.Count - iCount * threadCount;
                //остатки раскидываем по потокам
                for (int i = 0; i < threadCount; i++)
                {
                    imgCount[i] = iCount;
                    if (delta > 0)
                    {
                        imgCount[i]++;
                        delta--;
                    }

                }

                images = new List<MyImage>();
                Task[] tasks = new Task[threadCount];
                //Task task = new Task(() => downloadImg(0, 30));
                int start = 0;
                //task.Start();
                for (int i = 0; i < threadCount; i++)
                {
                    int z = i;
                    int startT = start;
                    int imgC = imgCount[z];
                    tasks[i] = new Task(() => downloadImg(startT, startT + imgC));

                    start += imgCount[i];
                }
                for (int i = 0; i < threadCount; i++)
                {
                    tasks[i].Start();
                }
                Task.WaitAll(tasks, 1000);
                createJson();
            }
            catch
            {
                Inform.WriteLog("Ошибка при выделении потока");
            }
        }
    }
}
