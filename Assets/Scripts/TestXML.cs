using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class Test
{
   public string name;
}

[System.Serializable]
public class TestXML
{
   public int id;

   public Test test;

   public List<int> list=new List<int>();
}
