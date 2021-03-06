using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace ACT.Hojoring.Updater
{
    public static class Program
    {
        static Program()
        {
            AssemblyResolver.Instance.Initialize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Main(
            string[] args)
        {
            try
            {
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls;
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Tls11;
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var dest = "update";
                var usePre = false;

                if (args.Length > 0)
                {
                    dest = args[0];
                    bool.TryParse(args[1], out usePre);
                }

#if false
                var client = new GitHubClient(new ProductHeaderValue("ACT.Hojoring.Updater"))
                {
                    Credentials = new Credentials("4a380243ea7a6894be1c1cfc154f4fecd1a46bd0")
                };
#else
                // rate limit に引っかかるため token を使用しない
                // 1IP につき 60access/h となる
                var client = new GitHubClient(new ProductHeaderValue("ACT.Hojoring.Updater"));
#endif

                var releases = client.Repository.Release.GetAll("anoyetta", "ACT.Hojoring").Result;

                var lastest = releases.FirstOrDefault();
                if (!usePre)
                {
                    if (lastest.Prerelease)
                    {
                        lastest = releases.FirstOrDefault(x => !x.Prerelease);
                    }
                }

                if (lastest == null)
                {
                    return;
                }

                Console.WriteLine($"ver: {lastest.Name}");
                Console.WriteLine($"tag: {lastest.Name}");
                Console.WriteLine(string.Empty);

                using (var sr = new StringReader(lastest.Body))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Console.WriteLine(sr.ReadLine());
                    }

                    Console.WriteLine("...etc");
                }

                var asset = lastest.Assets.FirstOrDefault(x => x.Name.Contains("ACT.Hojoring"));
                if (asset == null)
                {
                    return;
                }

                Console.WriteLine(string.Empty);

                var file = Path.Combine(
                    dest,
                    asset.Name);

                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                if (Directory.Exists(dest))
                {
                    Directory.Delete(dest, true);
                }

                Directory.CreateDirectory(dest);
                Thread.Sleep(200);

                DownloadAssets(asset.BrowserDownloadUrl, file);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                AssemblyResolver.Free();
            }
        }

        private static void DownloadAssets(
            string url,
            string file)
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var web = new WebClient())
                    {
                        var preprogress = 0d;

                        web.DownloadProgressChanged += (x, y) =>
                        {
                            lock (web)
                            {
                                try
                                {
                                    var progress = (double)y.BytesReceived / (double)y.TotalBytesToReceive;
                                    if ((progress - preprogress) >= 0.05d)
                                    {
                                        Console.Write("+");
                                        Console.Out.Flush();
                                        preprogress = progress;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }
                        };

                        Console.WriteLine("Downloading...");
                        await web.DownloadFileTaskAsync(
                            new Uri(url),
                            file);

                        Thread.Sleep(200);

                        Console.WriteLine(" Done!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }).Wait();
        }
    }
}
