using System;
using System.Linq;
using IctBaden.Framework.Tron;
using IctBaden.Framework.Types;
using Sharp7;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Sharp7
{
    public class PlcItem
    {
        private readonly PlcConnection _connection;
        private readonly PlcDataInfo _info;

        public PlcItem(PlcConnection connection, PlcDataInfo info)
        {
            _connection = connection;
            _info = info;
        }

        public string Name => _info.Name;

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsBoolean => _info.PlcDataType == PlcDataTypes.X;
        public bool IsString => _info.PlcDataType == PlcDataTypes.STRING;
        public bool IsNumeric => (_info.PlcDataType == PlcDataTypes.B)
                                || (_info.PlcDataType == PlcDataTypes.DINT)
                                || (_info.PlcDataType == PlcDataTypes.INT);

        private object _value = 0;
        private object _oldValue;

        public event Action<PlcItem> ValueChanged;

        public override string ToString()
        {
            return $"{Name}";
        }


        public void UpdateValueFromPlc()
        {
            var client = _connection?.PlcClient;
            if (client == null) return;

            lock (_connection.PlcClient)
            {
                var retry = 3;
                while (retry-- >= 0)
                {
                    if (!client.Connected)
                    {
                        TronTrace.TraceWarning("No PLC connection");
                        client.Connect();
                    }

                    if (ReadValue()) return;
                }
                TronTrace.TraceError($"PlcItem.UpdateValueFromPlc failed to read {_info.Name}");
            }
        }
        private bool ReadValue()
        {
            var client = _connection?.PlcClient;
            if (client == null) return false;

            var buffer = new byte[Math.Max(64, _info.ByteCount + 2)];
            var result = client.DBRead(_info.DbNumber, _info.Offset, _info.ByteCount, buffer);
            if (result != 0)
            {
                TronTrace.TraceError($"PlcItem.DBRead({_info.DbNumber}, {_info.Offset}) failed - {result} {PlcResult.GetResultText(result)}");
                return false;
            }

            switch (_info.PlcDataType)
            {
                case PlcDataTypes.X:
                    _value = S7.GetBitAt(buffer, 0, _info.Bit) ? 1 : 0;
                    break;
                case PlcDataTypes.B:
                    _value = S7.GetByteAt(buffer, 0);
                    break;
                case PlcDataTypes.INT:
                    _value = S7.GetIntAt(buffer, 0);
                    break;
                case PlcDataTypes.DINT:
                    _value = S7.GetDIntAt(buffer, 0);
                    break;
                case PlcDataTypes.DT:
                    _value = S7.GetDateTimeAt(buffer, 0);
                    break;
                case PlcDataTypes.STRING:
                    _value = S7.GetStringAt(buffer, 0);
                    break;
            }

            if (!_value.Equals(_oldValue))
            {
                _oldValue = _value;
                ValueChanged?.Invoke(this);
            }
            return true;
        }

        private void UpdatePlc(object value)
        {
            var client = _connection?.PlcClient;
            if (client == null) return;

            lock (client)
            {
                var retry = 3;
                while (retry-- >= 0)
                {
                    if (!client.Connected)
                    {
                        TronTrace.TraceWarning("No PLC connection");
                        client.Connect();
                    }

                    if (WriteValue(value)) return;
                }
            }
            TronTrace.TraceError($"PlcItem.UpdateValueFromPlc failed to write {_info.Name}");
        }

        private bool WriteValue(object value)
        {
            var client = _connection?.PlcClient;
            if (client == null) return false;

            if (!client.Connected) return false;

            var buffer = new byte[_info.ByteCount + 64];
            _value = value ?? throw new ArgumentNullException(nameof(value));

            try
            {
                switch (_info.PlcDataType)
                {
                    case PlcDataTypes.X:
                        S7.SetBitAt(ref buffer, 0, _info.Bit, UniversalConverter.ConvertTo<bool>(_value));
                        break;
                    case PlcDataTypes.B:
                        S7.SetByteAt(buffer, 0, UniversalConverter.ConvertTo<byte>(_value));
                        break;
                    case PlcDataTypes.INT:
                        S7.SetIntAt(buffer, 0, UniversalConverter.ConvertTo<short>(_value));
                        break;
                    case PlcDataTypes.DINT:
                        S7.SetDIntAt(buffer, 0, UniversalConverter.ConvertTo<int>(_value));
                        break;
                    case PlcDataTypes.DT:
                        S7.SetDateTimeAt(buffer, 0, UniversalConverter.ConvertTo<DateTime>(_value));
                        break;
                    case PlcDataTypes.STRING:
                        var text = UniversalConverter.ConvertTo<string>(_value);
                        if (text.Length > _info.MaxStringLength)
                        {
                            text = text.Substring(0, _info.MaxStringLength);
                        }
                        S7.SetStringAt(buffer, 0, _info.MaxStringLength, text);
                        break;
                }

                var result = (_info.PlcDataType == PlcDataTypes.X)
                                    ? client.WriteArea(_info.PlcArea, _info.DbNumber, _info.Offset, _info.ByteCount, _info.PlcWordLen, buffer)
                                    : client.DBWrite(_info.DbNumber, _info.Offset, _info.ByteCount, buffer);
                if (result == 0) return true;

                TronTrace.TraceError($"PlcItem.DBWrite({_info.DbNumber}, {_info.Offset}) failed - {result} {PlcResult.GetResultText(result)}");
            }
            catch (Exception ex)
            {
                TronTrace.TraceError($"PlcItem.DBWrite({_info.DbNumber}, {_info.Offset}) failed - {ex.Message}");
            }
            return false;
        }

        public bool ValueBool
        {
            get
            {
                var client = _connection?.PlcClient;
                if (client == null) return false;

                lock (client)
                {
                    if (IsBoolean)
                    {
                        return UniversalConverter.ConvertTo<bool>(_value);
                    }
                    return UniversalConverter.ConvertTo<int>(_value) != 0;
                }
            }
            set => UpdatePlc(value);
        }

        public long ValueLong
        {
            get
            {
                var client = _connection?.PlcClient;
                if (client == null) return 0;

                lock (client)
                {
                    if (_value == null)
                    {
                        return 0;
                    }
                    if (_value is bool value)
                    {
                        return value ? 1 : 0;
                    }
                    return UniversalConverter.ConvertTo<long>(_value);
                }
            }
            set => UpdatePlc(value);
        }

        public string ValueString
        {
            get
            {
                var client = _connection?.PlcClient;
                if (client == null) return string.Empty;

                lock (client)
                {
                    if (_value == null)
                    {
                        return string.Empty;
                    }
                    if (_value.GetType().IsArray)
                    {
                        var array = (Array)_value;
                        var strings = (from object val in array select UniversalConverter.ConvertTo<string>(val)).ToList();
                        return string.Join(";", strings);
                    }
                    return UniversalConverter.ConvertTo<string>(_value);
                }
            }
            set => UpdatePlc(value);
        }

        public DateTime ValueDateTime
        {
            get
            {
                var client = _connection?.PlcClient;
                if (client == null) return DateTime.MinValue;

                lock (client)
                {
                    return _value == null
                        ? DateTime.MinValue
                        : UniversalConverter.ConvertTo<DateTime>(_value);
                }
            }
            set => UpdatePlc(value);
        }

    }
}
