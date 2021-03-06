// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class BringUpTests
{
    const int Pass = 100;
    const int Fail = -1;

    public static int Main()
    {
        int result = Pass;

        if (!TestValueTypeDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestVirtualDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestInterfaceDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestStaticOpenClosedDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        if (!TestMulticastDelegates())
        {
            Console.WriteLine("Failed");
            result = Fail;
        }

        return result;
    }

    public static bool TestValueTypeDelegates()
    {
        Console.Write("Testing delegates to value types...");

        {
            TestValueType t = new TestValueType { X = 123 };
            Func<string, string> d = t.GiveX;
            string result = d("MyPrefix");
            if (result != "MyPrefix123")
                return false;
        }

        {
            object t = new TestValueType { X = 456 };
            Func<string> d = t.ToString;
            string result = d();
            if (result != "456")
                return false;
        }

        {
            Func<int, TestValueType> d = TestValueType.MakeValueType;
            TestValueType result = d(789);
            if (result.X != 789)
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestVirtualDelegates()
    {
        Console.Write("Testing delegates to virtual methods...");

        {
            Mid t = new Mid();
            if (t.GetBaseDo()() != "Base")
                return false;

            if (t.GetDerivedDo()() != "Mid")
                return false;
        }

        {
            Mid t = new Derived();
            if (t.GetBaseDo()() != "Base")
                return false;

            if (t.GetDerivedDo()() != "Derived")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestInterfaceDelegates()
    {
        Console.Write("Testing delegates to interface methods...");

        {
            IFoo t = new ClassWithIFoo("Class");
            Func<int, string> d = t.DoFoo;
            if (d(987) != "Class987")
                return false;
        }

        {
            IFoo t = new StructWithIFoo("Struct");
            Func<int, string> d = t.DoFoo;
            if (d(654) != "Struct654")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestStaticOpenClosedDelegates()
    {
        Console.Write("Testing static open and closed delegates...");

        {
            Func<string, string, string> d = ExtensionClass.Combine;
            if (d("Hello", "World") != "HelloWorld")
                return false;
        }

        {
            Func<string, string> d = "Hi".Combine;
            if (d("There") != "HiThere")
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    public static bool TestMulticastDelegates()
    {
        Console.Write("Testing multicast delegates...");

        {
            ClassThatMutates t = new ClassThatMutates();

            Action d = t.AddOne;
            d();

            if (t.State != 1)
                return false;
            t.State = 0;

            d += t.AddTwo;
            d();

            if (t.State != 3)
                return false;
            t.State = 0;

            d += t.AddOne;
            d();

            if (t.State != 4)
                return false;
        }

        Console.WriteLine("OK");
        return true;
    }

    struct TestValueType
    {
        public int X;

        public string GiveX(string prefix)
        {
            return prefix + X.ToString();
        }

        public static TestValueType MakeValueType(int value)
        {
            return new TestValueType { X = value };
        }

        public override string ToString()
        {
            return X.ToString();
        }
    }

    class Base
    {
        public virtual string Do()
        {
            return "Base";
        }
    }

    class Mid : Base
    {
        public override string Do()
        {
            return "Mid";
        }

        public Func<string> GetBaseDo()
        {
            return base.Do;
        }

        public Func<string> GetDerivedDo()
        {
            return Do;
        }
    }

    class Derived : Mid
    {
        public override string Do()
        {
            return "Derived";
        }
    }

    interface IFoo
    {
        string DoFoo(int x);
    }

    class ClassWithIFoo : IFoo
    {
        string _prefix;

        public ClassWithIFoo(string prefix)
        {
            _prefix = prefix;
        }

        public string DoFoo(int x)
        {
            return _prefix + x.ToString();
        }
    }

    struct StructWithIFoo : IFoo
    {
        string _prefix;

        public StructWithIFoo(string prefix)
        {
            _prefix = prefix;
        }

        public string DoFoo(int x)
        {
            return _prefix + x.ToString();
        }
    }

    class ClassThatMutates
    {
        public int State;

        public void AddOne()
        {
            State++;
        }

        public void AddTwo()
        {
            State += 2;
        }
    }
}

static class ExtensionClass
{
    public static string Combine(this string s1, string s2)
    {
        return s1 + s2;
    }
}