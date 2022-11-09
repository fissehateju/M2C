using RR.Computations;
using RR.Dataplane.PacketRouter;
using RR.Dataplane.NOS;
using RR.Intilization;
using RR.Comuting.Routing;
using RR.Models.Mobility;
using RR.Models.Charging;
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
using RR.Cluster;

namespace RR.Dataplane
{
    /// <summary>
    /// Interaction logic for Sink.xaml
    /// </summary>
    public partial class Sink : UserControl
    {

        public BaseStation Bstation { get; set; }
        //public Task myTask;
        private MoveToNextPosition RandomWaypoint { get; set; }        
        public bool isFree { get; set; }
        public bool isgoingBack { get; set; }
        public List<NetCluster> visitedCluster = new List<NetCluster>();
        public NetCluster currentCluster { get; set; }
        //{ get
        //    { 
        //        double minDs = double.MaxValue;
        //        Sensor holder = null;

        //        foreach (var sen in PublicParamerters.MainWindow.myNetWork)
        //        {
        //            double Ds = Operations.DistanceBetweenTwoPoints(CenterLocation, sen.CenterLocation);

        //            if (Ds < minDs)
        //            {
        //                minDs = Ds;
        //                holder = sen;
        //            }
        //        }

        //        return holder != null ? holder.myCluster : null;
        //    }
        //}  
        //
        private double speed { get; set; }
        private Point next_position { get; set; }
        private Point destination { get; set; }
        private int TimeToNextSojoun { get; set; }
        public double ResidualEnergy { get; set; }
        public DateTime TourStated { get; set; }
        public int ID
        {
            get;
            set;
        }
        public Sink()
        {
            InitializeComponent();
            int id = PublicParamerters.SinkCount+1;
            lbl_sink_id.Text = "C"+id.ToString();
            //lbl_sink_id.Text = "MC";
            lbl_sink_id.FontWeight = FontWeights.Bold;
            ID = id;
            isFree = true;
            PublicParamerters.MainWindow.mySinks.Add(this);
            Dispatcher.Invoke(() => Mobile_CS.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
            ResidualEnergy = PublicParamerters.BatteryIntialEnergyForMC;

            Position = new Point(PublicParamerters.NetworkSquareSideLength/2, PublicParamerters.NetworkSquareSideLength/2);

            //RandomAgentAtInitialization();

            if (Settings.Default.IsMobileSink)
            {
                //SetMobility();
            }

            Width = 16;
            Height = 16;
            
        }

        public Packet getClosestNode(Point MC_loc, Queue<Packet> reqs)
        {
            double closer = double.MaxValue;
            Packet holder = null;

            foreach (Packet reqPacket in reqs)
            {
                if (!visitedNode.Contains(reqPacket.Source) && reqPacket.Source.myCluster.Id == currentCluster.Id)
                {
                    double Ds_next = Operations.DistanceBetweenTwoPoints(MC_loc, reqPacket.Source.CenterLocation);

                    if (Ds_next < closer)
                    {
                        closer = Ds_next;
                        holder = reqPacket;
                    }
                }
            }

            return holder;
        }

        /// <summary>
        /// set the mobility model
        /// </summary>
        public void SetMobilityforDatacollection()
        {

            RandomWaypoint = new MoveToNextPosition(this, PublicParamerters.NetworkSquareSideLength, PublicParamerters.NetworkSquareSideLength, this);

            if (this.headers.Count > 0)
            {
                //Packet req = getClosestNode(CenterLocation, ChargingReqs);
                Sensor head = this.headers.Dequeue();
                currentCluster = head.myCluster;

                double MeToDes = Operations.DistanceBetweenTwoPoints(CenterLocation, head.CenterLocation);
                double DesToBs = Operations.DistanceBetweenTwoPoints(head.CenterLocation, Bstation.Position);
                double MeToBs = Operations.DistanceBetweenTwoPoints(CenterLocation, Bstation.Position);

                if (ResidualEnergy - (PublicParamerters.E_MCmove * MeToDes) - (PublicParamerters.E_MCmove * DesToBs) < 100)
                {

                    // return to the Base station
                    Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));
                    PublicParamerters.TotalDistance_CoveredMC += MeToBs;
                    PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToBs;
                    ResidualEnergy -= PublicParamerters.E_MCmove * MeToBs;

                    RandomWaypoint.StartMove(null);
                }
                else
                {
                    ////// compute travel cost
                    ///
                    Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));

                    PublicParamerters.TotalDistance_CoveredMC += MeToDes;
                    PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToDes;
                    ResidualEnergy -= PublicParamerters.E_MCmove * MeToDes;

