using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct ValueGroup//Unity doesn't support Tuple
{
    public int index;
    public float value;
    public ValueGroup(int i, float v)
    {
        index = i;
        value = v;
    }
}
