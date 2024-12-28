using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Blockchain: INotifyPropertyChanged
    {
        public ObservableCollection<Connection> _connections { get; private set; } = new ObservableCollection<Connection>();
        public ObservableCollection<Block> _ledger = new ObservableCollection<Block>();
        public ObservableCollection<Block> CostumBlockQueue = new ObservableCollection<Block>();
        public event PropertyChangedEventHandler? PropertyChanged;
        private StandardConnectionServer _stdServer = new StandardConnectionServer(10548);
        public bool RunLedgerUpdate = true;
        public bool GenerateBlocksRunning = true;
        private string _localNodeId = Node.GenerateUUID();
        private readonly int SecondsBetweenNewBlocks = 10;
        private readonly int BlocksBetweenDifficultyChange = 10;
        private readonly int LEDGER_TOO_SHORT = 778;
        private readonly int LEDGER_TOO_LONG = 887;
        public int _difficulty { get; private set; } = 5;
        public int Difficulty
        {
            get { return _difficulty; }
            private set
            {
                _difficulty = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Difficulty"));
            }
        }
        public Blockchain() {
            Connection.LocalNodeId = _localNodeId;
            StartStdServer();
            Task.Run(() => UpdateLedger());
            Task.Run(() => GenerateNewBlocks());
        }
        public Blockchain(int StdServerPort) {
            _stdServer._port = StdServerPort;
            Connection.LocalNodeId = _localNodeId;
            StartStdServer();
            Task.Run(() => UpdateLedger());
            Task.Run(() => GenerateNewBlocks());
        }
        public StandardConnectionServer StdServer
        {
            get { return _stdServer; }
            set
            {
                _stdServer = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StdServer"));
            }
        }
        public bool AddNode(Node node)
        {
           if (_connections.Any(x => x._remoteNode == node) && _connections.Any(x => x._localNode == node))
           {
               return false;
           }
           Connection connection = new Connection(node, Ledger);
           Connections.Add(connection);
           return true;
        }
        public void StartStdServer() {
           Task.Run(() => _stdServer.StartStandardServer(_connections, Ledger));
        }
        public bool SendBlock(Block block)
        {
           foreach (Connection connection in _connections)
           {
               connection.Send(block.ToByteArray());
           }
           return false;
        }
        public ObservableCollection<Block> Ledger
        {
           get { return _ledger; }
           set
           {
               _ledger = value;
               PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ledger"));
           }
        }
        public string LocalNodeId
        {
            get { return _localNodeId; }
        }
        public void SetLedger(ObservableCollection<Block> ledger)
        {
            Ledger = ledger;
        }
        public ObservableCollection<Block> GetLedger()
        {
            return Ledger;
        }
        private async Task UpdateLedger()
        {
            while (RunLedgerUpdate)
            {
                await Task.Delay(5);
                if (Connection.NewDataRecieved)
                {
                    foreach (Connection connection in _connections)
                    {
                        while (connection.RecievedData())
                        {
                            Block block = new Block(connection.PopRecieved());
                            int flags = 0;
                            if (ValidateBlock(block, out flags))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Ledger.Add(block);
                                });
                            }
                            else
                            {
                                if (flags == LEDGER_TOO_SHORT)
                                {
                                    //Če je naš ledger prekratek, dobimo daljšega
                                    connection.ReceveLongerLedger();
                                }
                                else if (flags == LEDGER_TOO_LONG)
                                {
                                    //Če je naš ledger predolg, pošljemo daljšega
                                    connection.SendLongerLedger();
                                }
                            }
                        }
                        Connection.NewDataRecieved = false;
                    }
                }
            }
        }
        private bool ValidateBlock(Block block, out int flags)
        {
            flags = 0;
            if (block.Index == 0)
            {
                return true;
            }
            Block previousBlock = _ledger[_ledger.Count - 1];
            if(block.Index != previousBlock.Index + 1)
            {
                if(block.Index < previousBlock.Index)
                {
                    flags = LEDGER_TOO_LONG;
                }
                else if(block.Index > previousBlock.Index + 1)
                {
                    flags = LEDGER_TOO_SHORT;
                }
                return false;
            }
            if (block.PreviousHash != previousBlock.Hash)
            {
                return false;
            }
            if (block.Hash != block.GetHash())
            {
                return false;
            }
            return true;
        }
        public ObservableCollection<Connection> Connections
        {
            get { return _connections; }
            set
            {
                _connections = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Connections"));
            }
        }
        private void MineBlock(Block block)
        {
            block.GenerateHash();
            Application.Current.Dispatcher.Invoke(() =>
            {
                _ledger.Add(block);
                foreach (Connection connection in _connections)
                {
                    connection.Send(block.ToByteArray());
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ledger"));
            });
        }
        private async Task GenerateNewBlocks()
        {
            int numBlocksBeforeDifficultyChange = BlocksBetweenDifficultyChange;
            while (GenerateBlocksRunning)
            {
                await Task.Delay(100);
                Block block;
                if (numBlocksBeforeDifficultyChange <= 0)
                {
                    double averageTime = 0.0;
                    ObservableCollection<Block>? blocks = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        while (blocks == null)
                            blocks = new ObservableCollection<Block>(_ledger);
                    });
                    for (int i = blocks.Count - 9; i < blocks.Count; i++)
                    {
                        averageTime += (blocks[i].TimeStamp - blocks[i - 1].TimeStamp).TotalSeconds;
                    }
                    averageTime /= 10;
                    if (averageTime < SecondsBetweenNewBlocks)
                    {
                        Difficulty++;
                    }
                    else
                    {
                        Difficulty--;
                    }
                    numBlocksBeforeDifficultyChange = BlocksBetweenDifficultyChange;
                }
                if (CostumBlockQueue.Count > 0)
                {
                    block = CostumBlockQueue.First();
                    block.Difficulty = _difficulty;
                    MineBlock(block);
                    CostumBlockQueue.Remove(block);
                    numBlocksBeforeDifficultyChange--;
                }
                else
                {
                    block = new Block(Ledger.Count, _difficulty, RandomString(32), System.DateTime.Now, GetLastBlockHash());
                    MineBlock(block);
                    numBlocksBeforeDifficultyChange--;
                }
            }
        }
        public string GetLastBlockHash()
        {
            if (_ledger.Count == 0)
                return "0";
            return _ledger.Last().Hash;
        }
        public void AddBlock(Block block)
        {
            CostumBlockQueue.Add(block);
        }
        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder sb = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }
    }
}
