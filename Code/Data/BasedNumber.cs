using System;
using System.Collections.Generic;
using System.Text;

public partial class BasedNumber
{
    byte numberBase;
    private Dictionary<byte, byte> value = new Dictionary<byte, byte>();
    // Rcommended representation:
    // 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ
    string representation;
    byte highestPosition = 0;

    public BasedNumber(byte _numberBase, string _representation)
    {
        if (_numberBase < 2)
        {
            throw new ArgumentException("Base of number must be at least 2");
        }
        if (_representation.Length != _numberBase)
        {
            throw new ArgumentException("Representation must be the same amount of characters as the base of the number");
        }

        numberBase = _numberBase;
        this.representation = _representation;
    }

    public Dictionary<byte, byte> GetValue()
    {
        return value;
    }

    public Dictionary<byte, byte> Add(Dictionary<byte, byte> addedValue)
    {
        foreach (KeyValuePair<byte, byte> position in addedValue)
        {
            byte newValue = (byte)(value[position.Key] + position.Value);
            if (value[position.Key] >= numberBase)
            {
                if (position.Key == byte.MaxValue)
                {
                    throw new OverflowException("The number got too big,");
                }
                newValue -= numberBase;
                Dictionary<byte, byte> newAddedValue = new Dictionary<byte, byte>
                    { { (byte)(position.Key + 1), newValue } };
                Add(newAddedValue);
            }
            value[position.Key] = newValue;
        }
        return value;
    }

    public override string ToString()
    {
        StringBuilder output = new StringBuilder();
        for (byte i = highestPosition; i >= 0; i--)
        {
            output.Append(representation[value[i]]);
        }
        return base.ToString();
    }
}
