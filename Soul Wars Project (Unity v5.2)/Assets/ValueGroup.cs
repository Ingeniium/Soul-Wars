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

    public static bool operator==(ValueGroup lhs, ValueGroup rhs)
    {
        return lhs.value == rhs.value && lhs.index == rhs.index; 
    }

    public static bool operator !=(ValueGroup lhs, ValueGroup rhs)
    {
        return !(lhs == rhs);
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

    public static bool operator ==(ValueGroup<I,T> lhs, ValueGroup<I,T> rhs)
    {
        return lhs.value.Equals(rhs.value) && lhs.index.Equals(rhs.index);
    }

    public static bool operator !=(ValueGroup<I,T> lhs, ValueGroup<I,T> rhs)
    {
        return !(lhs == rhs);
    }

}
