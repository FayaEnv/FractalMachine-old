using FractalMachine.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading;

namespace FractalMachine.Classes
{
    public class Net
    {
        #region FileDownload

        static int fileDownloadLastPercentage;
        static bool fileDownloadEnded;
        static DateTime fileDownloadLastUpdate;
        static WebClient fileDownloadClient;

        public static void FileDownload(string url, string output)
        {
        retry:

            fileDownloadEnded = false;
            fileDownloadLastPercentage = -1;

            fileDownloadClient = new WebClient();
            fileDownloadClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
            fileDownloadClient.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
            fileDownloadClient.DownloadFileAsync(new Uri(url), output);

            fileDownloadLastUpdate = DateTime.Now;
            while (!fileDownloadEnded)
            {
                // If there is no update within 20 seconds so restart the download
                if (DateTime.Now.Subtract(fileDownloadLastUpdate).TotalSeconds > 20)
                {
                    Console.WriteLine("Maybe download is blocked, retry...");
                    fileDownloadClient.CancelAsync();
                    fileDownloadClient.Dispose();
                    Thread.Sleep(1000);
                    goto retry;
                }

                Thread.Sleep(100);
            }
        }
        static void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            int percentage = (int)(bytesIn / totalBytes * 100);
            if (fileDownloadLastPercentage != percentage)
            {
                Console.Write((int)percentage + "% ");
                fileDownloadLastPercentage = percentage;
            }
            fileDownloadLastUpdate = DateTime.Now;
        }
        static void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                if (sender == fileDownloadClient)
                {
                    throw e.Error;
                }
            }

            fileDownloadEnded = true;
            Console.WriteLine("\r\nCompleted!");
        }
        #endregion
    }
}
