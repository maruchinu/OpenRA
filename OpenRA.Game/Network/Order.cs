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
using System.IO;
using System.Linq;
using OpenRA.Network;

namespace OpenRA
{
	[Flags]
	enum OrderFields : byte
	{
		TargetActor = 0x01, 
		TargetLocation = 0x02, 
		TargetString = 0x04, 
		Queued = 0x08, 
		ExtraLocation = 0x10,
	}

	static class OrderFieldsExts
	{
		public static bool HasField(this OrderFields of, OrderFields f)
		{
			return (of & f) != 0;
		}
	}

	public sealed class Order
	{
		public readonly string OrderString;
		public readonly Actor Subject;
		public readonly bool Queued;
		public Actor TargetActor;
		public int2 TargetLocation;
		public string TargetString;
		public int2 ExtraLocation;
		public bool IsImmediate;
		
		public Player Player { get { return Subject.Owner; } }

		Order(string orderString, Actor subject, 
			Actor targetActor, int2 targetLocation, string targetString, bool queued, int2 extraLocation)
		{
			this.OrderString = orderString;
			this.Subject = subject;
			this.TargetActor = targetActor;
			this.TargetLocation = targetLocation;
			this.TargetString = targetString;
			this.Queued = queued;
			this.ExtraLocation = extraLocation;
		}
		
		// For scripting special powers
		public Order() 
			: this(null, null, null, int2.Zero, null, false, int2.Zero) { }
				 
		public Order(string orderString, Actor subject, bool queued) 
			: this(orderString, subject, null, int2.Zero, null, queued, int2.Zero) { }
		
		public Order(string orderstring, Order order)
			: this(orderstring, order.Subject, order.TargetActor, order.TargetLocation,
			       order.TargetString, order.Queued, order.ExtraLocation) {}
		
		public byte[] Serialize()
		{
			if (IsImmediate)		/* chat, whatever */
			{
				var ret = new MemoryStream();
				var w = new BinaryWriter(ret);
				w.Write((byte)0xfe);
				w.Write(OrderString);
				w.Write(TargetString);
				return ret.ToArray();
			}

			switch (OrderString)
			{
				// Format:
				//		u8    : orderID.
				//		            0xFF: Full serialized order.
				//		varies: rest of order.
				default:
					// TODO: specific serializers for specific orders.
					{
						var ret = new MemoryStream();
						var w = new BinaryWriter(ret);
						w.Write( (byte)0xFF );
						w.Write(OrderString);
						w.Write(UIntFromActor(Subject));

						OrderFields fields = 0;
						if (TargetActor != null) fields |= OrderFields.TargetActor;
						if (TargetLocation != int2.Zero) fields |= OrderFields.TargetLocation;
						if (TargetString != null) fields |= OrderFields.TargetString;
						if (Queued) fields |= OrderFields.Queued;
						if (ExtraLocation != int2.Zero) fields |= OrderFields.ExtraLocation;

						w.Write((byte)fields);

						if (TargetActor != null)
							w.Write(UIntFromActor(TargetActor));
						if (TargetLocation != int2.Zero)
							w.Write(TargetLocation);
						if (TargetString != null)
							w.Write(TargetString);
						if (ExtraLocation != int2.Zero)
							w.Write(ExtraLocation);

						return ret.ToArray();
					}
			}
		}

		public static Order Deserialize(World world, BinaryReader r)
		{
			switch (r.ReadByte())
			{
				case 0xFF:
					{
						var order = r.ReadString();
						var subjectId = r.ReadUInt32();
						var flags = (OrderFields)r.ReadByte();
						
						var targetActorId = flags.HasField(OrderFields.TargetActor) ?  r.ReadUInt32() : 0xffffffff;
						var targetLocation = flags.HasField(OrderFields.TargetLocation) ? r.ReadInt2() : int2.Zero;
						var targetString = flags.HasField(OrderFields.TargetString) ? r.ReadString() : null;
						var queued = flags.HasField(OrderFields.Queued);
						var extraLocation = flags.HasField(OrderFields.ExtraLocation) ? r.ReadInt2() : int2.Zero;

						Actor subject, targetActor;
						if( !TryGetActorFromUInt( world, subjectId, out subject ) || !TryGetActorFromUInt( world, targetActorId, out targetActor ) )
							return null;

						return new Order( order, subject, targetActor, targetLocation, targetString, queued, extraLocation);
					}

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new Order( name, null, false ) { IsImmediate = true, TargetString = data };
					}

				default:
					throw new NotImplementedException();
			}
		}
		
		public override string ToString()
		{
			return ("OrderString: \"{0}\" \n\t Subject: \"{1}\". \n\t TargetActor: \"{2}\" \n\t TargetLocation: {3}." +
				"\n\t TargetString: \"{4}\".\n\t IsImmediate: {5}.\n\t Player(PlayerName): {6}\n").F(
				OrderString, Subject, TargetActor != null ? TargetActor.Info.Name : null , TargetLocation, TargetString, IsImmediate, Player != null ? Player.PlayerName : null);
		}

		static uint UIntFromActor(Actor a)
		{
			if (a == null) return 0xffffffff;
			return a.ActorID;
		}

		static bool TryGetActorFromUInt(World world, uint aID, out Actor ret )
		{
			if( aID == 0xFFFFFFFF )
			{
				ret = null;
				return true;
			}
			else
			{
				foreach( var a in world.Actors.Where( x => x.ActorID == aID ) )
				{
					ret = a;
					return true;
				}
				ret = null;
				return false;
			}
		}

		// Named constructors for Orders.
		// Now that Orders are resolved by individual Actors, these are weird; you unpack orders manually, but not pack them.
		public static Order Chat(string text)
		{
			return new Order("Chat", null, false) { IsImmediate = true, TargetString = text};
		}

		public static Order TeamChat(string text)
		{
			return new Order("TeamChat", null, false) { IsImmediate = true, TargetString = text };
		}
		
		public static Order HandshakeResponse(string text)
		{
			return new Order("HandshakeResponse", null, false) { IsImmediate = true, TargetString = text };
		}
		
		public static Order Command(string text)
		{
			return new Order("Command", null, false) { IsImmediate = true, TargetString = text };
		}

		public static Order StartProduction(Actor subject, string item, int count)
		{
			return new Order("StartProduction", subject, false) { TargetLocation = new int2(count, 0), TargetString = item };
		}

		public static Order PauseProduction(Actor subject, string item, bool pause)
		{
			return new Order("PauseProduction", subject, false) { TargetLocation = new int2(pause ? 1 : 0, 0), TargetString = item };
		}

		public static Order CancelProduction(Actor subject, string item, int count)
		{
			return new Order("CancelProduction", subject, false) { TargetLocation = new int2(count, 0), TargetString = item };
		}
	}
}
