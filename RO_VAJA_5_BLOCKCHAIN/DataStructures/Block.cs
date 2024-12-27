﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RO_VAJA_5_BLOCKCHAIN.DataStructures
{
    public class Block
    {
        public int Index { get; } = 0;
        public int Difficulty { get; } = 1;
        public string Data { get; } = "";
        public DateTime TimeStamp { get; } = DateTime.Now;
        public string Hash { get; private set; } = "NULL";
        public string PreviousHash { get; } = "";
        public Block(int index, int dificulty, string data, DateTime timeStamp, string hash, string previousHash)
        {
            Index = index;
            Difficulty = dificulty;
            Data = data;
            TimeStamp = timeStamp;
            Hash = hash;
            PreviousHash = previousHash;
        }
        public Block(int index, int dificulty, string data, DateTime timeStamp, string previousHash)
        {
            Index = index;
            Data = data;
            TimeStamp = timeStamp;
            PreviousHash = previousHash;
            Difficulty = dificulty;
        }
        public Block(byte[] data) {
            List<byte> byteList = new List<byte>(data);
            Index = BitConverter.ToInt32(byteList.GetRange(0, 4).ToArray(), 0);
            byteList.RemoveRange(0, 5);
            Difficulty = BitConverter.ToInt32(byteList.GetRange(0, 4).ToArray(), 0);
            byteList.RemoveRange(0, 5);
            Data = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
            byteList.RemoveRange(0, byteList.IndexOf(10) + 1);
            TimeStamp = new DateTime(BitConverter.ToInt64(byteList.GetRange(0, 8).ToArray(), 0));
            byteList.RemoveRange(0, 9);
            Hash = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
            byteList.RemoveRange(0, byteList.IndexOf(10) + 1);
            PreviousHash = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
        }
        public Block() { }
        public string ToString(string separator = "")
        {
            return $"{Index}{separator}{Difficulty}{separator}{Data}{separator}{TimeStamp}{separator}{Hash}{separator}{PreviousHash}";
        }
        public byte[] ToByteArray()
        {
            List<byte> byteList = new List<byte>();

            byteList.AddRange(BitConverter.GetBytes(Index));
            byteList.Add(10);
            byteList.AddRange(BitConverter.GetBytes(Difficulty));
            byteList.Add(10);
            byteList.AddRange(Encoding.UTF8.GetBytes(Data));
            byteList.Add(10);
            byteList.AddRange(BitConverter.GetBytes(TimeStamp.Ticks));
            byteList.Add(10);
            byteList.AddRange(Encoding.UTF8.GetBytes(Hash));
            byteList.Add(10);
            byteList.AddRange(Encoding.UTF8.GetBytes(PreviousHash));
            byteList.Add(10);

            return byteList.ToArray();
        }
        public static Block FromByteArray(byte[] data) {
            List<byte> byteList = new List<byte>(data);
            int id = BitConverter.ToInt32(byteList.GetRange(0, 4).ToArray(), 0);
            byteList.RemoveRange(0, 5);
            int difficulty = BitConverter.ToInt32(byteList.GetRange(0, 4).ToArray(), 0);
            byteList.RemoveRange(0, 5);
            string dataString = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
            byteList.RemoveRange(0, byteList.IndexOf(10) + 1);
            long ticks = BitConverter.ToInt64(byteList.GetRange(0, 8).ToArray(), 0);
            byteList.RemoveRange(0, 9);
            string hash = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
            byteList.RemoveRange(0, byteList.IndexOf(10) + 1);
            string previousHash = Encoding.UTF8.GetString(byteList.GetRange(0, byteList.IndexOf(10)).ToArray());
        
            return new Block(id, difficulty, dataString, new DateTime(ticks), hash, previousHash);
        }
        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
        public bool GenerateHash()
        {
            if(Hash != "NULL")
                return false;
            string target = new string('0', Difficulty);
            int nonce = 0;
            while (!Hash.StartsWith(target))
            {
                nonce++;
                Hash = GetHashString($"{this.ToString()}{nonce}");
            }
            return true;
        }
    }
}
