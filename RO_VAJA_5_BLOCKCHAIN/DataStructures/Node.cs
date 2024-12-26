using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Node: INotifyPropertyChanged
    {
        private string _id { get; set; }
        private string _iP { get; set; }
        private int _port { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public IPEndPoint IPEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.Parse(_iP), _port);
            }
        }
        public Node()
        { 
            this._id = "0";
            this._iP = "127.0.0.1";
            this._port = 10548;
        }
        public Node(string id)
        {
            this._id = id;
            this._iP = "127.0.0.1";
            this._port = 10548;
        }
        public Node(string id, int _Port)
        {
            this._id = id;
            this._iP = "127.0.0.1";
            this._port = _Port;
        }
        public Node(string id, string IP, int Port)
        {
            this._id = id;
            this._iP = IP;
            this._port = Port;
        }
        public string Id
        {
            get { return _id; }
        }
        public string IP
        {
            get { return _iP; }
            set
            {
                _iP = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IP"));
            }
        }
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Port"));
            }
        }
        public static string GenerateUUID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
