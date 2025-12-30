using System;
using Unity.Collections;
using Unity.Netcode;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Network-serializable inventory slot data
    /// </summary>
    public struct NetworkedInventorySlot : INetworkSerializable, IEquatable<NetworkedInventorySlot>
    {
        public FixedString64Bytes ItemId;
        public int Amount;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ItemId);
            serializer.SerializeValue(ref Amount);
        }

        public bool Equals(NetworkedInventorySlot other)
        {
            return ItemId.Equals(other.ItemId) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkedInventorySlot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, Amount);
        }
    }
}
