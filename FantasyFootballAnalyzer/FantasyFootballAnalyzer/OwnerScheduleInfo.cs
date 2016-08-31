using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class containing information to describe a fantasy owner's season
    //includes breakdowns of weekly points for and against, but past and present
    //as well as stats such as strength of victory and strength of loss
    public class OwnerScheduleInfo
    {
        public List<double> WeeklyPointsAgainst { get; set; } 
        public List<double> WeeklyPreviousExpectedPointsAgainst { get; set; } 
        public List<double> WeeklyOpponentPercentAbberation { get; set; } 
        public List<double> WeeklyFutureExpectedPointsAgainst { get; set; }
        public List<double> WeeklyExpectedPointsFor { get; set; } 
        public List<double> StrengthOfVictory { get; set; }
        public List<double> StrengthOfLoss { get; set; } 
        public List<double> WeeklyPreviousPointsFor { get; set; } 
        public Dictionary<string, double> PointsAgainstPlayer { get; set; } 
        public int Wins { get; set; }
        public int Losses { get; set; }

        public OwnerScheduleInfo()
        {
            this.WeeklyFutureExpectedPointsAgainst = new List<double>();
            this.WeeklyOpponentPercentAbberation = new List<double>();
            this.WeeklyPointsAgainst = new List<double>();
            this.WeeklyPreviousExpectedPointsAgainst = new List<double>();
            this.StrengthOfLoss = new List<double>();
            this.StrengthOfVictory = new List<double>();
            this.PointsAgainstPlayer = new Dictionary<string, double>();
            this.WeeklyExpectedPointsFor = new List<double>();
            this.WeeklyPreviousPointsFor = new List<double>();
            Wins = 0;
            Losses = 0;
        }
    }
}
