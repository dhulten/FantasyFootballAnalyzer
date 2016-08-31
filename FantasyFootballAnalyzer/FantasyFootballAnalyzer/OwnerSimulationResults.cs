using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class to track the wins and loses for an owner within a given season simulation
    public class OwnerSimulationResults
    {
        public List<int> Wins { get; set; } 
        public List<int> Losses { get; set; } 
        public string TeamName { get; set; }

        public OwnerSimulationResults(string teamName)
        {
            Wins = new List<int>();
            Losses = new List<int>();
            TeamName = teamName;
        }
    }
}
