using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace SubnetPing
{

    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("SubnetPing.exe");
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            List<n> subnetClients = new List<n>();

            int subnetStartIP = 0;
            int subnetStopIP = 254;
            string subnetCheckIP = "";
            string subnetOption = "";

            if (args.Length == 1)
            {
                subnetOption = args[0];
                Console.WriteLine("SubnetPing " + args[0]);
                foreach (IPAddress localNetworkIP in host.AddressList)
                {
                    if (localNetworkIP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] octets = IPAddress.Parse(localNetworkIP.ToString()).GetAddressBytes();
                        string subnet = octets[0].ToString() + "." + octets[1].ToString() + "." + octets[2].ToString() + ".[" + subnetStartIP + "/" + subnetStopIP + "]";
                        if (localNetworkIP.ToString() == subnetOption)
                        {
                            Console.WriteLine("Interface\t" + localNetworkIP.ToString() + "\tscan\t" + subnet);
                        }
                        else
                        {
                            Console.WriteLine("Interface\t" + localNetworkIP.ToString() + "\tskip\t" + subnet);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("SubnetPing [IPv4 Address]");
                foreach (IPAddress localNetworkIP in host.AddressList)
                {
                    byte[] octets = IPAddress.Parse(localNetworkIP.ToString()).GetAddressBytes();
                    string subnet = octets[0].ToString() + "." + octets[1].ToString() + "." + octets[2].ToString() + ".[" + subnetStartIP + "/" + subnetStopIP + "]";
                    if (localNetworkIP.AddressFamily == AddressFamily.InterNetwork)
                    {
                        Console.WriteLine("Interface\t" + localNetworkIP.ToString() + "\tscan\t" + subnet);
                    }
                }
            }
            Console.WriteLine("");

            Console.WriteLine(fixedLength("IPv4-Address", 15) + "\t" + fixedLength("Mac-Address", 17) + "\t" + fixedLength("DNS-Name", 25) + "\t" + fixedLength("ms", 4, true) + "\t" + fixedLength("hops", 4, true));

            using (var pool = new Pool(32))
            {
                foreach (IPAddress localNetworkIP in host.AddressList)
                {
                    if (localNetworkIP.AddressFamily == AddressFamily.InterNetwork && (subnetOption == localNetworkIP.ToString() || subnetOption == ""))
                    {
                        byte[] octets = IPAddress.Parse(localNetworkIP.ToString()).GetAddressBytes();
                        string subnet = octets[0].ToString() + "." + octets[1].ToString() + "." + octets[2].ToString() + ".";

                        for (int pos = subnetStartIP; pos <= subnetStopIP; pos++)
                        {
                            subnetCheckIP = subnet + pos.ToString();
                            n client = new n(subnetCheckIP);
                            subnetClients.Add(client);
                            pool.QueueTask(() => client.Arp());
                        }
                    }
                }
            }

        }

        private static string fixedLength(string text, int len, bool right = false)
        {
            if (text.Length < len)
            {
                for (int i = text.Length; i < len; i++)
                {
                    if (right == true)
                    {
                        text = " " + text;
                    }
                    else
                    {
                        text += " ";
                    }
                }
            }
            else if (text.Length > len)
            {
                text = text.Substring(0, len);
            }
            return text;
        }
    }

    class n
    {

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);
        public string _ipAddress;
        public string _macAddress;
        public string _hostname = "-";
        public string _ms = "-";
        public int _line = -1;
        public int _prevline;
        public string _hops = "-";
        public int _timeout = 1000;

        private static readonly object ConsoleWriterLock = new object();

        public n(string checkIPAddress)
        {
            _ipAddress = checkIPAddress;
        }

        private string fixedLength(string text, int len, bool right = false)
        {
            if (text.Length < len)
            {
                for (int i = text.Length; i < len; i++)
                {
                    if (right == true)
                    {
                        text = " " + text;
                    }
                    else
                    {
                        text += " ";
                    }
                }
            }
            else if (text.Length > len)
            {
                text = text.Substring(0, len);
            }
            return text;
        }

        public void refresh()
        {
            try
            {
                lock (ConsoleWriterLock)
                {
                    if (Environment.UserInteractive)
                    {
                        if (_line == -1)
                        {
                            _line = Console.CursorTop;
                            Console.WriteLine(fixedLength(_ipAddress, 15) + "\t" + fixedLength(_macAddress.ToString(), 17) + "\t" + fixedLength(_hostname.ToString(), 25) + "\t" + fixedLength(_ms.ToString(), 4,true) + "\t" + fixedLength(_hops.ToString(), 4, true));
                        }
                        else
                        {
                            _prevline = Console.CursorTop;
                            Console.SetCursorPosition(0, _line);
                            Console.Write(fixedLength(_ipAddress, 15) + "\t" + fixedLength(_macAddress.ToString(), 17) + "\t" + fixedLength(_hostname.ToString(), 25) + "\t" + fixedLength(_ms.ToString(), 4, true) + "\t" + fixedLength(_hops.ToString(), 4, true));
                            Console.SetCursorPosition(0, _prevline);
                        }
                    }
                }
            }
            catch
            {

            }
        }

        public void Ping()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    AutoResetEvent waiter = new AutoResetEvent(false);
                    Ping pingSender = new Ping();
                    pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

                    PingOptions options = new PingOptions(64, true);

                    string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    pingSender.SendAsync(_ipAddress, _timeout, buffer, options, waiter);
                    waiter.WaitOne();
                }
            }
            catch
            {

            }
        }

        public void Trace()
        {

            try
            {
                if (Environment.UserInteractive)
                {
                    System.Collections.ArrayList arlPingReply = new System.Collections.ArrayList();
                    Ping myPing = new Ping();
                    PingReply prResult;
                    int iHopcount = 8;
                    for (int iC1 = 1; iC1 < iHopcount; iC1++)
                    {
                        prResult = myPing.Send(_ipAddress, _timeout, new byte[10], new PingOptions(iC1, false));
                        if (prResult.Status == IPStatus.Success)
                        {
                            iC1 = iHopcount;
                        }
                        arlPingReply.Add(prResult);
                    }
                    _hops = arlPingReply.Count.ToString();
                    refresh();
                }
            }
            catch
            {

            }
        }

        private void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    if (e.Cancelled == false && e.Error == null)
                    {
                        PingReply reply = e.Reply;
                        if (reply.Status == IPStatus.Success)
                        {
                            _ms = reply.RoundtripTime.ToString();
                            Trace();
                        }
                        refresh();
                    }
                }
            ((AutoResetEvent)e.UserState).Set();
            }
            catch
            {

            }
        }

        public void Resolve()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    IPHostEntry host = Dns.GetHostEntry(_ipAddress);
                    _hostname = host.HostName.ToString();
                    refresh();
                }
            }
            catch
            {

            }
        }

        public void Arp()
        {
            try
            {
                if (Environment.UserInteractive)
                {
                    IPAddress Destination = IPAddress.Parse(_ipAddress);
                    byte[] macAddr = new byte[6];
                    uint macAddrLen = (uint)macAddr.Length;
                    int intAddress = BitConverter.ToInt32(Destination.GetAddressBytes(), 0);
                    if (SendARP(intAddress, 0, macAddr, ref macAddrLen) == 0)
                    {
                        _macAddress = BitConverter.ToString(macAddr);
                        refresh();
                        Resolve();
                        Ping();
                    }
                }
            }
            catch
            {

            }
        }
    }


    public sealed class Pool : IDisposable
    {
        public Pool(int size)
        {
            this._workers = new LinkedList<Thread>();
            for (var i = 0; i < size; ++i)
            {
                var worker = new Thread(this.Worker) { Name = string.Concat("Worker ", i) };
                worker.Start();
                this._workers.AddLast(worker);
            }
        }

        public void Dispose()
        {
            var waitForThreads = false;
            lock (this._tasks)
            {
                if (!this._disposed)
                {
                    GC.SuppressFinalize(this);

                    this._disallowAdd = true; // wait for all tasks to finish processing while not allowing any more new tasks
                    while (this._tasks.Count > 0)
                    {
                        Monitor.Wait(this._tasks);
                    }

                    this._disposed = true;
                    Monitor.PulseAll(this._tasks); // wake all workers (none of them will be active at this point; disposed flag will cause then to finish so that we can join them)
                    waitForThreads = true;
                }
            }
            if (waitForThreads)
            {
                foreach (var worker in this._workers)
                {
                    worker.Join();
                }
            }
        }

        public void QueueTask(Action task)
        {
            lock (this._tasks)
            {
                if (this._disallowAdd) { throw new InvalidOperationException("This Pool instance is in the process of being disposed, can't add anymore"); }
                if (this._disposed) { throw new ObjectDisposedException("This Pool instance has already been disposed"); }
                this._tasks.AddLast(task);
                Monitor.PulseAll(this._tasks); // pulse because tasks count changed
            }
        }

        private void Worker()
        {
            Action task = null;
            while (true) // loop until threadpool is disposed
            {
                busy = true;
                lock (this._tasks) // finding a task needs to be atomic
                {
                    while (true) // wait for our turn in _workers queue and an available task
                    {
                        if (this._disposed)
                        {
                            return;
                        }
                        if (null != this._workers.First && object.ReferenceEquals(Thread.CurrentThread, this._workers.First.Value) && this._tasks.Count > 0) // we can only claim a task if its our turn (this worker thread is the first entry in _worker queue) and there is a task available
                        {
                            task = this._tasks.First.Value;
                            this._tasks.RemoveFirst();
                            this._workers.RemoveFirst();
                            Monitor.PulseAll(this._tasks); // pulse because current (First) worker changed (so that next available sleeping worker will pick up its task)
                            break; // we found a task to process, break out from the above 'while (true)' loop
                        }
                        Monitor.Wait(this._tasks); // go to sleep, either not our turn or no task to process
                    }
                }

                task(); // process the found task
                lock (this._tasks)
                {
                    this._workers.AddLast(Thread.CurrentThread);
                }
                task = null;
                busy = false;
            }
        }

        private readonly LinkedList<Thread> _workers; // queue of worker threads ready to process actions
        private readonly LinkedList<Action> _tasks = new LinkedList<Action>(); // actions to be processed by worker threads
        private bool _disallowAdd; // set to true when disposing queue but there are still tasks pending
        private bool _disposed; // set to true when disposing queue and no more tasks are pending
        public bool busy;
    }

}
