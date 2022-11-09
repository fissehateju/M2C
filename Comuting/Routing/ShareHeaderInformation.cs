using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.computing;
using RR.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RR.Comuting.Routing
{
    class ShareHeaderInformation
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        private Queue<Packet> queue = new Queue<Packet>();
        public ShareHeaderInformation(Sensor source, Sensor dest, string action)
        {
            counter = new NetworkOverheadCounter();
            Packet packt;

            switch (action)
            {
                case "HeaderInfo":
                    packt = GeneragtePacket(source);
                    DetermineDestination(source, packt);
                    while (queue.Count > 0)
                    {
                        SendPacket(source, queue.Dequeue());
                    }
                    break;
                case "AssigningAsHeader":
                    packt = GeneragtePacket(source, dest);
                    packt.PacketType = PacketType.AssigningAsHeader;
                    SendPacket(source, packt);
                    break;
                case "ReportBatteryLevel":
                    packt = GeneragtePacket(source, dest);
                    packt.PacketType = PacketType.ReportBatteryLevel;
                    packt.remainingEnergy_Joule = source.ResidualEnergy;
                    SendPacket(source, packt);
                    break;
            }
        }

        public ShareHeaderInformation()
        {
            counter = new NetworkOverheadCounter();
        }

        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }

        private Packet GeneragtePacket(Sensor sender)
        {
            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = sender;
            pck.Path = "" + sender.ID;
            pck.PacketType = PacketType.HeaderInfo;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            counter.DisplayRefreshAtGenertingPacket(pck.Source, PacketType.HeaderInfo);
            return pck;
        }

        private Packet GeneragtePacket(Sensor sender, Sensor dest)
        {

            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = sender;
            pck.Path = "" + sender.ID;
            pck.Destination = dest;           
            pck.PID = PublicParamerters.NumberofGeneratedPackets;            
            pck.Hops = 0;
            pck.ReTransmissionTry = 0;
            double DIS = Operations.DistanceBetweenTwoPoints(pck.Source.CenterLocation, pck.Destination.CenterLocation);
            pck.TimeToLive = 3 + Convert.ToInt16(DIS / PublicParamerters.CommunicationRangeRadius);
            counter.DisplayRefreshAtGenertingPacket(pck.Source, PacketType.HeaderInfo);
            return pck;
        }

        private void DetermineDestination(Sensor sender, Packet Pack)
        {          

            foreach(Sensor dest in sender.myCluster.MemberNodes)
            {
                if (dest.isHeader) continue;

                Packet newpack = Pack.Clone() as Packet;
                newpack.Destination = dest;
                newpack.Hops = 0;
                newpack.ReTransmissionTry = 0;
                double DIS = Operations.DistanceBetweenTwoPoints(newpack.Source.CenterLocation, newpack.Destination.CenterLocation);
                newpack.TimeToLive = 3 + Convert.ToInt16(DIS / PublicParamerters.CommunicationRangeRadius);

                queue.Enqueue(newpack);
            }
        }
        private void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.isDelivered)
            {
                Console.WriteLine(pck.PacketType + " delivered.");
                return;
            }

            if (pck.Destination.ID != sender.ID)
            {
                Sensor Reciver = SelectNextHop(sender, pck);

                if (Operations.DistanceBetweenTwoSensors(sender, pck.Destination) <= PublicParamerters.CommunicationRangeRadius)
                {
                    Reciver = pck.Destination;
                }

                if (Reciver != null)
                {
                    sender.SwichToActive(); // switch on me.
                    //counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                    counter.Animate(sender, Reciver, pck);
                    RecivePacket(Reciver, pck);
                }
                else
                {
                    // wait:
                    counter.SaveToQueue(sender, pck);
                }
            }
            else
            {
                //Drop the packet something went wrong
                counter.DropPacket(pck, sender, PacketDropedReasons.RingNodesError);
            }
        }

        private void RecivePacket(Sensor Reciver, Packet packt)
        {
            packt.Path += ">" + Reciver.ID;
            if (loopMechan.isLoop(packt))
            {
                counter.DropPacket(packt, Reciver, PacketDropedReasons.Loop);
            }
            else
            {
                packt.ReTransmissionTry = 0;
                if (Reciver == packt.Destination)
                {
                    packt.isDelivered = true;
                    //counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    counter.DisplayRefreshAtReceivingPacket(packt.Source);

                    Reciver.HandOffToTheSensorForEvaluation(packt);

                    if (Settings.Default.SavePackets)
                        PublicParamerters.FinishedRoutedPackets.Add(packt);
                    else
                        packt.Dispose();
                   
                }
                else
                {
                    if (packt.Hops <= packt.TimeToLive)
                    {
                        //counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                        SendPacket(Reciver, packt);
                    }
                    else
                    {
                        counter.DropPacket(packt, Reciver, PacketDropedReasons.TimeToLive);
                    }
                }
            }

        }


        private Sensor SelectNextHop(Sensor ni, Packet packet)
        {
            Point endPoint = (packet.Destination != null) ? packet.Destination.CenterLocation : packet.DestinationPoint;

            List<CoordinationEntry> coordinationEntries = new List<CoordinationEntry>();
            Sensor sj = null;
            double sum = 0;
            foreach (Sensor nj in ni.NeighborsTable)
            {
                double dj = Operations.DistanceBetweenTwoPoints(nj.CenterLocation, endPoint);
                double aggregatedValue = dj;
                coordinationEntries.Add(new CoordinationEntry() { Priority = aggregatedValue, Sensor = nj }); // candidaite
                sum += aggregatedValue;
            }
            // coordination"..... here
            sj = counter.CoordinateGetMin(coordinationEntries, packet, sum);

            return sj; ;
        }

    }
}
