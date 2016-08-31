using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyFootballAnalyzer
{
    //class that tracks the projected points for a given owner's roster in any given week
    //uses a list of positions on the roster to help determine which positions might be missing from
    //naively adding up the most projected points in a legal roster formation for an owner
    public class WeeklyRosterPoints
    {
        public List<string> Positions { get; set; }
        public double ProjectedPoints { get; set; }

        public WeeklyRosterPoints(List<string> positions, double projectedPoints )
        {
            Positions = positions;
            ProjectedPoints = projectedPoints;
        }
    }
}
