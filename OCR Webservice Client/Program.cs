using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using OCR_Webservice_Model;

namespace OCR_Webservice_Client
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Post("http://localhost:49912/api/tiff", "Tif", "HintDesk.tif");
			Post("http://localhost:49912/api/pdf", "Pdf", "HintDesk.pdf");
			Post("http://localhost:49912/api/zip", "Zip", "HintDesk.zip");

			Console.ReadLine();
		}

		private static void Post(string url, string name, string fileName)
		{
			Uri server = new Uri(url);
			HttpClient httpClient = new HttpClient();
			httpClient.Timeout = new TimeSpan(httpClient.Timeout.Ticks * 5);
			StreamContent streamConent = new StreamContent(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read));
			MultipartFormDataContent multipartFormDataContent = new MultipartFormDataContent();
			multipartFormDataContent.Add(streamConent, name, fileName);

			HttpResponseMessage responseMessage = httpClient.PostAsync(server, multipartFormDataContent).Result;

			if (responseMessage.IsSuccessStatusCode)
			{
				IList<HDFile> hdFiles = responseMessage.Content.ReadAsAsync<IList<HDFile>>().Result;

				foreach (var item in hdFiles)
				{
					Console.WriteLine(item.Text);
				}
			}
		}
	}
}