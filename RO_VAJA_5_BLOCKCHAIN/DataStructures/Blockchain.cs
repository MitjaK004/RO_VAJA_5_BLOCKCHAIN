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
using System.Windows.Media.Animation;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Blockchain: INotifyPropertyChanged
    {
        public ObservableCollection<Connection> _connections { get; private set; } = new ObservableCollection<Connection>();
        public ObservableCollection<Block> _ledger = new ObservableCollection<Block>();
        public ObservableCollection<DisplayLedgerItem> _displayLedger = new ObservableCollection<DisplayLedgerItem>();
        public ObservableCollection<Block> CostumBlockQueue = new ObservableCollection<Block>();
        public event PropertyChangedEventHandler? PropertyChanged;
        private StandardConnectionServer _stdServer = new StandardConnectionServer(10548);
        public bool RunLedgerUpdate = true;
        public bool GenerateBlocksRunning = true;
        private string _localNodeId = Node.GenerateUUID();
        private readonly int SecondsBetweenNewBlocks = 18;
        private readonly int BlocksBetweenDifficultyChange = 10;
        private readonly uint LEDGER_TOO_SHORT = 8;
        private readonly uint LEDGER_TOO_LONG = 4;
        private readonly uint COMULATIVE_DIFFICULTY_MISMATCH = 12;
        private readonly uint LOCAL_LEDGER_BAD = 48;
        private readonly uint LOCAL_LEDGER_GOOD = 3;
        private readonly uint TIMESTAMP_MISMATCH = 64;
        public bool _Pause = false;
        public bool _PauseMining = false;
        public int _difficulty { get; private set; } = 5;
        public int Difficulty
        {
            get { return _difficulty; }
            set
            {
                _difficulty = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Difficulty"));
            }
        }
        public ObservableCollection<DisplayLedgerItem> DisplayLedger
        {
            get { return _displayLedger; }
            set
            {
                _displayLedger = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DisplayLedger"));
            }
        }
        public Blockchain() {
            Connection.LocalNodeId = _localNodeId;
            Connection.Pause = Pause;
            Connection.Resume = Resume;
            StartStdServer();
            Task.Run(() => UpdateLedger());
            Task.Run(() => GenerateNewBlocks());
        }
        public Blockchain(int StdServerPort) {
            _stdServer._port = StdServerPort;
            Connection.LocalNodeId = _localNodeId;
            Connection.Pause = Pause;
            Connection.Resume = Resume;
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
           Connection connection = new Connection(node, SetLedger, GetLedger);
           Connections.Add(connection);
           return true;
        }
        public void StartStdServer() {
           Task.Run(() => _stdServer.StartStandardServer(_connections, SetLedger, GetLedger));
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
        private void AddLedgerItem(Block b)
        {
            Ledger.Add(b);
            DisplayLedger.Add(new DisplayLedgerItem(_ledger.Last(), true));
        }
        public string LocalNodeId
        {
            get { return _localNodeId; }
        }
        public void SetLedger(ObservableCollection<Block> ledger)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _Pause = true;
                Ledger.Clear();
                DisplayLedger.Clear();
                foreach (Block block in ledger)
                {
                    AddLedgerItem(block);
                }
                Difficulty = ledger.Last().Difficulty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ledger"));
                _Pause = false;
            });
        }
        public ObservableCollection<Block> GetLedger()
        {
            return Ledger;
        }
        public void Pause()
        {
            _Pause = true;
        }
        public void Resume()
        {
            _Pause = false;
        }
        public void PauseMining()
        {
            _PauseMining = true;
        }
        public void ResumeMining()
        {
            _PauseMining = false;
        }
        private async Task UpdateLedger()
        {
            while (RunLedgerUpdate)
            {
                await Task.Delay(5);
                while (_Pause) { await Task.Delay(250); }
                if (Connection.NewDataRecieved)
                {
                    foreach (Connection connection in _connections)
                    {
                        while (_Pause) { await Task.Delay(250); }
                        while (connection.RecievedData())
                        {
                            while (_Pause) { await Task.Delay(250); }
                            Block block = new Block(connection.PopRecieved());
                            uint flags = 0;
                            if (ValidateBlock(block, out flags))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Difficulty = block.Difficulty;
                                    //Ledger.Add(block);
                                    AddLedgerItem(block);
                                });
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() => { 
                                    DisplayLedger.Add(new DisplayLedgerItem(block, false));
                                });
                                if (flags == (LEDGER_TOO_SHORT | LOCAL_LEDGER_BAD)) {
                                    connection.ReceveRemoteLedger();
                                    Pause();
                                }
                                else if(flags == (LEDGER_TOO_SHORT | LOCAL_LEDGER_GOOD)) {
                                    connection.SendLocalLedger();
                                    Pause();
                                }
                                else if(flags == (LEDGER_TOO_LONG | LOCAL_LEDGER_BAD)) {
                                    connection.ReceveRemoteLedger();
                                    Pause();
                                }
                                else if(flags == (LEDGER_TOO_LONG | LOCAL_LEDGER_GOOD)) {
                                    connection.SendLocalLedger();
                                    Pause();
                                }
                                else if(flags == (COMULATIVE_DIFFICULTY_MISMATCH | LOCAL_LEDGER_BAD)) {
                                    connection.ReceveRemoteLedger();
                                    Pause();
                                }
                                else if(flags == (COMULATIVE_DIFFICULTY_MISMATCH | LOCAL_LEDGER_GOOD)) {
                                    connection.SendLocalLedger();
                                    Pause();
                                }
                            }
                            while (_Pause) { await Task.Delay(250); }
                        }
                        Connection.NewDataRecieved = false;
                    }
                }
            }
        }
        private bool ValidateBlock(Block block, out uint flags)
        {
            flags = 0;
            if (block.Index == 0 && _ledger.Count == 0)
            {
                return true;
            }
            Block previousBlock = _ledger[_ledger.Count - 1];
            long localComDiff = (previousBlock.ComulativeDifficulty + (long)Math.Pow(2, block.Difficulty));
            if (block.Index != previousBlock.Index + 1)
            {
                if(block.Index < previousBlock.Index)
                {
                    flags = LEDGER_TOO_LONG;
                    if (localComDiff > block.ComulativeDifficulty)
                    {
                        flags = flags | LOCAL_LEDGER_GOOD;
                    }
                    else
                    {
                        flags = flags | LOCAL_LEDGER_BAD;
                    }
                }
                else if(block.Index > previousBlock.Index + 1)
                {
                    flags = LEDGER_TOO_SHORT;
                    if (localComDiff > block.ComulativeDifficulty)
                    {
                        flags = flags | LOCAL_LEDGER_GOOD;
                    }
                    else
                    {
                        flags = flags | LOCAL_LEDGER_BAD;
                    }
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
            if((block.TimeStamp - previousBlock.TimeStamp).Seconds > 60)
            {
                flags = TIMESTAMP_MISMATCH;
                return false;
            }
            if ((previousBlock.TimeStamp - block.TimeStamp).Seconds > 60)
            {
                flags = TIMESTAMP_MISMATCH;
                return false;
            }
            if (localComDiff != block.ComulativeDifficulty)
            {
                flags = COMULATIVE_DIFFICULTY_MISMATCH;
                if(localComDiff > block.ComulativeDifficulty)
                {
                    flags = flags | LOCAL_LEDGER_GOOD;
                }
                else
                {
                    flags = flags | LOCAL_LEDGER_BAD;
                }
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
            block.GenerateHash(ref _Pause, ref _PauseMining);
            Application.Current.Dispatcher.Invoke(() =>
            {
                //Ledger.Add(block);
                uint flags = 0;
                if (ValidateBlock(block, out flags))
                {
                    AddLedgerItem(block);
                    foreach (Connection connection in _connections)
                    {
                        connection.Send(block.ToByteArray());
                    }
                }
                else
                {
                    DisplayLedger.Add(new DisplayLedgerItem(block, false));
                    foreach (Connection connection in _connections)
                    {
                        connection.Send(block.ToByteArray());
                    }
                }
            });
        }
        private async Task GenerateNewBlocks()
        {
            int numBlocksBeforeDifficultyChange = BlocksBetweenDifficultyChange;
            while (GenerateBlocksRunning)
            {
                await Task.Delay(100);
                while (_Pause || _PauseMining) { await Task.Delay(250); }
                Block block;
                if (numBlocksBeforeDifficultyChange <= 0)
                {
                    double timeTaken = 0.0, timeExpected = 0.0;
                    ObservableCollection<Block>? blocks = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        while (blocks == null)
                            blocks = new ObservableCollection<Block>(_ledger);
                    });
                    Block previousAdjustmentBlock = blocks[blocks.Count - BlocksBetweenDifficultyChange];
                    timeTaken = (blocks.Last().TimeStamp - previousAdjustmentBlock.TimeStamp).TotalSeconds;
                    timeExpected = SecondsBetweenNewBlocks * BlocksBetweenDifficultyChange;
                    if (timeTaken < (SecondsBetweenNewBlocks / 2))
                    {
                        Difficulty++;
                    }
                    else if (timeTaken > (SecondsBetweenNewBlocks * 2))
                    {
                        if (Difficulty > 1)
                        {
                            Difficulty--;
                        }
                    }
                    else { /*NOP*/ }
                    numBlocksBeforeDifficultyChange = BlocksBetweenDifficultyChange;
                }
                if (CostumBlockQueue.Count > 0)
                {
                    block = CostumBlockQueue.First();
                    block.Difficulty = _difficulty;
                    block.ComulativeDifficulty = GetComulativeDifficulty();
                    MineBlock(block);
                    CostumBlockQueue.Remove(block);
                    numBlocksBeforeDifficultyChange--;
                }
                else
                {
                    block = new Block(Ledger.Count, _difficulty, GetComulativeDifficulty(), RandomString(32), System.DateTime.Now, GetLastBlockHash());
                    MineBlock(block);
                    numBlocksBeforeDifficultyChange--;
                }
            }
        }
        private long GetComulativeDifficulty()
        {
            long difficulty = 0;
            foreach(Block bl in _ledger)
            {
                difficulty += (long)Math.Pow(2, bl.Difficulty);
            }
            return difficulty;
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
