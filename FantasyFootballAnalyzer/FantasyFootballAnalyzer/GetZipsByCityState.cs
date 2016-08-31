using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetZipsByCityState
{
    class Program
    {
        static void Main(string[] args)
        {
            QueryGoogleLocationApi();
        }

		//reads in a text file with rows of city and state data, separated by tabs
		//queries Google's public API to get the zip code of that city
		private static void QueryGoogleLocationApi()
        {

            List<List<string>> cities = new List<List<string>>();
            using (StreamReader sr = new StreamReader("CityInput.txt"))
            {
                string line = sr.ReadLine();

                while (line != null)
                {
                    string[] lineArr = line.Split('\t');
                    cities.Add(new List<string> { lineArr[0], lineArr[1] });

                    line = sr.ReadLine();
                }

            }

            using (StreamWriter sw = new StreamWriter(@"Data\CityOutput.Tab"))
            {
                sw.WriteLine("City, ST\tLat\tLon");
                foreach (List<string> cityList in cities)
                {
                    string resultString = cityList[0] + ", " + cityList[1] + "\t";
                    string cityAndState = cityList[0] + ",+" + cityList[1];


                    string url =
                        "https://maps.googleapis.com/maps/api/geocode/xml?address=" + cityAndState +
                        "&key=AIzaSyDuMzqW-F6DY_yFdP7FNMySrRkOdLNvAwg";

                    // Create a request for the URL. 
                    WebRequest request = WebRequest.Create(url);

                    // Get the response.
                    WebResponse response = request.GetResponse();

                    // Get the stream containing content returned by the server.
                    Stream dataStream = response.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    //all matches in a season
                    var responseXml = XElement.Parse(responseFromServer);

                    var geoCodeNodes = responseXml.Elements();

                    foreach (var geoCodeNode in geoCodeNodes)
                    {
                        if (geoCodeNode.Name == "result")
                        {
                            var resultNodes = geoCodeNode.Elements();

                            foreach (var resultNode in resultNodes)
                            {
                                if (resultNode.Name == "geometry")
                                {
                                    var geometryNodes = resultNode.Elements();

                                    foreach (var geometryNode in geometryNodes)
                                    {
                                        if (geometryNode.Name == "location")
                                        {
                                            var locationNodes = geometryNode.Elements();

                                            foreach (var node in locationNodes)
                                            {
                                                resultString += node.Value + "\t";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    sw.WriteLine(resultString);
                }
            }
        }
    }
}
