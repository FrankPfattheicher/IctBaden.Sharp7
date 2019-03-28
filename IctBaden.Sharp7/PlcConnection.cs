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
        public int PlcRack { get; set; }
        public int PlcSlot { get; set; }
        public int PlcPollIntervalMilliseconds { get; set; }

        internal S7Client PlcClient { get; private set; }
        public bool IsConnected => PlcClient?.Connected ?? false;

        //public event Action<PlcItem> ItemChanged;

        private bool _oldPlcConnected;

        private List<PlcItem> _pollItems;
        private Timer _pollTimer;
        private bool _pollActive;

        public static PlcConnection GetS7_300Connection(string plcAddress) =>
            new PlcConnection(plcAddress, 0, 2);
        public static PlcConnection GetS7_1200Connection(string plcAddress) =>
            new PlcConnection(plcAddress, 0, 0);
        public static PlcConnection GetS7_1500Connection(string plcAddress) =>
            new PlcConnection(plcAddress, 0, 0);

        public PlcConnection(string plcAddress, int rack, int slot)
        {
            PlcAddress = plcAddress;
            PlcRack = rack;
            PlcSlot = slot;
        }

        public bool Connect()
        {
            PlcClient = new S7Client();
            TronTrace.TraceInformation("PlcConnection: Connect to " + PlcAddress);
            PlcClient.ConnectTo(PlcAddress, PlcRack, PlcSlot);
            if (PlcClient._LastError != 0)
            {
                var text = PlcResult.GetResultText(PlcClient._LastError);
                TronTrace.TraceError("PlcConnection: Connect FAILED : " + text);
            }
            return IsConnected;
        }

        public void Disconnect()
        {
            PlcClient?.Disconnect();
            PlcClient = null;
        }

        public void Dispose()
        {
            _pollTimer?.Dispose();
            _pollTimer = null;

            Disconnect();
        }

        public void StartPolling(List<PlcItem> items)
        {
            StartPolling(items, PlcPollIntervalMilliseconds);
        }

        public void StartPolling(List<PlcItem> items, int plcPollIntervalMilliseconds)
        {
            _pollItems = items;
            PlcPollIntervalMilliseconds = plcPollIntervalMilliseconds;

            if (PlcPollIntervalMilliseconds > 0)
            {
                TronTrace.TraceInformation($"PlcConnection: Start polling using {PlcPollIntervalMilliseconds}ms interval");
                _pollTimer = new Timer(_ => PollPlc(), this,
                    TimeSpan.FromMilliseconds(PlcPollIntervalMilliseconds),
                    TimeSpan.FromMilliseconds(PlcPollIntervalMilliseconds));
            }
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

            foreach (var plcItem in _pollItems)
            {
                plcItem.UpdateValueFromPlc();
            }
        }

    }
}