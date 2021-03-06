﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA
{
	class GivesBountyInfo : TraitInfo<GivesBounty>
	{
		public readonly int Percentage = 10;
		public readonly int LevelMod = 125;
	}

	class GivesBounty : INotifyKilled
	{
		int GetMultiplier(Actor self)
		{
			// returns 100's as 1, so as to keep accuracy for longer.
			var info = self.Info.Traits.Get<GivesBountyInfo>();
			var gainsExp = self.TraitOrDefault<GainsExperience>();
			if (gainsExp == null)
				return 100;

			var slevel = gainsExp.Level;
			return (slevel > 0) ? slevel * info.LevelMod : 100;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			var info = self.Info.Traits.Get<GivesBountyInfo>();
			// Prevent TK from giving Bounty
			if (e.Attacker == null || e.Attacker.Destroyed || e.Attacker.Owner.Stances[self.Owner] == Stance.Ally)
				return;

			var cost = self.GetSellValue();
			// 2 hundreds because of GetMultiplier and info.Percentage.
			var bounty = cost * GetMultiplier(self) * info.Percentage / 10000;

			if (bounty > 0 && e.Attacker.World.LocalPlayer != null && e.Attacker.Owner.Stances[e.Attacker.World.LocalPlayer] == Stance.Ally)
				e.Attacker.World.AddFrameEndTask(w => w.Add(new CashTick(bounty, 20, 1, self.CenterLocation, e.Attacker.Owner.ColorRamp.GetColor(0))));

			e.Attacker.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(bounty);
		}

	}
}
