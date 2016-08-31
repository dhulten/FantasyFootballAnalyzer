using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class that contains the projected points of players during a given week
    public class WeeklyScoreBoard
    {
        public int Week { get; set; }
        public List<FootballPlayer> WeeklyPlayers { get; set; }

        public WeeklyScoreBoard(int week)
        {
            WeeklyPlayers = new List<FootballPlayer>();
            Week = week;
        }
    }
}
