using RR.Computations;
using RR.Dataplane.PacketRouter;
using RR.Intilization;
using RR.Comuting.Routing;
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
using RR.Dataplane.NOS;
using RR.Cluster;

namespace RR.Dataplane
{
    
    
    /// <summary>
    /// Interaction logic for BaseStation.xaml
    /// </summary>
    /// 
    public partial class BaseStation : UserControl
    {
        public int ID { get; set; }
        public DispatcherTimer timer_checkingMC= new DispatcherTimer(); 
        public DispatcherTimer QueuTimer = new DispatcherTimer();
        
        public List<Packet> Arriving_reqPackets = new List<Packet>();
        //public List<Packet> reqPackets_Unsorted = new List<Packet>();

        public Queue<Sensor> SortedClusterHeaders = new Queue<Sensor>();

        public List<Packet> Emerg_req = new List<Packet>();
        //public List<int> firstKreq = new List<int>();
        //public Sink sink = PublicParamerters.MainWindow.mySinks[0];

        public TravellingSalesmanAlg tsm;

        /// <summary>
        /// 
        /// </summary>
        public BaseStation()
        {
            InitializeComponent();
            int id = 1;
            //lbl_BaseStation_id.Text = id.ToString();
            ID = id;                  
            Width = 20;
            Height = 30;
            setPosition();           

            PublicParamerters.MainWindow.myBstation = this;

            timer_checkingMC.Interval = TimeSpan.FromMinutes(5);
            timer_checkingMC.Tick += TriggerCharger;
            timer_checkingMC.Start();
        }

        public void StopScheduling()
        {
            QueuTimer.Stop();
            timer_checkingMC.Stop();
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


        public void setPosition ()
        {
            //Point pt = PublicParamerters.MainWindow.myNetWork[1].CenterLocation;
            Position = new Point(PublicParamerters.NetworkSquareSideLength / 2 - Width, PublicParamerters.NetworkSquareSideLength / 2 - Height);
        }

        
        List<Packet> Emerg_temp = new List<Packet>();
        public void SaveToQueue(Packet packet)
        {           
            Arriving_reqPackets.Add(packet);           
 
            PublicParamerters.TotalNumRequests += 1;

            //TriggerCharger();
        }

        public void TriggerCharger(object sender, EventArgs e)
        {           
            bool condition = true;
            foreach (var mySink in PublicParamerters.MainWindow.mySinks)
            {
                if (!mySink.isFree)
                {
                    condition = false;
                }
            }

            if (condition && Arriving_reqPackets.Count >= PublicParamerters.listOfRegs.Count)// * 2)
            {
                Scheduling();
            }
        }
       
        public void Scheduling()
        {
            reOrdering();
            //createTasks();

            forwardingTasks();          
        }

        private void reOrdering()
        {
            List<Sensor> Headers_Unsorted = new List<Sensor>();

            foreach (NetCluster valu in PublicParamerters.listOfRegs)
            {
                Headers_Unsorted.Add(valu.Header);
            }

            System.Console.WriteLine("\n unordered requests");
            foreach (Sensor p in Headers_Unsorted)
            {
                System.Console.Write(p.ID + " , ");
            }

            //// 
            /// Travelling Salesman algorithm to find the efficient path for Mobile charger traveling
            Sensor closest = getClosestNode(Position, Headers_Unsorted);

            Headers_Unsorted.Remove(closest);
            Headers_Unsorted.Insert(0, closest);

            var tsmRout = new StartTSM();
            List<int> orderdID = tsmRout.Startit(Headers_Unsorted);

            System.Console.WriteLine("\n ordered requests using TSM.");
            foreach (int Id in orderdID)
            {
                System.Console.Write(Id + " , ");
            }

            SortedClusterHeaders.Clear();
            foreach (int Id in orderdID)
            {
                foreach (Sensor sen in Headers_Unsorted)
                {
                    if (sen.ID == Id)
                    {
                        SortedClusterHeaders.Enqueue(sen);
                        break;
                    }
                }
            }

        }
        
        private void forwardingTasks()
        {
            Queue<Packet> requests = new Queue<Packet>();
            while (Arriving_reqPackets.Count > 0)
            {
                requests.Enqueue(Arriving_reqPackets[0]);
                Arriving_reqPackets.RemoveAt(0);
            }

            double numOfRegforMC = PublicParamerters.listOfRegs.Count / PublicParamerters.MainWindow.mySinks.Count;
            int sinkCounter = 0;
            foreach (var mySink in PublicParamerters.MainWindow.mySinks)
            {
                Queue<Sensor> ClHeads = new Queue<Sensor>();
                for (int i = 1; i <= Math.Ceiling(numOfRegforMC); i++)
                {
                    if(SortedClusterHeaders.Count > 0)
                    {
                        ClHeads.Enqueue(SortedClusterHeaders.Dequeue());
                    }                 
                }

                sinkCounter += 1;
                if(sinkCounter == PublicParamerters.MainWindow.mySinks.Count)
                {
                    while(SortedClusterHeaders.Count > 0)
                    {
                        ClHeads.Enqueue(SortedClusterHeaders.Dequeue());
                    }
                }

                mySink.Bstation = this;
                mySink.initiateCharger(ClHeads, requests);
            }
            
        }


        public Sensor getClosestNode(Point MC_loc, List<Sensor> head)
        {
            double closer = double.MaxValue;
            Sensor holder = head[0];

            foreach (Sensor sen in head)
            {
                double Ds_next = Operations.DistanceBetweenTwoPoints(MC_loc, sen.CenterLocation);

                if (Ds_next < closer)
                {
                    closer = Ds_next;
                    holder = sen;
                }
            }
            return holder;
        }


    }
}
