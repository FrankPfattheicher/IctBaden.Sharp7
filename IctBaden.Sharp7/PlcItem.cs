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
        private readonly S7Client _client;
        private readonly PlcDataInfo _info;

        public PlcItem(S7Client client, PlcDataInfo info)
        {
            _client = client;
            _info = info;
        }

        // ReSharper disable once InconsistentlySynchronizedField
        public bool IsBoolean => _info.PlcDataType == PlcDataTypes.X;
        public bool IsString => _info.PlcDataType == PlcDataTypes.STRING;
        public bool IsNumeric => (_info.PlcDataType == PlcDataTypes.B) 
                                || (_info.PlcDataType == PlcDataTypes.DINT)
                                || (_info.PlcDataType == PlcDataTypes.INT);

        private object _value;
        private object _oldValue;

        public event Action<PlcItem> ValueChanged;

        public void UpdateValueFromPlc()
        {
            lock (_client)
            {
                var retry = 3;
                while (retry-- >= 0)
                {
                    if (!_client.Connected)
                    {
                        TronTrace.TraceWarning("PLC connection lost");
                        _client.Connect();
                    }

                    if (ReadValue()) return;
                }
                TronTrace.TraceError($"PlcItem.UpdateValueFromPlc failed to read {_info.Name}");
            }
        }
        private bool ReadValue()
        {
            var buffer = new byte[Math.Max(64, _info.Size)];
            var result = _client.DBRead(_info.DbNumber, _info.Offset, _info.Size, buffer);
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
            lock (_client)
            {
                var retry = 3;
                while (retry-- >= 0)
                {
                    if (!_client.Connected)
                    {
                        TronTrace.TraceWarning("PLC connection lost");
                        _client.Connect();
                    }

                    if (WriteValue(value)) return;
                }
            }
            TronTrace.TraceError($"PlcItem.UpdateValueFromPlc failed to write {_info.Name}");
        }

        private bool WriteValue(object value)
        {
            if (!_client.Connected) return false;

            var buffer = new byte[_info.Size];
            _value = value;

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
                    S7.SetStringAt(buffer, 0, _info.Size, UniversalConverter.ConvertTo<string>(_value));
                    break;
            }

            var result = _client.DBWrite(_info.DbNumber, _info.Offset, _info.Size, buffer);
            if (result != 0)
            {
                TronTrace.TraceError($"PlcItem.DBWrite({_info.DbNumber}, {_info.Offset}) failed - {result} {PlcResult.GetResultText(result)}");
                return false;
            }

            return true;
        }

        public bool ValueBool
        {
            get
            {
                lock (_client)
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
                lock (_client)
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
                lock (_client)
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
                lock (_client)
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
