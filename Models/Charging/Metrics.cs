using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Models.Charging
{
    public class Metrics
    {
        public double remainEnergy;
        public double TrasmitRate;
        public double distFromMC;
        public double distFromAvgLocation;
        
        public Metrics(double remainEnergy, double trasmitRate, double distFromMC, double distFromAvgLocation)
        {
            this.remainEnergy = remainEnergy;
            this.TrasmitRate = trasmitRate;
            this.distFromMC = distFromMC;
            this.distFromAvgLocation = distFromAvgLocation;
        }

        public static double EnergyDistribution(double CurentEn, double intialEnergy)
        {
            if (CurentEn > 0)
            {
                double σ = CurentEn / intialEnergy;
                double γ = 1.0069, ε = 0.70848, ϑ = 17.80843;
                double re = γ / (1 + Math.Exp(-ϑ * (σ - ε)));
                return re;
            }
            else
                return 0;
        }

    }
}
