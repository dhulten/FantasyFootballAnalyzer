using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmailUtility;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace PointsScraper
{
    class Program
    {
        //urls to be scraped come in three varities: one for offensive players, one for kickers and one for team defenses
        //each URL consists of three parts: the base URL, the week in interest, and the suffix url
        private static string preWeekOffenseUrl = "http://football.fantasysports.yahoo.com/f1/[leagueid]/players?status=ALL&pos=O&cut_type=9&stat1=S_PW_";
        private static string postWeekUrl = "&myteam=0&sort=PTS&sdir=1&count=";
        private static string preWeekDefenseUrl = "http://football.fantasysports.yahoo.com/f1/[leagueid]/players?status=ALL&pos=DEF&cut_type=9&stat1=S_PW_";
        private static string preWeekKickerUrl = "http://football.fantasysports.yahoo.com/f1/[leagueid]/players?status=ALL&pos=K&cut_type=9&stat1=S_PW_";
        private static string dataLogPath = "C:\\Users\\SomeFolder";

        //scrapes projected points for each player in the league based on league specific scoring rules
        //needs to be re-run every week to get most up to date projections
        static void Main(string[] args)
        {
            using (StreamWriter logger = new StreamWriter("C:\\Users\\SomeFolder\\Logs" + DateTime.Now.ToString("MM_dd_yy_hh_mm_ss") + ".txt"))
            {
                try
                {
                    DateTime startTime = DateTime.Now;

                    logger.WriteLine("Starting scrape at {0}", startTime.ToString("hh:mm:ss"));

                    IWebDriver driver = new FirefoxDriver();
                    driver.Navigate().GoToUrl(preWeekOffenseUrl + 1 + postWeekUrl + 0);

                    //login
                    AutomatedVerfierEntry(driver);

                    //execution can be sped up by removing weeks from this list that have already been played before running.
                    List<int> weeks = new List<int> { 1,2,3,4,5,6,7,8,9,10,11,12,13 };

                    //for offensive field players, get the top 300 projected scorers
                    //for defense and kicker, get only the top 50
                    List<int> fieldPlayerPageIndices = new List<int> { 0, 25, 50, 75, 100, 125, 150, 175, 200, 225, 250, 275, 300 };
                    List<int> defenseKickerPageIndices = new List<int> {0, 25};

                    //scrape offense player projections
                    foreach (int week in weeks)
                    {
                        logger.WriteLine("Begginning offenseive players week {0} at {1}, {2} have elapsed since start", week, DateTime.Now.ToString("hh:mm:ss"),
                            (DateTime.Now - startTime).Seconds);

                        string previousPageSource = "";

                        foreach (int playerCount in fieldPlayerPageIndices)
                        {
                            logger.WriteLine("Getting players from start index of {0} at {1}, {2} have elapsed since start", playerCount, DateTime.Now.ToString("hh:mm:ss"),
                                (DateTime.Now - startTime).Seconds);

                            driver.Navigate().GoToUrl(preWeekOffenseUrl + week + postWeekUrl + playerCount);
                            Thread.Sleep(10000);//ensure page loads properly before saving

                            //try again if necessary
                            while (previousPageSource == driver.PageSource)
                            {
                                driver.Navigate().GoToUrl(preWeekOffenseUrl + week + postWeekUrl + playerCount);
                                Thread.Sleep(10000);
                            }

                            using (
                                StreamWriter sw =
                                    new StreamWriter(dataLogPath + "\\Week_" + week + "_Players_" + playerCount + ".txt", false)
                                )
                            {
                                sw.WriteLine(driver.PageSource);
                            }

                            previousPageSource = driver.PageSource;

                        }
                    }


                    //scrape defensive team scores
                    foreach (int week in weeks)
                    {
                        logger.WriteLine("Begginning defensive teams week {0} at {1}, {2} have elapsed since start", week, DateTime.Now.ToString("hh:mm:ss"),
                            (DateTime.Now - startTime).Seconds);

                        foreach (int defenseCount in defenseKickerPageIndices)
                        {
                            logger.WriteLine("Getting defenses from start index of {0} at {1}, {2} have elapsed since start", defenseCount, DateTime.Now.ToString("hh:mm:ss"),
                                (DateTime.Now - startTime).Seconds);

                            driver.Navigate().GoToUrl(preWeekDefenseUrl + week + postWeekUrl + defenseCount);
                            Thread.Sleep(10000);

                            using (
                                StreamWriter sw =
                                    new StreamWriter(dataLogPath + "\\Week_" + week + "_Def_" + defenseCount + ".txt", false))
                            {
                                sw.WriteLine(driver.PageSource);
                            }

                        }
                    }


                    //scrape kicker scores
                    foreach (int week in weeks)
                    {
                        logger.WriteLine("Begginning kickers week {0} at {1}, {2} have elapsed since start", week, DateTime.Now.ToString("hh:mm:ss"),
                            (DateTime.Now - startTime).Seconds);

                        foreach (int kickerCount in defenseKickerPageIndices)
                        {
                            logger.WriteLine("Getting kickers from start index of {0} at {1}, {2} have elapsed since start", kickerCount, DateTime.Now.ToString("hh:mm:ss"),
                                (DateTime.Now - startTime).Seconds);

                            driver.Navigate().GoToUrl(preWeekKickerUrl + week + postWeekUrl + kickerCount);
                            Thread.Sleep(10000);

                            using (
                                StreamWriter sw =
                                    new StreamWriter(dataLogPath + "\\Week_" + week + "_Kicker_" + kickerCount + ".txt", false))
                            {
                                sw.WriteLine(driver.PageSource);
                            }

                        }
                    }

                    driver.Close();
                }
                catch (Exception e)
                {
                    logger.WriteLine(e.Message + "\r\n" + e.StackTrace);
                    Emailer.SendEmail("Warning: error occurred during Page Scraping", e.Message + "\r\n" + e.StackTrace);
                }
            }
        }

        //automatically logs in and scrapes the verifier code from yahoo
        private static void AutomatedVerfierEntry(IWebDriver driver)
        {
            
            List<IWebElement> loginElements = driver.FindElements(By.Id("login-username")).ToList();

            //if already logged in, it won't take you to the login screen
            //cannot test a single element with FindElement by id without throwing exception if it's not there
            if (loginElements.Count > 0)
            {
                //enter passwords and hit login
                IWebElement userName = loginElements.FirstOrDefault();
                userName.SendKeys("username");
                driver.FindElement(By.Id("login-signin")).Click();
                //wait for password entry field
                Thread.Sleep(1000);
                IWebElement password = driver.FindElement(By.Id("login-passwd"));
                password.SendKeys("password");
                // Now submit the form. WebDriver will find the form for us from the element
                driver.FindElement(By.Id("login-signin")).Click();

                //wait for redirect
                Thread.Sleep(2000);
            }
        }
    }
}
