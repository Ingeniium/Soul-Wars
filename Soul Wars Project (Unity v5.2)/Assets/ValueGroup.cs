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

    public override string ToString()
    {
        return index.ToString() + " : " + value.ToString();
    }
}

public struct ValueGroup<I,T>
{
    public I index;
    public T value;
    public ValueGroup(I i,T t)
    {
       index = i;
       value = t;
    }

    public override string ToString()
    {
        return index.ToString() + " : " + value.ToString();
    }

}
