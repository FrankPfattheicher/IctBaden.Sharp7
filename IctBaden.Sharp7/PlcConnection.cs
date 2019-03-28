using System;
using System.Collections.Generic;
using System.Threading;
using IctBaden.Framework.Tron;
using Sharp7;
// ReSharper disable UnusedMember.Global

namespace IctBaden.Sharp7
{
    // ReSharper disable once UnusedMember.Global
    public class PlcConnection : IDisposable
    {
        public string PlcAddress { get; set; }
        public int PlcPollIntervalMilliseconds { get; set; }

        internal S7Client PlcClient { get; private set; }

        public List<PlcItem> ItemsToPoll { get; set; }

        public event Action<PlcItem> ItemChanged;

        private bool _oldPlcConnected;

        private Timer _poll;
        private bool _pollActive;


        public PlcConnection(string plcAddress)
            : this(plcAddress, 0)
        {
        }
        public PlcConnection(string plcAddress, int plcPollIntervalMilliseconds)
        {
            PlcAddress = plcAddress;
            PlcPollIntervalMilliseconds = plcPollIntervalMilliseconds;
            ItemsToPoll = new List<PlcItem>();
        }

        public void Start()
        {
            PlcClient = new S7Client();
            TronTrace.TraceInformation("PlcConnection: Connect to " + PlcAddress);
            PlcClient.ConnectTo(PlcAddress, 0, 2);

            if(PlcPollIntervalMilliseconds > 0)
            {
                TronTrace.TraceInformation($"PlcConnection: Start polling using {PlcPollIntervalMilliseconds}ms interval");
                _poll = new Timer(_ => PollPlc(), this, 
                    TimeSpan.FromMilliseconds(PlcPollIntervalMilliseconds),
                    TimeSpan.FromMilliseconds(PlcPollIntervalMilliseconds));
            }
        }

        public void Dispose()
        {
            _poll?.Dispose();
            _poll = null;

            PlcClient?.Disconnect();
            PlcClient = null;
        }


        private void PollPlc()
        {
            if (_pollActive) return;

            try
            {
                _pollActive = true;
                DoPollPlc();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PollPlc: " + ex.Message);
            }
            finally
            {
                _pollActive = false;
            }
        }

        private void DoPollPlc()
        {
            if (PlcClient.Connected != _oldPlcConnected)
            {
                TronTrace.TraceInformation("PlcConnection: Connected = " + PlcClient.Connected);
                _oldPlcConnected = PlcClient.Connected;
            }

            foreach (var plcItem in ItemsToPoll)
            {
                
                plcItem.UpdateValueFromPlc();

            }
        }

    }
}