using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class to contain information regarding a given player, including name, id, position and projected points by week
    public class FootballPlayer
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public List<string> PlayerPositions { get; set; }
        public bool IsStarter { get; set; }
        public int ByeWeek { get; set; }
        public int DraftRound { get; set; }

        public double Week1ProjPoints { get; set; }
        public double Week2ProjPoints { get; set; }
        public double Week3ProjPoints { get; set; }
        public double Week4ProjPoints { get; set; }
        public double Week5ProjPoints { get; set; }
        public double Week6ProjPoints { get; set; }
        public double Week7ProjPoints { get; set; }
        public double Week8ProjPoints { get; set; }
        public double Week9ProjPoints { get; set; }
        public double Week10ProjPoints { get; set; }
        public double Week11ProjPoints { get; set; }
        public double Week12ProjPoints { get; set; }
        public double Week13ProjPoints { get; set; }

        //used for weekly scoreboard
        public double CurrentWeekPoints { get; set; }

        public FootballPlayer()
        {
            PlayerPositions = new List<string>();
        }
    }
}
