using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class which associates a team name with a list of FootballPlayers currently on that team
    public class FantasyRoster
    {
        public string TeamName { get; set; }
        public List<FootballPlayer> CurrentRoster { get; set; }

        public FantasyRoster(string teamName)
        {
            TeamName = teamName;
            CurrentRoster = new List<FootballPlayer>();
        }
    }
}
