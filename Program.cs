using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using ScrapySharp;
using ScrapySharp.Html;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace khinsiderLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine("Download music from downloads.khinsider.com");
                Console.WriteLine();

                bool flac = false;
                string host = "https://downloads.khinsider.com";
                Console.WriteLine("Where would you like to save your files? ");
                string downloadDestination = checkPath();

                Console.WriteLine("\n1. Download whole letter | skip/take (optional)");
                Console.WriteLine("2. Download by name");
                int downloadBy = checkCharInput(new int[] { 1, 2 });               

                string downloadByExpression = "";
                string letterToDownload = "";
                if (downloadBy == 2)
                {
                    Console.WriteLine("\nEnter a name: ");
                    downloadByExpression = Console.ReadLine();
                    letterToDownload = downloadByExpression[0].ToString();
                }

                int skipamount = 0;
                int takeamount = 0;
                if (downloadBy == 1)
                {
                    Console.WriteLine("\nWhat letter do you want to donwload? ");
                    letterToDownload = Console.ReadLine();
                    Console.WriteLine("Skip games from top: ");
                    skipamount = checkInput(Enumerable.Range(0, 5000).ToArray());
                    Console.WriteLine("Take games: (0 = all) ");
                    takeamount = checkInput(Enumerable.Range(0, 5000).ToArray());
                }

                Console.WriteLine("Download Flac if available?");
                Console.WriteLine("1. no");
                Console.WriteLine("2. yes");
                int flacpic = checkCharInput(new int[] { 1, 2 });
                if (flacpic == 2)
                    flac = true;
                Console.WriteLine("\nHow fast?");
                Console.WriteLine("1. Normal\t(three simultaneous downloads)");
                Console.WriteLine("2. Fast\t\t(six simultaneous donwloads)");
                int threads = checkCharInput(new int[] { 1, 2 });
                if (threads == 1)
                    threads = 3;
                else
                    threads = 6;

                string[] alphabet = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };

                string requestUrl = "https://downloads.khinsider.com/game-soundtracks/browse/";

                ScrapingBrowser browser = new ScrapingBrowser();
                browser.Encoding = Encoding.UTF8;

                int checkInput(int[] expectedResults)
                {
                    int temp = -1;
                    while (!expectedResults.Contains(temp))
                    {
                        var input = Console.ReadLine();
                        if (int.TryParse(input, out temp))
                        {
                            if (expectedResults.Contains(temp))
                                return temp;
                            else
                            {
                                Console.WriteLine("try again");
                            }
                        }
                        else
                        {
                            Console.WriteLine("try again");
                            temp = -1;
                        }
                    }
                    return -1;
                }
                int checkCharInput(int[] expectedResults)
                {
                    int temp = -1;
                    while (!expectedResults.Contains(temp))
                    {
                        var input = Console.ReadKey();
                        if (int.TryParse(input.KeyChar.ToString(), out temp))
                        {
                            if (expectedResults.Contains(temp))
                                return temp;
                            else
                            {
                                Console.WriteLine("\ntry again");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\ntry again");
                            temp = -1;
                        }
                    }
                    return -1;
                }
                string checkPath()
                {
                    string path = "";
                    while (!Directory.Exists(path))
                    {
                        path = Console.ReadLine();
                        if (Directory.Exists(path))
                        {
                            IsDirectoryWritable(path);
                            return path;
                        }
                            
                        else
                            Console.WriteLine($"directory {path} doesn't exist");
                    }
                    return "";
                }

                bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
                {
                    try
                    {
                        using (FileStream fs = File.Create(Path.Combine(dirPath, Path.GetRandomFileName()),1,FileOptions.DeleteOnClose))
                        { }
                        return true;
                    }
                    catch
                    {
                        if (throwIfFails)
                            throw;
                        else
                        {
                            Console.WriteLine("\nWarning: Application may not be able to write in that directory.");
                            Console.WriteLine("Run the application as another user or change the directory.\n");

                            Console.WriteLine("1. change directory");
                            Console.WriteLine("2. cOntiNuE AnYwAy");
                            Console.WriteLine("3. exit application");
                            int what = checkCharInput(new int[] { 1, 2, 3 });
                            switch (what)
                            {
                                case 1:
                                    Console.Clear();
                                    Main(new string[] { });
                                    break;
                                case 2:
                                    Console.Clear();
                                    break;
                                case 3:
                                    Environment.Exit(4);
                                    break;
                                default:
                                    break;
                            }
                            Console.Clear();
                            //Main(new string[] { });
                            return false;
                        }
                    }                            
                }

                DownloadLetter();
                Console.WriteLine("\n");                
                Console.WriteLine("Download finished.  Press any key");
                Console.ReadKey();
                Console.Clear();
                void DownloadLetter()
                {
                    if (downloadBy == 1)
                    {
                        Console.WriteLine("\nStarting download of letter: " + letterToDownload.ToUpper());
                    }
                    if (downloadBy == 2)
                    {
                        Console.WriteLine("\nSearching for games by expression...");
                    }

                    WebPage homePage = browser.NavigateToPage(new Uri(requestUrl + letterToDownload.ToUpper()));
                    // Get Letter hrefs
                    HtmlNode container = homePage.Find("div", By.Id("EchoTopic")).FirstOrDefault();
                    HtmlNode pnode = container.Descendants("p").ElementAt(1);
                    IEnumerable<HtmlNode> everyGameLink = pnode.Ancestors();
                    if (downloadBy == 1 && takeamount == 0)
                        everyGameLink = pnode.Descendants("a").Skip(skipamount);
                    else if (downloadBy == 1 && takeamount != 0)
                        everyGameLink = pnode.Descendants("a").Skip(skipamount).Take(takeamount);
                    else if (downloadBy == 2)
                        everyGameLink = pnode.Descendants("a").Where(x => x.InnerText.ToLower().Contains(downloadByExpression.ToLower()));

                    Console.WriteLine($"\nResults: {everyGameLink.Count()}\n");

                    foreach (HtmlNode item in everyGameLink)
                    {
                        string GameName = item.InnerText.Replace("?", "0").Replace(".", "").Replace(":", "").Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace("*", "").Replace("\"", "");
                        while (GameName[GameName.Length - 1] == ' ')
                            GameName = GameName.TrimEnd();
                        while (GameName[0] == ' ')
                            GameName = GameName.TrimStart();
                        string gamelnik = item.GetAttributeValue("href");
                        Console.WriteLine("\nDownloading game: " + GameName+"\n");
                        try
                        {
                            DownloadGame(gamelnik, Path.Combine(downloadDestination, letterToDownload.ToUpper(), GameName), threads);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }

                void DownloadGame(string gamelink, string downloadpath, int workingThreads)
                {
                    WebPage gamePage = browser.NavigateToPage(new Uri(host + gamelink));
                    HtmlNode gameContainer = gamePage.Find("table", By.Id("songlist")).FirstOrDefault();
                    List<HtmlNode> ancestors = gameContainer.SelectNodes("tr").ToList();
                    var ka = ancestors.First().ChildNodes[3].FirstChild.InnerHtml;
                    int selectno;
                    if (ancestors.First().ChildNodes[3].FirstChild.InnerHtml == "#")
                        selectno = 2;
                    else if (ancestors.First().ChildNodes[3].FirstChild.InnerHtml.ToUpper() == "CD")
                        selectno = 3;
                    else
                        selectno = 1;

                    IEnumerable<HtmlNode> onlyGametrs = ancestors.Skip(1).Take(ancestors.Count - 2);
                    int counter = 0;
                    List<Downloader> downloadpool = new List<Downloader>();
                    for (int i = 0; i < onlyGametrs.Count(); i++)
                    {
                        counter++;
                        string href = onlyGametrs.ElementAt(i).Descendants("td").ElementAt(selectno).FirstChild.GetAttributeValue("href");
                        string songName = counter + ". " + onlyGametrs.ElementAt(i).Descendants("td").ElementAt(selectno).InnerText.Replace("&#8203;", "").Replace(".mp3", "").Replace(".flac", "").Replace("?", "0").Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace("*", "").Replace("\"", "");

                        downloadpool.Add(new Downloader() { downloadPath = downloadpath, songLink = host + href, songName = songName, flac = flac });
                    }

                    List<Thread> threads = new List<Thread>();
                    foreach (var item in downloadpool)
                    {
                        threads.Add(new Thread(new ThreadStart(item.DownloadSong)));
                    }
                    Handler handler = new Handler() { threads = threads, maxThreads = workingThreads };
                    handler.work();
                }
            }
        }
    }
    public class Downloader
    {
        public string songLink { get; set; }
        public string songName { get; set; }
        public string downloadPath { get; set; }
        public bool flac { get; set; } = false;

        public void DownloadSong()
        {
            ScrapingBrowser dbrowser = new ScrapingBrowser();
            dbrowser.Encoding = Encoding.UTF8;
            WebPage songPage = dbrowser.NavigateToPage(new Uri(songLink));
            HtmlNode pnodes = songPage.Find("div", By.Id("EchoTopic")).First();
            var downloadLinks = pnodes.Descendants("p").Skip(3).SkipLast(1);
            string downloadThis;
            string extension;
            if (downloadLinks.Count() > 1 && flac)
            {
                downloadThis = downloadLinks.ElementAt(1).ChildNodes[0].GetAttributeValue("href");
                extension = ".flac";
            }
            else
            {
                downloadThis = downloadLinks.ElementAt(0).ChildNodes[0].GetAttributeValue("href");
                extension = ".mp3";
            }

            try
            {
                WriteFile(Path.Combine(songName + extension), downloadPath, downloadThis);
            }
            catch (Exception e)
            {      
                    Console.WriteLine(e.Message);                                 
            }

            void WriteFile(string songName, string downloadPath, string downloadLink)
            {
                if (!Directory.Exists(downloadPath))
                    Directory.CreateDirectory(downloadPath);
                var musicFile = dbrowser.DownloadWebResource(new Uri(downloadLink)).Content;
                byte[] byteFile = musicFile.ToArray();
                File.WriteAllBytes(Path.Combine(downloadPath, songName), byteFile);
                Console.WriteLine(songName);
            }
        }
    }
    public class Handler
    {
        private Semaphore semaphore = new Semaphore(0, 12);

        public List<Thread> threads = new List<Thread>();

        public int maxThreads { get; set; } = 3;
        public void work()
        {   
            if(maxThreads == 3)
            {
                for (int i = 0; i < threads.Count;)
                {
                    if (threads.Count - i > maxThreads)
                    {
                        semaphore.WaitOne(3);
                        threads[i].Start();
                        threads[i + 1].Start();
                        threads[i + 2].Start();
                        while (threads[i].IsAlive && threads[i + 1].IsAlive && threads[i + 2].IsAlive)
                        {
                            Thread.Sleep(500);
                        }
                        i += 3;
                        semaphore.Release();
                    }
                    else
                    {
                        semaphore.WaitOne(1);
                        threads[i].Start();
                        while (threads[i].IsAlive)
                        {
                            Thread.Sleep(500);
                        }
                        i++;
                        semaphore.Release(1);
                    }
                }
            }
            if (maxThreads == 6)
            {
                for (int i = 0; i < threads.Count;)
                {
                    if (threads.Count - i > maxThreads)
                    {
                        semaphore.WaitOne(6);
                        threads[i].Start();
                        threads[i + 1].Start();
                        threads[i + 2].Start();
                        threads[i + 3].Start();
                        threads[i + 4].Start();
                        threads[i + 5].Start();
                        while (threads[i].IsAlive && threads[i + 1].IsAlive && threads[i + 2].IsAlive && threads[i + 3].IsAlive && threads[i + 4].IsAlive && threads[i + 5].IsAlive)
                        {
                            Thread.Sleep(500);
                        }
                        i += 6;
                        semaphore.Release();
                    }
                    else if (threads.Count - i > maxThreads/2)
                    {
                        semaphore.WaitOne(3);
                        threads[i].Start();
                        threads[i + 1].Start();
                        threads[i + 2].Start();
                        while (threads[i].IsAlive && threads[i + 1].IsAlive && threads[i + 2].IsAlive)
                        {
                            Thread.Sleep(500);
                        }
                        i += 3;
                        semaphore.Release(3);
                    }
                    else
                    {
                        semaphore.WaitOne(1);
                        threads[i].Start();
                        while (threads[i].IsAlive)
                        {
                            Thread.Sleep(500);
                        }
                        i++;
                        semaphore.Release(1);
                    }
                }
            }

        }
    }
    class Logger
    {

    }
}


