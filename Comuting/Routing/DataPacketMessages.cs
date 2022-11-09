using MP.MergedPath.Routing;
using RR.Dataplane;
using RR.Dataplane.NOS;
using RR.Intilization;
using RR.Comuting.computing;
using System;
using System.Collections.Generic;

namespace RR.Comuting.Routing
{
    

    class DataPacketMessages
    {
        private LoopMechanizimAvoidance loopMechan = new LoopMechanizimAvoidance();
        private NetworkOverheadCounter counter;
        /// <summary>
        /// currentBifSensor: current node that has the packet.
        /// Branches: the branches 
        /// isSourceAnAgent: the source is an agent for a sink. That is to say no need for clustering the source itslef this time.
        /// </summary>
        /// <param name="currentBifSensor"></param>
        /// <param name="Branches"></param>
        /// <param name="packet"></param>
        /// <param name="isSourceAnAgent"></param>
        public DataPacketMessages(Sensor sender, Packet packet)
        {
            counter = new NetworkOverheadCounter();

            // the source node creates new
            //if (packet.PacketType == PacketType.ResponseSinkPosition) //  the packet arrived to the source node. it was response. now we will generate of duplicate the packet.
            //{
            //    // create new:
            //    foreach (SinksAgentsRow row in packet.SinksAgentsList)
            //    {
            //        if (sender.ID == row.AgentNode.ID)
            //        {
            //            //skip the test here and send to the known sink by urself
            //            //Hand of to the sink by urself 
            //            Packet pkt = GeneragtePacket(sender, row); //                                                         
            //            pkt.SinksAgentsList = packet.SinksAgentsList;
            //            HandOffToTheSinkOrRecovry(sender, pkt);

            //        }
            //        else
            //        {
            //            double DsToMS = Operations.DistanceBetweenTwoSensors(sender, row.AgentNode);
            //            double DsToBs = Operations.DistanceBetweenTwoSensors(sender, PublicParamerters.MainWindow.myNetWork[0]);

            //            if(DsToMS > DsToBs + PublicParamerters.CommunicationRangeRadius)
            //            {
            //                row.AgentNode = PublicParamerters.MainWindow.myNetWork[0];
            //                Packet pck = GeneragtePacket(sender, row); // duplicate.                                                            
            //                pck.SinksAgentsList = null;
            //                SendPacket(sender, pck);
            //            }
            //            else
            //            {
            //                Packet pck = GeneragtePacket(sender, row); // duplicate.                                                            
            //                pck.SinksAgentsList = packet.SinksAgentsList;
            //                SendPacket(sender, pck);
            //            }
                       
            //        }

            //    }
            //}
            //else if (packet.PacketType == PacketType.Data)
            //{
            //    // recovery packets:
            //    Packet dupPck = Duplicate(packet, sender);
            //    SendPacket(sender, dupPck);
            //}   
    }

        public DataPacketMessages()
        {
            counter = new NetworkOverheadCounter();
        }
        public DataPacketMessages(Sensor sender)
        {
            counter = new NetworkOverheadCounter();
            
            if (sender.clusterHeader != null)
            {
                Packet pck = GeneragtePacket(sender);
                SendPacket(sender, pck);
            }                                                                  
            
        }

        public void HandelInQueuPacket(Sensor currentNode, Packet InQuepacket)
        {
            SendPacket(currentNode, InQuepacket);
        }
      

        private Packet GeneragtePacket(Sensor sender)
        {
            //Should not enter here if its an agent
            PublicParamerters.NumberofGeneratedPackets += 1;
            Packet pck = new Packet();
            pck.Source = sender;
            pck.Path = "" + sender.ID;
            pck.Destination = sender.clusterHeader; 
            pck.PacketType = PacketType.Data;
            pck.PID = PublicParamerters.NumberofGeneratedPackets;
            pck.TimeToLive = Convert.ToInt16((Operations.DistanceBetweenTwoPoints(sender.CenterLocation, pck.Destination.CenterLocation) / (PublicParamerters.CommunicationRangeRadius / 3)));
            pck.TimeToLive += PublicParamerters.HopsErrorRange;
            pck.generateTime = DateTime.Now;
            counter.DisplayRefreshAtGenertingPacket(sender, PacketType.Data);
            return pck;
        }

        public void SendPacket(Sensor sender, Packet pck)
        {
            if (pck.PacketType == PacketType.Data)
            {
                // neext hope:
                sender.SwichToActive(); // switch on me.
                Sensor Reciver = SelectNextHop(sender, pck);
                if (Reciver != null)
                {
                    // overhead:
                    counter.ComputeOverhead(pck, EnergyConsumption.Transmit, sender, Reciver);
                    counter.Animate(sender, Reciver, pck);
                    //:
                    Reciver.numOfPacketsPassingThrough += 1; 
                    RecivePacket(Reciver, pck);
                }
                else
                {
                    counter.SaveToQueue(sender, pck); // save in the queue.
                }
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
                if (packt.Destination.ID == Reciver.ID)
                {
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    counter.DisplayRefreshAtReceivingPacket(Reciver);   
                    Reciver.collectedPacks.Enqueue(packt);
                }
                else
                {
                    counter.ComputeOverhead(packt, EnergyConsumption.Recive, null, Reciver);
                    if (packt.Hops <= packt.TimeToLive)
                    {
                        SendPacket(Reciver, packt);
                    }
                    else
                    {
                        counter.DropPacket(packt, Reciver, PacketDropedReasons.TimeToLive);
                    }
                }
            }
        }
  


        /// <summary>
        /// find x in inlist
        /// </summary>
        /// <param name="x"></param>
        /// <param name="inlist"></param>
        /// <returns></returns>
        private bool Find(SinksAgentsRow x, List<SinksAgentsRow> inlist)
        {
            foreach (SinksAgentsRow rec in inlist)
            {
                if (rec.Sink.ID == x.Sink.ID)
                {
                    return true;
                }
            }

            return false;
        }

        private List<SinksAgentsRow> GetMySinksFromPacket(Sensor Agent, Packet pck)
        {
            int AgentID = Agent.ID;
            bool isFollowup = pck.isFollowUp;
            List<SinksAgentsRow> inpacketSinks = pck.SinksAgentsList;

            List<SinksAgentsRow> re = new List<SinksAgentsRow>();
            foreach (SinksAgentsRow x in inpacketSinks)
            {
                if (x.AgentNode.ID == AgentID)
                {
                    re.Add(x);
                }
            }
            return re;
        }

 
        /// <summary>
        /// get the max value
        /// </summary>
        /// <param name="ni"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        public Sensor SelectNextHop(Sensor ni, Packet packet)
        {
                return new GreedyRoutingMechansims().RrGreedy(ni, packet);
        }


    }
}
