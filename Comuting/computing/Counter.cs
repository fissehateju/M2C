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
using static RR.Comuting.computing.Sorters;

namespace RR.Comuting.computing
{

    public enum EnergyConsumption { Transmit, Recive } // defualt is not used. i 
    public class NetworkOverheadCounter
    {
        FirstOrderRadioModel EnergyModel;
        public NetworkOverheadCounter ()
        {
            EnergyModel = new FirstOrderRadioModel(); // energy model.
        }

        private double ConvertToJoule(double UsedEnergy_Nanojoule) //the energy used for current operation
        {
            double _e9 = 1000000000; // 1*e^-9
            double _ONE = 1;
            double oNE_DIVIDE_e9 = _ONE / _e9;
            double re = UsedEnergy_Nanojoule * oNE_DIVIDE_e9;
            return re;
        }


        /// <summary>
        /// redunduant transmisin
        /// </summary>
        /// <param name="pacekt"></param>
        /// <param name="reciverNode"></param>
        public void RedundantTransmisionCost(Packet pacekt, Sensor reciverNode)
        {
            // logs.
            PublicParamerters.TotalReduntantTransmission += 1;
            double UsedEnergy_Nanojoule = EnergyModel.Receive(128); // preamble packet length.
            double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule);
            reciverNode.ResidualEnergy = reciverNode.ResidualEnergy - UsedEnergy_joule;
            pacekt.UsedEnergy_Joule += UsedEnergy_joule;
            PublicParamerters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
            PublicParamerters.TotalWastedEnergyJoule += UsedEnergy_joule;
            reciverNode.MainWindow.Dispatcher.Invoke(() => reciverNode.MainWindow.lbl_Redundant_packets.Content = PublicParamerters.TotalReduntantTransmission);
            reciverNode.MainWindow.Dispatcher.Invoke(() => reciverNode.MainWindow.lbl_Wasted_Energy_percentage.Content = PublicParamerters.WastedEnergyPercentage);
        }


