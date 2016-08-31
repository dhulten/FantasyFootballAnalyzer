using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class for each weekly matchup, describing a head to head matchup between two different teams
    //contains team names, expected points, actual points, and week of matchup
    public class Matchup
    {
        public string Team1Name { get; set; }
        public string Team2Name { get; set; }
        public double Team1ExpPoints { get; set; }
        public double Team2ExpPoints { get; set; }
        public double Team1Points { get; set; }
        public double Team2Points { get; set; }
        public int Week { get; set; }

        public Matchup(string team1Name, string team2Name, double team1ExpPoints, double team2ExpPoints, double team1Points, double team2Points, int week)
        {
            Team1Name = team1Name;
            Team2Name = team2Name;
            Team1ExpPoints = team1ExpPoints;
            Team2ExpPoints = team2ExpPoints;
            Team1Points = team1Points;
            Team2Points = team2Points;
            Week = week;
        }
    }
}
