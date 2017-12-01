﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HVCC.History.Models
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[global::System.Data.Linq.Mapping.DatabaseAttribute(Name="HVCC")]
	public partial class HVCCDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    #endregion
		
		public HVCCDataContext() : 
				base(global::HVCC.History.Properties.Settings.Default.HVCCConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public HVCCDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<v_PropertyHistory> v_PropertyHistories
		{
			get
			{
				return this.GetTable<v_PropertyHistory>();
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.v_PropertyHistory")]
	public partial class v_PropertyHistory
	{
		
		private int _Section;
		
		private int _Block;
		
		private int _Lot;
		
		private int _SubLot;
		
		private string _Notes;
		
		private int _Year;
		
		private decimal _AmountOwed;
		
		private bool _IsInGoodStanding;
		
		private int _NumGolfCart;
		
		private decimal _CartFeesOwed;
		
		public v_PropertyHistory()
		{
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Section", DbType="Int NOT NULL")]
		public int Section
		{
			get
			{
				return this._Section;
			}
			set
			{
				if ((this._Section != value))
				{
					this._Section = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Block", DbType="Int NOT NULL")]
		public int Block
		{
			get
			{
				return this._Block;
			}
			set
			{
				if ((this._Block != value))
				{
					this._Block = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Lot", DbType="Int NOT NULL")]
		public int Lot
		{
			get
			{
				return this._Lot;
			}
			set
			{
				if ((this._Lot != value))
				{
					this._Lot = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_SubLot", DbType="Int NOT NULL")]
		public int SubLot
		{
			get
			{
				return this._SubLot;
			}
			set
			{
				if ((this._SubLot != value))
				{
					this._SubLot = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Notes", DbType="VarChar(500)")]
		public string Notes
		{
			get
			{
				return this._Notes;
			}
			set
			{
				if ((this._Notes != value))
				{
					this._Notes = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Year", DbType="Int NOT NULL")]
		public int Year
		{
			get
			{
				return this._Year;
			}
			set
			{
				if ((this._Year != value))
				{
					this._Year = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_AmountOwed", DbType="Money NOT NULL")]
		public decimal AmountOwed
		{
			get
			{
				return this._AmountOwed;
			}
			set
			{
				if ((this._AmountOwed != value))
				{
					this._AmountOwed = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_IsInGoodStanding", DbType="Bit NOT NULL")]
		public bool IsInGoodStanding
		{
			get
			{
				return this._IsInGoodStanding;
			}
			set
			{
				if ((this._IsInGoodStanding != value))
				{
					this._IsInGoodStanding = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_NumGolfCart", DbType="Int NOT NULL")]
		public int NumGolfCart
		{
			get
			{
				return this._NumGolfCart;
			}
			set
			{
				if ((this._NumGolfCart != value))
				{
					this._NumGolfCart = value;
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_CartFeesOwed", DbType="Money NOT NULL")]
		public decimal CartFeesOwed
		{
			get
			{
				return this._CartFeesOwed;
			}
			set
			{
				if ((this._CartFeesOwed != value))
				{
					this._CartFeesOwed = value;
				}
			}
		}
	}
}
#pragma warning restore 1591