        /// <summary>
        /// this counts the energy consumption, the delay and the hops.
        /// </summary>
        /// <param name="packt"></param>
        /// <param name="enCon"></param>
        /// <param name="sender"></param>
        /// <param name="Reciver"></param>
        public void ComputeOverhead(Packet packt, EnergyConsumption enCon, Sensor sender, Sensor Reciver)
        {
            if (enCon == EnergyConsumption.Transmit)
            {
                // calculate the energy 
                double Distance_M = Operations.DistanceBetweenTwoSensors(sender, Reciver);
                double UsedEnergy_Nanojoule = EnergyModel.Transmit(packt.PacketLength, Distance_M);
                double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule) * 5; // * 
                sender.ResidualEnergy = sender.ResidualEnergy - UsedEnergy_joule ; 
                PublicParamerters.TotalEnergyConsumptionJoule += UsedEnergy_joule;
                packt.UsedEnergy_Joule += UsedEnergy_joule;
                packt.Hops += 1;
                double delay = DelayModel.DelayModel.Delay(sender, Reciver, packt);
                packt.Delay += delay;
                PublicParamerters.TotalDelayMs += delay;
                PublicParamerters.TotalRoutingDistance += Distance_M;
                PublicParamerters.TotalNumberOfHops += 1;

                switch (packt.PacketType)
                {
                    case PacketType.Data:
                        PublicParamerters.EnergyConsumedForDataPackets += UsedEnergy_joule; // data packets.
                        break;
                    default:
                        PublicParamerters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                        break;
                }
            }
            else if (enCon == EnergyConsumption.Recive)
            {
                if (Reciver != null)
                {
                    double UsedEnergy_Nanojoule = EnergyModel.Receive(packt.PacketLength);
                    double UsedEnergy_joule = ConvertToJoule(UsedEnergy_Nanojoule) * 5; // * 
                    Reciver.ResidualEnergy = Reciver.ResidualEnergy - UsedEnergy_joule;
                    packt.UsedEnergy_Joule += UsedEnergy_joule;
                    PublicParamerters.TotalEnergyConsumptionJoule += UsedEnergy_joule;

                    switch (packt.PacketType)
                    {
                        case PacketType.Data:
                            PublicParamerters.EnergyConsumedForDataPackets += UsedEnergy_joule; // data packets.
                            break;
                        default:
                            PublicParamerters.EnergyComsumedForControlPackets += UsedEnergy_joule; // other packets.
                            break;
                    }
                }
            }

        }

        public void DropPacket(Packet packt, Sensor Reciver, PacketDropedReasons packetDropedReasons)
        {
            PublicParamerters.NumberofDropedPacket += 1;
            packt.PacketDropedReasons = packetDropedReasons;
            packt.isDelivered = false;
            
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_Number_of_Droped_Packet.Content = PublicParamerters.NumberofDropedPacket, DispatcherPriority.Send);

            if (Settings.Default.SavePackets)
                PublicParamerters.FinishedRoutedPackets.Add(packt);
            else
                packt.Dispose();

        }

        public void SuccessedDeliverdPacket(Packet packt)
        {
            packt.isDelivered = true;
            if(packt.PacketType == PacketType.Data)
            {
                PublicParamerters.NumberofDeliveredPacket += 1;
            }
            
            if (Settings.Default.SavePackets)
                PublicParamerters.FinishedRoutedPackets.Add(packt);
            else
                packt.Dispose();
        }

        

        public void Animate(Sensor sender, Sensor Reciver, Packet pck)
        {
            if (Settings.Default.ShowRoutingPaths)
            {
                sender.Animator.StartAnimate(Reciver.ID, pck.PacketType);
            }
        }
       

        public void DisplayRefreshAtGenertingPacket(Sensor packetSource, PacketType type)
        {
            //Console.WriteLine(packetSource.ID + "->" + type);
            packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_num_of_gen_packets.Content = PublicParamerters.NumberofGeneratedPackets, DispatcherPriority.Normal);

            switch(type)
            {
                case PacketType.Data:
                    PublicParamerters.NumberOfGenerDataPacket += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_data_packets.Content = PublicParamerters.NumberOfGenerDataPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.ObtainSinkPosition:
                    PublicParamerters.NumberOfObtainSinkPositionPacket += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_obtian_packets.Content = PublicParamerters.NumberOfObtainSinkPositionPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.ReportSinkPosition:
                    PublicParamerters.NumberOfReportSinkPositionPacket += 1;
                    //packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_report_packets.Content = PublicParamerters.NumberOfReportSinkPositionPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.ResponseSinkPosition:
                    PublicParamerters.NumberOfResponseSinkPositionPacket += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_response_packets.Content = PublicParamerters.NumberOfResponseSinkPositionPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.ShareSinkPosition:
                    PublicParamerters.NumberOfShareSinkPositionPacket += 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_share_packets.Content = PublicParamerters.NumberOfShareSinkPositionPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.AssigningAsHeader:
                    PublicParamerters.NumberOfHeaderInfoPacket = PublicParamerters.NumberOfHeaderInfoPacket + 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_HeaderInfo_packets.Content = PublicParamerters.NumberOfHeaderInfoPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.HeaderInfo:
                    PublicParamerters.NumberOfHeaderInfoPacket = PublicParamerters.NumberOfHeaderInfoPacket + 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_HeaderInfo_packets.Content = PublicParamerters.NumberOfHeaderInfoPacket, DispatcherPriority.Normal);
                    break;
                case PacketType.ReportBatteryLevel:
                    PublicParamerters.NumberOfHeaderInfoPacket = PublicParamerters.NumberOfHeaderInfoPacket + 1;
                    packetSource.MainWindow.Dispatcher.Invoke(() => packetSource.MainWindow.lbl_HeaderInfo_packets.Content = PublicParamerters.NumberOfHeaderInfoPacket, DispatcherPriority.Normal);
                    break;
            }
        }

        public void DisplayRefreshAtReceivingPacket(Sensor Reciver)
        {
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_consumed_energy.Content = PublicParamerters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParamerters.NumberofDeliveredPacket, DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_sucess_ratio.Content = PublicParamerters.DeliveredRatio, DispatcherPriority.Send);
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_nymber_inQueu.Content = PublicParamerters.InQueuePackets.ToString());

            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_tot_ChargedNodes.Content = PublicParamerters.TotalNumChargedSensors.ToString());
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_distance.Content = PublicParamerters.TotalDistance_CoveredMC.ToString());
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_traveling.Content = PublicParamerters.TotalEnergyForTravelMC.ToString());
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_transferedEnerg.Content = PublicParamerters.TotalTransferredEnergy.ToString());

            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_servicetime.Content = PublicParamerters.ServiceTimeInSecond.ToString());
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_avg_Chargedelay.Content = (PublicParamerters.ChargingDelayInSecond / PublicParamerters.TotalNumChargedSensors).ToString());

            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_avg_delay.Content = (PublicParamerters.DataCollDelaysInSecond / PublicParamerters.NumberofDeliveredPacket).ToString());
            Reciver.MainWindow.Dispatcher.Invoke(() => Reciver.MainWindow.lbl_total_consump.Content = PublicParamerters.TotalEnergyConsumptionJoule.ToString());
        }

        public void DisplayRefreshAfterEnteringQueue(Sensor Sender)
        {
            Sender.MainWindow.Dispatcher.Invoke(() => Sender.MainWindow.lbl_total_consumed_energy.Content = PublicParamerters.TotalEnergyConsumptionJoule + " (JOULS)", DispatcherPriority.Send);
            Sender.MainWindow.Dispatcher.Invoke(() => Sender.MainWindow.lbl_Number_of_Delivered_Packet.Content = PublicParamerters.NumberofDeliveredPacket, DispatcherPriority.Send);
            Sender.MainWindow.Dispatcher.Invoke(() => Sender.MainWindow.lbl_sucess_ratio.Content = PublicParamerters.DeliveredRatio, DispatcherPriority.Send);
            Sender.MainWindow.Dispatcher.Invoke(() => Sender.MainWindow.lbl_nymber_inQueu.Content = PublicParamerters.InQueuePackets.ToString());
        }

        /// <summary>
        /// the packet should wait in the queue
        /// </summary>
        public void SaveToQueue(Sensor sender, Packet packet)
        {
            sender.WaitingPacketsQueue.Enqueue(packet);
            PublicParamerters.TotalWaitingTime += 1; // total;
            packet.WaitingTimes += 1;
            if (!sender.QueuTimer.IsEnabled)
            {
                sender.QueuTimer.Start();
            }
            if (Settings.Default.ShowRadar) sender.Myradar.StartRadio();
            PublicParamerters.MainWindow.Dispatcher.Invoke(() => sender.Ellipse_indicator.Fill = Brushes.DeepSkyBlue);
            PublicParamerters.MainWindow.Dispatcher.Invoke(() => sender.Ellipse_indicator.Visibility = Visibility.Visible);
            DisplayRefreshAfterEnteringQueue(sender);
        }


        /// <summary>
        /// min priority is prefer
        /// </summary>
        /// <param name="coordinationEntries"></param>
        /// <param name="packet"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public Sensor CoordinateGetMin(List<CoordinationEntry> coordinationEntries, Packet packet, double sum)
        {

          
            // normalized to 1:
            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                double newPr = neiEntry.Priority / sum;
                neiEntry.Priority = newPr;
            }

           // coordinationEntries.Sort(new CoordinationEntrySorter());


            List<CoordinationEntry> Forwarders = new List<CoordinationEntry>();
            int n = coordinationEntries.Count;
            double average = 1 / Convert.ToDouble(n); // this needs to be considered
            int maxForwarders = Convert.ToInt16(Math.Floor(Math.Sqrt(Math.Sqrt(n)))) - 1; // theshold.
            int MaxforwardersCount = 0;
            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                // take the first (maxForwarders)
                // the smaller priority is better. select the nodes with smaller priority
                if (MaxforwardersCount <= maxForwarders && neiEntry.Priority <= average)
                {
                    Forwarders.Add(neiEntry);
                    MaxforwardersCount++;
                }
            }


            Forwarders.Sort(new CoordinationEntrySorter());

            // one forwarder:
            // forward:
            Sensor forwarder = null;
            foreach (CoordinationEntry neiEntry in Forwarders)
            {
                if (neiEntry.Sensor.CurrentSensorState == SensorState.Active)
                {
                    if (forwarder == null)
                    {
                        forwarder = neiEntry.Sensor;
                    }
                    else
                    {
                        RedundantTransmisionCost(packet, neiEntry.Sensor);
                    }
                }
            }

            return forwarder;
        }
         
        public Sensor CoordinateGetMin1(List<CoordinationEntry> coordinationEntries, Packet packet, double sum)
        {


            // normalized to 1:
            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                double newPr = neiEntry.Priority / sum;
                neiEntry.Priority = newPr;
            }

            // coordinationEntries.Sort(new CoordinationEntrySorter());


            List<CoordinationEntry> Forwarders = new List<CoordinationEntry>();
            int n = coordinationEntries.Count;
            double average = 1 / Convert.ToDouble(n); // this needs to be considered
            int maxForwarders = 1 + Convert.ToInt16(Math.Ceiling(Math.Sqrt(Math.Sqrt(n)))); // theshold.
            int MaxforwardersCount = 0;
            foreach (CoordinationEntry neiEntry in coordinationEntries)
            {
                // take the first (maxForwarders)
                // the smaller priority is better. select the nodes with smaller priority
                if (MaxforwardersCount <= maxForwarders && neiEntry.Priority <= average)
                {
                    Forwarders.Add(neiEntry);
                    MaxforwardersCount++;
                }
            }


            Forwarders.Sort(new CoordinationEntrySorter());

            // one forwarder:
            // forward:
            Sensor forwarder = null;
            foreach (CoordinationEntry neiEntry in Forwarders)
            {
                if (neiEntry.Sensor.CurrentSensorState == SensorState.Active)
                {
                    if (forwarder == null)
                    {
                        forwarder = neiEntry.Sensor;
                    }
                    else
                    {
                        RedundantTransmisionCost(packet, neiEntry.Sensor);
                    }
                }
            }

            return forwarder;
        }


    }
}