                    RandomWaypoint.StartMove(head);
                }

            }
            else
            {
                //// compute travel cost
                destination = new Point(PublicParamerters.NetworkSquareSideLength / 2, PublicParamerters.NetworkSquareSideLength / 2);
                double MeToBs = Operations.DistanceBetweenTwoPoints(CenterLocation, destination);

                // return to the Base station
                Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));
                PublicParamerters.TotalDistance_CoveredMC += MeToBs;
                PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToBs;
                ResidualEnergy -= PublicParamerters.E_MCmove * MeToBs;

                RandomWaypoint.StartMove(null);


            }

        }

        public void SetMobilityforCharging(Packet req)
        {
            RandomWaypoint = new MoveToNextPosition(this, PublicParamerters.NetworkSquareSideLength, PublicParamerters.NetworkSquareSideLength, this);

            double MeToDes = Operations.DistanceBetweenTwoPoints(CenterLocation, req.Source.CenterLocation);
            double DesToBs = Operations.DistanceBetweenTwoPoints(req.Source.CenterLocation, Bstation.Position);
            double MeToBs = Operations.DistanceBetweenTwoPoints(CenterLocation, Bstation.Position);

            if (ResidualEnergy - (PublicParamerters.E_MCmove * MeToDes) - (PublicParamerters.E_MCmove * DesToBs) < 100)
            {

                // return to the Base station
                Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));
                PublicParamerters.TotalDistance_CoveredMC += MeToBs;
                PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToBs;
                ResidualEnergy -= PublicParamerters.E_MCmove * MeToBs;

                RandomWaypoint.StartMove(null);
            }
            else
            {
                ////// compute travel cost
                ///
                Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.Moving)));

                PublicParamerters.TotalDistance_CoveredMC += MeToDes;
                PublicParamerters.TotalEnergyForTravelMC += PublicParamerters.E_MCmove * MeToDes;
                ResidualEnergy -= PublicParamerters.E_MCmove * MeToDes;

                RandomWaypoint.StartMove(req.Source);
            }

        }

        public Queue<Packet> ChargingReqs = new Queue<Packet>();
        public List<Sensor> visitedNode = new List<Sensor>();
        public Queue<Sensor> headers = new Queue<Sensor>();
        public void initiateCharger(Queue<Sensor> heads, Queue<Packet> task)
        {
            ChargingReqs = task;
            headers = heads;
            isFree = false;

            TourStated = DateTime.Now;
            SetMobilityforDatacollection();
        }
       
        public void ChargingAlert()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col1_9)));
        }
        public void BreakTime()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
        }

        public void StopMoving()
        {
            Dispatcher.Invoke(() => Mobile_CS.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(BatteryLevelColoring.col0)));
            ChargingReqs.Clear();
            headers.Clear();
            if (RandomWaypoint != null) RandomWaypoint.StopOperation();
        }

        public void PrintTourInfo()
        {

            while (ChargingReqs.Count > 0)
            {
                Bstation.SaveToQueue(ChargingReqs.Dequeue());
            }


            //System.Console.WriteLine("\n #####################################");
            //System.Console.WriteLine(" _______________ # MC {0} ______________", this.ID);
            //System.Console.WriteLine("The Total distance coverred     : {0}", PublicParamerters.TotalDistance_CoveredMC);
            //System.Console.WriteLine("The MC's Travel Energy          : {0}", PublicParamerters.TotalEnergyForTravelMC);
            //System.Console.WriteLine("The Total transfered Energy     : {0}", PublicParamerters.TotalTransferredEnergy);
            //System.Console.WriteLine("The Total # of Request Received : {0}", PublicParamerters.TotalNumRequests);
            //System.Console.WriteLine("The Total # of charged sensors  : {0}", PublicParamerters.TotalNumChargedSensors);
            //System.Console.WriteLine("The # of Data Collected         : {0}", PublicParamerters.NumberofDeliveredPacket);
            //System.Console.WriteLine("Collected Data Ratio            : {0}", PublicParamerters.CollectedDataPercentage);

            //System.Console.WriteLine("Average Delivery Delay    : {0} ", PublicParamerters.DataCollDelaysInSecond / PublicParamerters.NumberofDeliveredPacket);

            //System.Console.WriteLine("Average Charging Delay    : {0} ", PublicParamerters.ChargingDelayInSecond / PublicParamerters.TotalNumChargedSensors);
            //System.Console.WriteLine("Service Time              : {0} ", PublicParamerters.ServiceTimeInSecond);
            //System.Console.WriteLine("\n #####################################");

        }

        /// <summary>
        /// Real postion of object.
        /// </summary>
        public Point Position
        {
            get
            {
                double x = Margin.Left;
                double y = Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
            set
            {
                Point p = value;
                Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        /// <summary>
        /// center location of node.
        /// </summary>
        public Point CenterLocation
        {
            get
            {
                double x = Margin.Left;
                double y = Margin.Top;
                Point p = new Point(x, y);
                return p;
            }
        }

        /// <summary>
        /// report the new position of the sink.
        /// </summary>
        public void ReportMyPosition()
        {
            if (MySinksAgentsRow != null)
            {
                if (MySinksAgentsRow.AgentNode != null)
                {

                    ReportSinkPositionMessage rep = new ReportSinkPositionMessage(MySinksAgentsRow);
                }
            }
        }

        public SinksAgentsRow MySinksAgentsRow { get; set; }

        /// <summary>
        /// intailization:
        /// </summary>
        public void RandomAgentAtInitialization()
        {
            int count = PublicParamerters.MainWindow.myNetWork.Count;
            // select random sensor and set that as my agent.
            if (count > 0)
            {
                int index;
                if (Settings.Default.SinksStartAtNetworkCenter)
                {
                    index = 0;
                }
                else
                {
                    // select random:
                    bool agentisHighTier = false;
                    do
                    {
                        index = RandomvariableStream.UniformRandomVariable.GetIntValue(0, count - 1);
                        agentisHighTier = PublicParamerters.MainWindow.myNetWork[index].IsHightierNode;
                    } while (agentisHighTier);
                }
                index = 0;
                Sensor agent = PublicParamerters.MainWindow.myNetWork[index];
                SinksAgentsRow sinksAgentsRow = new SinksAgentsRow();
                sinksAgentsRow.AgentNode = agent;
                sinksAgentsRow.Sink = this;
                agent.AddNewAGent(sinksAgentsRow);
                MySinksAgentsRow = sinksAgentsRow;
                Position = agent.CenterLocation;
                //ReportMyPosition();
            }
        }

        /// <summary>
        /// check if the distance almost out.
        /// 
        /// </summary>
        /// <returns></returns>
        public bool AlmostOutOfMyAgent()
        {
            if (MySinksAgentsRow != null)
            {
                if (MySinksAgentsRow.AgentNode != null)
                {
                    double dis = Operations.DistanceBetweenTwoPoints(CenterLocation, MySinksAgentsRow.AgentNode.CenterLocation);
                    if (dis >= (PublicParamerters.CommunicationRangeRadius * 0.7))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void ReselectAgentNode()
        {
            double mindis = double.MaxValue;
            Sensor newAgent = null;
            foreach(Sensor sj in MySinksAgentsRow.AgentNode.NeighborsTable)
            {
                double curDis = Operations.DistanceBetweenTwoPoints(CenterLocation, sj.CenterLocation);
                if (curDis < PublicParamerters.CommunicationRangeRadius)
                {
                    if (curDis < mindis)
                    {
                        mindis = curDis;
                        newAgent = sj;
                    }
                }
            }

            // found:
            if (newAgent != null)
            {
                // Prev one:
                Sensor prevAgent = MySinksAgentsRow.AgentNode;
                prevAgent.AgentStartFollowupMechansim(ID, newAgent);
                bool preRemoved = prevAgent.RemoveFromAgent(MySinksAgentsRow);

                if (preRemoved)
                {
                    // set the new one:
                    SinksAgentsRow newsinksAgentsRow = new SinksAgentsRow();
                    newsinksAgentsRow.AgentNode = newAgent;
                    newsinksAgentsRow.Sink = this;
                    newAgent.AddNewAGent(newsinksAgentsRow);
                    MySinksAgentsRow = newsinksAgentsRow;
                    //Console.WriteLine("Sink:" + ID + " reselected " + newAgent.ID + " as new agent. Prev. Agent:" + prevAgent.ID);
                    ReportMyPosition();
                }
                else
                {
                    //MessageBox.Show("sink->ReselectAgentNode()-> preRemoved=false.");
                }
            }
            else
            {
                //Console.WriteLine("Sink:" + ID + "Out of network and has no agent.");
                // use the same prev agent:
                Position = MySinksAgentsRow.AgentNode.CenterLocation;
                
            }
        }



        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

           //bool imOut= AlmostOutOfMyAgent();
           // if (imOut)
           // {
           //     // reselect:
           //     ReselectAgentNode();
           // }
           // else
           // {
           //     // do no thing.
           // }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            ToolTip = new Label() { Content = ResidualEnergy.ToString() };
        }
    }
}
