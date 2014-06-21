﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jhu.SkyQuery.Format.Fits
{
    public enum FitsFileMode
    {
        Unknown,
        Read,
        Write
    }

    public enum Endianness
    {
        LittleEndian,
        BigEndian,
    }
}