using System;
using System.Collections.Generic;
using System.Threading;
using IctBaden.Framework.Tron;
using Sharp7;

namespace IctBaden.Sharp7
{
    // ReSharper disable once UnusedMember.Global
    public class PlcConnection : IDisposable
    {
        public string PlcAddress { get; set; }
        public int PlcPollIntervalMilliseconds { get; set; }

        public List<PlcItem> ItemsToPoll { get; set; }

        public event Action<PlcItem> ItemChanged;


        private S7Client _plcClient;
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
            _plcClient = new S7Client();
            TronTrace.TraceInformation("PlcConnection: Connect to " + PlcAddress);
            _plcClient.ConnectTo(PlcAddress, 0, 2);

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

            _plcClient?.Disconnect();
            _plcClient = null;
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
            if (_plcClient.Connected != _oldPlcConnected)
            {
                TronTrace.TraceInformation("PlcConnection: Connected = " + _plcClient.Connected);
                _oldPlcConnected = _plcClient.Connected;
            }

            foreach (var plcItem in ItemsToPoll)
            {
                
                plcItem.UpdateValueFromPlc();

            }
        }

    }
}