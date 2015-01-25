using System;
using System.Collections.Generic;
using Sip.Message;
using Base.Message;

namespace Sip.Server
{
    class TrunkManager
        : ITrunkManager
    {
        public event Action<Trunk> TrunkAdded;
        public event Action<Trunk> TrunkRemoved;
        public event Action<Trunk> TrunkUpdated;

        private List<Trunk> trunks;
        private object sync;

        public TrunkManager()
        {
            sync = new object();
            trunks = new List<Trunk>();
        }

        public void Clear()
        {
            lock (sync)
            {
                foreach (var trunk in trunks)
                    trunk.Changed -= Trunk_Changed;

                trunks.Clear();
            }
        }

        public void Add(Trunk trunk)
        {
            lock (sync)
            {
                trunk.Changed += Trunk_Changed;

                trunks.Add(trunk);
                OnTrunkAdded(trunk);
            }
        }

        private void Trunk_Changed(Trunk trunk)
        {
            OnTrunkUpdated(trunk);
        }

        public Trunk GetTrunk(string username, string hostname)
        {
            lock (sync)
            {
                foreach (var trunk in trunks)
                    if (trunk.Username == username && trunk.Domain.ToString() == hostname)
                        return trunk;
                return null;
            }
        }

        public Trunk GetTrunkByDomain(ByteArrayPart host)
        {
            lock (sync)
            {
                for (int i = 0; i < trunks.Count; i++)
                    if (trunks[i].Domain.Equals(host))
                        return trunks[i].IsConnected ? trunks[i] : null;
                return null;
            }
        }

        public Trunk GetTrunkById(int id)
        {
            lock (sync)
            {
                for (int i = 0; i < trunks.Count; i++)
                {
                    if (trunks[i].Id == id)
                        return trunks[i];
                }

                return null;
            }
        }

        private void OnTrunkAdded(Trunk trunk)
        {
            var method = TrunkAdded;
            if (method != null)
                method(trunk);
        }

        private void OnTrunkRemoved(Trunk trunk)
        {
            var method = TrunkRemoved;
            if (method != null)
                method(trunk);
        }

        private void OnTrunkUpdated(Trunk trunk)
        {
            var method = TrunkUpdated;
            if (method != null)
                method(trunk);
        }
    }
}
