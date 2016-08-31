using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevDefined.OAuth.Consumer;
using FantasyFootballAnalyzer;

namespace HistoricalDataAndTestMethods
{
    class Program
    {
        private static Random rand = new Random((int)DateTime.Now.Ticks);
        private const string ApiUrl = "http://fantasysports.yahooapis.com/fantasy/v2/";
        private static StreamWriter errorLogger = new StreamWriter("C:\\Users\\SomeFOlde\\Logs\\ErrorLog.txt", true);
        private static OAuthSession session;
        private static string dataFilePath =
            "C:\\Users\\SomeFolder\\Data\\";
        

        static void Main(string[] args)
        {
            //this class contains methods for retrieving historical league data
            //as well as methods to used to test various parts of the model when it was first created
        }

        private static void TestNormalDistribution()
        {
            using (StreamWriter sw = new StreamWriter(@"Data\RandomGeneratorOutput"))
            {
                DateTime start = DateTime.Now;

                for (int i = 1; i < 10000; i++)
                {
                    sw.WriteLine(GetRandomError());
                    Thread.Sleep(20);
                }

                sw.WriteLine("10,000 iterations completed in {0} seconds", DateTime.Now.Second - start.Second);
            }
        }

        private static double GetRandomError()
        {
            double modelStdDev = 21.94;

            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return modelStdDev * randStdNormal; //random normal(mean,stdDev^2)

        }

        private static void GetHistoricalMatchupData()
        {
            StreamWriter sw = new StreamWriter(@"Data\AllMatchupScores" + DateTime.Now.ToString("yy-MM-dd.hh.mm.ss") + ".tab",
                false);
            List<string> weeks = new List<string>
            {
                "1",
                "2",
                "3",
                "4",
                "5",
                "6",
                "7",
                "8",
                "9",
                "10",
                "11",
                "12",
                "13",
                "14",
                "15",
                "16"
            };

            try
            {
                Dictionary<string, List<string>> seasonIds = GetAllSeasonIds();

                //write header line
                sw.WriteLine(
                    "Week\tWeekStart\tWeekEnd\tIsPlayoffs\tTeam1\tTeam2\tTeam1Points\tTeam2Points\tTeam1ExpPoints\tTeam2ExpPoints");

                foreach (string season in seasonIds.Keys)
                {

                    List<string> seasonKeys = seasonIds[season];

                    foreach (string week in weeks)
                    {
                        string scoreBoardQuery = "league/" + seasonKeys[0] + ".l." + seasonKeys[1] + "/scoreboard;week=" +
                                                 week;

                        IConsumerRequest responseRequest = session.Request().Get().ForUrl(ApiUrl + scoreBoardQuery);

                        List<Matchup> matchupData = new List<Matchup>();
                        //WriteOutScoreboardData(responseRequest, sw, week, ref matchupData, currentRosters);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }

            sw.Close();
        }

        //gets all nfl season ids from previously generated data file
        private static Dictionary<string, List<string>> GetAllSeasonIds()
        {
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();
            using (StreamReader sr = new StreamReader(dataFilePath + "AllSeasonIds.Tab"))
            {
                //skip header line
                sr.ReadLine();

                string line = sr.ReadLine();

                while (line != null)
                {
                    string[] lineArr = line.Split('\t');
                    results.Add(lineArr[0], new List<string> { lineArr[1], lineArr[2] });
                    line = sr.ReadLine();
                }
            }

            return results;

        }

        private static void LogError(Exception e)
        {
            errorLogger.WriteLine("Error at:" + DateTime.Now.ToString()
                    + "Error:\r\n" + e.Message + "\r\n" + "Stack trace: \r\n" + e.StackTrace);
        }
    }
}
