using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Energy;
using RR.Intilization;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace RR.Models.Charging
{
    internal class StartTSM
    {
        public List<Location> SensLocat = new List<Location>();
        public List<int> orderedID = new List<int>();
        public List<int> Startit(List<Sensor> heads)
        {
            foreach (Sensor sen in heads)
            {
                SensLocat.Add(new Location(sen.ID, sen.CenterLocation));
            }

            var problem = new TravellingSalesmanAlg(SensLocat); 
           
            var route = problem.Solve();
            foreach (Location loc in route.Locations)
            {
                orderedID.Add(loc.ID);
            }
            return orderedID;
        }
    }
}
