﻿using System.Net;
using System.Text;

using Darkages.Models;

namespace Darkages.Network.Formats.Models.ServerFormats
{
    public class ServerFormat03 : NetworkFormat
    {
        /// <summary>
        /// Redirect
        /// </summary>
        public ServerFormat03()
        {
            Encrypted = false;
            Command = 0x03;
        }

        public bool CalledFromMethod = false;
        public IPEndPoint EndPoint { get; init; }
        public Redirect Redirect { get; init; }
        private byte Remaining => (byte)(Redirect.Salt.Length + Redirect.Name.Length + 7);

        public override void Serialize(NetworkPacketReader reader) { }

        public override void Serialize(NetworkPacketWriter writer)
        {
            if (!CalledFromMethod) return;

            writer.Write(EndPoint);
            writer.Write(Remaining);
            writer.Write(Convert.ToByte(Redirect.Seed));
            writer.Write((byte)Redirect.Salt.Length);
            writer.Write(Encoding.UTF8.GetBytes(Redirect.Salt));
            writer.WriteStringA(Redirect.Name);
            writer.Write(Convert.ToInt32(Redirect.Serial));
        }
    }
}