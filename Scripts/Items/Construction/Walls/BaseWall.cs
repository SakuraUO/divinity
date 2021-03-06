using System;

namespace Server.Items
{
	public abstract class BaseWall : Item
	{
		public BaseWall( int itemID ) : base( itemID )
		{
			Movable = false;
		}

		public BaseWall( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}

    
	public class VisRangeItem : Item
	{
		private int m_VisRange;

		[CommandProperty( AccessLevel.GameMaster )]
		public int VisRange { get { return m_VisRange; } set { m_VisRange = value; } }

		[Constructable]
		public VisRangeItem() : this( 0x241, 1 )
		{
		}

		[Constructable]
		public VisRangeItem( int itemID ) : this( itemID, 1 )
		{
		}

		[Constructable]
		public VisRangeItem( int itemID, int range ) : base( itemID )
		{
			m_VisRange = range;
			Movable = false;
		}

		public override int GetUpdateRange( Mobile m )
		{
            if ( m.AccessLevel >= AccessLevel.GameMaster )
                return 18;
            else
			    return m_VisRange;
		}

		public VisRangeItem( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_VisRange );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt(); 

			switch ( version )
			{
				case 0:
					m_VisRange = reader.ReadInt();
					break;
			}
		}

        protected override Server.Network.Packet  GetWorldPacketFor(Server.Network.NetState state)
        {
            if ( state.Mobile != null && state.Mobile.AccessLevel >= AccessLevel.GameMaster )
                state.Send( new GMItemPacket(this, true) );
 	        return base.GetWorldPacketFor(state);
        }

		public override bool HandlesOnMovement{ get{ return true; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			base.OnMovement( m, oldLocation );
			if ( m.NetState == null )
				return;

			bool isInRange = Utility.InRange( m.Location, this.Location, m_VisRange );
			bool wasInRange = Utility.InRange( oldLocation, this.Location, m_VisRange );

            if (!isInRange && wasInRange)
            {
                m.NetState.Send(this.RemovePacket);

                if (m.AccessLevel >= AccessLevel.GameMaster)
                    this.SendInfoTo(m.NetState);
            }
		}

		public sealed class GMItemPacket : Server.Network.Packet
		{
			public GMItemPacket( VisRangeItem item ) : this( item, false )
			{
			}

			public GMItemPacket( VisRangeItem item, bool forceHidden ) : base( 0x1A )
			{
				this.EnsureCapacity( 20 );

				// 14 base length
				// +2 - Amount
				// +2 - Hue
				// +1 - Flags

				uint serial = (uint)item.Serial.Value;
				int itemID = item.ItemID;
				int amount = item.Amount;
				Point3D loc = item.Location;
				int x = loc.X;
				int y = loc.Y;
				int hue = item.Hue;
				int flags = item.GetPacketFlags();
				if ( forceHidden )
					flags |= 0x80;
				int direction = (int)item.Direction;

				if ( amount != 0 )
					serial |= 0x80000000;
				else
					serial &= 0x7FFFFFFF;

				m_Stream.Write( (uint) serial );
				m_Stream.Write( (short) (itemID & 0x7FFF) );

				if ( amount != 0 )
					m_Stream.Write( (short) amount );

				x &= 0x7FFF;

				if ( direction != 0 )
					x |= 0x8000;

				m_Stream.Write( (short) x );

				y &= 0x3FFF;

				if ( hue != 0 )
					y |= 0x8000;

				if ( flags != 0 )
					y |= 0x4000;

				m_Stream.Write( (short) y );

				if ( direction != 0 )
					m_Stream.Write( (byte) direction );

				m_Stream.Write( (sbyte) loc.Z );

				if ( hue != 0 )
					m_Stream.Write( (ushort) hue );

				if ( flags != 0 )
					m_Stream.Write( (byte) flags );
			}
		}
	}
}
