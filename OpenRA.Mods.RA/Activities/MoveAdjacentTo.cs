﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Activities
{
	public class MoveAdjacentTo : Activity
	{
		readonly Target target;

		public MoveAdjacentTo( Actor target ) { this.target = Target.FromActor(target); }
		public MoveAdjacentTo( Target target ) { this.target = target; }

		public override Activity Tick( Actor self )
		{
			if( IsCanceled || !target.IsValid) return NextActivity;

			var mobile = self.Trait<Mobile>();

			var ps1 = new PathSearch( self.World, mobile.Info, self.Owner )
			{
				checkForBlocked = true,
				heuristic = location => 0,
				inReverse = true
			};

			foreach( var cell in Util.AdjacentCells(target) )
				if (cell == self.Location)
					return NextActivity;
				else
					ps1.AddInitialCell( cell );

			ps1.heuristic = PathSearch.DefaultEstimator( mobile.toCell );

			var ps2 = PathSearch.FromPoint( self.World, mobile.Info, self.Owner, mobile.toCell, Util.CellContaining(target.CenterLocation), true );
			var ret = self.World.WorldActor.Trait<PathFinder>().FindBidiPath( ps1, ps2 );

			return Util.SequenceActivities( mobile.MoveTo( () => ret ), this );
		}
	}
}
