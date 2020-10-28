/*
   Copyright 2020 (c) Riccardo Cecchini
   
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

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
