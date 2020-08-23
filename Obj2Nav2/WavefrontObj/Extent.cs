﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obj2Nav2.WavefrontObj
{
    class Extent
    {
		public double XMax { get; set; }
		public double XMin { get; set; }
		public double YMax { get; set; }
		public double YMin { get; set; }
		public double ZMax { get; set; }
		public double ZMin { get; set; }

		public double XSize { get { return XMax - XMin; } }
		public double YSize { get { return YMax - YMin; } }
		public double ZSize { get { return ZMax - ZMin; } }
	}
}
