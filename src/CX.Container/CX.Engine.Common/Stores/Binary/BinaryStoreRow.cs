namespace CX.Engine.Common.Stores.Binary;

public readonly record struct BinaryStoreRow(string Key, byte[] Value);