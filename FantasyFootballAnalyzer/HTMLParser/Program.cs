using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmailUtility;
using HtmlAgilityPack;

namespace HTMLParser
{
    class Program
    {
        private static string dataFilePath =
            "C:\\Users\\SomeFolder\\Data";

        //reads in scraped html files that contain projected points for each player in the league
        //writes out a neatly formatted txt file containing week, player name, playerId, projected points and position
        private static void Main(string[] args)
        {
            using (
                StreamWriter errorLogger =
                    new StreamWriter("C:\\Users\\SomeFolder\\Logs\\" + DateTime.Now.ToString("MM_dd_yy_hh_mm_ss") + ".txt", false))
            {
                try
                {
                    //get list of all input data files and instantiate new HtmlDoc
                    string[] files = Directory.GetFiles(dataFilePath);
                    HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.OptionFixNestedTags = true;

                    List<string> results = new List<string>();

                    //process each file, order does not matter
                    foreach (string fileName in files)
                    {
                        //week data is from is inferred from filename
                        int firstUnderscore = fileName.IndexOf("_");
                        int secondUnderscore = fileName.IndexOf("_", firstUnderscore + 1);
                        string week = fileName.Substring(firstUnderscore + 1, secondUnderscore - firstUnderscore - 1);

                        int lastUnderScore = fileName.LastIndexOf("_");
                        int firstPeriod = fileName.IndexOf(".");
                        string playerPage = fileName.Substring(lastUnderScore + 1, firstPeriod - lastUnderScore - 1);

                        htmlDoc.Load(fileName);

                        if (htmlDoc.DocumentNode != null)
                        {
                            HtmlAgilityPack.HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");

                            if (bodyNode != null)
                            {
                                List<HtmlNode> allTables = bodyNode.Descendants("tbody").ToList();

                                for (int i = 0; i < allTables.Count; i++)
                                {

                                    var playerTable = allTables[i];

                                    List<HtmlNode> playerRows = playerTable.Descendants("tr").ToList();

                                    foreach (HtmlNode playerRow in playerRows)
                                    {
                                        try
                                        {
                                            List<HtmlNode> rowCells = playerRow.Descendants("td").ToList();

                                            HtmlNode playerNameNode = rowCells[1];
                                            HtmlNode projectedPointsNode = rowCells[5];

                                            HtmlNode playerIdSubNode =
                                                playerNameNode.Descendants("a")
                                                    .FirstOrDefault(a => a.Attributes.Contains("data-ys-playerid"));
                                            string playerId = playerIdSubNode.Attributes["data-ys-playerid"].Value;

                                            HtmlNode playerPosSubNode =
                                                playerNameNode.Descendants("span")
                                                    .FirstOrDefault(a => a.Attributes["class"].Value == "Fz-xxs");
                                            string playerPos =
                                                playerPosSubNode.InnerHtml.Substring(
                                                    playerPosSubNode.InnerHtml.IndexOf("-") + 1,
                                                    playerPosSubNode.InnerHtml.Length -
                                                    playerPosSubNode.InnerHtml.IndexOf("-") - 1);

                                            HtmlNode playerNameSubNode =
                                                playerNameNode.Descendants("a")
                                                    .FirstOrDefault(
                                                        a => a.Attributes["class"].Value == "Nowrap name F-link");

                                            string playerName = playerNameSubNode.InnerHtml;

                                            string projectedPoints = projectedPointsNode.InnerText;

                                            string line = string.Format("{3}\t{0}\t{1}\t{2}\t{4}\t{5}", playerName,
                                                playerId, projectedPoints, week, playerPage, playerPos);

                                            if (!results.Contains(line))
                                            {
                                                results.Add(line);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            errorLogger.WriteLine(e.Message + "\r\n" + e.StackTrace);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //sometimes scraper does not download all pages successfully
                    //calculate the number of players that have projected points from each week
                    //to ensure that new data was retrived for all players
                    int week1Count = 0;
                    int week2Count = 0;
                    int week3Count = 0;
                    int week4Count = 0;
                    int week5Count = 0;
                    int week6Count = 0;
                    int week7Count = 0;
                    int week8Count = 0;
                    int week9Count = 0;
                    int week10Count = 0;
                    int week11Count = 0;
                    int week12Count = 0;
                    int week13Count = 0;

                    using (StreamWriter sw = new StreamWriter("C:\\Users\\SomeFolder\\Data\\" + DateTime.Now.ToString("MM_dd_yy") + ".txt", false))
                    {
                        sw.WriteLine("Week\tPlayerName\tPlayerId\tPoints\tPageIndex");
                        foreach (string line in results)
                        {
                            string[] lineArr = line.Split('\t');
                            string week = lineArr[0];
                            switch (week)
                            {
                                case "1":
                                {
                                    week1Count++;
                                    break;
                                }
                                case "2":
                                {
                                    week2Count++;
                                    break;
                                }
                                case "3":
                                {
                                    week3Count++;
                                    break;
                                }
                                case "4":
                                {
                                    week4Count++;
                                    break;
                                }
                                case "5":
                                {
                                    week5Count++;
                                    break;
                                }
                                case "6":
                                {
                                    week6Count++;
                                    break;
                                }
                                case "7":
                                {
                                    week7Count++;
                                    break;
                                }
                                case "8":
                                {
                                    week8Count++;
                                    break;
                                }
                                case "9":
                                {
                                    week9Count++;
                                    break;
                                }
                                case "10":
                                {
                                    week10Count++;
                                    break;
                                }
                                case "11":
                                {
                                    week11Count++;
                                    break;
                                }
                                case "12":
                                {
                                    week12Count++;
                                    break;
                                }
                                case "13":
                                {
                                    week13Count++;
                                    break;
                                }
                            }
                            sw.WriteLine(line);
                        }
                    }

                    Console.WriteLine("Week 1 player count: {0}", week1Count);
                    Console.WriteLine("Week 2 player count: {0}", week2Count);
                    Console.WriteLine("Week 3 player count: {0}", week3Count);
                    Console.WriteLine("Week 4 player count: {0}", week4Count);
                    Console.WriteLine("Week 5 player count: {0}", week5Count);
                    Console.WriteLine("Week 6 player count: {0}", week6Count);
                    Console.WriteLine("Week 7 player count: {0}", week7Count);
                    Console.WriteLine("Week 8 player count: {0}", week8Count);
                    Console.WriteLine("Week 9 player count: {0}", week9Count);
                    Console.WriteLine("Week 10 player count: {0}", week10Count);
                    Console.WriteLine("Week 11 player count: {0}", week11Count);
                    Console.WriteLine("Week 12 player count: {0}", week12Count);
                    Console.WriteLine("Week 13 player count: {0}", week13Count);

                    List<int> weekCounts = new List<int>
                    {
                        week1Count, week2Count, week3Count, week4Count, week5Count, week6Count, week7Count, week8Count, week9Count, week10Count,
                        week11Count, week12Count, week13Count
                    };

                    //ignore past weeks that have no player data and 0 player rows
                    List<int> uniqueCounts = weekCounts.Where(w => w != 0).Distinct().ToList();

                    //send an alert email with information showing which week was missing data
                    if (uniqueCounts.Count != 1)
                    {
                        Emailer.SendEmail("Warning: incomplete weekly scoring data detected", string.Format("Weekly players found:\r\n1: {0}\r\n2: {1}\r\n3: {2}\r\n4: {3}\r\n5: {4}\r\n6: {5}\r\n" +
                              "7: {6}\r\n8: {7}\r\n9: {8}\r\n10: {9}\r\n11: {10}\r\n12: {11}\r\n13: {12}\r\n", week1Count, week2Count, week3Count, week4Count, week5Count, week6Count, week7Count,
                              week8Count, week9Count, week10Count, week11Count, week12Count, week13Count));
                    }
                }
                catch (Exception e)
                {
                    errorLogger.WriteLine(e.Message + "\r\n" + e.StackTrace);
                    Emailer.SendEmail("Warning Error occurred during HTML Parser", e.Message + "\r\n" + e.StackTrace);
                }
            }
        }
    }
}
