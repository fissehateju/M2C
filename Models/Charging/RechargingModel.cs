using RR.Computations;
using RR.Dataplane;
using RR.Intilization;
using RR.Comuting.Routing;
using RR.Models.Mobility;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using RR.Dataplane.NOS;
using RR.Comuting.computing;

namespace RR.Models.Charging
{
    internal class RechargingModel
    {
        private DispatcherTimer timer_Recharging = new DispatcherTimer(); // Making this static will restric the values inside the fuction called by Tick
        private Sensor tobeCharged;
        public Sink charger;
        private NetworkOverheadCounter counter;
        public RechargingModel(Sink charg, Sensor sen)
        {
            charger = charg;
            tobeCharged = sen;
            counter = new NetworkOverheadCounter();
        }

        double Rechargingtime { get; set; }
        double ChargeTimecount { get; set; }
        public void startRecharing()
        {

            double senConsummedE = PublicParamerters.BatteryIntialEnergy - tobeCharged.ResidualEnergy;
            Rechargingtime = 1 + (int)Math.Ceiling(senConsummedE / PublicParamerters.chargingRate);
            ChargeTimecount = 0;
            PublicParamerters.TotalTransferredEnergy += senConsummedE;          

            timer_Recharging.Interval = TimeSpan.FromSeconds(1);
            timer_Recharging.Start();
            timer_Recharging.Tick += Recharging;
        }
        private void Recharging(Object sender, EventArgs e)
        {

            ChargeTimecount++;
                     
            if (ChargeTimecount == Rechargingtime)
            {
                tobeCharged.ResidualEnergy = PublicParamerters.BatteryIntialEnergy;
                PublicParamerters.TotalNumChargedSensors += 1;
                timer_Recharging.Stop();

                PublicParamerters.requestedList.Remove(tobeCharged);
                charger.visitedNode.Add(tobeCharged);

                if (!tobeCharged.ChargingRequestInitiate.Equals(PublicParamerters.defualtDateValue))  // != "1/1/0001 12:00:00 AM"
                {
                    var delay = DateTime.Now - tobeCharged.ChargingRequestInitiate;
                    PublicParamerters.ChargingDelayInSecond += delay.TotalSeconds;
                    tobeCharged.ChargingRequestInitiate = PublicParamerters.defualtDateValue;
                }

                Packet req = charger.getClosestNode(charger.CenterLocation, charger.ChargingReqs);

                if (req == null)
                {
                    charger.SetMobilityforDatacollection();
                }
                else
                {
                    charger.SetMobilityforCharging(req);
                }
            }
            else if (ChargeTimecount > Rechargingtime)
            {
                //System.Console.WriteLine("Still recharging sensor {0}", tobeCharged.Source.ID);

                PublicParamerters.requestedList.Remove(tobeCharged);
                ChargeTimecount = Rechargingtime;
            }
        }       

    }
}
