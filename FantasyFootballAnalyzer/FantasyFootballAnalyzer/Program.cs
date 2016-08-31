// .Net Framework Libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using DevDefined.OAuth.Consumer;
using DevDefined.OAuth.Framework;
using EmailUtility;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace FantasyFootballAnalyzer
{
    /// <summary>
    /// Simulates the remainnig fantasy football season 20,000 times to predict who will make the playoffs
    /// Also collects useful information about available free agent players
    /// TODO: THERE ARE MANY CONSTANTS (BOTH INTEGNER AND STRING) THAT NEED TO BE DECLARED AS VARIABLES FOR BETTER STYLE
    /// </summary>
    public static class Program
    {
        private const string RequestUrl = "https://api.login.yahoo.com/oauth/v2/get_request_token";
        private const string UserAuthorizeUrl = "https://api.login.yahoo.com/oauth/v2/request_auth";
        private const string AccessUrl = "https://api.login.yahoo.com/oauth/v2/get_token";

        private const string ConsumerKey =
            "[custom key aquired from yahoo";

        private const string HmacSecert = "[secret hmac key";
        private static string _authenticateUrl = "https://api.login.yahoo.com/oauth/v2/request_auth?oauth_token=";
        private const string ApiUrl = "http://fantasysports.yahooapis.com/fantasy/v2/";
        private static StreamWriter errorLogger = new StreamWriter("C:\\Users\\SomeFolder\\ErrorLog.txt", true);
        private static Dictionary<string, double> parsedScoreValues = new Dictionary<string, double>();
        private static OAuthSession session;
        private static List<double> variationsUsed = new List<double>();
        //a static Random object must be used to generate new results each time Random.Next() is called, as the values it returns are based on timestamp of creation
        //the simulation runs fast enough that if a new Random object is created every time it is needed, there will be thousdands of calls to Random.Next() returning the same result
        private static Random rand = new Random((int) DateTime.Now.Ticks);

        private const double QbReplacementPoints = 16;
        private const double WrReplacementPoints = 7.5;
        private const double RbReplacementPoints = 7.5;
        private const double TeReplacementPoints = 5.5;
        private const double KickerReplacementPoints = 7;
        private const double DefenseReplacementPoints = 6.5;

        private static Dictionary<int, string> teamIdsToNames = new Dictionary<int, string>(); 
        private static string emailBody = "";
        private static List<int> CurrentlyRosteredPlayerIds = new List<int>(); 
        private static List<WeeklyScoreBoard> UnrosteredPlayerScores = new List<WeeklyScoreBoard>();

        private static string dataFilePath =
            "C:\\Users\\SomeFolder\\";

        private static void Main(string[] args)
        {
            try
            {
                //generate oauth consumer context with keys
                var consumerContext = new OAuthConsumerContext
                {
                    ConsumerKey = ConsumerKey,
                    SignatureMethod = SignatureMethod.HmacSha1,
                    ConsumerSecret = HmacSecert
                };

                session = new OAuthSession(consumerContext, RequestUrl, UserAuthorizeUrl, AccessUrl, "oob");
                var requestToken = session.GetRequestToken();
                string token = requestToken.ToString();
                Dictionary<string, string> returnedParamsAndValues = GetResponseDictionary(token);
                string tokenString = returnedParamsAndValues["oauth_token"];
                _authenticateUrl += tokenString;

                IWebDriver driver = new FirefoxDriver();
                driver.Navigate().GoToUrl(_authenticateUrl);

                //original manual verification method should only be uncommented if needed
                //ManualVerifierEntry(session, requestToken);
                
                //automated entry will log in for the user automatically so they don't have to retrieve the auth key and enter it into the console
                AutomatedVerfierEntry(session, requestToken, driver);

                //close driver or yahoo doesn't let you proceed with results
                driver.Close();

                //needs to be run at the start of each season to get the new ids
                //GetOldSeasonIds();

                //once validated, analyze season
                AnalyzeSeason();

                //send email with results
                Emailer.SendEmail("Fantasy Football Digest", emailBody);
            }
            catch (Exception e)
            {
                Emailer.SendEmail("Warning Error occurred during analysis", e.Message + "\r\n" + e.StackTrace);
            }

            errorLogger.Close();

        }

        private static void AnalyzeSeason()
        {
            //must be updated immediately before analyzing season to reflect recent roster changes
            List<FantasyRoster> currentRosters = GetCurrentRosters();

            //get player projected points data for currently rostered players
            GetPlayerData(currentRosters);

            //get projected points data for unrostered players, to identify targets who might be useful additions
            GetUnrosteredPlayerData();

            //get head to head matchup data for current season
            List<Matchup> seasonMatchupData = GetSeasonsData(currentRosters);

            //simulate every matchup based on projected points
            List<OwnerSimulationResults> playerSimulations = new List<OwnerSimulationResults>();
            Dictionary<string, double> playoffOdds = SimulateSeason(seasonMatchupData, ref playerSimulations);

            //get high level stats summary for each owner
            Dictionary<string, OwnerScheduleInfo> scheduleAnalysis = GetOpponentInfo(seasonMatchupData);

            emailBody += "Standings summary:\r\n\r\n";

            //write out season simulation results
            using (StreamWriter sw = new StreamWriter(dataFilePath +"WeeklySummary_" + DateTime.Now.ToString("MM-dd-yy.hh.mm.ss") + ".txt"))
            {
                string headerLine = String.Format("{0}\t{1}\t{2}\t{18}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                    "Team Name", "Wins", "Losses", "Total Expected Points Against", "Total Points Against", "Percent Diff", "Median Expected Points Against", "Median Points Against",
                    "Percent Diff", "Median Strength of Vicotry", "Median Strength of Loss", "Playoff Probabilty", "Mean Expected Wins", "Mean Expected Losses", "Total Expected Points For", "Median Project Points For", "Total Expected Points Against",
                    "Median Expected Points Against", "Total Points For");
                sw.WriteLine(headerLine);
                emailBody += headerLine + "\r\n";

                foreach (string teamName in playoffOdds.Keys)
                {
                    OwnerScheduleInfo poi = scheduleAnalysis[teamName];

                    double totalPointsAgainst = poi.WeeklyPointsAgainst.Sum();
                    double totalExpPointsAgainst = poi.WeeklyPreviousExpectedPointsAgainst.Sum();
                    double totalPercentAberration = (totalPointsAgainst - totalExpPointsAgainst) / totalExpPointsAgainst;
                    double weeklyMedianAberration = GetMedian(poi.WeeklyOpponentPercentAbberation);
                    double medianPointsAgainst = GetMedian(poi.WeeklyPointsAgainst);
                    double medianExpectedPointsAgainst = GetMedian(poi.WeeklyPreviousExpectedPointsAgainst);
                    double totalFuturePointsAgainst = poi.WeeklyFutureExpectedPointsAgainst.Sum();
                    double medianFuturePointsAgainst = GetMedian(poi.WeeklyFutureExpectedPointsAgainst);
                    double totalFuturePointsFor = poi.WeeklyExpectedPointsFor.Sum();
                    double medianFuturePointsFor = GetMedian(poi.WeeklyExpectedPointsFor);
                    OwnerSimulationResults ownerSimulationResults = playerSimulations.FirstOrDefault(ps => ps.TeamName == teamName);
                    double averageExpectedWins = ownerSimulationResults.Wins.Average();
                    double averageExpectedLosses = ownerSimulationResults.Losses.Average();
                    double medianStrengthOfVictory = GetMedian(poi.StrengthOfVictory);
                    double medianStrengthOfLoss = GetMedian(poi.StrengthOfLoss);
                    double totalPointsFor = poi.WeeklyPreviousPointsFor.Sum();

                    string output =
                        string.Format(
                            "{0}\t{1}\t{2}\t{18}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}",
                            teamName, poi.Wins, poi.Losses, totalExpPointsAgainst, totalPointsAgainst,
                            totalPercentAberration, medianExpectedPointsAgainst, medianPointsAgainst,
                            weeklyMedianAberration, medianStrengthOfVictory, medianStrengthOfLoss,
                            playoffOdds[teamName], averageExpectedWins, averageExpectedLosses, totalFuturePointsFor,
                            medianFuturePointsFor, totalFuturePointsAgainst, medianFuturePointsAgainst, totalPointsFor);

                    emailBody += output + "\r\n";
                    sw.WriteLine(output);
                }
            }
        }

        //method to identify unrostered players who were drafted and might have keeper value, as well as players who have high projected points in upcoming weeks
        private static void GetUnrosteredPlayerData()
        {
            Console.WriteLine("Getting unrostered player data");
            
            List<FootballPlayer> UnrosteredDraftedPlayers = new List<FootballPlayer>();
            Dictionary<string, List<string>> seasonIds = GetAllSeasonIds();

            string currentSeasonYear = seasonIds.Keys.OrderByDescending(y => y).FirstOrDefault();
            string currentSeasonId = seasonIds[currentSeasonYear][1];
            string currentLeagueId = seasonIds[currentSeasonYear][0];

            string draftResultsQuery = "league/" + currentLeagueId + ".l." + currentSeasonId + "/draftresults";

            IConsumerRequest responseRequest = session.Request().Get().ForUrl(ApiUrl + draftResultsQuery);

            var resultXml = XElement.Parse(responseRequest.ToString());

            foreach (var node in resultXml.Elements())
            {
                if (GetNodeName(node) == "league")
                {
                    foreach (var leagueNode in node.Elements())
                    {
                        if (GetNodeName(leagueNode) == "draft_results")
                        {
                            foreach (var draftResults in leagueNode.Elements())
                            {
                                if (GetNodeName(draftResults) == "draft_result")
                                {
                                    int roundPicked = 0;
                                    int playerId = 0;
                                    foreach (var draftResult in draftResults.Elements())
                                    {
                                        if (GetNodeName(draftResult) == "round")
                                        {
                                            roundPicked = Int32.Parse(draftResult.Value);
                                        }
                                        else if (GetNodeName(draftResult) == "player_key")
                                        {
                                            //formatted like 348.p.26684
                                            int periodIndex = draftResult.Value.LastIndexOf(".");
                                            playerId = Int32.Parse(draftResult.Value.Substring(periodIndex + 1,
                                                draftResult.Value.Length - periodIndex - 1));
                                        }

                                        if (!CurrentlyRosteredPlayerIds.Contains(playerId) && playerId != 0)
                                        {
                                            FootballPlayer unrosteredPlayer = new FootballPlayer();
                                            unrosteredPlayer.PlayerId = playerId;
                                            unrosteredPlayer.DraftRound = roundPicked;
                                            UnrosteredDraftedPlayers.Add(unrosteredPlayer);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            emailBody += "Unrostered players who were drafted:\r\n";

            foreach (FootballPlayer unrosteredPlayer in UnrosteredDraftedPlayers)
            {
                string playerQuery = "player/" + currentLeagueId + ".p." + unrosteredPlayer.PlayerId;

                responseRequest = session.Request().Get().ForUrl(ApiUrl + playerQuery);

                resultXml = XElement.Parse(responseRequest.ToString());

                string playerName = "";

                foreach (var node in resultXml.Elements())
                {
                    if (GetNodeName(node) == "player")
                    {
                        foreach (var playerNode in node.Elements())
                        {
                            if (GetNodeName(playerNode) == "name")
                            {
                                foreach (var nameNode in playerNode.Elements())
                                {
                                    if (GetNodeName(nameNode) == "full")
                                    {
                                        playerName = nameNode.Value;
                                        break;  
                                    }
                                }
                            }
                        }
                    }
                }

                emailBody += string.Format("{0}, round picked: {1}\r\n", playerName, unrosteredPlayer.DraftRound);
            }

            emailBody += "\r\n\r\nUpcoming weekly scorers available ";

            int currentWeek = UnrosteredPlayerScores.Select(s => s.Week).OrderBy(w => w).FirstOrDefault();

            int playersWanted = 10;
            int kickersDefWanted = 5;
            
            for (int i = currentWeek; i <= currentWeek + 2; i++)
            {
                int qbsAdded = 0;
                int rbsAdded = 0;
                int wrsAdded = 0;
                int tesAdded = 0;
                int defAdded = 0;
                int kickersAdded = 0;

                string qbFreeAgents = "QB:\r\n";
                string rbFreeAgents = "RB:\r\n";
                string wrFreeAgents = "WR:\r\n";
                string teFreeAgents = "TE:\r\n";
                string defFreeAgets = "DEF:\r\n";
                string kickerFreeAgents = "K:\r\n";

                WeeklyScoreBoard weeklyScoreBoard = UnrosteredPlayerScores.FirstOrDefault(s => s.Week == i);

                if (weeklyScoreBoard != null)
                {
                    List<FootballPlayer> currentScorers = weeklyScoreBoard.WeeklyPlayers.OrderByDescending(p => p.CurrentWeekPoints).ToList();

                    foreach (FootballPlayer f in currentScorers)
                    {
                        string position = f.PlayerPositions.FirstOrDefault().Trim();

                        switch (position)
                        {
                            case "RB":
                            {
                                if (rbsAdded < playersWanted)
                                {
                                    rbsAdded++;
                                    rbFreeAgents += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                            case "QB":
                            {
                                if (qbsAdded < playersWanted)
                                {
                                    qbsAdded++;
                                    qbFreeAgents += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                            case "WR":
                            {
                                if (wrsAdded < playersWanted)
                                {
                                    wrsAdded++;
                                    wrFreeAgents += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                            case "TE":
                            {
                                if (tesAdded < playersWanted)
                                {
                                    tesAdded++;
                                    teFreeAgents += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                            case "K":
                            {
                                if (kickersAdded < kickersDefWanted)
                                {
                                    kickersAdded++;
                                    kickerFreeAgents += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                            case "DEF":
                            {
                                if (defAdded < kickersDefWanted)
                                {
                                    defAdded++;
                                    defFreeAgets += string.Format("{0}, projected points: {1}\r\n",
                                        f.PlayerName, f.CurrentWeekPoints);
                                }
                                break;
                            }
                        }
                    }

                    emailBody += String.Format("Week {0}:\r\n\r\n", i);
                    emailBody += qbFreeAgents + "\r\n";
                    emailBody += rbFreeAgents + "\r\n";
                    emailBody += wrFreeAgents + "\r\n";
                    emailBody += teFreeAgents + "\r\n";
                    emailBody += kickerFreeAgents + "\r\n";
                    emailBody += defFreeAgets + "\r\n";
                    emailBody += "\r\n\r\n";
                }
            }
        }


        //gets projectd points data for all currently rostered players
        private static void GetPlayerData(List<FantasyRoster> currentRosters)
        {
            Console.WriteLine("Getting player projected point data");
            string sourceFilePath = "C:\\Users\\SomeFolder\\Data";
            string[] files = Directory.GetFiles(sourceFilePath);

            foreach (string fileName in files)
            {
                if (fileName.Contains(DateTime.Now.ToString("MM_dd_yy")))
                {
                    List<FootballPlayer> pointList = new List<FootballPlayer>();

                    using (StreamReader sr = new StreamReader(fileName))
                    {
                        //get past header row
                        string line = sr.ReadLine();
                        line = sr.ReadLine();

                        while (line != null)
                        {
                            string[] lineArr = line.Split('\t');
                            int week = Int32.Parse(lineArr[0]);
                            string playerName = lineArr[1];
                            int playerId = Int32.Parse(lineArr[2]);
                            double projectedPoints = double.Parse(lineArr[3]);
                            string position = lineArr[5];

                            FootballPlayer player = pointList.FirstOrDefault(p => p.PlayerId == playerId);

                            if (player == null)
                            {
                                player = new FootballPlayer();
                                player.PlayerId = playerId;
                                player.PlayerName = playerName;
                                player.CurrentWeekPoints = projectedPoints;
                                player.PlayerPositions.Add(position);
                                pointList.Add(player);
                            }

                            if (!UnrosteredPlayerScores.Any(s => s.Week == week))
                            {
                                UnrosteredPlayerScores.Add(new WeeklyScoreBoard(week));
                            }

                            WeeklyScoreBoard currentWeeklyScoreBoard =
                                UnrosteredPlayerScores.FirstOrDefault(s => s.Week == week);

                            //only need unrostered players for alert email
                            if (!CurrentlyRosteredPlayerIds.Contains(playerId))
                            {
                                FootballPlayer weeklyPlayer = new FootballPlayer();
                                weeklyPlayer.PlayerId = playerId;
                                weeklyPlayer.PlayerName = playerName;
                                weeklyPlayer.CurrentWeekPoints = projectedPoints;
                                weeklyPlayer.PlayerPositions.Add(position);
                                currentWeeklyScoreBoard.WeeklyPlayers.Add(weeklyPlayer);
                            }

                            switch (week)
                            {
                                case 1:
                                {
                                    player.Week1ProjPoints = projectedPoints;
                                    break;
                                }
                                case 2:
                                {
                                    player.Week2ProjPoints = projectedPoints;
                                    break;
                                }
                                case 3:
                                {
                                    player.Week3ProjPoints = projectedPoints;
                                    break;
                                }
                                case 4:
                                {
                                    player.Week4ProjPoints = projectedPoints;
                                    break;
                                }
                                case 5:
                                {
                                    player.Week5ProjPoints = projectedPoints;
                                    break;
                                }
                                case 6:
                                {
                                    player.Week6ProjPoints = projectedPoints;
                                    break;
                                }
                                case 7:
                                {
                                    player.Week7ProjPoints = projectedPoints;
                                    break;
                                }
                                case 8:
                                {
                                    player.Week8ProjPoints = projectedPoints;
                                    break;
                                }
                                case 9:
                                {
                                    player.Week9ProjPoints = projectedPoints;
                                    break;
                                }
                                case 10:
                                {
                                    player.Week10ProjPoints = projectedPoints;
                                    break;
                                }
                                case 11:
                                {
                                    player.Week11ProjPoints = projectedPoints;
                                    break;
                                }
                                case 12:
                                {
                                    player.Week12ProjPoints = projectedPoints;
                                    break;
                                }
                                case 13:
                                {
                                    player.Week13ProjPoints = projectedPoints;
                                    break;
                                }
                            }

                            line = sr.ReadLine();
                        }

                    }

                    //update master points list of all players for later usage with results of current rosters
                    foreach (FantasyRoster fR in currentRosters)
                    {
                        foreach (FootballPlayer p in fR.CurrentRoster)
                        {
                            FootballPlayer player2 = pointList.FirstOrDefault(pl => pl.PlayerId == p.PlayerId);

                            if (player2 != null)
                            {
                                CopyScoreProjections(p, player2);
                            }
                        }
                    }
                }
            }
        }

        //get current rosters for each owner
        private static List<FantasyRoster> GetCurrentRosters()
        {
            Console.WriteLine("getting teamIds");

            List<FantasyRoster> results = new List<FantasyRoster>();

            Dictionary<string, List<string>> seasonIds = GetAllSeasonIds();

            string currentSeasonYear = seasonIds.Keys.OrderByDescending(y => y).FirstOrDefault();
            string currentSeasonId = seasonIds[currentSeasonYear][1];
            string currentLeagueId = seasonIds[currentSeasonYear][0];

            string standingsQuery = "league/" + currentLeagueId + ".l." + currentSeasonId + "/standings";

            IConsumerRequest responseRequest = session.Request().Get().ForUrl(ApiUrl + standingsQuery);

            var resultXml = XElement.Parse(responseRequest.ToString());

            var leagueNode = resultXml.Elements().FirstOrDefault();

            foreach (var node in leagueNode.Elements())
            {
                if (GetNodeName(node) == "standings")
                {
                    foreach (var standingsNode in node.Elements())
                    {
                        if (GetNodeName(standingsNode) == "teams")
                        {
                            foreach (var teamNode in standingsNode.Elements())
                            {
                                if (GetNodeName(teamNode) == "team")
                                {
                                    string teamName = "";
                                    int teamId = 0;

                                    foreach (var teamInfoNode in teamNode.Elements())
                                    {
                                        if (GetNodeName(teamInfoNode) == "team_id")
                                        {
                                            teamId = Int32.Parse(teamInfoNode.Value);
                                        }
                                        else if (GetNodeName(teamInfoNode) == "name")
                                        {
                                            teamName = teamInfoNode.Value;
                                        }
                                    }

                                    teamIdsToNames.Add(teamId, teamName);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("getting rosters");
            foreach (KeyValuePair<int, string> teamPair in teamIdsToNames)
            {
                string rosterQuery = "team/" + currentLeagueId + ".l." + currentSeasonId + ".t." + teamPair.Key + "/roster/players";

                responseRequest = session.Request().Get().ForUrl(ApiUrl + rosterQuery);

                FantasyRoster newRoster = new FantasyRoster(teamPair.Value);

                resultXml = XElement.Parse(responseRequest.ToString());

                var teamNode = resultXml.Elements().FirstOrDefault();

                foreach (var node in teamNode.Elements())
                {
                    if (GetNodeName(node) == "roster")
                    {
                        foreach (var rosterNode in node.Elements())
                        {
                            if (GetNodeName(rosterNode) == "players")
                            {
                                foreach (var playerNode in rosterNode.Elements())
                                {
                                    if (GetNodeName(playerNode) == "player")
                                    {
                                        int playerId = 0;
                                        string playerName = "";
                                        List<string> selectedPositions = new List<string>();
                                        int byeWeek = 0;

                                        foreach (var playerInfoNode in playerNode.Elements())
                                        {
                                            if (GetNodeName(playerInfoNode) == "player_id")
                                            {
                                                playerId = Int32.Parse(playerInfoNode.Value);
                                            }
                                            else if (GetNodeName(playerInfoNode) == "name")
                                            {
                                                foreach (var nameNode in playerInfoNode.Elements())
                                                {
                                                    if (GetNodeName(nameNode) == "full")
                                                    {
                                                        playerName = nameNode.Value;
                                                    }
                                                }
                                            }
                                            else if (GetNodeName(playerInfoNode) == "eligible_positions")
                                            {
                                                foreach (var positionNode in playerInfoNode.Elements())
                                                {
                                                    if (GetNodeName(positionNode) == "position")
                                                    {
                                                        selectedPositions.Add(positionNode.Value);
                                                    }
                                                }
                                            }
                                            else if (GetNodeName(playerInfoNode) == "bye_weeks")
                                            {
                                                foreach (var byeWeekNode in playerInfoNode.Elements())
                                                {
                                                    if (GetNodeName(byeWeekNode) == "week")
                                                    {
                                                        byeWeek = Int32.Parse(byeWeekNode.Value);
                                                    }
                                                }
                                            }
                                        }

                                        FootballPlayer newPlayer = new FootballPlayer();
                                        newPlayer.PlayerName = playerName;
                                        newPlayer.PlayerId = playerId;
                                        newPlayer.PlayerPositions = selectedPositions;
                                        newPlayer.ByeWeek = byeWeek;

                                        CurrentlyRosteredPlayerIds.Add(playerId);

                                        newRoster.CurrentRoster.Add(newPlayer);
                                    }
                                }
                            }
                        }
                    }
                }

                results.Add(newRoster);
            }

            return results;
        }

        //get all information regarding the projected or actual points scored in a matchup depending on whether or not it was already playe
        private static Dictionary<string, OwnerScheduleInfo> GetOpponentInfo(List<Matchup> seasonMatchupData)
        {
            Dictionary<string, OwnerScheduleInfo> remainingSchedulePoints = new Dictionary<string, OwnerScheduleInfo>();

            foreach (Matchup matchupData in seasonMatchupData)
            {
                if (!remainingSchedulePoints.ContainsKey(matchupData.Team1Name))
                {
                    remainingSchedulePoints.Add(matchupData.Team1Name, new OwnerScheduleInfo());
                }

                if (!remainingSchedulePoints.ContainsKey(matchupData.Team2Name))
                {
                    remainingSchedulePoints.Add(matchupData.Team2Name, new OwnerScheduleInfo());
                }

                OwnerScheduleInfo team1ScheduleInfo = remainingSchedulePoints[matchupData.Team1Name];
                OwnerScheduleInfo team2ScheduleInfo = remainingSchedulePoints[matchupData.Team2Name];

                //game has not been played yet, get projected opponent points
                if (matchupData.Team1Points < 0.1 && matchupData.Team2Points < 0.1)
                {
                    team1ScheduleInfo.WeeklyFutureExpectedPointsAgainst.Add(matchupData.Team2ExpPoints);
                    team2ScheduleInfo.WeeklyFutureExpectedPointsAgainst.Add(matchupData.Team1ExpPoints);
                    team1ScheduleInfo.WeeklyExpectedPointsFor.Add(matchupData.Team1ExpPoints);
                    team2ScheduleInfo.WeeklyExpectedPointsFor.Add(matchupData.Team2ExpPoints);
                }
                else//Game was played get stats
                {
                    team1ScheduleInfo.WeeklyPreviousExpectedPointsAgainst.Add(matchupData.Team2ExpPoints);
                    team2ScheduleInfo.WeeklyPreviousExpectedPointsAgainst.Add(matchupData.Team1ExpPoints);

                    team1ScheduleInfo.WeeklyPointsAgainst.Add(matchupData.Team2Points);
                    team2ScheduleInfo.WeeklyPointsAgainst.Add(matchupData.Team1Points);

                    team1ScheduleInfo.WeeklyOpponentPercentAbberation.Add((matchupData.Team2Points - matchupData.Team2ExpPoints) / matchupData.Team2ExpPoints);
                    team2ScheduleInfo.WeeklyOpponentPercentAbberation.Add((matchupData.Team1Points - matchupData.Team1ExpPoints) / matchupData.Team1ExpPoints);

                    team1ScheduleInfo.WeeklyPreviousPointsFor.Add(matchupData.Team1Points);
                    team2ScheduleInfo.WeeklyPreviousPointsFor.Add(matchupData.Team2Points);

                    if (matchupData.Team1Points >= matchupData.Team2Points)
                    {
                        team1ScheduleInfo.StrengthOfVictory.Add(matchupData.Team1Points - matchupData.Team2Points);
                        team2ScheduleInfo.StrengthOfLoss.Add(matchupData.Team1Points - matchupData.Team2Points);
                        team1ScheduleInfo.Wins++;
                        team2ScheduleInfo.Losses++;
                    }
                    else
                    {
                        team2ScheduleInfo.StrengthOfVictory.Add(matchupData.Team2Points - matchupData.Team1Points);
                        team1ScheduleInfo.StrengthOfLoss.Add(matchupData.Team2Points - matchupData.Team1Points);
                        team2ScheduleInfo.Wins++;
                        team1ScheduleInfo.Losses++;
                    }

                    team1ScheduleInfo.PointsAgainstPlayer.Add(matchupData.Team2Name, matchupData.Team1Points);
                    team2ScheduleInfo.PointsAgainstPlayer.Add(matchupData.Team1Name, matchupData.Team2Points);
                }
            }

            return remainingSchedulePoints;
        }

        //once all data has been compiled, simulate the season 20,000 times to get detailed odds for reaching the playoffs
        private static Dictionary<string, double> SimulateSeason(List<Matchup> seasonMatchupData, ref List<OwnerSimulationResults> playerSimulations)
        {
            Dictionary<string, int> teamMadePlayOffs = new Dictionary<string, int>();

            for (int i = 0; i < 20000; i++)
            {

                Console.WriteLine("Running season simulation {0}", i + 1);

                List<OwnerSeasonStanding> standings = new List<OwnerSeasonStanding>();

                foreach (Matchup matchupData in seasonMatchupData)
                {
                    if (!standings.Any(s => s.TeamName == matchupData.Team1Name))
                    {
                        standings.Add(new OwnerSeasonStanding(matchupData.Team1Name));
                    }

                    if (!standings.Any(s => s.TeamName == matchupData.Team2Name))
                    {
                        standings.Add(new OwnerSeasonStanding(matchupData.Team2Name));
                    }

                    OwnerSeasonStanding Team1Standing =
                        standings.FirstOrDefault(s => s.TeamName == matchupData.Team1Name);
                    OwnerSeasonStanding Team2Standing =
                        standings.FirstOrDefault(s => s.TeamName == matchupData.Team2Name);

                    if (matchupData.Team1Points < 0.1 && matchupData.Team2Points < 0.1)
                    {
                        double team1SimPoints = SimulatePoints(matchupData.Team1ExpPoints);
                        double team2SimPoints = SimulatePoints(matchupData.Team2ExpPoints);

                        if (team1SimPoints >= team2SimPoints)
                        {
                            Team1Standing.Wins++;
                            Team2Standing.Losses++;
                        }
                        else
                        {
                            Team2Standing.Wins++;
                            Team1Standing.Losses++;
                        }

                        Team1Standing.PointsFor += team1SimPoints;
                        Team1Standing.PointsAgainst += team2SimPoints;
                        Team2Standing.PointsFor += team2SimPoints;
                        Team2Standing.PointsAgainst += team1SimPoints;
                    }
                    else
                    {
                        if (matchupData.Team1Points >= matchupData.Team2Points)
                        {
                            Team1Standing.Wins++;
                            Team2Standing.Losses++;
                        }
                        else
                        {
                            Team2Standing.Wins++;
                            Team1Standing.Losses++;
                        }

                        Team1Standing.PointsFor += matchupData.Team1Points;
                        Team1Standing.PointsAgainst += matchupData.Team2Points;
                        Team2Standing.PointsFor += matchupData.Team2Points;
                        Team2Standing.PointsAgainst += matchupData.Team1Points;
                    }
                }

                //rank owners by their overall standings based first on wins, thens points for, then points against
                //this determines who makes the playoffs based on tiebreakers
                OwnerSeasonStanding[] standingsArr =
                    standings.OrderByDescending(s => s.Wins)
                        .ThenByDescending(s => s.PointsFor)
                        .ThenByDescending(s => s.PointsAgainst)
                        .ToArray();

                for (int j = 0; j < standingsArr.Count(); j++)
                {
                    standingsArr[j].ClinchedPlayoffs = j < 8;
                }


                foreach (OwnerSeasonStanding standing in standingsArr)
                {
                    if (!playerSimulations.Any(ps => ps.TeamName == standing.TeamName))
                    {
                        playerSimulations.Add(new OwnerSimulationResults(standing.TeamName));
                    }

                    OwnerSimulationResults ownerSim = playerSimulations.FirstOrDefault(ps => ps.TeamName == standing.TeamName);

                    ownerSim.Wins.Add(standing.Wins);
                    ownerSim.Losses.Add(standing.Losses);

                    if (!teamMadePlayOffs.ContainsKey(standing.TeamName))
                    {
                        teamMadePlayOffs.Add(standing.TeamName, 0);
                    }

                    if (standing.ClinchedPlayoffs)
                    {
                        teamMadePlayOffs[standing.TeamName]++;
                    }
                }
            }

            Dictionary<string, double> finalPlayoffOdds = new Dictionary<string, double>();

            foreach (KeyValuePair<string, int> teamPlayoffCount in teamMadePlayOffs)
            {
                finalPlayoffOdds.Add(teamPlayoffCount.Key, teamPlayoffCount.Value / 20000.0);
            }

            return finalPlayoffOdds;

        }
    
        //get the data for each matchup within the season based on current rosters
        private static List<Matchup> GetSeasonsData(List<FantasyRoster> currentRosters)
        {
            StreamWriter sw = new StreamWriter(dataFilePath + "ThisSeasonMatchupScores" + DateTime.Now.ToString("MM-dd-yy.hh.mm.ss") + ".Tab", false);
            List<string> weeks = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13" };

            try
            {
                Dictionary<string, List<string>> seasonIds = GetAllSeasonIds();

                //write header line
                sw.WriteLine("Week\tWeekStart\tWeekEnd\tIsPlayoffs\tTeam1\tTeam2\tTeam1Points\tTeam2Points\tTeam1ExpPoints\tTeam2ExpPoints");

                string currentSeasonYear = seasonIds.Keys.OrderByDescending(y => y).FirstOrDefault();
                string currentSeasonId = seasonIds[currentSeasonYear][1];
                string currentLeagueId = seasonIds[currentSeasonYear][0];

                List<Matchup> finalMatchupData = new List<Matchup>();

                foreach (string week in weeks)
                {
                    string scoreBoardQuery = "league/" + currentLeagueId + ".l." + currentSeasonId + "/scoreboard;week=" + week;

                    IConsumerRequest responseRequest = session.Request().Get().ForUrl(ApiUrl + scoreBoardQuery);

                    List<Matchup> weeklyMatchupData = new List<Matchup>();
                    WriteOutScoreboardData(responseRequest, sw, week, ref weeklyMatchupData, currentRosters);

                    finalMatchupData.AddRange(weeklyMatchupData);
                }

                return finalMatchupData;

            }
            catch (Exception e)
            {
                LogError(e);
            }

            sw.Close();

            return new List<Matchup>();
        }

        //private method to write out the complete projected scores for each matchup
        //primarily used to check data quality
        private static void WriteOutScoreboardData(IConsumerRequest responseRequest, StreamWriter sw, string week, ref List<Matchup> matchupData, List<FantasyRoster> currentRosters)
        {
            var resultXml = XElement.Parse(responseRequest.ToString());

            var leagueNode = resultXml.Elements().FirstOrDefault();

            foreach (var node in leagueNode.Elements())
            {
                if (GetNodeName(node) == "scoreboard")
                {
                    foreach (var scoreboardNode in node.Elements())
                    {
                        if (GetNodeName(scoreboardNode) == "matchups")
                        {
                            foreach (var matchupNodes in scoreboardNode.Elements())
                            {
                                string weekStart = "";
                                string weekEnd = "";
                                string isPlayoffs = "";
                                string team1 = "";
                                string team2 = "";
                                string team1Points = "";
                                string team2Points = "";
                                string team1ExpPoints = "";
                                string team2ExpPoints = "";

                                int teamCounter = 0;

                                foreach (var matchupNode in matchupNodes.Elements())
                                {
                                    switch (GetNodeName(matchupNode))
                                    {
                                        case "week_start":
                                            weekStart = matchupNode.Value;
                                            break;
                                        case "week_end":
                                            weekEnd = matchupNode.Value;
                                            break;
                                        case "is_playoffs":
                                            isPlayoffs = matchupNode.Value;
                                            break;
                                        case "teams":
                                        {
                                            foreach (var teamsNode in matchupNode.Elements())
                                            {
                                                teamCounter++;
                                                foreach (var teamNode in teamsNode.Elements())
                                                {
                                                    switch (GetNodeName(teamNode))
                                                    {
                                                        case "name":
                                                        {
                                                            if (teamCounter == 1)
                                                            {
                                                                team1 = teamNode.Value;
                                                            }
                                                            else
                                                            {
                                                                team2 = teamNode.Value;
                                                            }
                                                            break;
                                                        }
                                                        case "team_points":
                                                        {
                                                            foreach (var pointsNode in teamNode.Elements())
                                                            {
                                                                if (GetNodeName(pointsNode) == "total")
                                                                {
                                                                    if (teamCounter == 1)
                                                                    {
                                                                        team1Points = pointsNode.Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        team2Points = pointsNode.Value;
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        }
                                                        case "team_projected_points":
                                                        {
                                                            foreach (var pointsNode in teamNode.Elements())
                                                            {
                                                                if (GetNodeName(pointsNode) == "total")
                                                                {
                                                                    if (teamCounter == 1)
                                                                    {
                                                                        team1ExpPoints = pointsNode.Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        team2ExpPoints = pointsNode.Value;
                                                                    }
                                                                }
                                                            }

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                                int intWeek = Int32.Parse(week);
                                double team1ExpPointsDouble = 0;
                                double team2ExpPointsDouble = 0;

                                if (double.Parse(team1Points) > 0 || double.Parse(team2Points) > 0)
                                {
                                    team1ExpPointsDouble = ConvertToDouble(team1ExpPoints);
                                    team2ExpPointsDouble = ConvertToDouble(team2ExpPoints);
                                }
                                else
                                {
                                    team1ExpPointsDouble = GetProjectedPoints(team1, currentRosters, intWeek);
                                    team2ExpPointsDouble = GetProjectedPoints(team2, currentRosters, intWeek);
                                }

                                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}", week, weekStart, weekEnd,
                                    isPlayoffs,
                                    team1, team2, team1Points, team2Points, team1ExpPointsDouble, team2ExpPointsDouble);
                                sw.Flush();
                                matchupData.Add(new Matchup(team1, team2, team1ExpPointsDouble, team2ExpPointsDouble, ConvertToDouble(team1Points), ConvertToDouble(team2Points), intWeek));
                            }
                        }
                    }
                }
            }
        }

        //calculate the projected points for a roster in any given week
        //do so by assembling the highest scoring legal roster possible
        //for rosters that do not have a complete set of legal players at all positions (eg a player is on bye in a given week, and the owner currently doesn't have a backup rostered)
        //insert a generic average number of points based on typical scores of replacement players at any position
        private static double GetProjectedPoints(string teamName, List<FantasyRoster> currentRosters, int week)
        {
            double totalProjectedPoints = 0;
            int qbsAdded = 0;
            int wrsAdded = 0;
            int rbsAdded = 0;
            int tesAdded = 0;
            int flexAdded = 0;
            int kickersAdded = 0;
            int defenseAdded = 0;

            FantasyRoster currentPlayerRoster = currentRosters.FirstOrDefault(fr => fr.TeamName.ToLower() == teamName.ToLower());

            List<WeeklyRosterPoints> teamPoints = new List<WeeklyRosterPoints>();

            foreach (FootballPlayer player in currentPlayerRoster.CurrentRoster)
            {
                if (player.ByeWeek != week)
                {
                    double projectedPoints = 0.0;

                    switch (week)
                    {
                        case 1:
                        {
                            projectedPoints = player.Week1ProjPoints;
                            break;
                        }
                        case 2:
                        {
                            projectedPoints = player.Week2ProjPoints;
                            break;
                        }
                        case 3:
                        {
                            projectedPoints = player.Week3ProjPoints;
                            break;
                        }
                        case 4:
                        {
                            projectedPoints = player.Week4ProjPoints;
                            break;
                        }
                        case 5:
                        {
                            projectedPoints = player.Week5ProjPoints;
                            break;
                        }
                        case 6:
                        {
                            projectedPoints = player.Week6ProjPoints;
                            break;
                        }
                        case 7:
                        {
                            projectedPoints = player.Week7ProjPoints;
                            break;
                        }
                        case 8:
                        {
                            projectedPoints = player.Week8ProjPoints;
                            break;
                        }
                        case 9:
                        {
                            projectedPoints = player.Week9ProjPoints;
                            break;
                        }
                        case 10:
                        {
                            projectedPoints = player.Week10ProjPoints;
                            break;
                        }
                        case 11:
                        {
                            projectedPoints = player.Week11ProjPoints;
                            break;
                        }
                        case 12:
                        {
                            projectedPoints = player.Week12ProjPoints;
                            break;
                        }
                        case 13:
                        {
                            projectedPoints = player.Week13ProjPoints;
                            break;
                        }
                    }

                    //in case projected points are zero do not use, use replacement values
                    if (projectedPoints != 0.0)
                    {
                        WeeklyRosterPoints r = new WeeklyRosterPoints(player.PlayerPositions, projectedPoints);
                        teamPoints.Add(r);
                    }
                }
            }

            List<WeeklyRosterPoints> sortedPoints = teamPoints.OrderByDescending(w => w.ProjectedPoints).ToList();

            foreach (WeeklyRosterPoints p in sortedPoints)
            {
                bool playerUsed = false;

                foreach (string position in p.Positions)
                {
                    //do not attribute same player's points twice
                    if (playerUsed)
                    {
                        break;
                    }

                    switch (position)
                    {
                        case "QB":
                        {
                            if (qbsAdded < 1)
                            {
                                qbsAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                        case "WR":
                        {
                            if (wrsAdded < 2)
                            {
                                wrsAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            else if (flexAdded < 1)
                            {
                                flexAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                        case "RB":
                        {
                            if (rbsAdded < 1)
                            {
                                rbsAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            else if (flexAdded < 1)
                            {
                                flexAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                        case "TE":
                        {
                            if (tesAdded < 1)
                            {
                                tesAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                        case "K":
                        {
                            if (kickersAdded < 1)
                            {
                                kickersAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                        case "DEF":
                        {
                            if (defenseAdded < 1)
                            {
                                defenseAdded++;
                                playerUsed = true;
                                totalProjectedPoints += p.ProjectedPoints;
                            }
                            break;
                        }
                    }
                }
            }

            if (qbsAdded != 1)
            {
                totalProjectedPoints += QbReplacementPoints;
            }
            if (wrsAdded != 2)
            {
                totalProjectedPoints += (2 - wrsAdded)* WrReplacementPoints;
            }
            if (rbsAdded != 1)
            {
                totalProjectedPoints += RbReplacementPoints;
            }
            if (flexAdded != 1)
            {
                totalProjectedPoints += RbReplacementPoints;
            }
            if (tesAdded != 1)
            {
                totalProjectedPoints += TeReplacementPoints;
            }
            if (kickersAdded != 1)
            {
                totalProjectedPoints += KickerReplacementPoints;
            }
            if (defenseAdded != 1)
            {
                totalProjectedPoints += DefenseReplacementPoints;
            }

            return totalProjectedPoints;
        }
        
        #region Helper Functions

        //copies scores from one FootballPlayer object to another
        private static void CopyScoreProjections(FootballPlayer player1, FootballPlayer player2)
        {
            player1.Week1ProjPoints = player2.Week1ProjPoints;
            player1.Week2ProjPoints = player2.Week2ProjPoints;
            player1.Week3ProjPoints = player2.Week3ProjPoints;
            player1.Week4ProjPoints = player2.Week4ProjPoints;
            player1.Week5ProjPoints = player2.Week5ProjPoints;
            player1.Week6ProjPoints = player2.Week6ProjPoints;
            player1.Week7ProjPoints = player2.Week7ProjPoints;
            player1.Week8ProjPoints = player2.Week8ProjPoints;
            player1.Week9ProjPoints = player2.Week9ProjPoints;
            player1.Week10ProjPoints = player2.Week10ProjPoints;
            player1.Week11ProjPoints = player2.Week11ProjPoints;
            player1.Week12ProjPoints = player2.Week12ProjPoints;
            player1.Week13ProjPoints = player2.Week13ProjPoints;
        }

        private static double GetMedian(List<double> list)
        {
            if (list.Count == 0)
            {
                return 0.0;
            }

            double[] orderedList = list.OrderBy(d => d).ToArray();

            int medianIndex = orderedList.Length / 2;
            double median = (orderedList.Length % 2 != 0) ? orderedList[medianIndex] : (orderedList[medianIndex] + orderedList[medianIndex - 1]) / 2;
            return median;
        }

        private static double ConvertToDouble(string value)
        {
            if (!parsedScoreValues.ContainsKey(value))
            {
                double parsedValue = double.Parse(value);
                parsedScoreValues.Add(value, parsedValue);
                return parsedValue;
            }
            else
            {
                return parsedScoreValues[value];
            }
        }    

        //user must manually log in and then enter verifier code into console to continue
        //to be used if automated verfication ever fails
        private static void ManualVerifierEntry(OAuthSession session, IToken requestToken)
        {
            Console.WriteLine("Enter the code provided by yahoo and hit enter:");
            string nextInput = Console.ReadLine();

            IToken accessToken = session.ExchangeRequestTokenForAccessToken(requestToken, nextInput);
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

        public static string ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds).ToString();
        }

        private static Random _Random = new Random();
        private static string _UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        public const string HMACSHA1 = "HMAC-SHA1";

        private static string GetRandomString(int length)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < length; i++) result.Append(_UnreservedChars[_Random.Next(0, 25)]);
            return result.ToString();
        }

        //returns a cleaned string of the name of the passed XML node
        //original format varies within the API results
        private static string GetNodeName(XElement node)
        {
            string longName = node.Name.ToString();
            string resultName = "";

            if (longName.Contains('}'))
            {
                string[] longNameArr = longName.Split('}');

                if (longNameArr.Count() >= 2)
                {
                    resultName = longNameArr[1];
                }
            }

            return resultName;
        }

        private static void LogError(Exception e)
        {
            errorLogger.WriteLine("Error at:" + DateTime.Now.ToString()
                    + "Error:\r\n" + e.Message + "\r\n" + "Stack trace: \r\n" + e.StackTrace);
        }

        //automatically logs in and scrapes the verifier code from yahoo
        public static void AutomatedVerfierEntry(OAuthSession session, IToken requestToken, IWebDriver driver)
        {
            //wait for login page to load
            Thread.Sleep(5000);

            //enter passwords and hit login
            IWebElement userName = driver.FindElement(By.Id("login-username"));
            userName.SendKeys("username");
            driver.FindElement(By.Id("login-signin")).Click();
            //wait for password entry field
            Thread.Sleep(1000);
            IWebElement password = driver.FindElement(By.Id("login-passwd"));
            password.SendKeys("password");
            // Now submit the form. WebDriver will find the form for us from the element
            driver.FindElement(By.Id("login-signin")).Click();

            //wait for verifier page to load
            Thread.Sleep(3000);

            //hit agree button to show verfier code
            driver.FindElement(By.Id("xagree")).Click();


            //wait for verifier page to load
            Thread.Sleep(2000);

            //get verifier code elemnet
            IWebElement verifierCodeElement = driver.FindElement(By.Id("shortCode"));

            string verifierCode = verifierCodeElement.Text;

            //provide access token to session
            IToken accessToken = session.ExchangeRequestTokenForAccessToken(requestToken, verifierCode);
        }

        //generate a value for actual poitns scored for a team based on projected points, previously generated model, and random error
        private static double SimulatePoints(double projectedPoints)
        {
            double m = 0.8456;
            double b = 17.099;
            double error = GetRandomError();

            variationsUsed.Add(error);

            return ((m * projectedPoints) + b) + error;

        }

        //returns a number of points to be added or subtracted from a team's weekly projected points, based on historical variance data
        private static double GetRandomError()
        {
            double modelStdDev = 21.94;

            double u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
            double u2 = rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return modelStdDev * randStdNormal; //random normal(mean,stdDev^2)

        }

        //helper method that parses authentication information returned from Yahoo
        private static Dictionary<string, string> GetResponseDictionary(string htmlResult)
        {
            Dictionary<string, string> returnedParamsAndValues = new Dictionary<string, string>();
            string[] returnedUnits = htmlResult.Split('&');

            foreach (string s in returnedUnits)
            {
                string[] paramAndValue = s.Split('=');
                returnedParamsAndValues.Add(paramAndValue[0], paramAndValue[1]);
            }

            return returnedParamsAndValues;
        }

        //Yahoo requires seasonIDs for each new season
        //This function retries those IDs and writes them to file for later use in API
        private static void GetOldSeasonIds()
        {
            StreamWriter sw = new StreamWriter(@"Data\AllSeasonIds.Tab", false);

            try
            {

                //example query
                string gamesQuery =
                    "users;use_login=1/games;game_codes=nfl;seasons=2007,2008,2009,2010,2011,2012,2013,2014,2015,2016";

                IConsumerRequest responseRequest = session.Request().Get().ForUrl(ApiUrl + gamesQuery);

                var resultXml = XElement.Parse(responseRequest.ToString());

                sw.WriteLine("Season Year\tSeason Key");

                var usersNode = resultXml.Elements().FirstOrDefault();
                var userNode = usersNode.Elements().FirstOrDefault();

                foreach (var node in userNode.Elements())
                {
                    if (GetNodeName(node) == "games")
                    {
                        string seasonName = "";
                        string gameKey = "";
                        foreach (var gameNode in node.Elements())
                        {
                            foreach (var gameDataNode in gameNode.Elements())
                            {
                                if (GetNodeName(gameDataNode) == "game_key")
                                {
                                    gameKey = gameDataNode.Value;
                                }
                                else if (GetNodeName(gameDataNode) == "season")
                                {
                                    seasonName = gameDataNode.Value;
                                }
                            }

                            sw.WriteLine("{0}\t{1}", seasonName,
                                gameKey);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }

            sw.Close();
        }

        #endregion // Helper Functions
    }
}