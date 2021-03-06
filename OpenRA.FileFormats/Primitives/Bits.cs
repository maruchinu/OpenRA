#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.FileFormats
{
	static class BitAllocator<T> where T : struct
	{
		static int nextVal = 1;
		static Cache<string,int> bits;

		static BitAllocator() {	bits = new Cache<string, int>( _ => Allocate() ); }

		static int Allocate()
		{
			if (nextVal == 0)
				throw new InvalidOperationException(
					"Too many values in BitAllocator<{0}>".F(typeof(T).Name));

			var val = nextVal;
			nextVal <<= 1;
			return val;
		}

		public static int GetValue(string[] val)
		{
			return val.Select( a => bits[a] ).Aggregate( 0, (a,b) => a | b );
		}

		public static IEnumerable<string> GetStrings(int val)
		{
			for( var i = 0; i < 32; i++ )
			{
				var x = 1 << i;
				if ((val & x) != 0)
					yield return bits.Single( a => a.Value == x ).Key;
			}
		}
	}

	public struct Bits<T> where T : struct
	{
		public int Value;

		public Bits(string[] val) { Value = BitAllocator<T>.GetValue(val); }
		public Bits(Bits<T> other) { Value = other.Value; }

		public override string ToString()
		{
			return string.Join(",", BitAllocator<T>.GetStrings(Value).ToArray());
		}

		public override int GetHashCode() { return Value.GetHashCode(); }
	}
}
