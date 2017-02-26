using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

interface IRecordable
{
    XElement RecordValuesToSaveFile();
    void RecordValuesFromSaveFile(XElement element);
}
