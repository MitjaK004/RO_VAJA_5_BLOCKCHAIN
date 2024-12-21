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
        private static int idCounter = 1;
        private int _id { get; set; }
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
            this._id = idCounter++;
            this._iP = "127.0.0.1";
            this._port = 10548;
        }
        public Node(int _Port)
        {
            this._id = idCounter++;
            this._iP = "127.0.0.1";
            this._port = _Port;
        }
        public Node(string IP, int Port)
        {
            this._id = idCounter++;
            this._iP = IP;
            this._port = Port;
        }
        public Node(int id, string IP, int Port)
        {
            this._id = id;
            this._iP = IP;
            this._port = Port;
        }
        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Id"));
            }
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
    }
}
