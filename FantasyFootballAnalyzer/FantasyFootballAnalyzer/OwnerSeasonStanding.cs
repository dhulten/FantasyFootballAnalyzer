using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //tracks an owner's wins, losses, points for and against for a season
    public class OwnerSeasonStanding
    {
        public string TeamName { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double PointsFor { get; set; }
        public double PointsAgainst { get; set; }
        public bool ClinchedPlayoffs { get; set; }
       

        public OwnerSeasonStanding(string teamName)
        {
            TeamName = teamName;
            Wins = 0;
            Losses = 0;
            PointsAgainst = 0.0;
            PointsFor = 0.0;
            ClinchedPlayoffs = false;
        }
    }
}
